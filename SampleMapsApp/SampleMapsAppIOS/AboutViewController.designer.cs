// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace SampleMapsAppIOS
{
    [Register ("AboutViewController")]
    partial class AboutViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        SampleMapsAppIOS.AboutView AboutView { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextView LicenseTextView { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (AboutView != null) {
                AboutView.Dispose ();
                AboutView = null;
            }

            if (LicenseTextView != null) {
                LicenseTextView.Dispose ();
                LicenseTextView = null;
            }
        }
    }
}