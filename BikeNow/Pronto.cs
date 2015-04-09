using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

namespace BikeNow
{
	public class Pronto : IObservable<Station[]>
	{
		const string ProntoApiEndpoint = "http://bikenowapp.azurewebsites.net/cron/latest_stations.json";

		public static readonly Func<Station, bool> AvailableBikeStationPredicate = s => s.BikeCount > 1 && s.EmptySlotCount > 1;

		HttpClient client = new HttpClient ();
		TimeSpan freshnessTimeout;
		string savedData;

		List<ProntoSubscriber> subscribers = new List<ProntoSubscriber> ();

		public static readonly Pronto Instance = new Pronto ();

		public Pronto () : this (TimeSpan.FromMinutes (5))
		{

		}

		public Pronto (TimeSpan freshnessTimeout)
		{
			this.freshnessTimeout = freshnessTimeout;
			client.Timeout = TimeSpan.FromSeconds (30);
		}

		public DateTime LastUpdateTime {
			get;
			private set;
		}

		public Station[] LastStations {
			get;
			private set;
		}

		public static Station[] GetStationsAround (Station[] stations, GeoPoint location, double minDistance = 100, int maxItems = 4)
		{
			var dic = new SortedDictionary<double, Station> ();
			foreach (var s in stations) {
				var d = GeoUtils.Distance (location, s.Location);
				if (d < minDistance)
					dic.Add (d, s);
			}
			return dic.Select (ds => ds.Value).Take (maxItems).ToArray ();
		}

		public Station[] GetClosestStationTo (Station[] stations, params GeoPoint[] locations)
		{
			return GetClosestStationTo (stations, null, locations);
		}

		public Station[] GetClosestStationTo (Station[] stations, Func<Station, bool> filter, params GeoPoint[] locations)
		{
			var distanceToGeoPoints = new SortedDictionary<double, Station>[locations.Length];
			var ss = filter == null ? (IEnumerable<Station>)stations : stations.Where (filter);
			foreach (var station in ss) {
				for (int i = 0; i < locations.Length; i++) {
					if (distanceToGeoPoints [i] == null)
						distanceToGeoPoints [i] = new SortedDictionary<double, Station> ();
					distanceToGeoPoints [i].Add (GeoUtils.Distance (locations[i], station.Location), station);
				}
			}

			return distanceToGeoPoints.Select (ds => ds.First ().Value).ToArray ();
		}

		public bool HasCachedData {
			get {
				return savedData != null && DateTime.Now < (LastUpdateTime + freshnessTimeout);
			}
		}
		int attempt = 0;
		public async Task<Station[]> GetStations (bool forceRefresh = false, Action<string> dataCacher = null)
		{
			string data = null;

			if (forceRefresh)
				attempt = 0;

			if (HasCachedData && !forceRefresh)
				data = savedData;
			else {
				attempt++;
				while (data == null) {
					try {
						data = await client.GetStringAsync (ProntoApiEndpoint).ConfigureAwait (false);
						attempt = 0;
					} catch (Exception e) {
						Android.Util.Log.Error ("ProntoDownloader", e.ToString ());
						Xamarin.Insights.Report (e);
						if (attempt >= 3) {
							attempt = 0;
							return new Station[]{ };
						}
					}
					if (data == null)
						await Task.Delay (500);
				}
			}

			if (dataCacher != null)
				dataCacher (data);

			var stationRoot = Newtonsoft.Json.JsonConvert.DeserializeObject<ProntoRootObject> (data);
				
			var stations = stationRoot.Stations.ToArray ();

			LastUpdateTime = FromUnixTime (stationRoot.Timestamp);
			LastStations = stations;

			if (subscribers.Any ())
				foreach (var sub in subscribers)
					sub.Observer.OnNext (stations);
			return stations;
		}

		DateTime FromUnixTime (long secs)
		{
			return (new DateTime (1970, 1, 1, 0, 0, 1, DateTimeKind.Utc) + TimeSpan.FromSeconds (secs / 1000.0)).ToLocalTime ();
		}

		public IDisposable Subscribe (IObserver<Station[]> observer)
		{
			var sub = new ProntoSubscriber (subscribers.Remove, observer);
			subscribers.Add (sub);
			return sub;
		}

		class ProntoSubscriber : IDisposable
		{
			Func<ProntoSubscriber, bool> unsubscribe;

			public ProntoSubscriber (Func<ProntoSubscriber, bool> unsubscribe, IObserver<Station[]> observer)
			{
				Observer = observer;
				this.unsubscribe = unsubscribe;
			}

			public IObserver<Station[]> Observer {
				get;
				set;
			}

			public void Dispose ()
			{
				unsubscribe (this);
			}
		}
	}
}

