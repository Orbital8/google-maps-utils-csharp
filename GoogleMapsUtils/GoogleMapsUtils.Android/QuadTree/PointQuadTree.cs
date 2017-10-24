using System;
using System.Collections.Generic;
using GoogleMapsUtils.Android.Geometry;

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
namespace GoogleMapsUtils.Android.QuadTree
{
    public class PointQuadTree
    {
        private const int MaxElements = 50;
        private const int MaxDepth = 40;

        private readonly Bounds _bounds;
        private readonly int _depth;

        private List<IQuadTreeItem> _items;
        private List<PointQuadTree> _children;

        public PointQuadTree(double minX, double maxX, double minY, double maxY)
            : this(new Bounds(minX, maxX, minY, maxY))
        {
        }

		public PointQuadTree(double minX, double maxX, double minY, double maxY, int depth)
            : this(new Bounds(minX, maxX, minY, maxY), depth)
		{
		}

        public PointQuadTree(Bounds bounds) : this(bounds, 0)
        {
        }

		public PointQuadTree(Bounds bounds, int depth)
		{
            _bounds = bounds;
            _depth = depth;
		}

        public void Add(IQuadTreeItem item)
        {
            var point = item.Point;

            if(_bounds.Contains(point.X, point.Y))
            {
                Insert(point.X, point.Y, item);
            }
        }

        public bool Remove(IQuadTreeItem item)
        {
            var point = item.Point;
            if(_bounds.Contains(point.X, point.Y))
            {
                return Remove(point.X, point.Y, item);
            }
            else
            {
                return false;
            }
        }

        private bool Remove(double x, double y, IQuadTreeItem item)
        {
            if(_children != null)
            {
                if(y < _bounds.MidY)
                {
                    if(x < _bounds.MidX)
                    {
                        // top left
                        return _children[0].Remove(x, y, item);
                    }
                    else
                    {
                        // top right
                        return _children[1].Remove(x, y, item);
                    }
                } else {
					if (x < _bounds.MidX)
					{
						// bottom left
						return _children[2].Remove(x, y, item);
					}
					else
					{
						// bottom right
						return _children[3].Remove(x, y, item);
					}
                }
            }
            else
            {
                if(_items == null)
                {
                    return false;
                }
                else
                {
                    return _items.Remove(item);
                }
            }
        }

        public void Clear()
        {
            _children = null;

            if(_items != null)
            {
                _items.Clear();
            }
        }

        public IList<IQuadTreeItem> Search(Bounds searchBounds)
        {
            var results = new List<IQuadTreeItem>();
            Search(searchBounds, results);
            return results;
        }

        private void Search(Bounds searchBounds, List<IQuadTreeItem> results)
        {
            if (!_bounds.Intersects(searchBounds))
            {
                return;
            }

            if(_children != null)
            {
                foreach(var quad in _children)
                {
                    quad.Search(searchBounds, results);
                }
            }
            else if(_items != null)
            {
                if(searchBounds.Contains(_bounds))
                {
                    results.AddRange(_items);
                }
                else
                {
                    foreach(var item in _items)
                    {
                        if(searchBounds.Contains(item.Point))
                        {
                            results.Add(item);
                        }
                    }
                }
            }
        }

        private void Insert(double x, double y, IQuadTreeItem item)
        {
            if(_children != null)
            {
                if(y < _bounds.MidY)
                {
                    if(x < _bounds.MidX)
                    {
                        // top left
                        _children[0].Insert(x, y, item);
                    }
                    else
                    {
                        // top right
                        _children[1].Insert(x, y, item);
                    }
                }
                else
                {
					if (x < _bounds.MidX)
					{
						// bottom left
						_children[2].Insert(x, y, item);
					}
					else
					{
						// bottom right
						_children[3].Insert(x, y, item);
					}
                }

                return;
            }

            if(_items == null)
            {
                _items = new List<IQuadTreeItem>();
            }

            _items.Add(item);
            if(_items.Count > MaxElements && _depth < MaxDepth)
            {
                Split();
            }
        }

        private void Split()
        {
            _children = new List<PointQuadTree>(4);
            _children.Add(new PointQuadTree(_bounds.MinX, _bounds.MidX, _bounds.MinY, _bounds.MidY, _depth + 1));
            _children.Add(new PointQuadTree(_bounds.MidX, _bounds.MaxX, _bounds.MinY, _bounds.MidY, _depth + 1));
            _children.Add(new PointQuadTree(_bounds.MinX, _bounds.MidX, _bounds.MidY, _bounds.MaxY, _depth + 1));
            _children.Add(new PointQuadTree(_bounds.MidX, _bounds.MaxX, _bounds.MidY, _bounds.MaxY, _depth + 1));

            var items = _items;
            _items = null;

            foreach(var item in items)
            {
                // re-insert the items into child quads
                Insert(item.Point.X, item.Point.Y, item);
            }
        }
    }
}
