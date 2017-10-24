using System;
using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using UIKit;

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
namespace GoogleMapsUtils.iOS.Clustering.View
{
    public class GMUDefaultClusterIconGenerator : IGMUClusterIconGenerator
    {
        private static readonly UIColor[] kGMUBucketBackgroundColors = {
			ColorFromHex(0x0099cc),
			ColorFromHex(0x669900),
			ColorFromHex(0xff8800),
			ColorFromHex(0xcc0000),
			ColorFromHex(0x9933cc)
        };

        private NSCache _iconCache = new NSCache();
        private int[] _buckets;
        private UIImage[] _backgroundImages;

        public GMUDefaultClusterIconGenerator() : this(new[] { 10, 50, 100, 200, 1000 })
        {
        }

        public GMUDefaultClusterIconGenerator(int[] buckets) : this(buckets, null)
        {
        }

        public GMUDefaultClusterIconGenerator(int[] buckets, UIImage[] backgroundImages)
        {
            if(buckets == null)
            {
                throw new ArgumentNullException(nameof(buckets));
            }

            if (backgroundImages != null && buckets.Length != backgroundImages.Length)
            {
                throw new ArgumentException("buckets size does not equal backgroundImages size");
            }

			if (buckets.Length == 0)
			{
				throw new ArgumentException("buckets are empty");
			}

			foreach (var bucket in buckets)
			{
				if (bucket <= 0)
				{
					throw new ArgumentOutOfRangeException("buckets have non-positive value");
				}
			}

			for (var i = 0; i < buckets.Length - 1; ++i)
			{
				if (buckets[i] >= buckets[i + 1])
				{
					throw new ArgumentException("buckets are not strictly increasing");
				}
			}

			_buckets = new int[buckets.Length];
			buckets.CopyTo(_buckets, 0);

            if (backgroundImages != null)
            {
                _backgroundImages = new UIImage[backgroundImages.Length];
                backgroundImages.CopyTo(_backgroundImages, 0);
            } 
            else 
            {
                _backgroundImages = null;
            }
        }

        ~GMUDefaultClusterIconGenerator()
        {
            Dispose(false);
        }

        public virtual UIImage IconForSize(int size)
        {
            var bucketIndex = BucketIndexForSize(size);
            string text;

            // If size is smalelr to first bucket size, use the size as is otherwise round it down to the
            // nearest bucket to limit the number of cluster icons we need to generate.
            if(size < _buckets[0])
            {
                text = size.ToString();
            }
            else
            {
                text = $"{_buckets[bucketIndex]}+";
            }

            if(_backgroundImages != null)
            {
                var image = _backgroundImages[bucketIndex];
                return IconForText(text, image);
            }

            return IconForText(text, bucketIndex);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_iconCache != null)
            {
                _iconCache.Dispose();
                _iconCache = null;
            }

            if (_backgroundImages != null)
            {
                foreach(var image in _backgroundImages)
                {
                    image.Dispose();
                }

                _backgroundImages = null;
            }
        }

        private int BucketIndexForSize(int size)
        {
            var index = 0;
            while(index + 1 < _buckets.Length && _buckets[index + 1] <= size)
            {
                ++index;
            }

            return index;
        }

        private UIImage IconForText(string text, UIImage image)
        {
            var nsString = new NSString(text);
            var icon = _iconCache.ObjectForKey(nsString) as UIImage;
            if(icon != null)
            {
                return icon;
            }

            var font = UIFont.BoldSystemFontOfSize(12f);
            var size = image.Size;

            UIGraphics.BeginImageContextWithOptions(size, false, 0f);
            image.Draw(new CGRect(0f, 0f, size.Width, size.Height));
            var rect = new CGRect(0f, 0f, image.Size.Width, image.Size.Height);

            var paragraphStyle = (NSParagraphStyle)NSParagraphStyle.Default.MutableCopy();
            paragraphStyle.Alignment = UITextAlignment.Center;

            var attributes = new UIStringAttributes {
                Font = font,
                ParagraphStyle = paragraphStyle,
                ForegroundColor = UIColor.White
            };

            var textSize = nsString.GetSizeUsingAttributes(attributes);
            var textRect = rect.Inset((rect.Width - textSize.Width) / 2, (rect.Height - textSize.Height) / 2);
            nsString.DrawString(textRect, attributes);

            var newImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            _iconCache.SetObjectforKey(newImage, nsString);
            return newImage;
        }

        private UIImage IconForText(string text, int bucketIndex)
        {
            var nsString = new NSString(text);
            var icon = _iconCache.ObjectForKey(nsString) as UIImage;
            if(icon != null)
            {
                return icon;
            }

            var font = UIFont.BoldSystemFontOfSize(14f);

            var paragraphStyle = (NSParagraphStyle)NSParagraphStyle.Default.MutableCopy();
            paragraphStyle.Alignment = UITextAlignment.Center;

            var attributes = new UIStringAttributes {
                Font = font,
                ParagraphStyle = paragraphStyle,
                ForegroundColor = UIColor.White
            };

            var textSize = nsString.GetSizeUsingAttributes(attributes);

            // Create an image context with a square shape to contain the text (with more padding for
            // larger buckets).
            var rectDimension = Math.Max(20, Math.Max(textSize.Width, textSize.Height)) + 3 * bucketIndex + 6;
            var rect = new CGRect(0f, 0f, rectDimension, rectDimension);

            UIGraphics.BeginImageContext(rect.Size);

            // draw background circle
            UIGraphics.BeginImageContextWithOptions(rect.Size, false, 0f);
            var ctx = UIGraphics.GetCurrentContext();
            ctx.SaveState();

            bucketIndex = Math.Min(bucketIndex, kGMUBucketBackgroundColors.Length - 1);
            var backColour = kGMUBucketBackgroundColors[bucketIndex];

            ctx.SetFillColor(backColour.CGColor);
            ctx.FillEllipseInRect(rect);
            ctx.RestoreState();

            UIColor.White.SetColor();
            var textRect = rect.Inset((rect.Width - textSize.Width) / 2, (rect.Height - textSize.Height) / 2);
            nsString.DrawString(textRect, attributes);

            var newImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            _iconCache.SetObjectforKey(newImage, nsString);
            return newImage;
        }

        private static UIColor ColorFromHex(int hexValue)
        {
            var colorWithRed = ((hexValue & 0xff0000) >> 16) / 255f;
            var colorWithGreen = ((hexValue & 0x00ff00) >> 8) / 255f;
            var colorWithBlue = ((hexValue & 0x0000ff) >> 0) / 255f;
            return UIColor.FromRGB(colorWithRed, colorWithGreen, colorWithBlue);
        }
    }
}
