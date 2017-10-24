using System;
using System.Collections.Generic;
using Android.Gms.Maps.Model;
using GoogleMapsUtils.Android.Geometry;
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
    public class QuadItem : IQuadTreeItem, ICluster
    {
        public QuadItem(IClusterItem item)
        {
            ClusterItem = item;
            Position = item.Position;
            Point = NonHierarchicalDistanceBasedAlgorithm.Projection.ToPoint(Position);
            Items = new List<IClusterItem> { ClusterItem };
        }

        public IClusterItem ClusterItem { get; private set; }
        public Point Point { get; private set; }
        public LatLng Position { get; private set; }
        public IList<IClusterItem> Items { get; private set; }
        public int Count => 1;

        public override int GetHashCode()
        {
            return ClusterItem.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as QuadItem;
            if(other == null)
            {
                return false;
            }

            return other.ClusterItem.Equals(ClusterItem);
        }
    }
}
