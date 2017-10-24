﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoogleMapsUtils.Android.Util;
using Java.Lang;

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
    public class PreCachingAlgorithmDecorator : IAlgorithm
    {
        private readonly LruCache<int, IEnumerable<ICluster>> _cache= new LruCache<int, IEnumerable<ICluster>>(5);
        private readonly object _cacheLock = new object();
        private readonly IAlgorithm _algorithm;

        public PreCachingAlgorithmDecorator(IAlgorithm algorithm)
        {
            _algorithm = algorithm;
        }

        public IEnumerable<IClusterItem> Items
        {
            get
            {
                 throw new NotImplementedException();
            }
        }

        public void AddItem(IClusterItem item)
        {
            _algorithm.AddItem(item);
            ClearCache();
        }

        public void AddItems(IEnumerable<IClusterItem> items)
        {
            _algorithm.AddItems(items);
            ClearCache();
        }

        public void ClearItems()
        {
            _algorithm.ClearItems();
            ClearCache();
        }

		public void RemoveItem(IClusterItem item)
		{
            _algorithm.RemoveItem(item);
            ClearCache();
		}

        public IEnumerable<ICluster> GetClusters(float zoom)
        {
            var discreteZoom = Convert.ToInt32(zoom);
            var results = GetClustersInternal(discreteZoom);

            if (_cache.Get(discreteZoom + 1) == null)
            {
                Task.Run(() => PrecacheRunAsync(discreteZoom + 1)); 
            }

            if (_cache.Get(discreteZoom - 1) == null)
            {
                Task.Run(() => PrecacheRunAsync(discreteZoom - 1));
			}

            return results;
        }

        private void ClearCache()
        {
            _cache.EvictAll();
        }

        private IEnumerable<ICluster> GetClustersInternal(int discreteZoom)
        {
            IEnumerable<ICluster> results = null;

            lock(_cacheLock)
            {
                results = _cache.Get(discreteZoom);
            }

            if(results == null)
            {
                lock(_cacheLock)
                {
                    results = _cache.Get(discreteZoom);
                    if(results == null)
                    {
                        results = _algorithm.GetClusters(discreteZoom);
                        _cache.Put(discreteZoom, results);
                    }
                }
            }

            return results;
        }

        private async Task PrecacheRunAsync(int discreteZoom)
        {
            // wait between 500 - 1000 ms
            var random = new Random();
            var milliSeconds = Convert.ToInt32(random.NextDouble() * 500 + 500);
            await Task.Delay(milliSeconds);

            GetClustersInternal(discreteZoom);
        }
    }
}
