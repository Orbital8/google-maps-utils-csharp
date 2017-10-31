using System;
using CoreGraphics;
using Google.Maps;
using GoogleMapsUtils.iOS.Clustering;
using GoogleMapsUtils.iOS.Clustering.Algo;
using GoogleMapsUtils.iOS.Clustering.View;
using UIKit;

namespace SampleMapsAppIOS
{
    internal class ClusteringDemo : Demo
    {
        private bool _disposed = false;

        private const int ClusterItemCount = 10000;
        private GMUClusterManager _clusterManager;
        private MapView _mapView;

        private Random _rand = new Random();

        public override void SetUpDemo(DetailViewController viewController)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
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
            });

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

        private void OnDidTapCluster(object sender, IGMUCluster e)
        {
            UIApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                var newCamera = CameraPosition.FromCamera(e.Position, _mapView.Camera.Zoom + 1);

                var update = CameraUpdate.SetCamera(newCamera);
                _mapView.MoveCamera(update);
            });
        }

        private double RandomeScale()
        {
            return _rand.NextDouble() * (1.0 - (-1.0)) + -1.0;
        }

        protected override void Dispose(bool disposing)
        {
            Console.WriteLine($"Disposing {Title}");
            if (_disposed)
                return;
            if (disposing)
            {
                if (_mapView != null)
                {
                    _mapView.Clear();
                    _mapView = null;
                }
                if (_clusterManager != null)
                {
                    _clusterManager.DidTapCluster -= OnDidTapCluster;
                    _clusterManager.Dispose();
                    _clusterManager = null;
                }
            }
            _disposed = true;
            // Call base class implementation.
            base.Dispose(disposing);
        }
    }
}