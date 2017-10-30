using System;
using CoreGraphics;
using Google.Maps;
using UIKit;

namespace SampleMapsAppIOS
{
	public partial class DetailViewController : UIViewController
	{
	    MapView _mapView;
		public object DetailItem { get; set; }

	    public MapView Map
	    {
	        get { return _mapView; }
	    }

	    public DetailViewController (IntPtr handle) : base (handle)
		{
		}

		public void SetDetailItem (object newDetailItem)
		{
			if (DetailItem != newDetailItem) {
				DetailItem = newDetailItem;
				
				// Update the view
				ConfigureView ();
			}
		}

		void ConfigureView ()
		{
			// Update the user interface for the detail item
		    if (IsViewLoaded && DetailItem != null)
		    {
		        if (DetailItem is Demo demo)
		        {
		            Title = demo.Title;
		            detailDescriptionLabel.Text = demo.Description;
		            // Create a GMSCameraPosition that tells the map to display the
		            // coordinate 37.79,-122.40 at zoom level 6.
		            var camera = CameraPosition.FromCamera(latitude: Demo.CameraLatitude,
		                longitude: Demo.CameraLongitude,
		                zoom: 14);
		            _mapView = MapView.FromCamera(CGRect.Empty, camera);
		            _mapView.MyLocationEnabled = true;
		            View = _mapView;
                    demo.SetUpDemo(this);

                }

            }

		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.
			ConfigureView ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}


