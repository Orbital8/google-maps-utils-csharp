using System;
using System.Collections.Generic;

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
    public class GMUSimpleClusterAlgorithm : IGMUClusterAlgorithm
    {
        private const int ClusterCount = 10;

        private IList<IGMUClusterItem> _items = new List<IGMUClusterItem>();

        public void AddItems(IEnumerable<IGMUClusterItem> items)
        {
            foreach(var item in items)
            {
                _items.Add(item);
            }
        }

        public void ClearItems()
        {
            _items.Clear();
        }

		public void RemoveItem(IGMUClusterItem item)
		{
            _items.Remove(item);
		}

        public IEnumerable<IGMUCluster> ClustersAtZoom(float zoom)
        {
            var clusters = new List<IGMUCluster>(ClusterCount);

            for (var i = 0; i < ClusterCount; ++i)
            {
                if (i >= _items.Count)
                {
                    break;
                }

                var item = _items[i];
                clusters.Add(new GMUStaticCluster(item.Position));
            }

            var clusterIndex = 0;
            for (int i = ClusterCount; i < _items.Count; ++i)
            {
                var item = _items[i];
                var cluster = (GMUStaticCluster)clusters[clusterIndex % ClusterCount];
                cluster.AddItem(item);
                ++clusterIndex;
            }

            return clusters;
        }
    }
}
