using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Maps;
using GoogleMapsUtils.iOS.Clustering.Algo;
using GoogleMapsUtils.iOS.Clustering.View;
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
namespace GoogleMapsUtils.iOS.Clustering
{
    public class GMUClusterManager
    {
        private MapView _mapView;
        private CameraPosition _previousCamera;
        private int _clusterRequestCount;
        private IGMUClusterRenderer _renderer;

        public event EventHandler<IGMUCluster> DidTapCluster;
        public event EventHandler<IGMUClusterItem> DidTapClusterItem;

        public GMUClusterManager(MapView mapView, IGMUClusterAlgorithm algorithm, IGMUClusterRenderer renderer)
        {
            Algorithm = new GMUSimpleClusterAlgorithm();
            _mapView = mapView;
            _previousCamera = mapView.Camera;
            Algorithm = algorithm;
            _renderer = renderer;

            _mapView.CameraPositionChanged += OnCameraPositionChanged;
            _mapView.TappedMarker += OnTappedMarker;
        }

        public IGMUClusterAlgorithm Algorithm { get; private set; }

        ~GMUClusterManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void AddItem(IGMUClusterItem item)
        {
            Algorithm.AddItems(new[] { item });
        }

        public void AddItems(IEnumerable<IGMUClusterItem> items)
        {
            Algorithm.AddItems(items);
        }

        public void RemoveItem(IGMUClusterItem item)
        {
            Algorithm.RemoveItem(item);
        }

        public void ClearItems()
        {
            Algorithm.ClearItems();
            RequestCluster();
        }

        public void Cluster()
        {
            Task.Run(() => {
                lock (_renderer)
                {
                    var integralZoom = Convert.ToSingle(Math.Floor(_mapView.Camera.Zoom + 0.5f));
                    var clusters = Algorithm.ClustersAtZoom(integralZoom);

                    UIApplication.SharedApplication.InvokeOnMainThread(() =>
                    {
                        _renderer.RenderClusters(clusters);
                        _previousCamera = _mapView.Camera;
                    });
                }
            });
        }

        private void Dispose(bool disposing)
        {
            if (_previousCamera != null)
            {
                _previousCamera.Dispose();
                _previousCamera = null;
            }

            if(disposing)
            {
                if (_mapView != null)
                {
                    _mapView.CameraPositionChanged -= OnCameraPositionChanged;
                    _mapView.TappedMarker -= OnTappedMarker;
                    _mapView.Dispose();
                    _mapView = null;
                }
                
            }
        }

        private void RequestCluster()
        {
            ++_clusterRequestCount;
            var requestNumber = _clusterRequestCount;

            var weakSelf = new WeakReference<GMUClusterManager>(this);
            UIApplication.SharedApplication.InvokeOnMainThread(async () => {

                await Task.Delay(200);

                GMUClusterManager strongSelf;
                if(!weakSelf.TryGetTarget(out strongSelf))
                {
                    return;
                }

                if (requestNumber != strongSelf._clusterRequestCount)
                {
                    return;
                }

                strongSelf.Cluster();
            });
        }

        private bool OnTappedMarker(MapView mapView, Marker marker)
        {
            var userData = marker.UserData as UserDataHolder;
            if (userData != null)
            {
                var cluster = userData.Object as IGMUCluster;
                if (cluster != null)
                {
                    DidTapCluster?.Invoke(this, cluster);
                    return true;
                }

                var clusterItem = userData.Object as IGMUClusterItem;
                if (clusterItem != null)
                {
                    DidTapClusterItem?.Invoke(this, clusterItem);
                    return true;
                }
            }

            return false;
        }

        private void OnCameraPositionChanged(object sender, GMSCameraEventArgs e)
        {
            var camera = _mapView.Camera;
            var previousIntegralZoom = Convert.ToUInt32(Math.Floor(_previousCamera.Zoom + 0.5f));
            var currentIntegralZoom = Convert.ToUInt32(Math.Floor(camera.Zoom + 0.5f));

            if(previousIntegralZoom != currentIntegralZoom)
            {
                RequestCluster();
            } else {
                _renderer.Update();
            }
        }
    }
}
