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
    public class GQTPointQuadTreeChild
    {
		private const int kMaxElements = 64;
		private const int kMaxDepth = 30;

        private GQTPointQuadTreeChild _topRight;
        private GQTPointQuadTreeChild _topLeft;
        private GQTPointQuadTreeChild _bottomRight;
        private GQTPointQuadTreeChild _bottomLeft;
        private IList<GQTPointQuadTreeItem> _items = new List<GQTPointQuadTreeItem>();

        public GQTPointQuadTreeChild()
        {
            _topRight = null;
            _topLeft = null;
            _bottomLeft = null;
            _bottomRight = null;
		}

        public static GQTPoint BoundsMidpoint(GQTBounds bounds)
        {
            return new GQTPoint((bounds.MinX + bounds.MaxX) / 2, (bounds.MinY + bounds.MaxY) / 2);
        }

        public static GQTBounds BoundsTopRightChildQuadBounds(GQTBounds parentBounds)
        {
            var midPoint = BoundsMidpoint(parentBounds);
            var minX = midPoint.X;
            var minY = midPoint.Y;
            var maxX = parentBounds.MaxX;
            var maxY = parentBounds.MaxY;
            return new GQTBounds(minX, minY, maxX, maxY);
        }

        public static GQTBounds BoundsTopLeftChildQuadBounds(GQTBounds parentBounds)
        {
			var midPoint = BoundsMidpoint(parentBounds);
            var minX = parentBounds.MinX;
			var minY = midPoint.Y;
            var maxX = midPoint.X;
			var maxY = parentBounds.MaxY;
			return new GQTBounds(minX, minY, maxX, maxY);
        }

		public static GQTBounds BoundsBottomRightChildQuadBounds(GQTBounds parentBounds)
		{
			var midPoint = BoundsMidpoint(parentBounds);
            var minX = midPoint.X;
			var minY = parentBounds.MinY;
			var maxX = parentBounds.MaxX;
			var maxY = midPoint.Y;
			return new GQTBounds(minX, minY, maxX, maxY);
		}

        public static GQTBounds BoundsBottomLeftChildQuadBounds(GQTBounds parentBounds)
		{
			var midPoint = BoundsMidpoint(parentBounds);
			var minX = parentBounds.MinX;
			var minY = parentBounds.MinY;
			var maxX = midPoint.X;
			var maxY = midPoint.Y;
			return new GQTBounds(minX, minY, maxX, maxY);
		}

        public static bool BoundsIntersectBounds(GQTBounds bounds1, GQTBounds bounds2)
        {
            return (!(bounds1.MaxY < bounds2.MinY || bounds2.MaxY < bounds1.MinY) &&
	                  !(bounds1.MaxX < bounds2.MinX || bounds2.MaxX < bounds1.MinX));
		}

        public void Add(GQTPointQuadTreeItem item, GQTBounds bounds, int depth)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if(_items.Count >= kMaxElements && depth < kMaxDepth)
            {
                SplitWithOwnBounds(bounds, depth);
            }

            if(_topRight != null)
            {
                var itemPoint = item.Point;
                var midPoint = BoundsMidpoint(bounds);

                if(itemPoint.Y > midPoint.Y)
                {
                    if(itemPoint.X > midPoint.X)
                    {
                        _topRight.Add(item, BoundsTopRightChildQuadBounds(bounds), depth + 1);
                    } else {
                        _topLeft.Add(item, BoundsTopLeftChildQuadBounds(bounds), depth + 1);
                    }
                } else {
                    if(itemPoint.X > midPoint.X)
                    {
                        _bottomRight.Add(item, BoundsBottomRightChildQuadBounds(bounds), depth + 1);
                    } else {
                        _bottomLeft.Add(item, BoundsBottomLeftChildQuadBounds(bounds), depth + 1);
                    }
                }
            } else {
                _items.Add(item);
            }
        }

        private void SplitWithOwnBounds(GQTBounds ownBounds, int depth)
        {
            _topRight = new GQTPointQuadTreeChild();
            _topLeft = new GQTPointQuadTreeChild();
            _bottomRight = new GQTPointQuadTreeChild();
            _bottomLeft = new GQTPointQuadTreeChild();

            var items = _items;
            _items = new List<GQTPointQuadTreeItem>();

            foreach(var item in items)
            {
                Add(item, ownBounds, depth);
            }
        }

        public bool Remove(GQTPointQuadTreeItem item, GQTBounds bounds)
        {
            if(_topRight != null)
            {
                var itemPoint = item.Point;
                var midPoint = BoundsMidpoint(bounds);

                if(itemPoint.Y > midPoint.Y)
                {
                    if(itemPoint.X > midPoint.X)
                    {
                        return _topRight.Remove(item, BoundsTopRightChildQuadBounds(bounds));
                    }
                    else
                    {
                        return _topLeft.Remove(item, BoundsTopLeftChildQuadBounds(bounds));
                    }
                }
                else
                {
                    if(itemPoint.X > midPoint.X)
                    {
                        return _bottomRight.Remove(item, BoundsBottomRightChildQuadBounds(bounds));
                    }
                    else
                    {
                        return _bottomLeft.Remove(item, BoundsBottomLeftChildQuadBounds(bounds));
                    }
                }
            }

            var index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SearchWithBounds(GQTBounds searchBounds, GQTBounds ownBounds, IList<GQTPointQuadTreeItem> accumulator)
        {
            if (_topRight != null)
            {
                var topRightBounds = BoundsTopRightChildQuadBounds(ownBounds);
                var topLeftBounds = BoundsTopLeftChildQuadBounds(ownBounds);
                var bottomRight = BoundsBottomRightChildQuadBounds(ownBounds);
                var bottomLeft = BoundsBottomLeftChildQuadBounds(ownBounds);

                if (BoundsIntersectBounds(topRightBounds, searchBounds))
                {
                    _topRight.SearchWithBounds(searchBounds, topRightBounds, accumulator);
                }

                if (BoundsIntersectBounds(topLeftBounds, searchBounds))
				{
                    _topLeft.SearchWithBounds(searchBounds, topLeftBounds, accumulator);
				}

                if (BoundsIntersectBounds(bottomRight, searchBounds))
				{
                    _bottomRight.SearchWithBounds(searchBounds, bottomRight, accumulator);
				}

                if (BoundsIntersectBounds(bottomLeft, searchBounds))
				{
                    _bottomLeft.SearchWithBounds(searchBounds, bottomLeft, accumulator);
				}
            } else {

                foreach(var item in _items)
                {
                    var point = item.Point;

                    if(point.X <= searchBounds.MaxX && point.X >= searchBounds.MinX &&
                       point.Y <= searchBounds.MaxY && point.Y >= searchBounds.MinY) 
                    {
                        accumulator.Add(item);
                    }
                }
            }
        }
    }
}
