using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.Maps;
using GoogleMapsUtils.iOS.QuadTree;

//   Copyright 2017 Google Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
//  Ported to C# from https://github.com/googlemaps/google-maps-ios-utils
//
namespace GoogleMapsUtils.iOS.Clustering.Algo
{
    public class GMUNonHierarchicalDistanceBasedAlgorithm : IGMUClusterAlgorithm
    {
        private const int kGMUClusterDistancePoints = 100;
        private const double kGMUMapPointWidth = 2.0;  // MapPoint is in a [-1,1]x[-1,1] space.

        private List<IGMUClusterItem> _items = new List<IGMUClusterItem>();
        private GQTPointQuadTree _quadTree;

        public GMUNonHierarchicalDistanceBasedAlgorithm()
        {
            var bounds = new GQTBounds(-1, -1, 1, 1);
            _quadTree = new GQTPointQuadTree(bounds);
        }

        public virtual void AddItems(IEnumerable<IGMUClusterItem> items)
        {
            _items.AddRange(items);

            foreach (var item in items)
            {
                var quadItem = new GMUClusterItemQuadItem(item);
                _quadTree.Add(quadItem);
            }
        }

        public virtual void RemoveItem(IGMUClusterItem item)
        {
            _items.Remove(item);

            var quadItem = new GMUClusterItemQuadItem(item);
            // This should remove the corresponding quad item since GMUClusterItemQuadItem forwards its hash
            // and isEqual to the underlying item.
            _quadTree.Remove(quadItem);
        }

        public virtual void ClearItems()
        {
            _items.Clear();
            _quadTree.Clear();
        }

        public virtual IEnumerable<IGMUCluster> ClustersAtZoom(float zoom)
        {
            var clusters = new List<GMUStaticCluster>();
            var itemToClusterMap = new Dictionary<IGMUClusterItem, GMUStaticCluster>();
            var itemToClusterDistanceMap = new Dictionary<IGMUClusterItem, double>();
            var processedItem = new List<IGMUClusterItem>();

            foreach (var item in _items)
            {
                if (processedItem.Contains(item))
                {
                    continue;
                }

                var cluster = new GMUStaticCluster(item.Position);
                var point = GeometryUtils.Project(item.Position);

                // Query for items within a fixed point distance from the current item to make up a cluster
                // around it.
                var radius = kGMUClusterDistancePoints * kGMUMapPointWidth / Math.Pow(2.0, zoom + 8.0);
                var bounds = new GQTBounds(point.X - radius, point.Y - radius, point.X + radius, point.Y + radius);
                var nearbyItems = _quadTree.SearchWithBounds(bounds);

                foreach (GMUClusterItemQuadItem quadItem in nearbyItems)
                {
                    var nearbyItem = quadItem.ClusterItem;

                    processedItem.Add(nearbyItem);
                    var nearbyItemPoint = GeometryUtils.Project(nearbyItem.Position);
                    var key = nearbyItem;

                    var distanceSquared = DistanceSquared(point, nearbyItemPoint);

                    double existingDistance;
                    if (itemToClusterDistanceMap.TryGetValue(key, out existingDistance))
                    {
                        if (existingDistance < distanceSquared)
                        {
                            // already belongs to a closer cluster
                            continue;
                        }

                        var existingCluster = itemToClusterMap[key];
                        existingCluster.RemoveItem(nearbyItem);
                    }

                    var number = distanceSquared;
                    if (itemToClusterDistanceMap.ContainsKey(key))
                    {
                        itemToClusterDistanceMap[key] = distanceSquared;
                    } 
                    else 
                    {
                        itemToClusterDistanceMap.Add(key, distanceSquared);
                    }

                    if (itemToClusterMap.ContainsKey(key))
                    {
                        itemToClusterMap[key] = cluster;
                    } 
                    else 
                    {
                        itemToClusterMap.Add(key, cluster);
                    }

                    cluster.AddItem(nearbyItem);
                }

                clusters.Add(cluster);
            }

            Debug.Assert(itemToClusterDistanceMap.Count == _items.Count, "All items should be mapped to a distance");
            Debug.Assert(itemToClusterMap.Count == _items.Count, "All items should be mapped to a cluster");

#if DEBUG
            var totalCount = 0;
            foreach(var cluster in clusters)
            {
                totalCount += cluster.Count;
            }

            Debug.Assert(_items.Count == totalCount, "All clusters combined should make up the original item set");
#endif

            return clusters;
        }

        private double DistanceSquared(MapPoint pointA, MapPoint pointB)
        {
            var deltaX = pointA.X - pointB.X;
            var deltaY = pointA.Y - pointB.Y;
            return deltaX * deltaX + deltaY * deltaY;
        }

        private class GMUClusterItemQuadItem : GQTPointQuadTreeItem
        {
			private readonly IGMUClusterItem _clusterItem;
			private GQTPoint _clusterItemPoint;

            public GMUClusterItemQuadItem(IGMUClusterItem clusterItem)
            {
				_clusterItem = clusterItem;
                MapPoint point = GeometryUtils.Project(clusterItem.Position);
				_clusterItemPoint.X = point.X;
				_clusterItemPoint.Y = point.Y;
            }

            public IGMUClusterItem ClusterItem => _clusterItem;
            public override GQTPoint Point => _clusterItemPoint;

            public override int GetHashCode()
            {
                return _clusterItem.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if(this == obj)
                {
                    return true;
                }

                var other = obj as GMUClusterItemQuadItem;
                if(other == null)
                {
                    return false;
                }

                return _clusterItem.Equals(other._clusterItem);
            }
        }
    }
}
