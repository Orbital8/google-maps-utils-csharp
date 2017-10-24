using System;
using System.Collections.Generic;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Views;

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
namespace GoogleMapsUtils.Android
{
    public class MarkerManager 
        : Java.Lang.Object, GoogleMap.IOnInfoWindowClickListener, GoogleMap.IOnMarkerClickListener, 
    GoogleMap.IOnMarkerDragListener, GoogleMap.IInfoWindowAdapter
    {
        private readonly GoogleMap _map;
        private readonly Dictionary<string, Collection> _namedCollections = new Dictionary<string, Collection>();
        private readonly Dictionary<string, Collection> _allMarkers = new Dictionary<string, Collection>();

        public MarkerManager(GoogleMap map)
        {
            _map = map;
        }

        public Collection NewCollection()
        {
            return new Collection(this);
        }

        public Collection NewCollection(string id)
        {
            if(_namedCollections.ContainsKey(id))
            {
                throw new ArgumentException("Collection id is not unique: " + id);
            }

            var collection = new Collection(this);
            _namedCollections.Add(id, collection);
            return collection;
        }

        public Collection GetCollection(string id)
        {
            Collection value;

            if (_namedCollections.TryGetValue(id, out value))
            {
                return value;
            }

            return null;
        }

        public bool Remove(Marker marker)
        {
            Collection value;

            if (_allMarkers.TryGetValue(marker.Id, out value))
            {
                return value.Remove(marker);
            }

            return false;
        }

        public void OnInfoWindowClick(Marker marker)
        {
			Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
			{
                if (collection.InfoWindowClickListener != null)
				{
                    collection.InfoWindowClickListener.OnInfoWindowClick(marker);
				}
			}
        }

        public bool OnMarkerClick(Marker marker)
        {
			Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
			{
                if (collection.MarkerClickListener != null)
				{
                    return collection.MarkerClickListener.OnMarkerClick(marker);
				}
			}

            return false;
        }

        public void OnMarkerDrag(Marker marker)
        {
			Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
			{
				if (collection.MarkerDragListener != null)
				{
                    collection.MarkerDragListener.OnMarkerDrag(marker);
				}
			}
        }

        public void OnMarkerDragEnd(Marker marker)
        {
			Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
			{
				if (collection.MarkerDragListener != null)
				{
                    collection.MarkerDragListener.OnMarkerDragEnd(marker);
				}
			}
        }

        public void OnMarkerDragStart(Marker marker)
        {
			Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
			{
                if (collection.MarkerDragListener != null)
				{
                    collection.MarkerDragListener.OnMarkerDragStart(marker);
				}
			}
        }

        public View GetInfoContents(Marker marker)
        {
            Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
            {
                if (collection.InfoWindowAdapter != null)
                {
                    return collection.InfoWindowAdapter.GetInfoContents(marker);
                }
            }

            return null;
        }

        public View GetInfoWindow(Marker marker)
        {
            Collection collection;

            if (_allMarkers.TryGetValue(marker.Id, out collection))
            {
                if (collection.InfoWindowAdapter != null)
                {
                    return collection.InfoWindowAdapter.GetInfoWindow(marker);
                }
            }

            return null;
        }

        public class Collection
        {
            private readonly MarkerManager _parent;
            private readonly IList<Marker> _markers = new List<Marker>();

            public Collection(MarkerManager parent)
            {
                _parent = parent;
            }

            public IEnumerable<Marker> Markers => _markers;
            public GoogleMap.IOnInfoWindowClickListener InfoWindowClickListener { get; set; }
            public GoogleMap.IOnMarkerClickListener MarkerClickListener { get; set; }
            public GoogleMap.IOnMarkerDragListener MarkerDragListener { get; set; }
            public GoogleMap.IInfoWindowAdapter InfoWindowAdapter { get; set; }

            public Marker AddMarker(MarkerOptions opts)
            {
                var marker = _parent._map.AddMarker(opts);
                _markers.Add(marker);
                _parent._allMarkers.Add(marker.Id, this);
                return marker;
            }

            public bool Remove(Marker marker)
            {
                if(_markers.Remove(marker))
                {
                    _parent._allMarkers.Remove(marker.Id);
                    marker.Remove();
                    return true;
                }

                return false;
            }

            public void Clear()
            {
                foreach(var marker in _markers)
                {
                    marker.Remove();
                    _parent._allMarkers.Remove(marker.Id);
                }

                _markers.Clear();
            }
        }
    }
}
