using Android.App;
using Android.OS;

namespace app
{
    [Activity(Label = "app", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity);

            if (bundle == null)
            {
                FragmentManager.BeginTransaction()
                    .Add(Resource.Id.container_map_fragment, new MyMapFragment())
                    .Commit();
            }
        }
    }
}