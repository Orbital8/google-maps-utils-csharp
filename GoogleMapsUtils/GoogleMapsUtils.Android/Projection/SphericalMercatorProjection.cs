using System;
using Android.Gms.Maps.Model;
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
namespace GoogleMapsUtils.Android.Projection
{
    public class SphericalMercatorProjection
    {
        private readonly double _worldWidth;

        public SphericalMercatorProjection(double worldWidth)
        {
            _worldWidth = worldWidth;
        }

        public Point ToPoint(LatLng latLng)
        {
            var x = latLng.Longitude / 360 + 0.5;
            var sinY = Math.Sin(ToRadians(latLng.Latitude));
            var y = 0.5 * Math.Log((1 + sinY) / (1 - sinY)) / -(2 * Math.PI) + 0.5;

            return new Point(x * _worldWidth, y * _worldWidth);
        }

        public LatLng ToLatLng(Point point)
        {
            var x = point.X / _worldWidth - 0.5;
            var lng = x * 360;

            var y = 0.5 - (point.Y / _worldWidth);
            var lat = 90 - ToDegrees(Math.Atan(Math.Exp(-y * 2 * Math.PI)) * 2);

            return new LatLng(lat, lng);
        }

        private double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private double ToDegrees(double radians)
        {
            return radians * (180 / Math.PI);
        }
    }
}
