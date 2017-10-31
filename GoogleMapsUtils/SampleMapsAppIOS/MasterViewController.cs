using System;
using System.Collections.Generic;
using Foundation;
using UIKit;

namespace SampleMapsAppIOS
{
    public partial class MasterViewController : UITableViewController
    {
        private Demo[] _samples;
        private DataSource _dataSource;
        private UIButton _infoButton;

        protected MasterViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            if (segue.Identifier == "showDetail")
            {
                var controller =
                    (DetailViewController) ((UINavigationController) segue.DestinationViewController).TopViewController;
                var indexPath = TableView.IndexPathForSelectedRow;
                var item = _dataSource.Objects[indexPath.Row];

                controller.SetDetailItem(item);
                controller.NavigationItem.LeftBarButtonItem = SplitViewController.DisplayModeButtonItem;
                controller.NavigationItem.LeftItemsSupplementBackButton = true;
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = NSBundle.MainBundle.LocalizedString("Demos", "Demos");

            // Perform any additional setup after loading the view, typically from a nib.

            _samples = Samples.LoadSamples();
            _infoButton = new UIButton(UIButtonType.InfoDark);
            _infoButton.AccessibilityLabel = "infoButton";
            NavigationItem.RightBarButtonItem = new UIBarButtonItem(_infoButton);
            TableView.Source = _dataSource = new DataSource(this, _samples);
        }

        public override void ViewWillAppear(bool animated)
        {
            ClearsSelectionOnViewWillAppear = SplitViewController.Collapsed;
            base.ViewWillAppear(animated);
            _infoButton.TouchUpInside += DisplayAboutPage;

        }

        public override void ViewWillDisappear(bool animated)
        {
            _infoButton.TouchUpInside -= DisplayAboutPage;
            base.ViewWillDisappear(animated);
        }

        void DisplayAboutPage(object sender, EventArgs args)
        {
            if (Storyboard.InstantiateViewController
                ("AboutViewController") is AboutViewController about)
            {
                NavigationController.PushViewController(about, true);
            }
        }

        class DataSource : UITableViewSource
        {
            static readonly NSString CellIdentifier = new NSString("Cell");
            private readonly MasterViewController _controller;
            private readonly List<object> _objects = new List<object>();

            public DataSource(MasterViewController controller, IEnumerable<Demo> samples)
            {
                _controller = controller;
                foreach (var sample in samples)
                {
                    _objects.Add(sample);
                }
            }

            public IList<object> Objects => _objects;

            public override bool CanEditRow(UITableView tableView, NSIndexPath indexPath)
            {
                // Return false if you do not want the specified item to be editable.
                return false;
            }

            // Customize the appearance of table view cells.
            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var cell = tableView.DequeueReusableCell(CellIdentifier, indexPath);

                if (_objects[indexPath.Row] is Demo demo)
                {
                    cell.TextLabel.Text = demo.Title;
                    cell.DetailTextLabel.Text = demo.Description;
                }


                return cell;
            }

            // Customize the number of sections in the table view.
            public override nint NumberOfSections(UITableView tableView)
            {
                return 1;
            }

            public override nint RowsInSection(UITableView tableview, nint section)
            {
                return _objects.Count;
            }
        }
    }
}