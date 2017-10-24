using System;
using System.Collections.Generic;
using Android.Gms.Maps;

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
    public interface IClusterRenderer
    {
        bool EnableAnimation { get; set; }
        ClusterManager.IOnClusterClickListener ClusterClickListener { get; set; }
        ClusterManager.IOnClusterInfoWindowClickListener ClusterInfoWindowClickListener { get; set; }
        ClusterManager.IOnClusterItemClickListener ClusterItemClickListener { get; set; }
        ClusterManager.IOnClusterItemInfoWindowClickListener ClusterItemInfoWindowClickListener { get; set; }
		GoogleMap.IInfoWindowAdapter ItemInfoWindowAdapter { get; set; }
		GoogleMap.IInfoWindowAdapter ClusterInfoWindowAdapter { get; set; }

        void ClustersChanged(IEnumerable<ICluster> clusters);

        void ViewAdded();
        void ViewRemoved();
    }
}
