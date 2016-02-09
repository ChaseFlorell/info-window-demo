using Android.Gms.Maps.Model;

namespace app
{
    public class Spot
    {
        private readonly string _name;
        private readonly LatLng _position;

        public Spot(string name, LatLng position)
        {
            _name = name;
            _position = position;
        }

        public string Name
        {
            get { return _name; }
        }

        public LatLng Position
        {
            get { return _position; }
        }
    }
}