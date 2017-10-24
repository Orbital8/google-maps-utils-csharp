using System;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
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
    public class IconGenerator
    {
        public enum Style
        {
            Default = 1,
            White = 2,
            Red = 3,
            Blue = 4,
            Green = 5,
            Purple = 6,
            Orange = 7
        }

        private readonly Context _context;

        private ViewGroup _container;
        private RotationLayout _rotationLayout;
        private TextView _textView;
        private View _contentView;
        private int _rotation;
        private float _anchorU = 0.5f;
        private float _anchorV = 1f;
        private BubbleDrawable _background;

        public IconGenerator(Context context)
        {
            _context = context;
            _background = new BubbleDrawable(_context.Resources);
            _container = (ViewGroup)LayoutInflater.From(_context).Inflate(Resource.Layout.amu_text_bubble, null);
            _rotationLayout = (RotationLayout)_container.GetChildAt(0);
            _textView = _rotationLayout.FindViewById<TextView>(Resource.Id.amu_text);
            _contentView = _textView;
            SetStyle(Style.Default);
        }

        public float AnchorU
        {
            get { return RotateAnchor(_anchorU, _anchorV); }
        }

		public float AnchorV
		{
			get { return RotateAnchor(_anchorV, _anchorU); }
		}

        public void SetTextAppearance(Context context, int resId)
        {
            if(_textView != null)
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    _textView.SetTextAppearance(context, resId);
#pragma warning restore CS0618 // Type or member is obsolete
                }
                else
                {
                    _textView.SetTextAppearance(resId);
                }
            }
        }

        public void SetTextAppearance(int resId)
        {
            SetTextAppearance(_context, resId);
        }

        public Bitmap MakeIcon(string text)
        {
            if(_textView != null)
            {
                _textView.Text = text;
            }

            return MakeIcon();
        }

        public Bitmap MakeIcon()
        {
            var measureSpec = View.MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified);
            _container.Measure(measureSpec, measureSpec);

            var measuredWidth = _container.MeasuredWidth;
            var measuredHeight = _container.MeasuredHeight;

            _container.Layout(0, 0, measuredWidth, measuredHeight);

            if(_rotation == 1 || _rotation == 3)
            {
                measuredHeight = _container.MeasuredWidth;
                measuredWidth = _container.MeasuredHeight;
            }

            var r = Bitmap.CreateBitmap(measuredWidth, measuredHeight, Bitmap.Config.Argb8888);
            r.EraseColor(Color.Transparent);

            var canvas = new Canvas(r);

            if(_rotation == 0)
            {
                // do nothing
            }
            else if(_rotation == 1)
            {
                canvas.Translate(measuredWidth, 0);
                canvas.Rotate(90);
            }
            else if(_rotation == 2)
            {
                canvas.Rotate(180, measuredWidth / 2, measuredHeight / 2);
            }
            else
            {
                canvas.Translate(0, measuredHeight);
                canvas.Rotate(270);
            }

            _container.Draw(canvas);
            return r;
        }

        public void SetContentView(View contentView)
        {
            _rotationLayout.RemoveAllViews();
            _rotationLayout.AddView(contentView);
            _contentView = contentView;

            var view = _rotationLayout.FindViewById(Resource.Id.amu_text);
            _textView = view as TextView;
        }

        public void SetContentRotation(int degrees)
        {
            _rotationLayout.SetViewRotation(degrees);
        }

        public void SetRotation(int degrees)
        {
            _rotation = ((degrees + 360) % 360) / 90;
        }

        public void SetStyle(Style style)
        {
            SetColor(GetStyleColor(style));
            SetTextAppearance(_context, GetTextStyle(style));
        }

        public void SetColor(Color color)
        {
            _background.Color = color;
            SetBackground(_background);
        }

        public void SetBackground(Drawable background)
        {
            _container.Background = background;

            if(background != null)
            {
                var rect = new Rect();
                background.GetPadding(rect);
                _container.SetPadding(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
            else
            {
                _container.SetPadding(0, 0, 0, 0);
            }
        }

        public void SetContentPadding(int left, int top, int right, int bottom)
        {
            _contentView.SetPadding(left, top, right, bottom);
        }

        private Color GetStyleColor(Style style)
        {
            switch(style)
            {
				default:
                case Style.Default:
                case Style.White:
                    return Color.ParseColor("#ffffffff");
                case Style.Red:
					return Color.ParseColor("#ffcc0000");
                case Style.Blue:
					return Color.ParseColor("#ff0099cc");
                case Style.Green:
					return Color.ParseColor("#ff669900");
                case Style.Purple:
					return Color.ParseColor("#ff9933cc");
                case Style.Orange:
					return Color.ParseColor("#ffff8800");
            }
        }

        private int GetTextStyle(Style style)
        {
            switch(style)
            {
                default:
                case Style.Default:
                case Style.White:
                    return Resource.Style.amu_Bubble_TextAppearance_Dark;
                case Style.Red:
                case Style.Blue:
                case Style.Green:
                case Style.Purple:
                case Style.Orange:
                    return Resource.Style.amu_Bubble_TextAppearance_Light;
            }
        }

        private float RotateAnchor(float u, float v)
        {
            switch(_rotation)
            {
                case 0:
                    return u;
                case 1:
                    return 1 - v;
                case 2:
                    return 1 - u;
                case 3:
                    return v;
            }

            throw new IllegalStateException();
        }
    }
}
