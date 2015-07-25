

using Android.App;
using Android.OS;
using Android.Views;
using Android.Preferences;
using Android.Content.PM;
using Android.Support.V7.Widget;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace BikeNow
{
	[Activity (Label = "About", Icon = "@drawable/ic_launcher", Theme="@style/BikeNowTheme.Settings",
		ScreenOrientation = ScreenOrientation.Portrait,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]			
    public class SettingsActivity : PreferenceActivity, Android.Views.View.IOnClickListener
	{

		private Toolbar actionbar;
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			AddPreferencesFromResource(Resource.Xml.preferences_general);
			actionbar.Title = Title;
		}

		public override void SetContentView (int layoutResID)
		{
			var contentView = (ViewGroup) LayoutInflater.From(this).Inflate(
				Resource.Layout.Settings, new LinearLayout(this), false);

			actionbar = contentView.FindViewById<Toolbar>(Resource.Id.toolbar);
			actionbar.SetNavigationOnClickListener(this);

			ViewGroup contentWrapper = (ViewGroup) contentView.FindViewById(Resource.Id.content_wrapper);
			LayoutInflater.From(this).Inflate(layoutResID, contentWrapper, true);

			Window.SetContentView(contentView);
		}



		public void OnClick (View v)
		{
			Finish ();
		}
	
		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
			case Android.Resource.Id.Home:
				Finish();
				break;
			}
			return base.OnOptionsItemSelected(item);
		}

	}
}

