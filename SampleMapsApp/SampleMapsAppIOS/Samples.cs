using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Foundation;
using Google.Maps;
using GoogleMapsUtils.iOS.Clustering;
using GoogleMapsUtils.iOS.Clustering.Algo;
using GoogleMapsUtils.iOS.Clustering.View;
using UIKit;

namespace SampleMapsAppIOS
{
    class Samples
    {
        public static Demo[] LoadSamples()
        {
            var array = new Demo[] {new ClusteringDemo { Title = "Clustering", Description = "MarkerClustering"}};
            return array;
        }
    }

    internal class ClusteringDemo: Demo
    {
        private GMUClusterManager _clusterManager;
        private MapView _mapView;

        private const int ClusterItemCount = 10000;

        private Random _rand = new Random();

        public override void SetUpDemo(DetailViewController viewController)
        {
            _mapView = viewController.Map;
            // Set up the cluster manager with default icon generator and renderer.
            var iconGenerator = new GMUDefaultClusterIconGenerator();
            var algorithm = new GMUNonHierarchicalDistanceBasedAlgorithm();
            var renderer = new GMUDefaultClusterRenderer(_mapView, iconGenerator);

            _clusterManager = new GMUClusterManager(_mapView, algorithm, renderer);

            // Generate and add random items to the cluster manager.
            GenerateClusterItems();
            // Call cluster() after items have been added to perform the clustering and rendering on map.
            _clusterManager.Cluster();

            // Register self to listen to both GMUClusterManagerDelegate and GMSMapViewDelegate events.
            _clusterManager.DidTapCluster += OnDidTapCluster; // TODO unregister to prevent leak
            _clusterManager.DidTapClusterItem += OnDidTapClusterItem;

        }

        private void OnDidTapClusterItem(object sender, IGMUClusterItem e)
        {
//            var newCamera =  CameraPosition.FromCamera( e.Position, _mapView.Camera.Zoom+1);
//
//            var update = CameraUpdate.SetCamera(newCamera);
//            _mapView.MoveCamera(update);
        }

        private void OnDidTapCluster(object sender, IGMUCluster e)
        {
            var newCamera = CameraPosition.FromCamera(e.Position, _mapView.Camera.Zoom + 1);

            var update = CameraUpdate.SetCamera(newCamera);
            _mapView.MoveCamera(update);
        }

        private void GenerateClusterItems()
        {
            var extent = 0.2;
            for (int i = 0; i < ClusterItemCount; i++)
            {
                var lat = CameraLatitude + extent * RandomeScale();
                var lng = CameraLongitude + extent * RandomeScale();
                var name = $"Item {i}";
                var item = new POIItem(lat, lng, name);
                _clusterManager.AddItem(item);
            }
        }

        private double RandomeScale()
        {
            return _rand.NextDouble() * (1.0 - (-1.0)) + -1.0; 
        }
    }

    internal abstract class Demo
    {
        public const double CameraLatitude = -33.8;
        public const double CameraLongitude = 151.2;

        public string Title { get; set; }
        public string Description { get; set; }

        public abstract void SetUpDemo(DetailViewController detailViewController);
    }
}