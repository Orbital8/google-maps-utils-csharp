using System;
using UIKit;

namespace SampleMapsAppIOS
{
    public partial class AboutViewController : UIViewController
    {
        public AboutViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var text = Google.Maps.MapServices.OpenSourceLicenseInfo;
            LicenseTextView.Text = text;
        }
    }
}