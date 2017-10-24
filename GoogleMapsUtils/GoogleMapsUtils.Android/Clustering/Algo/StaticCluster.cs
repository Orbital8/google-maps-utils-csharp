using System;
using System.Collections.Generic;
using Android.Gms.Maps.Model;

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
    public class StaticCluster : ICluster
    {
        private readonly List<IClusterItem> _items = new List<IClusterItem>();

        public StaticCluster(LatLng center)
        {
            Position = center;
        }

        public bool Add(IClusterItem item)
        {
            if(_items.Contains(item))
            {
                return false;
            }

            _items.Add(item);
            return true;
        }

        public bool Remove(IClusterItem item)
        {
            return _items.Remove(item);
        }

        public LatLng Position { get; private set; }

        public IList<IClusterItem> Items => _items;

        public int Count => _items.Count;

        public override string ToString()
        {
            return string.Format("[StaticCluster: Position={0}, Item count={1}]", Position, Count);
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() + _items.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as StaticCluster;
            if(other == null)
            {
                return false;
            }

            return other.Position.Equals(Position) && other.Items.Equals(Items);
        }
    }
}
