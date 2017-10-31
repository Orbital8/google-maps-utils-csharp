namespace SampleMapsAppIOS
{
    internal class Samples
    {
        public static Demo[] LoadSamples()
        {
            var array = new Demo[] {new ClusteringDemo {Title = "Clustering", Description = "Marker Clustering"}};
            return array;
        }
    }
}