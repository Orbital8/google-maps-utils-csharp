using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Widget;

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
    public class SquareTextView : TextView
    {
        private int _offsetTop = 0;
        private int _offsetLeft = 0;

        public SquareTextView(Context context) : base(context)
        {
        }

		public SquareTextView(Context context, IAttributeSet attrs) : base(context, attrs)
		{
		}

        public SquareTextView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
		{
		}

        public SquareTextView(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            var width = MeasuredWidth;
            var height = MeasuredHeight;
            var dimension = Math.Max(width, height);

            if(width > height)
            {
                _offsetTop = width - height;
                _offsetLeft = 0;
            }
            else
            {
                _offsetTop = 0;
                _offsetLeft = height - width;
            }

            SetMeasuredDimension(dimension, dimension);
        }

        public override void Draw(Canvas canvas)
        {
            canvas.Translate(_offsetLeft / 2, _offsetTop / 2);
            base.Draw(canvas);
        }
    }
}
