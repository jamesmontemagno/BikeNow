

using Android.App;
using Android.OS;
using Android.Views;
using Android.Preferences;
using Android.Content.PM;

namespace Moyeu
{
	[Activity (Label = "About", Icon = "@drawable/ic_launcher", Theme="@style/Theme.Bikenow",
		ScreenOrientation = ScreenOrientation.Portrait,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]			
	public class SettingsActivity : PreferenceActivity
	{

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			AddPreferencesFromResource(Resource.Xml.preferences_general);

			ActionBar.SetDisplayHomeAsUpEnabled(true);
			ActionBar.SetDisplayShowHomeEnabled(true);
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

