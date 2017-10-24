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
namespace GoogleMapsUtils.Android.Clustering.View
{
    internal class MarkerCache<T>
    {
        private Dictionary<T, Marker> _cache = new Dictionary<T, Marker>();
        private Dictionary<string, T> _cacheReverse = new Dictionary<string, T>();

        public Marker Get(T item)
        {
            Marker marker;
            if(_cache.TryGetValue(item, out marker))
            {
                return marker;
            }

            return null;
        }

        public T Get(Marker marker)
        {
            T item;
            if(_cacheReverse.TryGetValue(marker.Id, out item))
            {
                return item;
            }

            return default(T);
        }

        public void Put(T item, Marker marker)
        {
            if (_cache.ContainsKey(item))
            {
                _cache[item] = marker;
            } 
            else 
            {
                _cache.Add(item, marker);
            }

            if (_cacheReverse.ContainsKey(marker.Id))
            {
                _cacheReverse[marker.Id] = item;
            }
            else
            {
                _cacheReverse.Add(marker.Id, item);
            }
        }

        public void Remove(Marker marker)
        {
            T item;
            if(_cacheReverse.TryGetValue(marker.Id, out item))
            {
                _cache.Remove(item);
                _cacheReverse.Remove(marker.Id);
            }
        }
    }
}
