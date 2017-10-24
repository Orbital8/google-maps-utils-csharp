using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using GoogleMapsUtils.Android.Clustering.Algo;
using GoogleMapsUtils.Android.Clustering.View;

// Copyright 2017 Google Inc.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ported to C# from https://github.com/googlemaps/android-maps-utils
//
namespace GoogleMapsUtils.Android.Clustering
{
    public class ClusterManager 
        : Java.Lang.Object, GoogleMap.IOnCameraChangeListener, 
    GoogleMap.IOnMarkerClickListener, GoogleMap.IOnInfoWindowClickListener,
    GoogleMap.IInfoWindowAdapter
    {
        private readonly MarkerManager _markerManager;
        private readonly MarkerManager.Collection _markers;
        private readonly MarkerManager.Collection _clusterMarkers;
        private readonly object AlgorithmLock = new object();
        private readonly object ClusterTaskLock = new object();

        private IAlgorithm _algorithm;
        private IClusterRenderer _renderer;

        private GoogleMap _map;
        private CameraPosition _previousCameraPosition;
        private CancellationTokenSource _clusterCancellationSource;

        private IOnClusterItemClickListener _clusterItemClickListener;
        private IOnClusterItemInfoWindowClickListener _clusterItemInfoWindowClickListener;
        private IOnClusterClickListener _clusterClickListener;
        private IOnClusterInfoWindowClickListener _clusterInfoWindowClickListener;

        public ClusterManager(Context context, GoogleMap map)
            : this(context, map, new MarkerManager(map))
        {
        }

        public ClusterManager(Context context, GoogleMap map, MarkerManager markerManager)
        {
            _map = map;
            _markerManager = markerManager;
            _clusterMarkers = markerManager.NewCollection();
            _markers = markerManager.NewCollection();
            _renderer = new DefaultClusterRenderer(context, map, this);
            _algorithm = new PreCachingAlgorithmDecorator(new NonHierarchicalDistanceBasedAlgorithm());
            _renderer.ViewAdded();
        }

        public MarkerManager.Collection MarkerCollection => _markers;
        public MarkerManager.Collection ClusterMarkerCollection => _clusterMarkers;
        public MarkerManager MarkerManager => _markerManager;

        public IOnClusterClickListener ClusterClickListener
        {
            get { return _clusterClickListener; }
            set
            {
                _clusterClickListener = value;

                if(_renderer != null)
                {
                    _renderer.ClusterClickListener = value;
                }

            }
        }

        public IOnClusterInfoWindowClickListener ClusterInfoWindowClickListener
		{
			get { return _clusterInfoWindowClickListener; }
			set
			{
				_clusterInfoWindowClickListener = value;

				if (_renderer != null)
				{
                    _renderer.ClusterInfoWindowClickListener = value;
				}

			}
		}

		public IOnClusterItemClickListener ClusterItemClickListener
		{
			get { return _clusterItemClickListener; }
			set
			{
				_clusterItemClickListener = value;

				if (_renderer != null)
				{
					_renderer.ClusterItemClickListener = value;
				}

			}
		}

		public IOnClusterItemInfoWindowClickListener ClusterItemInfoWindowClickListener
		{
            get { return _clusterItemInfoWindowClickListener; }
			set
			{
				_clusterItemInfoWindowClickListener = value;

				if (_renderer != null)
				{
					_renderer.ClusterItemInfoWindowClickListener = value;
				}

			}
		}

        public IClusterRenderer Renderer
        {
            get { return _renderer; }
            set
            {
                if(_renderer != null)
                {
                    _renderer.ClusterClickListener = null;
                    _renderer.ClusterItemClickListener = null;
                }

                _clusterMarkers.Clear();
                _markers.Clear();
                _renderer?.ViewRemoved();

                _renderer = value;
                _renderer.ViewAdded();
                _renderer.ClusterClickListener = _clusterClickListener;
                _renderer.ClusterInfoWindowClickListener = _clusterInfoWindowClickListener;
                _renderer.ClusterItemClickListener = _clusterItemClickListener;
                _renderer.ClusterItemInfoWindowClickListener = _clusterItemInfoWindowClickListener;

                Cluster();
            }
        }

        public IAlgorithm Algorithm
        {
            get { return _algorithm; }
            set
            {
                lock(AlgorithmLock)
                {
                    if(_algorithm != null)
                    {
                        value.AddItems(_algorithm.Items);
                    }

                    _algorithm = new PreCachingAlgorithmDecorator(value);
                }

                Cluster();
            }
        }

        public bool EnableAnimation
        {
            get { return _renderer != null ? _renderer.EnableAnimation : false; }
            set 
            {
                if(_renderer != null)
                {
                    _renderer.EnableAnimation = value;
                }
            }
        }

        public void ClearItems()
        {
            lock(AlgorithmLock)
            {
                _algorithm.ClearItems();
                Cluster();
            }
        }

        public void AddItems(IEnumerable<IClusterItem> items)
        {
            lock(AlgorithmLock)
            {
                _algorithm.AddItems(items);
            }
        }

        public void AddItem(IClusterItem item)
        {
            lock(AlgorithmLock)
            {
                _algorithm.AddItem(item);
            }
        }

        public void RemoveItem(IClusterItem item)
        {
            lock(AlgorithmLock)
            {
                _algorithm.RemoveItem(item);
            }
        }

        public void Cluster()
        {
			if (_clusterCancellationSource != null)
			{
				_clusterCancellationSource.Cancel();
			}

			_clusterCancellationSource = new CancellationTokenSource();

			var token = _clusterCancellationSource.Token;
            IEnumerable<ICluster> clusters = null;

            var threadHandler = new Handler(Looper.MainLooper);
            threadHandler.Post(async () => {

                try
                {
                    var zoom = _map.CameraPosition.Zoom;
                    await Task.Run(() =>
                    {
                        clusters = _algorithm.GetClusters(zoom);
                        token.ThrowIfCancellationRequested();

                        if (clusters != null && !token.IsCancellationRequested)
                        {
	                        // invoke the renderer on the UI thread
	                        threadHandler.PostAtFrontOfQueue(() => _renderer.ClustersChanged(clusters));
                        }
                    }, token);
                }
                catch(System.OperationCanceledException)
                {
                    // ignore
                }
            });
        }

        public void OnCameraChange(CameraPosition cameraPosition)
        {
            if(_renderer is GoogleMap.IOnCameraChangeListener)
            {
                ((GoogleMap.IOnCameraChangeListener)_renderer).OnCameraChange(cameraPosition);
            }

            // don't re-compute clusters if the map has just been panned/tilted/rotated
            if(_previousCameraPosition != null && _previousCameraPosition.Zoom == cameraPosition.Zoom)
            {
                return;
            }

            _previousCameraPosition = cameraPosition;
            Cluster();
        }

        public bool OnMarkerClick(Marker marker)
        {
            return MarkerManager.OnMarkerClick(marker);
        }

        public void OnInfoWindowClick(Marker marker)
        {
            MarkerManager.OnInfoWindowClick(marker);
        }

        public global::Android.Views.View GetInfoContents(Marker marker)
        {
            return MarkerManager.GetInfoContents(marker);
        }

        public global::Android.Views.View GetInfoWindow(Marker marker)
        {
            return MarkerManager.GetInfoWindow(marker);
        }

        public interface IOnClusterClickListener
        {
            bool OnClusterClick(ICluster cluster, Marker marker);
        }

        public interface IOnClusterInfoWindowClickListener
        {
            void OnClusterInfoWindowClick(ICluster cluster);
        }

		public interface IOnClusterItemClickListener
		{
			bool OnClusterItemClick(IClusterItem clusterItem, Marker marker);
		}

		public interface IOnClusterItemInfoWindowClickListener
		{
			void OnClusterItemInfoWindowClick(IClusterItem clusterItem);
		}
    }
}
