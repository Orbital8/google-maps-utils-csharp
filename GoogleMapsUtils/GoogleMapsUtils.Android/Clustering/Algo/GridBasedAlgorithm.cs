using System;
using System.Collections.Generic;
using GoogleMapsUtils.Android.Geometry;
using GoogleMapsUtils.Android.Projection;
using GoogleMapsUtils.Android.Util;

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
    public class GridBasedAlgorithm : IAlgorithm
    {
        private const int GridSize = 100;

        private readonly List<IClusterItem> _items = new List<IClusterItem>();

        public IEnumerable<IClusterItem> Items => _items;

        public void AddItem(IClusterItem item)
        {
            _items.Add(item);
        }

        public void AddItems(IEnumerable<IClusterItem> items)
        {
            _items.AddRange(items);
        }

        public void ClearItems()
        {
            _items.Clear();
        }

		public void RemoveItem(IClusterItem item)
		{
            _items.Remove(item);
		}

        public IEnumerable<ICluster> GetClusters(float zoom)
        {
            var numCells = Convert.ToInt64(Math.Ceiling(256 * Math.Pow(2, zoom) / GridSize));
            var projection = new SphericalMercatorProjection(numCells);

            var clusters = new List<ICluster>();
            var sparseArray = new LongSparseArray<StaticCluster>();

            lock(_items)
            {
                foreach(var item in _items)
                {
                    var p = projection.ToPoint(item.Position);
                    var coord = GetCoord(numCells, p.X, p.Y);

                    var cluster = sparseArray.Get(coord);
                    if(cluster == null)
                    {
                        cluster = new StaticCluster(projection.ToLatLng(new Point(Math.Floor(p.X) + 0.5, Math.Floor(p.Y) + 0.5)));
                        sparseArray.Put(coord, cluster);
                        clusters.Add(cluster);
                    }

                    cluster.Add(item);
                }
            }

            return clusters;
        }

        private static ulong GetCoord(long numCells, double x, double y)
        {
            return Convert.ToUInt64(numCells * Math.Floor(x) + Math.Floor(y));
        }
    }
}
