using System;
using System.Collections.Generic;
using GoogleMapsUtils.Android.Geometry;
using GoogleMapsUtils.Android.Projection;
using GoogleMapsUtils.Android.QuadTree;

// Copyright 2017 Google Inc.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ported to C# from https://github.com/googlemaps/android-maps-utils
//
namespace GoogleMapsUtils.Android.Clustering.Algo
{
    public class NonHierarchicalDistanceBasedAlgorithm : IAlgorithm
    {
        public const int MaxDistanceAtZoom = 100; // essentially 100dp
        public static readonly SphericalMercatorProjection Projection = new SphericalMercatorProjection(1);

        private readonly List<QuadItem> _items = new List<QuadItem>();
        private readonly PointQuadTree _quadTree = new PointQuadTree(0, 1, 0, 1);

        public void AddItem(IClusterItem item)
        {
            var quadItem = new QuadItem(item);

            lock(_quadTree)
            {
                _items.Add(quadItem);
                _quadTree.Add(quadItem);
            }
        }

        public void AddItems(IEnumerable<IClusterItem> items)
        {
            foreach(var item in items)
            {
                AddItem(item);
            }
        }

        public void ClearItems()
        {
            lock(_quadTree)
            {
                _items.Clear();
                _quadTree.Clear();
            }
        }

        public void RemoveItem(IClusterItem item)
        {
            var quadItem = new QuadItem(item);

            lock(_quadTree)
            {
                _items.Remove(quadItem);
                _quadTree.Remove(quadItem);
            }
        }

        public IEnumerable<IClusterItem> Items
        {
            get
            {
                var items = new List<IClusterItem>();

                lock(_quadTree)
                {
                    foreach(var quadItem in _items)
                    {
                        items.Add(quadItem.ClusterItem);
                    }
                }

                return items;
            }
        }

        public IEnumerable<ICluster> GetClusters(float zoom)
        {
            var discreteZoom = Convert.ToInt32(zoom);

            var zoomSpecificSpan = MaxDistanceAtZoom / Math.Pow(2, discreteZoom) / 256;

            var visitedCandidates = new List<IQuadTreeItem>();
            var results = new List<ICluster>();
            var distanceToCluster = new Dictionary<QuadItem, double>();
            var itemToCluster = new Dictionary<QuadItem, StaticCluster>();

            lock(_quadTree)
            {
                foreach(var candidate in _items)
                {
                    if(visitedCandidates.Contains(candidate))
                    {
                        // candidate is already part of another cluster
                        continue;
                    }

                    var searchBounds = CreateBoundsFromSpan(candidate.Point, zoomSpecificSpan);
                    var clusterItems = _quadTree.Search(searchBounds);

                    if(clusterItems.Count == 1)
                    {
                        // only the current marker is in range. Just add the single item to the results
                        results.Add(candidate);
                        visitedCandidates.Add(candidate);

                        if(distanceToCluster.ContainsKey(candidate))
                        {
                            distanceToCluster[candidate] = 0;
                        }
                        else
                        {
                            distanceToCluster.Add(candidate, 0);
                        }

                        continue;
                    }

                    var cluster = new StaticCluster(candidate.ClusterItem.Position);
                    results.Add(cluster);

                    foreach(QuadItem clusterItem in clusterItems)
                    {
                        double existingDistance;
                        var distance = DistanceSquared(clusterItem.Point, candidate.Point);

                        if(distanceToCluster.TryGetValue(clusterItem, out existingDistance))
                        {
                            // item already belongs to a cluster. Check if it's closer to this cluster
                            if (existingDistance < distance)
                            {
                                continue;
                            }

                            // move item to the closer cluster
                            itemToCluster[clusterItem].Remove(clusterItem.ClusterItem);
                        }

                        if(distanceToCluster.ContainsKey(clusterItem))
                        {
                            distanceToCluster[clusterItem] = distance;
                        } else {
                            distanceToCluster.Add(clusterItem, distance);
                        }

                        cluster.Add(clusterItem.ClusterItem);

                        if(itemToCluster.ContainsKey(clusterItem))
                        {
                            itemToCluster[clusterItem] = cluster;
                        } else {
                            itemToCluster.Add(clusterItem, cluster);
                        }
                    }

                    visitedCandidates.AddRange(clusterItems);
                }
            }

            return results;
        }

        private double DistanceSquared(Point a, Point b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        private Bounds CreateBoundsFromSpan(Point p, double span)
        {
            var halfSpan = span / 2;
            return new Bounds(p.X - halfSpan, p.X + halfSpan, p.Y - halfSpan, p.Y + halfSpan);
        }
    }
}
