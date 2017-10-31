using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreAnimation;
using CoreGraphics;
using CoreLocation;
using Foundation;
using Google.Maps;
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
namespace GoogleMapsUtils.iOS.Clustering.View
{
    public class GMUDefaultClusterRenderer : IGMUClusterRenderer
    {
		// Clusters smaller than this threshold will be expanded.
		private const int kGMUMinClusterSize = 4;

        // At zooms above this level, clusters will be expanded.
        // This is to prevent cases where items are so close to each other than they are always grouped.
        private const float kGMUMaxClusterZoom = 20f;

		// Animation duration for marker splitting/merging effects.
		private const double kGMUAnimationDuration = 0.5;  // seconds.

        // an invalid CLLocationCoordinate2D (anything that doesn't statify: -90 <= latitude <= 90, and -180 <= longitude <= 180) 
        private static readonly CLLocationCoordinate2D InvalidLocation = new CLLocationCoordinate2D(91, 181);

        private WeakReference<MapView> _mapView;
        private List<Marker> _markers;
        private IGMUClusterIconGenerator _clusterIconGenerator;
        private List<IGMUCluster> _clusters = new List<IGMUCluster>();
        private List<IGMUCluster> _renderedClusters;
        private List<IGMUClusterItem> _renderedClusterItems;
        private float _previousZoom;
        private Dictionary<IGMUClusterItem, IGMUCluster> _itemToOldClusterMap;
        private Dictionary<IGMUClusterItem, IGMUCluster> _itemToNewClusterMap;

        public GMUDefaultClusterRenderer(MapView mapView, IGMUClusterIconGenerator iconGenerator)
        {
            _mapView = new WeakReference<MapView>(mapView);
            _markers = new List<Marker>();
            _clusterIconGenerator = iconGenerator;
            _renderedClusters = new List<IGMUCluster>();
            _renderedClusterItems = new List<IGMUClusterItem>();
            AnimatesClusters = true;
            ZIndex = 1;
        }

        ~GMUDefaultClusterRenderer()
        {
            Dispose(false);
        }

        public event EventHandler<Marker> WillRenderMarker;
        public event EventHandler<Marker> DidRenderMarker;

        public bool AnimatesClusters { get; set; }
        public int ZIndex { get; set; }

        public Func<NSObject, Marker> MarkerForObject { get; set; }

        public virtual bool ShouldRenderAsCluster(IGMUCluster cluster, float zoom)
        {
            return cluster.Count >= kGMUMinClusterSize && zoom <= kGMUMaxClusterZoom;
        }

        public void RenderClusters(IEnumerable<IGMUCluster> clusters)
        {
			MapView mapView;
			if (!_mapView.TryGetTarget(out mapView))
			{
				return;
			}

            _renderedClusters.Clear();
            _renderedClusterItems.Clear();

            if (AnimatesClusters)
            {
                RenderAnimatedClusters(mapView, clusters); 
            }
            else
            {
                // No animation, just remove existing and add new ones
                _clusters = clusters.ToList();
                ClearMarkers(_markers);
                _markers = new List<Marker>();
                AddOrUpdateClusters(mapView, clusters, false);
            }
        }

        public void Update()
        {
			MapView mapView;
			if (!_mapView.TryGetTarget(out mapView))
			{
				return;
			}

            AddOrUpdateClusters(mapView, _clusters, false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {

            if (disposing)
            {
                Clear();
                if (_clusterIconGenerator != null)
                {
                    _clusterIconGenerator.Dispose();
                    _clusterIconGenerator = null;
                }
            }
        }

        private void RenderAnimatedClusters(MapView mapView, IEnumerable<IGMUCluster> clusters)
        {
            var zoom = mapView.Camera.Zoom;
            var isZoomingIn = zoom > _previousZoom;
            _previousZoom = zoom;

            PrepareClustersForAnimation(mapView, clusters, isZoomingIn);
            _clusters = clusters.ToList();

            var existingMarkers = _markers;
            _markers = new List<Marker>();

            AddOrUpdateClusters(mapView, clusters, isZoomingIn);

            if (isZoomingIn)
            {
                ClearMarkers(existingMarkers); 
            }
            else
            {
                ClearMarkersAnimated(mapView, existingMarkers);
            }
        }

        private void ClearMarkersAnimated(MapView mapView, IEnumerable<Marker> markers)
        {
            // Remove existing markers: animate to nearest new cluster
            var visibleBounds = new CoordinateBounds(mapView.Projection.VisibleRegion);

            foreach(var marker in markers)
            {
                // If the marker for the attached userData has just been added, do not perform animation.
                var userData = marker.UserData as UserDataHolder;
                if (userData != null && _renderedClusterItems.Contains(userData.Object))
                {
					marker.Map = null;
					continue;
                }

                // If the marker is outside the visible view port, do not perform animation.
                if(!visibleBounds.ContainsCoordinate(marker.Position))
                {
                    marker.Map = null;
                    continue;
                }

                // Find a candidate cluster to animate to
                IGMUCluster toCluster = null;
                var cluster = userData as IGMUCluster;
                if(cluster != null)
                {
                    toCluster = OverlappingCluster(cluster, _itemToNewClusterMap);
                }
                else
                {
                    var key = userData as IGMUClusterItem;
                    if(key != null && _itemToNewClusterMap.ContainsKey(key))
                    {
                        toCluster = _itemToNewClusterMap[key];
                    }
                }

                if (toCluster == null)
                {
                    marker.Map = null;
                    continue;
                }

                // All is good, perform the animation
                CATransaction.Begin();
                CATransaction.AnimationDuration = kGMUAnimationDuration;
                var toPosition = toCluster.Position;
                marker.Layer.Latitude = toPosition.Latitude;
                marker.Layer.Longitude = toPosition.Longitude;
                CATransaction.Commit();
			}

            // Clears existing markers after animation has presumably ended
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(kGMUAnimationDuration));
                UIApplication.SharedApplication.InvokeOnMainThread(() => ClearMarkers(markers));
            });
        }

        private void PrepareClustersForAnimation(MapView mapView, IEnumerable<IGMUCluster> newClusters, bool isZoomingIn)
        {
            var zoom = mapView.Camera.Zoom;

            if (isZoomingIn)
            {
                _itemToOldClusterMap = new Dictionary<IGMUClusterItem, IGMUCluster>();
                foreach(var cluster in _clusters)
                {
                    if (!ShouldRenderAsCluster(cluster, zoom))
                    {
                        continue;
                    }

                    foreach(var clusterItem in cluster.Items)
                    {
                        if (_itemToOldClusterMap.ContainsKey(clusterItem))
                        {
                            _itemToOldClusterMap[clusterItem] = cluster;
                        }
                        else
                        {
                            _itemToOldClusterMap.Add(clusterItem, cluster);
                        }

                    }
                }

                _itemToNewClusterMap = null;
            }
            else
            {
                _itemToOldClusterMap = null;
                _itemToNewClusterMap = new Dictionary<IGMUClusterItem, IGMUCluster>();

                foreach(var cluster in newClusters)
                {
                    if (!ShouldRenderAsCluster(cluster, zoom))
                    {
                        continue;
                    }

                    foreach (var clusterItem in cluster.Items)
                    {
                        if (_itemToNewClusterMap.ContainsKey(clusterItem))
                        {
                            _itemToNewClusterMap[clusterItem] = cluster;
                        }
                        else
                        {
                            _itemToNewClusterMap.Add(clusterItem, cluster);
                        }
                    }
                }
            }
        }

        private void AddOrUpdateClusters(MapView mapView, IEnumerable<IGMUCluster> clusters, bool animated)
        {
            var visibleBounds = new CoordinateBounds(mapView.Projection.VisibleRegion);

            foreach(var cluster in clusters)
            {
                if (_renderedClusters.Contains(cluster))
                {
                    continue;
                }

                var shouldShowCluster = visibleBounds.ContainsCoordinate(cluster.Position);
                if (!shouldShowCluster && animated)
                {
                    foreach (var item in cluster.Items)
                    {
                        if(_itemToOldClusterMap.ContainsKey(item))
                        {
                            var oldCluster = _itemToOldClusterMap[item];
                            if(visibleBounds.ContainsCoordinate(oldCluster.Position))
                            {
                                shouldShowCluster = true;
                                break;
                            }
                        }
                    }
                }

                if (shouldShowCluster)
                {
                    RenderCluster(mapView, cluster, animated);
                }
            }
        }

        private void RenderCluster(MapView mapView, IGMUCluster cluster, bool animated)
        {
            var zoom = mapView.Camera.Zoom;
            if (ShouldRenderAsCluster(cluster, zoom))
            {
                var fromPosition = InvalidLocation;
                if (animated)
                {
                    var fromCluster = OverlappingCluster(cluster, _itemToOldClusterMap);
                    if (fromCluster != null)
                    {
                        animated = true;
                        fromPosition = fromCluster.Position;
                    }
                    else
                    {
                        animated = false;
                    }
                }

                var icon = _clusterIconGenerator.IconForSize(cluster.Count);
                var marker = MarkerWithPosition(mapView, cluster.Position, fromPosition, new UserDataHolder(cluster), icon, animated);
                _markers.Add(marker);
            }
            else
            {
                foreach(var item in cluster.Items)
                {
                    var fromPosition = InvalidLocation;
                    bool shouldAnimate = animated;

                    if (shouldAnimate)
                    {
                        if(_itemToOldClusterMap.ContainsKey(item))
                        {
                            var fromCluster = _itemToOldClusterMap[item];
                            shouldAnimate = true;
                            fromPosition = fromCluster.Position;
                        }
                        else
                        {
                            shouldAnimate = false;
                        }
                    }

                    var marker = MarkerWithPosition(mapView, item.Position, fromPosition, new UserDataHolder(item), null, shouldAnimate);
                    _markers.Add(marker);
                    _renderedClusterItems.Add(item);
                }
            }

            _renderedClusters.Add(cluster);
        }

        private Marker MarkerWithPosition(MapView mapView,
                                          CLLocationCoordinate2D position,
                                          CLLocationCoordinate2D fromPosition,
                                          NSObject userData,
                                          UIImage clusterIcon,
                                          bool animated)
        {
            var marker = MarkerForObject != null ? MarkerForObject(userData) : null;
            marker = marker ?? new Marker();

            var initialPosition = animated ? fromPosition : position;
            marker.Position = initialPosition;
            marker.UserData = userData;

            if (clusterIcon != null)
            {
                marker.Icon = clusterIcon;
                marker.GroundAnchor = new CGPoint(0.5, 0.5);
            }

            marker.ZIndex = ZIndex;

            WillRenderMarker?.Invoke(this, marker);

            marker.Map = mapView;

            if (animated)
            {
                CATransaction.Begin();
                CATransaction.AnimationDuration = kGMUAnimationDuration;
                marker.Layer.Latitude = position.Latitude;
                marker.Layer.Longitude = position.Longitude;
                CATransaction.Commit();
            }

            DidRenderMarker?.Invoke(this, marker);
            return marker;
        }

        private List<IGMUCluster> VisibleClustersFromClusters(MapView mapView, IEnumerable<IGMUCluster> clusters)
        {
            var visibleClusters = new List<IGMUCluster>();

            var zoom = mapView.Camera.Zoom;
            var visibleBounds = new CoordinateBounds(mapView.Projection.VisibleRegion);

            foreach(var cluster in clusters)
            {
                if(!visibleBounds.ContainsCoordinate(cluster.Position))
                {
                    continue;
                }

                if(!ShouldRenderAsCluster(cluster, zoom))
                {
                    continue;
                }

                visibleClusters.Add(cluster);
            }

            return visibleClusters;
        }

        private IGMUCluster OverlappingCluster(IGMUCluster cluster, Dictionary<IGMUClusterItem, IGMUCluster> itemMap)
        {
            IGMUCluster found = null;

            foreach(var item in cluster.Items)
            {
                if(itemMap.ContainsKey(item))
                {
                    var candidate = itemMap[item];
                    if(candidate != null)
                    {
                        found = candidate;
                        break;
                    }
                }
            }

            return found;
        }

        private void Clear()
        {
            ClearMarkers(_markers);

            _markers.Clear();
            _renderedClusters.Clear();
            _renderedClusterItems.Clear();
            _itemToNewClusterMap?.Clear();
            _itemToOldClusterMap?.Clear();
            _clusters.Clear();
        }

        private void ClearMarkers(IEnumerable<Marker> markers)
        {
            foreach(var marker in markers)
            {
                marker.UserData = null;
                marker.Map = null;
            }
        }
    }
}
