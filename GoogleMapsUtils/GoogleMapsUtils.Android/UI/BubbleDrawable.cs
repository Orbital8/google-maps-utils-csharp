using System;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Support.V4.Content.Res;
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
namespace GoogleMapsUtils.Android.UI
{
    public class BubbleDrawable : Drawable
    {
        private readonly Drawable _shadow;
        private readonly Drawable _mask;

        public BubbleDrawable(Resources res)
        {
            _mask = ResourcesCompat.GetDrawable(res, Resource.Drawable.amu_bubble_mask, null);
            _shadow = ResourcesCompat.GetDrawable(res, Resource.Drawable.amu_bubble_shadow, null);
        }

        public Color Color { get; set; } = Color.White;

		public override void SetAlpha(int alpha)
		{
			throw new UnsupportedOperationException();
		}

        public override void SetColorFilter(ColorFilter colorFilter)
        {
            throw new UnsupportedOperationException();
        }

        public override int Opacity
        {
            get
            {
                return (int)Format.Translucent;
            }
        }

        public override void SetBounds(int left, int top, int right, int bottom)
        {
            _mask.SetBounds(left, top, right, bottom);
            _shadow.SetBounds(left, top, right, bottom);
        }

        public override bool GetPadding(Rect padding)
        {
            return _mask.GetPadding(padding);
        }

        public override void Draw(Canvas canvas)
        {
            _mask.Draw(canvas);
            canvas.DrawColor(Color, PorterDuff.Mode.SrcIn);
            _shadow.Draw(canvas);
        }
    }
}
