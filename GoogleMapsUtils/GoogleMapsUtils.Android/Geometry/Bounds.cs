using System;

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
namespace GoogleMapsUtils.Android.Geometry
{
    public class Bounds
    {
        public Bounds(double minX, double maxX, double minY, double maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;

            MidX = (minX + maxX) / 2;
            MidY = (minY + maxY) / 2;
        }

        public double MinX { get; private set; }
        public double MinY { get; private set; }
        public double MaxX { get; private set; }
        public double MaxY { get; private set; }
        public double MidX { get; private set; }
        public double MidY { get; private set; }

        public bool Contains(double x, double y)
        {
            return MinX <= x && x <= MaxX && MinY <= y && y <= MaxY;
        }

        public bool Contains(Point point)
        {
            return Contains(point.X, point.Y);
        }

        public bool Intersects(double minX, double maxX, double minY, double maxY)
        {
            return minX < MaxX && MinX < maxX && minY < MaxY && MinY < maxY;
        }

        public bool Intersects(Bounds bounds)
        {
            return Intersects(bounds.MinX, bounds.MaxX, bounds.MinY, bounds.MaxY);
        }

        public bool Contains(Bounds bounds)
        {
            return bounds.MinX >= MinX && bounds.MaxX <= MaxX && bounds.MinY >= MinY && bounds.MaxY <= MaxY;
        }
    }
}
