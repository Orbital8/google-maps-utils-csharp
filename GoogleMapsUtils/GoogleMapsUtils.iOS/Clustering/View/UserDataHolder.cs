using System;
using Foundation;

namespace GoogleMapsUtils.iOS.Clustering.View
{
    public class UserDataHolder : NSObject
    {
        private object _object;

        public UserDataHolder(IGMUCluster cluster)
        {
            _object = cluster;
        }

        public UserDataHolder(IGMUClusterItem clusterItem)
        {
            _object = clusterItem;
        }

        public UserDataHolder(NSObjectFlag t) : base(t)
        {
        }

        public UserDataHolder(IntPtr handle) : base(handle)
        {
        }

        public UserDataHolder(IntPtr handle, bool allocated) : base(handle, allocated)
        {
        }

        public object Object => _object;
    }
}
