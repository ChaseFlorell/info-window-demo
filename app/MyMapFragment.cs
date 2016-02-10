using System.Collections.Generic;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Beans;
using Java.Lang;

namespace app
{
    public class MyMapFragment : MapFragment,
        GoogleMap.IOnMapClickListener,
        GoogleMap.IOnMarkerClickListener,
        View.IOnClickListener
    {
        // Update interval position of the pop-up window.
        // Necessary for the smooth 60 fps, ie 1000 ms / 60 = 16 ms between updates .
        // Animation duration displacement map
        private const int AnimationDuration = 500;
        //Handler, runs the update window at a specified interval
        private static Handler _handler;
        // Container popup
        private static View _infoWindowContainer;
        public static int MarkerHeight;
        public static AbsoluteLayout.LayoutParams OverlayLayoutParams;
        // Offset the popup window , allowing
        // Adjust its position relative to the marker
        private static int _popupXOffset;
        private static int _popupYOffset;
        // Point on the map , respectively, which moved the movement popup
        private static LatLng _trackedPosition;
        private static MyMapFragment _this;
        public static int PopupPositionRefreshInterval = 10;

        private static readonly Spot[] SpotsArray =
        {
            new Spot("Киев", new LatLng(50.4546600, 30.5238000)),
            new Spot("Одесса", new LatLng(46.4774700, 30.7326200)),
            new Spot("Харьков", new LatLng(50.0000000, 36.2500000)),
            new Spot("Львов", new LatLng(49.8382600, 24.0232400)),
            new Spot("Донецк", new LatLng(48.0000000, 37.8000000))
        };

        private TextView _button;
        // Listener who will update the displacement
        private ViewTreeObserver.IOnGlobalLayoutListener _infoWindowLayoutListener;
        // Runnable, which updates the position of the window
        private IRunnable _positionUpdaterRunnable;
        private Dictionary<string, AnnotationModel> _spots;
        private TextView textView;


        public MyMapFragment()
        {
            _this = this;
        }

        private static GoogleMap NativeMap
        {
            get { return _this.Map; }
        }

        public void OnClick(View v)
        {
            var name = v.Tag;
            StartActivity(new Intent(Intent.ActionView).SetData(Uri.Parse("http://www.google.com/search?q=" + name)));
        }

        public void OnMapClick(LatLng point)
        {
            _infoWindowContainer.Visibility = ViewStates.Invisible;
        }

        public bool OnMarkerClick(Marker marker)
        {
            var map = Map;
            var projection = map.Projection;
            _trackedPosition = marker.Position;
            var trackedPoint = projection.ToScreenLocation(_trackedPosition);
            trackedPoint.Y -= _popupYOffset/2;
            var newCameraLocation = projection.FromScreenLocation(trackedPoint);
            map.AnimateCamera(CameraUpdateFactory.NewLatLng(newCameraLocation), AnimationDuration, null);

            var spot = _spots[marker.Id].Spot;
            textView.Text = spot.Name;
            _button.Tag = spot.Name;

            _infoWindowContainer.Visibility = ViewStates.Visible;

            return true;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _spots = new Dictionary<string, AnnotationModel>();
            MarkerHeight = Resources.GetDrawable(Resource.Drawable.pin).IntrinsicHeight;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var rootView = inflater.Inflate(Resource.Layout.fragment, null);

            var containerMap = (FrameLayout) rootView.FindViewById(Resource.Id.container_map);
            var mapView = base.OnCreateView(inflater, container, savedInstanceState);
            containerMap.AddView(mapView,
                new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));

            var map = Map;
            map.MoveCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(48.35, 31.16), 5.5f));
            map.UiSettings.RotateGesturesEnabled = false;
            map.SetOnMapClickListener(this);
            map.SetOnMarkerClickListener(this);

            map.Clear();
            _spots.Clear();
            var icon = BitmapDescriptorFactory.FromResource(Resource.Drawable.pin);
            foreach (var spot in SpotsArray)
            {
                var mo = new MarkerOptions();
                mo.SetPosition(spot.Position);
                mo.SetIcon(icon);
                mo.SetTitle(spot.Name);
                mo.SetSnippet("foo");
                var marker = map.AddMarker(mo);

                _spots.Add(marker.Id, new AnnotationModel(marker, spot));
            }

            _infoWindowContainer = rootView.FindViewById(Resource.Id.container_popup);

            // Subscribe to resize pop-up window
            _infoWindowLayoutListener = new InfoWindowLayoutListener();
            _infoWindowContainer.ViewTreeObserver.AddOnGlobalLayoutListener(_infoWindowLayoutListener);
            OverlayLayoutParams = (AbsoluteLayout.LayoutParams) _infoWindowContainer.LayoutParameters;

            textView = (TextView) _infoWindowContainer.FindViewById(Resource.Id.textview_title);
            _button = (Button) _infoWindowContainer.FindViewById(Resource.Id.foo);
            _button.SetOnClickListener(this);

            return rootView;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            // cleaning
            _handler = new Handler(Looper.MainLooper);
            _positionUpdaterRunnable = new PositionUpdaterRunnable();
            _handler.Post(_positionUpdaterRunnable);
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            _infoWindowContainer.ViewTreeObserver.RemoveGlobalOnLayoutListener(_infoWindowLayoutListener);
            _handler.RemoveCallbacks(_positionUpdaterRunnable);
            _handler = null;
        }

        private class InfoWindowLayoutListener : Object, ViewTreeObserver.IOnGlobalLayoutListener
        {
            public void OnGlobalLayout()
            {
                // Window size is changed , the offset update
                _popupXOffset = _infoWindowContainer.Width/2;
                _popupYOffset = _infoWindowContainer.Height;
            }
        }

        private class PositionUpdaterRunnable : Object, IRunnable
        {
            private int _lastXPosition = Integer.MinValue;
            private int _lastYPosition = Integer.MinValue;

            public void Run()
            {
                // Put in place the next cycle of updates
                _handler.PostDelayed(this, PopupPositionRefreshInterval);

                // If the pop-up window is hidden , do nothing
                if (_trackedPosition == null || _infoWindowContainer.Visibility != ViewStates.Visible)
                {
                    return;
                }


                var targetPosition = NativeMap.Projection.ToScreenLocation(_trackedPosition);

                // If the window position has not changed , do nothing
                if (_lastXPosition == targetPosition.X && _lastYPosition == targetPosition.Y)
                {
                    return;
                }

                // Update position
                //_infoWindowContainer.TranslationX
                OverlayLayoutParams.X = targetPosition.X - _popupXOffset;
                OverlayLayoutParams.Y = targetPosition.Y - _popupYOffset - MarkerHeight - 30;
                _infoWindowContainer.LayoutParameters = OverlayLayoutParams;

                // Store the current coordinates
                _lastXPosition = targetPosition.X;
                _lastYPosition = targetPosition.Y;
            }
        }
    }
}