using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Widget;
using Android.OS;
using Android.Support.V4.App;
using Android.Views;
using GoogleMapsUtils.Android.Clustering;
using Java.IO;
using Java.Lang;
//using Newtonsoft.Json;
using Org.Json;
using Console = System.Console;

namespace SampleMapsApp
{
    [Activity(Label = "SampleMapsApp", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private LinearLayout _list;

        public LinearLayout List => _list ?? (_list = FindViewById<LinearLayout>(Resource.Id.list));
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            AddDemo("Clustering", typeof(ClusteringDemoActivity));
        }

        protected override void OnResume()
        {
            base.OnResume();
            AddDemoClickHandlers();
        }


        protected override void OnPause()
        {
            RemoveDemoClickHandlers();
            base.OnPause();
        }

        private void AddDemoClickHandlers()
        {
            for (int i = 0; i < List.ChildCount; i++)
            {
                if (List.GetChildAt(i) is Button button)
                {
                    button.Click += OnDemoButtonClick;
                }
            }
        }

        private void RemoveDemoClickHandlers()
        {
            for (int i = 0; i < List.ChildCount; i++)
            {
                if (List.GetChildAt(i) is Button button)
                {
                    button.Click -= OnDemoButtonClick;
                }
            }
        }
        
        private void AddDemo(string demoName, Type activity)
        {
            var button = new Button(this);
            var layoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            button.LayoutParameters = layoutParameters;
            button.Text = demoName;
            button.Tag = new ActivityHolder {Activity = activity };
            List.AddView(button);
        }

        private void OnDemoButtonClick(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is ActivityHolder activityHolder)
                {
                    StartActivity(activityHolder.Activity);
                }
            }

        }

        private class ActivityHolder : Java.Lang.Object
        {
            public Type Activity { get; set; }
        }
    }

    [Activity(Label = "ClusteringDemoActivity")]
    public class ClusteringDemoActivity: DemoActivity
    {
        private ClusterManager _clusterManager;

        protected override async Task StartDemoAsync()
        {
            Map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(51.503186, -0.126446), 10));

            _clusterManager = new ClusterManager(this, Map);
            Map.SetOnCameraIdleListener(_clusterManager);

            try
            {
                await ReadItemsAsync();
            }
            catch (System.Exception e)
            {
            var builder = new AlertDialog.Builder(this);
            RunOnUiThread(() =>
                {
                    var alertDialog = builder.Create();
                    alertDialog.SetTitle("Error");
                    alertDialog.SetMessage("Problem reading list of markers.");
                    alertDialog.SetButton("OK", (s, ev) =>
                    {
                        Console.WriteLine("OK");
                    });
                    alertDialog.Show();
                }
            );
            }

        }

        private async Task ReadItemsAsync() 
        {
            var inputStream = Resources.OpenRawResource(Resource.Raw.radar_search);
            List<MyItem> items = await new MyItemReader().ReadAsync(inputStream);
            _clusterManager.AddItems(items);
    }

    internal class MyItemReader
    {
        public async Task<List<MyItem>> ReadAsync(Stream inputStream)
        {
            //Assuming data is small and can be held in memory
            var streamReader  = new StreamReader(inputStream);
            var text = await streamReader.ReadToEndAsync();
            var items = new List<MyItem>();
            JSONArray array = new JSONArray(text);
            for (int i = 0; i < array.Length(); i++)
            {
                string title = null;
                string snippet = null;
                JSONObject obj = array.GetJSONObject(i);
                double lat = obj.GetDouble("lat");
                double lng = obj.GetDouble("lng");
                if (!obj.IsNull("title"))
                {
                    title = obj.GetString("title");
                }
                if (!obj.IsNull("snippet"))
                {
                    snippet = obj.GetString("snippet");
                }
                items.Add(new MyItem(lat, lng, title, snippet));
            }
                // TODO getting reference errors with json.net
                // var items = JsonConvert.DeserializeObject<List<MyItem>>(text); 
            return items;
        }
    }

    internal class MyItem:IClusterItem
    {
        public MyItem(double lat, double lng, string title, string snippet)
        {
            Position = new LatLng(lat, lng);
            Title = title;
            Snippet = snippet;

        }
        public LatLng Position { get; }
        public string Title { get; }
        public string Snippet { get; }
    }
}

    public abstract class DemoActivity: FragmentActivity, IOnMapReadyCallback
    {
        private GoogleMap _map;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(LayoutId);
            SetUpMap();
        }

        private void SetUpMap()
        {
            ((SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map)).GetMapAsync(this);
        }

        public async void OnMapReady(GoogleMap googleMap)
        {
            if (Map != null)
            {
                return;
            }
            _map = googleMap;
            await StartDemoAsync();
        }

        protected abstract Task StartDemoAsync();

        protected int LayoutId  => Resource.Layout.map;

        protected GoogleMap Map => _map;
    }
}

