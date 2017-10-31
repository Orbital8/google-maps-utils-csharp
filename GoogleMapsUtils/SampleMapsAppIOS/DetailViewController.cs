using System;
using CoreGraphics;
using Google.Maps;
using UIKit;

namespace SampleMapsAppIOS
{
    public partial class DetailViewController : UIViewController
    {
        MapView _mapView;

        public DetailViewController(IntPtr handle) : base(handle)
        {
        }

        public object DetailItem { get; set; }

        public MapView Map => _mapView;

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        public void SetDetailItem(object newDetailItem)
        {
            if (DetailItem != newDetailItem)
            {
                DetailItem = newDetailItem;

                // Update the view
                ConfigureView();
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
            ConfigureView();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            if (DetailItem is Demo demo)
            {
                demo.SetUpDemo(this);
            }
        }

        public override void ViewWillDisappear(bool animated)
        {
            if (DetailItem is IDisposable disposable)
            {
                disposable.Dispose();
                DetailItem = null;
            }

            base.ViewWillDisappear(animated);
        }


        void ConfigureView()
        {
            // Update the user interface for the detail item
            if (IsViewLoaded && DetailItem != null)
            {
                if (DetailItem is Demo demo)
                {
                    Title = demo.Title;
                    detailDescriptionLabel.Text = demo.Description;
                    var camera = CameraPosition.FromCamera(latitude: Demo.CameraLatitude,
                        longitude: Demo.CameraLongitude,
                        zoom: 14);
                        _mapView = MapView.FromCamera(CGRect.Empty, camera);
                        _mapView.MyLocationEnabled = true;
                    View = _mapView;
//                    demo.SetUpDemo(this);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (DetailItem is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}