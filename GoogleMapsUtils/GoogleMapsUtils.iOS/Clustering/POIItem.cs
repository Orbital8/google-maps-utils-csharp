using CoreLocation;

namespace GoogleMapsUtils.iOS.Clustering
{
    public class POIItem : IGMUClusterItem
    {
        public POIItem(double latitude, double longitude, string name)
        {
            Position = new CLLocationCoordinate2D(latitude, longitude);
            Name = name;
        }

        public POIItem(CLLocationCoordinate2D position, string name)
        {
            Position = position;
            Name = name;
        }

        public string Name { get; private set; }
        public CLLocationCoordinate2D Position { get; private set; }
	}
}
