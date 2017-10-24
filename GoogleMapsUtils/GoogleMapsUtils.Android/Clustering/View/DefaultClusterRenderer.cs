using System;
using System.Collections.Generic;
using System.Linq;
using Android.Animation;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using GoogleMapsUtils.Android.Clustering.Algo;
using GoogleMapsUtils.Android.Projection;
using GoogleMapsUtils.Android.UI;
using GoogleMapsUtils.Android.Util;
using Java.Lang;
using Java.Util.Concurrent.Locks;

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
namespace GoogleMapsUtils.Android.Clustering.View
{
    public class DefaultClusterRenderer : Java.Lang.Object, IClusterRenderer
    {
        private static bool ShouldAnimate = Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb;
        private static int[] Buckets = { 10, 20, 50, 100, 200, 500, 1000 };

        private readonly GoogleMap _map;
        private readonly IconGenerator _iconGenerator;
        private readonly ClusterManager _clusterManager;
        private readonly float _density;
        private readonly ViewModifier _viewModifier;

        private ShapeDrawable _colouredCircleBackground;
        private List<MarkerWithPosition> _markers = new List<MarkerWithPosition>();
        private LongSparseArray<BitmapDescriptor> _icons = new LongSparseArray<BitmapDescriptor>();
        private MarkerCache<IClusterItem> _markerCache = new MarkerCache<IClusterItem>();
        private List<ICluster> _clusters;
        private Dictionary<string, ICluster> _markerToCluster = new Dictionary<string, ICluster>();
        private Dictionary<ICluster, Marker> _clusterToMarker = new Dictionary<ICluster, Marker>();
        private float _zoom;

        public DefaultClusterRenderer(Context context, GoogleMap map, ClusterManager clusterManager)
        {
            _viewModifier = new ViewModifier(this);

            _map = map;
            EnableAnimation = true;
            _density = context.Resources.DisplayMetrics.Density;
            _iconGenerator = new IconGenerator(context);
            _iconGenerator.SetContentView(MakeSquareTextView(context));
            _iconGenerator.SetTextAppearance(Resource.Style.amu_ClusterIcon_TextAppearance);
            _iconGenerator.SetBackground(MakeClusterBackground());
            _clusterManager = clusterManager;
        }

        public int MinClusterSize { get; set; } = 4;
        public bool EnableAnimation { get; set; }
        public ClusterManager.IOnClusterClickListener ClusterClickListener { get; set; }
        public ClusterManager.IOnClusterInfoWindowClickListener ClusterInfoWindowClickListener { get; set; }
        public ClusterManager.IOnClusterItemClickListener ClusterItemClickListener { get; set; }
        public ClusterManager.IOnClusterItemInfoWindowClickListener ClusterItemInfoWindowClickListener { get; set; }
        public GoogleMap.IInfoWindowAdapter ItemInfoWindowAdapter { get; set; }
        public GoogleMap.IInfoWindowAdapter ClusterInfoWindowAdapter { get; set; }

        public void ViewAdded()
        {
            _clusterManager.MarkerCollection.InfoWindowAdapter = new InfoWindowAdapterWrapper(m => {
                if(ItemInfoWindowAdapter != null)
                {
                    return ItemInfoWindowAdapter.GetInfoContents(m);
                }

                return null;
            }, m => {
				if (ItemInfoWindowAdapter != null)
				{
                    return ItemInfoWindowAdapter.GetInfoWindow(m);
				}

				return null;
            });

            _clusterManager.MarkerCollection.MarkerClickListener = new MarkerClickListener(marker =>
            {
                return ClusterItemClickListener != null && ClusterItemClickListener.OnClusterItemClick(_markerCache.Get(marker), marker);
            });

            _clusterManager.MarkerCollection.InfoWindowClickListener = new InfoWindowClickListener(marker =>
            {
                if (ClusterItemInfoWindowClickListener != null)
                {
                    ClusterItemInfoWindowClickListener.OnClusterItemInfoWindowClick(_markerCache.Get(marker));
                }
            });

            _clusterManager.ClusterMarkerCollection.InfoWindowAdapter = new InfoWindowAdapterWrapper(m => {
                if(ClusterInfoWindowAdapter != null)
                {
                    return ClusterInfoWindowAdapter.GetInfoContents(m);
                }

                return null;
            }, m => {
				if (ClusterInfoWindowAdapter != null)
				{
                    return ClusterInfoWindowAdapter.GetInfoWindow(m);
				}

				return null;
            });

            _clusterManager.ClusterMarkerCollection.MarkerClickListener = new MarkerClickListener(marker =>
            {

                ICluster cluster = null;
                _markerToCluster.TryGetValue(marker.Id, out cluster);

                return ClusterClickListener != null && ClusterClickListener.OnClusterClick(cluster, marker);
            });

            _clusterManager.ClusterMarkerCollection.InfoWindowClickListener = new InfoWindowClickListener(marker =>
            {
                if (ClusterInfoWindowClickListener != null)
                {
                    ICluster cluster = null;
                    _markerToCluster.TryGetValue(marker.Id, out cluster);

                    ClusterInfoWindowClickListener.OnClusterInfoWindowClick(cluster);
                }
            });
        }

        public void ViewRemoved()
        {
            _clusterManager.MarkerCollection.InfoWindowAdapter = null;
            _clusterManager.MarkerCollection.MarkerClickListener = null;
            _clusterManager.MarkerCollection.InfoWindowClickListener = null;
            _clusterManager.ClusterMarkerCollection.InfoWindowAdapter = null;
            _clusterManager.ClusterMarkerCollection.MarkerClickListener = null;
            _clusterManager.ClusterMarkerCollection.InfoWindowClickListener = null;
        }

        public void ClustersChanged(IEnumerable<ICluster> clusters)
        {
            _viewModifier.Queue(clusters.ToList());
        }

        public Marker GetMarker(IClusterItem clusterItem)
        {
            return _markerCache.Get(clusterItem);
        }

        public IClusterItem GetClusterItem(Marker marker)
        {
            return _markerCache.Get(marker);
        }

        public Marker GetMarker(ICluster cluster)
        {
            Marker marker;
            if(_clusterToMarker.TryGetValue(cluster, out marker))
            {
                return marker;
            }

            return null;
        }

        public ICluster GetCluster(Marker marker)
        {
            ICluster cluster;
            if(_markerToCluster.TryGetValue(marker.Id, out cluster))
            {
                return cluster;
            }

            return null;
        }

        protected virtual Color GetColor(int clusterSize)
        {
            var hueRange = 220f;
            var sizeRange = 300f;
            var size = System.Math.Min(clusterSize, sizeRange);
            var hue = (sizeRange - size) * (sizeRange - size) / (sizeRange * sizeRange) * hueRange;

            return Color.HSVToColor(new[] { hue, 1f, 0.6f });
        }

        protected virtual string GetClusterText(int bucket)
        {
            if (bucket < Buckets[0])
            {
                return bucket.ToString();
            }

            return bucket.ToString() + "+";
        }

        protected int GetBucket(ICluster cluster)
        {
            var size = cluster.Count;
            if (size <= Buckets[0])
            {
                return size;
            }

            for (int i = 0; i < Buckets.Length - 1; i++)
            {
                if (size < Buckets[i + 1])
                {
                    return Buckets[i];
                }
            }

            return Buckets[Buckets.Length - 1];
        }

        protected virtual bool ShouldRenderAsCluster(ICluster cluster)
        {
            return cluster.Count > MinClusterSize;
        }

        protected virtual void OnBeforeClusterItemRendered(IClusterItem item, MarkerOptions markerOptions)
		{
		}

        protected virtual void OnBeforeClusterRendered(ICluster cluster, MarkerOptions markerOptions)
        {
            var bucket = GetBucket(cluster);
            var descriptor = _icons.Get(bucket);

            if(descriptor == null)
            {
                _colouredCircleBackground.Paint.Color = GetColor(bucket);
                descriptor = BitmapDescriptorFactory.FromBitmap(_iconGenerator.MakeIcon(GetClusterText(bucket)));
                _icons.Put(bucket, descriptor);
            }

            markerOptions.InvokeIcon(descriptor);
        }

        protected virtual void OnClusterRendered(ICluster cluster, Marker marker)
        {
        }

        protected virtual void OnClusterItemRendered(IClusterItem clusterItem, Marker marker)
        {
        }

        private LayerDrawable MakeClusterBackground()
        {
            _colouredCircleBackground = new ShapeDrawable(new OvalShape());
            var outline = new ShapeDrawable(new OvalShape());
            outline.Paint.Color = new Color(255, 255, 255, 128); // transparent white

            var background = new LayerDrawable(new[] { outline, _colouredCircleBackground });
            var strokeWidth = Convert.ToInt32(_density * 3);
            background.SetLayerInset(1, strokeWidth, strokeWidth, strokeWidth, strokeWidth);
            return background;
        }

        private SquareTextView MakeSquareTextView(Context context)
        {
            var squareTextView = new SquareTextView(context);
            var layoutParams = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent);
            squareTextView.LayoutParameters = layoutParams;
            squareTextView.Id = Resource.Id.amu_text;

            var twelveDpi = (int)(12 * _density);
            squareTextView.SetPadding(twelveDpi, twelveDpi, twelveDpi, twelveDpi);
            return squareTextView;
        }

        private double DistanceSquared(Geometry.Point a, Geometry.Point b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        private Geometry.Point FindClosestCluster(List<Geometry.Point> markers, Geometry.Point point)
        {
            if(markers == null || markers.Count == 0)
            {
                return null;
            }

            const int maxDistanceAtZoom = NonHierarchicalDistanceBasedAlgorithm.MaxDistanceAtZoom;
            double minDistSquared = maxDistanceAtZoom * maxDistanceAtZoom;

            Geometry.Point closest = null;
            foreach(var candidate in markers)
            {
                var dist = DistanceSquared(candidate, point);
                if(dist < minDistSquared)
                {
                    closest = candidate;
                    minDistSquared = dist;
                }
            }

            return closest;
        }

        private class InfoWindowAdapterWrapper : Java.Lang.Object, GoogleMap.IInfoWindowAdapter
        {
            private Func<Marker, global::Android.Views.View> _getInfoContents;
            private Func<Marker, global::Android.Views.View> _getInfoWindow;

            public InfoWindowAdapterWrapper(Func<Marker, global::Android.Views.View> getInfoContents,
                                            Func<Marker, global::Android.Views.View> getInfoWindow)
            {
                _getInfoContents = getInfoContents;
                _getInfoWindow = getInfoWindow;
            }

            public global::Android.Views.View GetInfoContents(Marker marker)
            {
                return _getInfoContents?.Invoke(marker);
            }

            public global::Android.Views.View GetInfoWindow(Marker marker)
            {
                return _getInfoWindow?.Invoke(marker);
            }
        }

        private class MarkerClickListener : Java.Lang.Object, GoogleMap.IOnMarkerClickListener
        {
            private Func<Marker, bool> _action;

            public MarkerClickListener(Func<Marker, bool> action)
            {
                _action = action;
            }

            public bool OnMarkerClick(Marker marker)
            {
                return _action.Invoke(marker);
            }
        }

        private class InfoWindowClickListener : Java.Lang.Object, GoogleMap.IOnInfoWindowClickListener
        {
            private Action<Marker> _action;

            public InfoWindowClickListener(Action<Marker> action)
            {
                _action = action;
            }

            public void OnInfoWindowClick(Marker marker)
            {
                _action.Invoke(marker);
            }
        }

        private class ViewModifier : Handler
        {
            private const int RunTask = 0;
            private const int TaskFinished = 1;

            private readonly WeakReference<DefaultClusterRenderer> _parent;

            private bool _viewModificationInProgress = false;
            private RenderTask _nextClusters = null;

            public ViewModifier(DefaultClusterRenderer parent)
            {
                _parent = new WeakReference<DefaultClusterRenderer>(parent);
            }

            public override void HandleMessage(Message msg)
            {
                if (msg.What == TaskFinished)
                {
                    _viewModificationInProgress = false;
                    if (_nextClusters != null)
                    {
                        // run the task that was queued up
                        SendEmptyMessage(RunTask);
                    }

                    return;
                }

                try
                {
                    RemoveMessages(RunTask);

                    if (_viewModificationInProgress)
                    {
                        // busy - wait for the callback
                        return;
                    }

                    if (_nextClusters == null)
                    {
                        // nothing to do
                        return;
                    }

                    DefaultClusterRenderer renderer;
                    if (!_parent.TryGetTarget(out renderer))
                    {
                        return;
                    }

                    var map = renderer._map;
                    if (map == null)
                    {
                        return;
                    }

                    var projection = map.Projection;
                    if (projection == null)
                    {
                        return;
                    }

                    RenderTask renderTask;
                    lock (this)
                    {
                        renderTask = _nextClusters;
                        _nextClusters = null;
                        _viewModificationInProgress = true;
                    }

                    renderTask.Callback = () =>
                    {
                        SendEmptyMessage(TaskFinished);
                    };

                    renderTask.Projection = projection;
                    renderTask.MapZoom = map.CameraPosition.Zoom;
                    new Thread(renderTask).Start();
                }
                catch
                {
                    // consume
                }
            }

            public void Queue(List<ICluster> clusters)
            {
                lock (this)
                {
					DefaultClusterRenderer renderer;
					if (!_parent.TryGetTarget(out renderer))
					{
						return;
					}

                    _nextClusters = new RenderTask(renderer, clusters);
                }

                SendEmptyMessage(RunTask);
            }
        }

        private class RenderTask : Java.Lang.Object, IRunnable
        {
            private readonly DefaultClusterRenderer _parent;
            private readonly List<ICluster> _clusters;

            private SphericalMercatorProjection _sphericalMercatorProjection;
            private float _mapZoom;

            public RenderTask(DefaultClusterRenderer parent, List<ICluster> clusters)
            {
                _parent = parent;
                _clusters = clusters;
            }

            public Action Callback { get; set; }
            public global::Android.Gms.Maps.Projection Projection { get; set; }

            public float MapZoom
            {
                get { return _mapZoom; }
                set
                {
                    _mapZoom = value;
                    _sphericalMercatorProjection = new SphericalMercatorProjection(256 * System.Math.Pow(2, System.Math.Min(value, _mapZoom)));
                }
            }

            public void Run()
            {
                if(_clusters.Equals(_parent._clusters))
                {
                    Callback.Invoke();
                    return;
                }

                var markerModifier = new MarkerModifier(_parent);
                var zoom = MapZoom;
                var zoomingIn = zoom > _parent._zoom;
                var zoomDelta = zoom - _parent._zoom;
                var markersToRemove = _parent._markers;
                var visibleBounds = Projection.VisibleRegion.LatLngBounds;


                List<Geometry.Point> existingClustersOnScreen = null;
                if(_parent._clusters != null && ShouldAnimate)
                {
                    existingClustersOnScreen = new List<Geometry.Point>();
                    foreach(var cluster in _parent._clusters)
                    {
                        if(_parent.ShouldRenderAsCluster(cluster) && visibleBounds.Contains(cluster.Position))
                        {
                            var point = _sphericalMercatorProjection.ToPoint(cluster.Position);
                            existingClustersOnScreen.Add(point);
                        }
                    }
                }

                var newMarkers = new List<MarkerWithPosition>();
                foreach(var cluster in _clusters)
                {
                    var onScreen = visibleBounds.Contains(cluster.Position);
                    if(zoomingIn && onScreen && ShouldAnimate)
                    {
                        var point = _sphericalMercatorProjection.ToPoint(cluster.Position);
                        var closest = _parent.FindClosestCluster(existingClustersOnScreen, point);

                        if(closest != null && _parent.EnableAnimation)
                        {
                            var animateTo = _sphericalMercatorProjection.ToLatLng(closest);
                            markerModifier.Add(true, new CreateMarkerTask(_parent, cluster, newMarkers, animateTo));
                        }
                        else
                        {
                            markerModifier.Add(true, new CreateMarkerTask(_parent, cluster, newMarkers, null));
                        }
                    }
                    else
                    {
                        markerModifier.Add(onScreen, new CreateMarkerTask(_parent, cluster, newMarkers, null));
                    }
                }

				// Wait for all markers to be added.
				markerModifier.WaitUntilFree();

                // Don't remove any markers that were just added. This is basically anything that had
                // a hit in the MarkerCache.
                foreach(var m in newMarkers)
                {
                    markersToRemove.Remove(m);
                }

                // Find all of the new clusters that were added on-screen. These are candidates for
                // markers to animate from.
                List<Geometry.Point> newClustersOnScreen = null;
                if(ShouldAnimate)
                {
                    newClustersOnScreen = new List<Geometry.Point>();
                    foreach(var cluster in _clusters)
                    {
                        if(_parent.ShouldRenderAsCluster(cluster) && visibleBounds.Contains(cluster.Position))
                        {
                            var point = _sphericalMercatorProjection.ToPoint(cluster.Position);
                            newClustersOnScreen.Add(point);
                        }
                    }
                }

                // Remove the old markers, animating them into clusters if zooming out.
                foreach(var marker in markersToRemove)
                {
                    var onScreen = visibleBounds.Contains(marker.Position);

                    // Don't animate when zooming out more than 3 zoom levels.
                    if(!zoomingIn && zoomDelta > -3 && onScreen && ShouldAnimate)
                    {
                        var point = _sphericalMercatorProjection.ToPoint(marker.Position);
                        var closest = _parent.FindClosestCluster(newClustersOnScreen, point);

                        if(closest != null && _parent.EnableAnimation)
                        {
                            var animateTo = _sphericalMercatorProjection.ToLatLng(closest);
                            markerModifier.AnimateThenRemove(marker, marker.Position, animateTo);
                        }
                        else
                        {
                            markerModifier.Remove(true, marker.Marker);
                        }
                    }
                    else
                    {
                        markerModifier.Remove(onScreen, marker.Marker);
                    }
                }

                markerModifier.WaitUntilFree();

                _parent._markers = newMarkers;
                _parent._clusters = _clusters;
                _parent._zoom = zoom;

                Callback.Invoke();
			}
        }

        private class MarkerModifier : Handler, MessageQueue.IIdleHandler
        {
            private const int Blank = 0;

            private readonly DefaultClusterRenderer _parent;
            private readonly ILock _lock = new ReentrantLock();
            private readonly ICondition _busyCondition;

            private Queue<CreateMarkerTask> _createMarkerTasks = new Queue<CreateMarkerTask>();
            private Queue<CreateMarkerTask> _onScreenCreateMarkerTasks = new Queue<CreateMarkerTask>();
            private Queue<Marker> _removeMarkerTasks = new Queue<Marker>();
            private Queue<Marker> _onScreenRemoveMarkerTasks = new Queue<Marker>();
            private Queue<AnimationTask> _animationTasks = new Queue<AnimationTask>();
            private bool _listenerAdded;

            public MarkerModifier(DefaultClusterRenderer parent) : base(Looper.MainLooper)
            {
                _parent = parent;
                _busyCondition = _lock.NewCondition();
            }

            public bool IsBusy
            {
                get
                {
                    try
                    {
                        _lock.Lock();
                        return !(_createMarkerTasks.Count == 0 &&
                                 _onScreenCreateMarkerTasks.Count == 0 &&
                                 _onScreenRemoveMarkerTasks.Count == 0 &&
                                 _removeMarkerTasks.Count == 0 &&
                                 _animationTasks.Count == 0);
                    }
                    finally
                    {
                        _lock.Unlock();
                    }
                }
            }

            public void Add(bool priority, CreateMarkerTask c)
            {
                _lock.Lock();
                SendEmptyMessage(Blank);

                if(priority)
                {
                    _onScreenCreateMarkerTasks.Enqueue(c);
                }
                else
                {
                    _createMarkerTasks.Enqueue(c);
                }

                _lock.Unlock();
            }

            public void Remove(bool priority, Marker m)
            {
                _lock.Lock();
                SendEmptyMessage(Blank);

                if(priority)
                {
                    _onScreenRemoveMarkerTasks.Enqueue(m);
                }
                else
                {
                    _removeMarkerTasks.Enqueue(m);
                }

                _lock.Unlock();
            }

            public void Animate(MarkerWithPosition marker, LatLng from, LatLng to)
            {
                _lock.Lock();
                _animationTasks.Enqueue(new AnimationTask(_parent, marker, from, to));
                _lock.Unlock();
            }

            public void AnimateThenRemove(MarkerWithPosition marker, LatLng from, LatLng to)
            {
                _lock.Lock();
                var animationTask = new AnimationTask(_parent, marker, from, to);
                animationTask.RemoveOnAnimationComplete(_parent._clusterManager.MarkerManager);
                _animationTasks.Enqueue(animationTask);
                _lock.Unlock();
            }

            public override void HandleMessage(Message msg)
            {
                if(!_listenerAdded)
                {
                    Looper.MyQueue().AddIdleHandler(this);
                    _listenerAdded = true;
                }

                RemoveMessages(Blank);

                _lock.Lock();
                try
                {
                    // Perform up to 10 tasks at once.
                    // Consider only performing 10 remove tasks, not adds and animations.
                    // Removes are relatively slow and are much better when batched.
                    for (var i = 0; i < 10; i++)
                    {
                        PerformNextTask();
                    }

                    if(!IsBusy)
                    {
                        _listenerAdded = false;
                        Looper.MyQueue().RemoveIdleHandler(this);

                        // signal any other threads that are waiting
                        _busyCondition.SignalAll();
                    }
                    else
                    {
                        // Sometimes the idle queue may not be called - schedule up some work regardless
                        // of whether the UI thread is busy or not.
                        SendEmptyMessageDelayed(Blank, 10);
					}
				}
                finally
                {
                    _lock.Unlock();
                }
            }

            public void WaitUntilFree()
            {
                while(IsBusy)
                {
                    // Sometimes the idle queue may not be called - schedule up some work regardless
                    // of whether the UI thread is busy or not.
                    SendEmptyMessage(Blank);

                    _lock.Lock();
                    try
                    {
                        if(IsBusy)
                        {
                            _busyCondition.Await();
                        }
                    }
                    catch(InterruptedException ex)
                    {
                        throw new RuntimeException(ex);
                    }
                    finally
                    {
                        _lock.Unlock();
                    }
				}
            }

            public bool QueueIdle()
            {
                // When the UI is not busy, schedule some work.
                SendEmptyMessage(Blank);
                return true;
            }

            private void PerformNextTask()
            {
                if(_onScreenRemoveMarkerTasks.Count != 0)
                {
                    RemoveMarker(_onScreenRemoveMarkerTasks.Dequeue());
                }
                else if(_animationTasks.Count != 0)
                {
                    var task = _animationTasks.Dequeue();
                    task.Perform();
                }
                else if(_onScreenCreateMarkerTasks.Count != 0)
                {
                    var task = _onScreenCreateMarkerTasks.Dequeue();
                    task.Perform(this);
                }
                else if(_createMarkerTasks.Count != 0)
                {
                    var task = _createMarkerTasks.Dequeue();
                    task.Perform(this);
                }
                else if(_removeMarkerTasks.Count != 0)
                {
                    RemoveMarker(_removeMarkerTasks.Dequeue());
                }
            }

            private void RemoveMarker(Marker m)
            {
                ICluster cluster;
                if (_parent._markerToCluster.TryGetValue(m.Id, out cluster))
                {
                    _parent._clusterToMarker.Remove(cluster);
                }

                _parent._markerCache.Remove(m);
                _parent._markerToCluster.Remove(m.Id);
                _parent._clusterManager.MarkerManager.Remove(m);
            }
        }

        private class CreateMarkerTask
        {
            private readonly DefaultClusterRenderer _parent;
            private readonly ICluster _cluster;
            private readonly IList<MarkerWithPosition> _newMarkers;
            private readonly LatLng _animateFrom;

            public CreateMarkerTask(DefaultClusterRenderer parent, ICluster cluster, IList<MarkerWithPosition> markersAdded, LatLng animateFrom)
            {
                _parent = parent;
                _cluster = cluster;
                _newMarkers = markersAdded;
                _animateFrom = animateFrom;
            }

            public void Perform(MarkerModifier markerModifier)
            {
                Marker marker = null;
                MarkerWithPosition markerWithPosition;

                // Don't show small clusters. Render the markers inside, instead.
                if(!_parent.ShouldRenderAsCluster(_cluster))
                {
                    foreach(var item in _cluster.Items)
                    {
                        marker = _parent._markerCache.Get(item);

                        if(marker == null)
                        {
                            var markerOptions = new MarkerOptions();
                            if(_animateFrom != null)
                            {
                                markerOptions.SetPosition(_animateFrom);
                            } else {
                                markerOptions.SetPosition(item.Position);
                            }

                            if(!(item.Title == null) && !(item.Snippet == null))
                            {
                                markerOptions.SetTitle(item.Title);
                                markerOptions.SetSnippet(item.Snippet);
                            }
                            else if(!(item.Snippet == null))
                            {
                                markerOptions.SetTitle(item.Snippet);
                            }
                            else if(!(item.Title == null))
                            {
                                markerOptions.SetTitle(item.Title);
                            }

                            _parent.OnBeforeClusterItemRendered(item, markerOptions);
                            marker = _parent._clusterManager.MarkerCollection.AddMarker(markerOptions);
                            markerWithPosition = new MarkerWithPosition(marker);
                            _parent._markerCache.Put(item, marker);

                            if(_animateFrom != null)
                            {
                                markerModifier.Animate(markerWithPosition, _animateFrom, item.Position);
                            }
                        }
                        else
                        {
                            markerWithPosition = new MarkerWithPosition(marker);
                        }

                        _parent.OnClusterItemRendered(item, marker);
                        _newMarkers.Add(markerWithPosition);
                    }

                    return;
                }

                if(!_parent._clusterToMarker.TryGetValue(_cluster, out marker) || marker == null)
                {
                    var markerOptions = new MarkerOptions();
                    markerOptions.SetPosition(_animateFrom == null ? _cluster.Position : _animateFrom);

                    _parent.OnBeforeClusterRendered(_cluster, markerOptions);
                    marker = _parent._clusterManager.ClusterMarkerCollection.AddMarker(markerOptions);
                    _parent._markerToCluster.Add(marker.Id, _cluster);
                    _parent._clusterToMarker.Add(_cluster, marker);
                    markerWithPosition = new MarkerWithPosition(marker);

                    if(_animateFrom != null)
                    {
                        markerModifier.Animate(markerWithPosition, _animateFrom, _cluster.Position);
                    }
                }
                else
                {
                    markerWithPosition = new MarkerWithPosition(marker);
                }

                _parent.OnClusterRendered(_cluster, marker);
                _newMarkers.Add(markerWithPosition);
            }
        }

        private class AnimationTask : AnimatorListenerAdapter, ValueAnimator.IAnimatorUpdateListener
        {
            private static readonly ITimeInterpolator AnimationInterpolator = new DecelerateInterpolator();

            private readonly DefaultClusterRenderer _parent;
            private readonly MarkerWithPosition _markerWithPosition;
            private readonly Marker _marker;
            private readonly LatLng _from;
            private readonly LatLng _to;
            private bool _removeOnComplete;
            private MarkerManager _markerManager;

            public AnimationTask(DefaultClusterRenderer parent, MarkerWithPosition markerWithPosition, LatLng from, LatLng to)
            {
                _parent = parent;
                _markerWithPosition = markerWithPosition;
                _marker = markerWithPosition.Marker;
                _from = from;
                _to = to;
            }

            public void Perform()
            {
                var valueAnimator = ValueAnimator.OfFloat(0f, 1f);
                valueAnimator.SetInterpolator(AnimationInterpolator);
                valueAnimator.AddUpdateListener(this);
                valueAnimator.AddListener(this);
                valueAnimator.Start();
            }

            public override void OnAnimationEnd(Animator animation)
            {
                if(_removeOnComplete)
                {
                    ICluster cluster;
                    if (_parent._markerToCluster.TryGetValue(_marker.Id, out cluster))
                    {
                        _parent._clusterToMarker.Remove(cluster);
                    }

                    _parent._markerCache.Remove(_marker);
                    _parent._markerToCluster.Remove(_marker.Id);
                    _markerManager.Remove(_marker);
                }

                _markerWithPosition.Position = _to;
            }

            public void RemoveOnAnimationComplete(MarkerManager markerManager)
            {
                _markerManager = markerManager;
                _removeOnComplete = true;
            }

            public void OnAnimationUpdate(ValueAnimator valueAnimator)
            {
                var fraction = valueAnimator.AnimatedFraction;
                var lat = (_to.Latitude - _from.Latitude) * fraction + _from.Latitude;
                var lngDelta = _to.Longitude - _from.Longitude;

                // take the shortest parth across the 180th meridian
                if(System.Math.Abs(lngDelta) > 180)
                {
                    lngDelta -= Java.Lang.Math.Signum(lngDelta) * 360;
                }

                var lng = lngDelta * fraction + _from.Longitude;
                var position = new LatLng(lat, lng);
                _marker.Position = position;
            }
        }
    }
}
