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
namespace GoogleMapsUtils.iOS.QuadTree
{
    public class GQTPointQuadTree
    {
        private GQTBounds _bounds;
        private GQTPointQuadTreeChild _root;

        public GQTPointQuadTree(GQTBounds bounds)
        {
            _bounds = bounds;
            Clear();
        }

        public int Count { get; private set; }

        public bool Add(GQTPointQuadTreeItem item)
        {
            if (item == null)
            {
                return false;
            }

            var point = item.Point;
            if (point.X > _bounds.MaxX || point.X < _bounds.MinX ||
                point.Y > _bounds.MaxY || point.Y < _bounds.MinY)
            {
                return false;
            }

            _root.Add(item, _bounds, 0);
            ++Count;

            return true;
        }

        public bool Remove(GQTPointQuadTreeItem item)
        {
            var point = item.Point;
            if(point.X > _bounds.MaxX || point.X < _bounds.MinX || 
               point.Y > _bounds.MaxY || point.Y < _bounds.MinX)
            {
                return false;
            }

            var removed = _root.Remove(item, _bounds);
            if(removed)
            {
                --Count;
            }

            return removed;
        }

        public void Clear()
        {
            _root = new GQTPointQuadTreeChild();
            Count = 0;
        }

        public IList<GQTPointQuadTreeItem> SearchWithBounds(GQTBounds searchBounds)
        {
            var results = new List<GQTPointQuadTreeItem>();
            _root.SearchWithBounds(searchBounds, _bounds, results);
            return results;
        }
    }
}
