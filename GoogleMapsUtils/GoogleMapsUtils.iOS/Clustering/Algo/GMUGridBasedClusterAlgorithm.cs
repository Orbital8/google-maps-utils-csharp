using System;
using System.Collections.Generic;
using Google.Maps;

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
    public class GMUGridBasedClusterAlgorithm : IGMUClusterAlgorithm
    {
		// Grid cell dimension in pixels to keep clusters about 100 pixels apart on screen.
		private const int kGMUGridCellSizePoints = 100;

        private List<IGMUClusterItem> _items = new List<IGMUClusterItem>();

        public void AddItems(IEnumerable<IGMUClusterItem> items)
        {
            _items.AddRange(items);
        }

        public void RemoveItem(IGMUClusterItem item)
        {
            _items.Remove(item);
        }

        public void ClearItems()
        {
            _items.Clear();
        }

        public IEnumerable<IGMUCluster> ClustersAtZoom(float zoom)
        {
            var clusters = new Dictionary<double, GMUStaticCluster>();

            // Divide the whole map into a numCells x numCells grid and assign items to them.
            var numCells = (long)Math.Ceiling(256 * Math.Pow(2, zoom) / kGMUGridCellSizePoints);

            foreach (var item in _items)
            {
                var point = GeometryUtils.Project(item.Position);
                var col = (long)(numCells * (1.0 + point.X) / 2); // point.X is in [-1, 1] range
                var row = (long)(numCells * (1.0 + point.Y) / 2); // point.Y is in [-1, 1] range
                var index = numCells * row + col;

                var cellKey = index;
                GMUStaticCluster cluster;
                if (!clusters.TryGetValue(cellKey, out cluster))
                {
                    var point2 = new MapPoint(Convert.ToSingle((col + 0.5) * 2.0 / numCells - 1), 
                                              Convert.ToSingle((row + 0.5) * 2.0 / numCells - 1));
                    var position = GeometryUtils.Unproject(point2);
                    cluster = new GMUStaticCluster(position);
                    clusters.Add(cellKey, cluster);
                }

                cluster.AddItem(item);
            }

            return clusters.Values;
        }
    }
}
