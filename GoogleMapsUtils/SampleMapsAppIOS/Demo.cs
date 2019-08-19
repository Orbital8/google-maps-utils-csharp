using System;

namespace SampleMapsAppIOS
{
    internal abstract class Demo : IDisposable
    {
        public const double CameraLatitude = -33.8;
        public const double CameraLongitude = 151.2;
        private bool _disposed = false;

        ~Demo()
        {
            Dispose(false);
        }

        public string Description { get; set; }

        public string Title { get; set; }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        public abstract void SetUpDemo(DetailViewController detailViewController);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
        }
    }
}