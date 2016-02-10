using Android.Gms.Maps.Model;

namespace app
{
    internal class AnnotationModel
    {
        public AnnotationModel(Marker marker, Spot spot)
        {
            Marker = marker;
            Spot = spot;
        }

        public Spot Spot { get; private set; }
        public Marker Marker { get; private set; }
    }
}