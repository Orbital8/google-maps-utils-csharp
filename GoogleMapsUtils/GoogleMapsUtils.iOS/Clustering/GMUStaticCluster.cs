using System;
using System.Collections.Generic;
using CoreLocation;

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
namespace GoogleMapsUtils.iOS.Clustering
{
    public class GMUStaticCluster : IGMUCluster
    {
        private IList<IGMUClusterItem> _items = new List<IGMUClusterItem>();

        public GMUStaticCluster(CLLocationCoordinate2D position)
        {
            Position = position;
        }

        public int Count => _items.Count;
        public IList<IGMUClusterItem> Items => _items;
        public CLLocationCoordinate2D Position { get; private set; }

        public void AddItem(IGMUClusterItem item)
        {
            _items.Add(item);
        }

        public void RemoveItem(IGMUClusterItem item)
        {
            _items.Remove(item);
        }
    }
}
