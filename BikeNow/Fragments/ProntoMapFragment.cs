using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Android.Locations;
using Android.Animation;

using Android.Gms.MapsSdk;
using Android.Gms.MapsSdk.Model;

using XamSvg;
using Android.Support.V4.View;

namespace BikeNow
{
	public class ProntoMapFragment: Android.Support.V4.App.Fragment, ViewTreeObserver.IOnGlobalLayoutListener, IProntoSection, IOnMapReadyCallback, IOnStreetViewPanoramaReadyCallback
	{
		Dictionary<int, Marker> existingMarkers = new Dictionary<int, Marker> ();
		Marker locationPin;
		MapView mapFragment;
		GoogleMap map;
		StreetViewPanoramaView streetViewFragment;
		StreetViewPanorama streetPanorama;

		Pronto pronto = Pronto.Instance;
		ProntoHistory prontoHistory = new ProntoHistory ();

		bool loading;
		bool showedStale;
		FlashBarController flashBar;
		IMenuItem searchItem;
		FavoriteManager favManager;
		TextView lastUpdateText;
		PinFactory pinFactory;
		InfoPane pane;
		PictureBitmapDrawable starOnDrawable;
		PictureBitmapDrawable starOffDrawable;

		int currentShownID = -1;
		Marker currentShownMarker;
		CameraPosition oldPosition;

		public ProntoMapFragment (Context context)
		{
			HasOptionsMenu = true;
		}

		public string Name {
			get {
				return "MapFragment";
			}
		}

		public string Title {
			get {
				return "Map";
			}
		}

		internal int CurrentShownId {
			get {
				return currentShownID;
			}
		}

		public void RefreshData ()
		{
			FillUpMap (forceRefresh: false);
		}

		public override void OnActivityCreated (Bundle savedInstanceState)
		{
			base.OnActivityCreated (savedInstanceState);
			var context = Activity;
			this.pinFactory = new PinFactory (context);
			this.favManager = FavoriteManager.Obtain (context);
		}

		public override void OnStart ()
		{
			base.OnStart ();
			RefreshData ();
		}

		public void OnGlobalLayout ()
		{
			Activity.RunOnUiThread (() => pane.SetState (InfoPane.State.Closed, animated: false));
			View.ViewTreeObserver.RemoveGlobalOnLayoutListener (this);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate (Resource.Layout.MapLayout, container, false);
			mapFragment = view.FindViewById<MapView> (Resource.Id.map);
			mapFragment.OnCreate (savedInstanceState);
			lastUpdateText = view.FindViewById<TextView> (Resource.Id.UpdateTimeText);
			SetupInfoPane (view);
			flashBar = new FlashBarController (view);
			streetViewFragment = pane.FindViewById<StreetViewPanoramaView> (Resource.Id.streetViewPanorama);
			streetViewFragment.OnCreate (savedInstanceState);

			return view;
		}
			

		void SetupInfoPane (View view)
		{
			pane = view.FindViewById<InfoPane> (Resource.Id.infoPane);
			pane.StateChanged += HandlePaneStateChanged;
			view.ViewTreeObserver.AddOnGlobalLayoutListener (this);
		}

		public override void OnViewCreated (View view, Bundle savedInstanceState)
		{
			base.OnViewCreated (view, savedInstanceState);

			view.SetBackgroundDrawable (AndroidExtensions.DefaultBackground);

			mapFragment.GetMapAsync (this);



		
			// Setup info pane
			SetSvgImage (pane, Resource.Id.bikeImageView, Resource.Raw.bike);
			SetSvgImage (pane, Resource.Id.lockImageView, Resource.Raw.ic_lock);
			SetSvgImage (pane, Resource.Id.stationLock, Resource.Raw.station_lock);
			SetSvgImage (pane, Resource.Id.bikeNumberImg, Resource.Raw.bike_number);
			SetSvgImage (pane, Resource.Id.clockImg, Resource.Raw.clock);
			SetSvgImage (pane, Resource.Id.stationNotInstalled, Resource.Raw.not_installed);
			starOnDrawable = SvgFactory.GetDrawable (Resources, Resource.Raw.star_on);
			starOffDrawable = SvgFactory.GetDrawable (Resources, Resource.Raw.star_off);
			var starBtn = pane.FindViewById<ImageButton> (Resource.Id.StarButton);
			starBtn.Click += HandleStarButtonChecked;
			streetViewFragment.GetStreetViewPanoramaAsync (this);
		}

		void SetSvgImage (View baseView, int viewId, int resId)
		{
			var view = baseView.FindViewById<ImageView> (viewId);
			if (view == null)
				return;
			var img = SvgFactory.GetDrawable (Resources, resId);
			view.SetImageDrawable (img);
		}

		public void OnMapReady (GoogleMap googleMap)
		{
			this.map = googleMap;
			MapsInitializer.Initialize (Activity.ApplicationContext);

			googleMap.MyLocationEnabled = true;
			googleMap.UiSettings.MyLocationButtonEnabled = false;
			googleMap.MapClick += HandleMapClick;
			googleMap.MarkerClick += HandleMarkerClick;

			var position = PreviousCameraPosition;
			if(position != null)
			{
				//default map initialization;
				googleMap.MoveCamera (CameraUpdateFactory.NewCameraPosition(position));
			}
		}

		public void OnStreetViewPanoramaReady (StreetViewPanorama panorama)
		{
			this.streetPanorama = panorama;
			panorama.UserNavigationEnabled = false;
			panorama.StreetNamesEnabled = false;
			panorama.StreetViewPanoramaClick += HandleMapButtonClick;
		}

		void HandlePaneStateChanged (InfoPane.State state)
		{
			var time = Resources.GetInteger (Android.Resource.Integer.ConfigShortAnimTime);
			var enabled = state != InfoPane.State.FullyOpened;
			map.UiSettings.ScrollGesturesEnabled = enabled;
			map.UiSettings.ZoomGesturesEnabled = enabled;
			if (state == InfoPane.State.FullyOpened && currentShownMarker != null) {
				oldPosition = map.CameraPosition;
				var destX = mapFragment.Width / 2;
				var destY = (mapFragment.Height - pane.Height) / 2;
				var currentPoint = map.Projection.ToScreenLocation (currentShownMarker.Position);
				var scroll = CameraUpdateFactory.ScrollBy (- destX + currentPoint.X, - destY + currentPoint.Y);
				map.AnimateCamera (scroll, time, null);
			} else if (oldPosition != null) {
				map.AnimateCamera (CameraUpdateFactory.NewCameraPosition (oldPosition), time, null);
				oldPosition = null;
			}
		}

		void HandleMapButtonClick (object sender, StreetViewPanorama.StreetViewPanoramaClickEventArgs e)
		{
			var stations = pronto.LastStations;
			if (stations == null || currentShownID == -1)
				return;

			var stationIndex = Array.FindIndex (stations, s => s.Id == currentShownID);
			if (stationIndex == -1)
				return;
			var station = stations [stationIndex];

			var data = new Dictionary<string, string> ();
			data.Add ("Station", station.Name);
			Xamarin.Insights.Track ("Navigate to Station", data);

			var location = station.GeoUrl;
			var uri = Android.Net.Uri.Parse (location);
			var intent = new Intent (Intent.ActionView, uri);
			StartActivity (intent);
		}

		public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
		{
			inflater.Inflate (Resource.Menu.map_menu, menu);
			searchItem = menu.FindItem (Resource.Id.menu_search);
			var test = MenuItemCompat.GetActionView(searchItem);
			var searchView = test.JavaCast<Android.Support.V7.Widget.SearchView>();

			SetupSearchInput (searchView);
		}

		void SetupSearchInput (Android.Support.V7.Widget.SearchView searchView)
		{
			var searchManager = Activity.GetSystemService (Context.SearchService).JavaCast<SearchManager> ();
			searchView.SetIconifiedByDefault (false);
			var searchInfo = searchManager.GetSearchableInfo (Activity.ComponentName);
			searchView.SetSearchableInfo (searchInfo);
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			if (item.ItemId == Resource.Id.menu_refresh) {
				FillUpMap (forceRefresh: true);
				return true;
			} else if (item.ItemId == Resource.Id.menu_mylocation) {
				CenterMapOnUser ();
				return true;
			}
			return base.OnOptionsItemSelected (item);
		}

		public override void OnViewStateRestored (Bundle savedInstanceState)
		{
			base.OnViewStateRestored (savedInstanceState);
			if (savedInstanceState != null && savedInstanceState.ContainsKey ("previousPosition")) {
				var pos = savedInstanceState.GetParcelable ("previousPosition") as CameraPosition;
				if (pos != null) {
					var update = CameraUpdateFactory.NewCameraPosition (pos);
					map.MoveCamera (update);
				}
			}
		}

		public override void OnResume ()
		{
			base.OnResume ();
			mapFragment.OnResume ();
			streetViewFragment.OnResume ();
		}

		public override void OnLowMemory ()
		{
			base.OnLowMemory ();
			mapFragment.OnLowMemory ();
			streetViewFragment.OnLowMemory ();
		}

		public override void OnPause ()
		{
			base.OnPause ();
			mapFragment.OnPause ();
			PreviousCameraPosition = map.CameraPosition;
			streetViewFragment.OnPause ();
		}

		public override void OnDestroy ()
		{
			base.OnDestroy ();
			mapFragment.OnDestroy ();
			streetViewFragment.OnDestroy ();
		}

		public override void OnSaveInstanceState (Bundle outState)
		{
			base.OnSaveInstanceState (outState);
			mapFragment.OnSaveInstanceState (outState);
			streetViewFragment.OnSaveInstanceState (outState);
		}

		void HandleMapClick (object sender, GoogleMap.MapClickEventArgs e)
		{
			currentShownID = -1;
			currentShownMarker = null;
			pane.SetState (InfoPane.State.Closed);
		}
		
		void HandleMarkerClick (object sender, GoogleMap.MarkerClickEventArgs e)
		{
			e.Handled = true;
      if (e.P0.Title == null)
				return;

      OpenStationWithMarker (e.P0);
		}

		void HandleStarButtonChecked (object sender, EventArgs e)
		{
			if (currentShownID == -1)
				return;
			var starButton = (ImageButton)sender;
			var favorites = favManager.LastFavorites ?? favManager.GetFavoriteStationIds ();
			bool contained = favorites.Contains (currentShownID);
			if (contained) {
				starButton.SetImageDrawable (starOffDrawable);
				favManager.RemoveFromFavorite (currentShownID);

				var data = new Dictionary<string, string> ();
				data.Add ("Station", currentShownID.ToString());
				Xamarin.Insights.Track ("Removed Favorited", data);
			} else {
				starButton.SetImageDrawable (starOnDrawable);
				favManager.AddToFavorite (currentShownID);
				var data = new Dictionary<string, string> ();
				data.Add ("Station", currentShownID.ToString());
				Xamarin.Insights.Track ("Added Favorited", data);
			}
		}

		public async void FillUpMap (bool forceRefresh)
		{
			if (loading)
				return;
			loading = true;
			if (pane != null && pane.Opened)
				pane.SetState (InfoPane.State.Closed, animated: false);
			flashBar.ShowLoading ();

			try {
				var stations = await pronto.GetStations (forceRefresh);
				if(stations.Length == 0){
					Toast.MakeText(Activity, Resource.String.load_error, ToastLength.Long).Show();
				}
				else{
					await SetMapStationPins (stations);
				}
				lastUpdateText.Text = "Last refreshed: " + DateTime.Now.ToShortTimeString ();
			} catch (Exception e) {
				e.Data ["method"] = "FillUpMaps";
				Xamarin.Insights.Report (e);
				Android.Util.Log.Debug ("DataFetcher", e.ToString ());
			}

			flashBar.ShowLoaded ();
			showedStale = false;
			loading = false;
		}

		async Task SetMapStationPins (Station[] stations, float alpha = 1)
		{
			var stationsToUpdate = stations.Where (station => {
				Marker marker;
				var stats = station.BikeCount + "|" + station.EmptySlotCount;
				if (existingMarkers.TryGetValue (station.Id, out marker)) {
					if (marker.Snippet == stats && !showedStale)
						return false;
					marker.Remove ();
				}
				return true;
			}).ToArray ();

			var pins = await Task.Run (() => stationsToUpdate.ToDictionary (station => station.Id, station => {
				var w = 24.ToPixels ();
				var h = 40.ToPixels ();
				if (station.Locked)
					return pinFactory.GetClosedPin (w, h);
				else if(!station.Installed)
					return pinFactory.GetNonInstalledPin(w,h);
				var ratio = (float)TruncateDigit (station.BikeCount / ((float)station.Capacity), 2);
				return pinFactory.GetPin (ratio,
				                          station.BikeCount,
				                          w, h,
				                          alpha: alpha);
			}));

			foreach (var station in stationsToUpdate) {
				var pin = pins [station.Id];

				var snippet = station.BikeCount + "|" + station.EmptySlotCount;
				if (station.Locked)
					snippet = string.Empty;
				else if (!station.Installed)
					snippet = "not_installed";

				var markerOptions = new MarkerOptions ()
          .InvokeTitle (station.Id + "|" + station.Street + "|" + station.Name)
          .InvokeSnippet (snippet)
          .InvokePosition (new Android.Gms.MapsSdk.Model.LatLng (station.Location.Lat, station.Location.Lon))
          .InvokeIcon (BitmapDescriptorFactory.FromBitmap (pin));
				existingMarkers [station.Id] = map.AddMarker (markerOptions);
			}
		}

		public void CenterAndOpenStationOnMap (long id,
		                                       float zoom = 13,
		                                       int animDurationID = Android.Resource.Integer.ConfigShortAnimTime)
		{
			Marker marker;
			if (!existingMarkers.TryGetValue ((int)id, out marker))
				return;
			CenterAndOpenStationOnMap (marker, zoom, animDurationID);
		}

		public void CenterAndOpenStationOnMap (Marker marker,
		                                       float zoom = 13,
		                                       int animDurationID = Android.Resource.Integer.ConfigShortAnimTime)
		{
			var latLng = marker.Position;
			var camera = CameraUpdateFactory.NewLatLngZoom (latLng, zoom);
			var time = Resources.GetInteger (animDurationID);
			map.AnimateCamera (camera, time, new MapAnimCallback (() => OpenStationWithMarker (marker)));
		}

		public void OpenStationWithMarker (Marker marker)
		{
			//if (string.IsNullOrEmpty (marker.Title) || string.IsNullOrEmpty (marker.Snippet))
			//		return;

			var name = pane.FindViewById<TextView> (Resource.Id.InfoViewName);
			var name2 = pane.FindViewById<TextView> (Resource.Id.InfoViewSecondName);
			var bikes = pane.FindViewById<TextView> (Resource.Id.InfoViewBikeNumber);
			var slots = pane.FindViewById<TextView> (Resource.Id.InfoViewSlotNumber);
			var starButton = pane.FindViewById<ImageButton> (Resource.Id.StarButton);

			var splitTitle = marker.Title.Split ('|');
			//var displayName = StationUtils.CutStationName (splitTitle [1], out displayNameSecond);
			name.Text = splitTitle[1];
			name2.Text = splitTitle[2];

			currentShownID = int.Parse (splitTitle [0]);
			currentShownMarker = marker;

			var isLocked = string.IsNullOrEmpty (marker.Snippet);
			var isNotInstalled = marker.Snippet == "not_installed";
			pane.FindViewById (Resource.Id.stationStats).Visibility = (isNotInstalled || isLocked) ? ViewStates.Gone : ViewStates.Visible;
			pane.FindViewById (Resource.Id.stationLock).Visibility = isLocked ? ViewStates.Visible : ViewStates.Gone;
			pane.FindViewById (Resource.Id.stationNotInstalled).Visibility = isNotInstalled ? ViewStates.Visible : ViewStates.Gone;
			if (!isLocked && !isNotInstalled) {
				var splitNumbers = marker.Snippet.Split ('|');
				bikes.Text = splitNumbers [0];
				slots.Text = splitNumbers [1];
			}

			var favs = favManager.LastFavorites ?? favManager.GetFavoriteStationIds ();
			bool activated = favs.Contains (currentShownID);
			starButton.SetImageDrawable (activated ? starOnDrawable : starOffDrawable);

			var streetView = streetPanorama;
			streetView.SetPosition (marker.Position);

			LoadStationHistory (currentShownID);

			pane.SetState (InfoPane.State.Opened);
		}

		async void LoadStationHistory (int stationID)
		{
			const char DownArrow = '↘';
			const char UpArrow = '↗';

			var historyTimes = new int[] {
				Resource.Id.historyTime1,
				Resource.Id.historyTime2,
				Resource.Id.historyTime3,
				Resource.Id.historyTime4,
				Resource.Id.historyTime5
			};
			var historyValues = new int[] {
				Resource.Id.historyValue1,
				Resource.Id.historyValue2,
				Resource.Id.historyValue3,
				Resource.Id.historyValue4,
				Resource.Id.historyValue5
			};

			foreach (var ht in historyTimes)
				pane.FindViewById<TextView> (ht).Text = "-:-";
			foreach (var hv in historyValues) {
				var v = pane.FindViewById<TextView> (hv);
				v.Text = "-";
				v.SetTextColor (Color.Rgb (0x90, 0x90, 0x90));
			}
			var history = (await prontoHistory.GetStationHistory (stationID)).ToArray ();
			if (stationID != currentShownID || history.Length == 0)
				return;

			var previousValue = history [0].Value;
			for (int i = 0; i < Math.Min (historyTimes.Length, history.Length - 1); i++) {
				var h = history [i + 1];

				var timeText = pane.FindViewById<TextView> (historyTimes [i]);
				var is24 = Android.Text.Format.DateFormat.Is24HourFormat (Activity);
				timeText.Text = h.Key.ToLocalTime ().ToString ((is24 ? "HH" : "hh") + ":mm");

				var valueText = pane.FindViewById<TextView> (historyValues [i]);
				var comparison = h.Value.CompareTo (previousValue);
				if (comparison == 0) {
					valueText.Text = "=";
				} else if (comparison > 0) {
					valueText.Text = (h.Value - previousValue).ToString () + UpArrow;
					valueText.SetTextColor (Color.Rgb (0x66, 0x99, 0x00));
				} else {
					valueText.Text = (previousValue - h.Value).ToString () + DownArrow;
					valueText.SetTextColor (Color.Rgb (0xcc, 00, 00));
				}
				previousValue = h.Value;
			}
		}

		public void CenterMapOnLocation (LatLng latLng)
		{
			var camera = CameraUpdateFactory.NewLatLngZoom (latLng, 16);
			map.AnimateCamera (camera,
			                               new MapAnimCallback (() => SetLocationPin (latLng)));
		}

		public void OnSearchIntent (Intent intent)
		{
			searchItem.CollapseActionView ();
			if (intent.Action != Intent.ActionSearch)
				return;
			var serial = (string)intent.Extras.Get (SearchManager.ExtraDataKey);
			if (serial == null)
				return;
			var latlng = serial.Split ('|');
			var finalLatLng = new LatLng (double.Parse (latlng[0]),
			                              double.Parse (latlng[1]));
			CenterMapOnLocation (finalLatLng);
		}

		CameraPosition PreviousCameraPosition {
			get {
				var prefs = Activity.GetPreferences (FileCreationMode.Private);
				if (!prefs.Contains ("lastPosition-bearing")
				    || !prefs.Contains ("lastPosition-tilt")
				    || !prefs.Contains ("lastPosition-zoom")
				    || !prefs.Contains ("lastPosition-lat")
				    || !prefs.Contains ("lastPosition-lon")) {
				
					return new CameraPosition.Builder ()
						.Zoom (13)
						.Target (new LatLng (47.60621, -122.332071))
						.Build ();
				}

				var bearing = prefs.GetFloat ("lastPosition-bearing", 0);
				var tilt = prefs.GetFloat ("lastPosition-tilt", 0);
				var zoom = prefs.GetFloat ("lastPosition-zoom", 0);
				var latitude = prefs.GetFloat ("lastPosition-lat", 0);
				var longitude = prefs.GetFloat ("lastPosition-lon", 0);

				return new CameraPosition.Builder ()
					.Bearing (bearing)
					.Tilt (tilt)
					.Zoom (zoom)
					.Target (new LatLng (latitude, longitude))
					.Build ();
			}
			set {
				var position = map.CameraPosition;
				var prefs = Activity.GetPreferences (FileCreationMode.Private);
				using (var editor = prefs.Edit ()) {
					editor.PutFloat ("lastPosition-bearing", position.Bearing);
					editor.PutFloat ("lastPosition-tilt", position.Tilt);
					editor.PutFloat ("lastPosition-zoom", position.Zoom);
					editor.PutFloat ("lastPosition-lat", (float)position.Target.Latitude);
					editor.PutFloat ("lastPosition-lon", (float)position.Target.Longitude);
					editor.Commit ();
				}
			}
		}

		bool CenterMapOnUser ()
		{
			var location = map.MyLocation;
			if (location == null)
				return false;
			var userPos = new LatLng (location.Latitude, location.Longitude);
			var camPos = map.CameraPosition.Target;
			var needZoom = TruncateDigit (camPos.Latitude, 4) == TruncateDigit (userPos.Latitude, 4)
				&& TruncateDigit (camPos.Longitude, 4) == TruncateDigit (userPos.Longitude, 4);
			var cameraUpdate = needZoom ?
				CameraUpdateFactory.NewLatLngZoom (userPos, map.CameraPosition.Zoom + 2) :
					CameraUpdateFactory.NewLatLng (userPos);
			map.AnimateCamera (cameraUpdate);
			return true;
		}

		void SetLocationPin (LatLng finalLatLng)
		{
			if (locationPin != null) {
				locationPin.Remove ();
				locationPin = null;
			}
			var proj = map.Projection;
			var location = proj.ToScreenLocation (finalLatLng);
			location.Offset (0, -(35.ToPixels ()));
			var startLatLng = proj.FromScreenLocation (location);

			new Handler (Activity.MainLooper).PostDelayed (() => {
				var opts = new MarkerOptions ()
          .InvokePosition (startLatLng)
					.InvokeIcon (BitmapDescriptorFactory.DefaultMarker (BitmapDescriptorFactory.HueViolet));
				var marker = map.AddMarker (opts);
				var animator = ObjectAnimator.OfObject (marker, "position", new LatLngEvaluator (), startLatLng, finalLatLng);
				animator.SetDuration (1000);
				animator.SetInterpolator (new Android.Views.Animations.BounceInterpolator ());
				animator.Start ();
				locationPin = marker;
			}, 800);
		}

		class LatLngEvaluator : Java.Lang.Object, ITypeEvaluator
		{
			public Java.Lang.Object Evaluate (float fraction, Java.Lang.Object startValue, Java.Lang.Object endValue)
			{
				var start = (LatLng)startValue;
				var end = (LatLng)endValue;

				return new LatLng (start.Latitude + fraction * (end.Latitude - start.Latitude),
				                   start.Longitude + fraction * (end.Longitude - start.Longitude));
			}
		}

		class MapAnimCallback : Java.Lang.Object, GoogleMap.ICancelableCallback
		{
			Action callback;

			public MapAnimCallback (Action callback)
			{
				this.callback = callback;
			}

			public void OnCancel ()
			{
			}

			public void OnFinish ()
			{
				if (callback != null)
					callback ();
			}
		}

		double TruncateDigit (double d, int digitNumber)
		{
			var power = Math.Pow (10, digitNumber);
			return Math.Truncate (d * power) / power;
		}
	}
}

