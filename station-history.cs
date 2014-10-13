using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using Moyeu.Pronto;

class T
{
	const int NumDataPoint = 6;

	public static void Main (string[] args)
	{
		string basePath, baseOutPath;
		baseOutPath = Environment.GetEnvironmentVariable("WEBROOT_PATH");

		basePath = Path.Combine(baseOutPath, "app");
		var currentTime = DateTime.UtcNow;
		// Round off time if not on a 30 minutes base
		if ((currentTime.Minute % 30) != 0) {
			var lastMinute = currentTime.Minute % 10;
			currentTime -= TimeSpan.FromMinutes (lastMinute - (lastMinute > 30 ? 30 : 0));
		}

		var client = new System.Net.Http.HttpClient ();
		client.Timeout = new TimeSpan (0, 1, 0);
		var result = client.GetStringAsync ("https://secure.prontocycleshare.com/data2/stations.json");
		result.Wait ();

		var baseName = string.Format ("bikeStations_{0:yyyy-MM-dd_HH-mm-ss}", currentTime);
		File.WriteAllText (Path.Combine(basePath, baseName + ".json"), result.Result);
		// station ID -> amount of bikes at that time
		var dataPoints = new Dictionary<string, List<int>> ();
		var times = new List<DateTime> ();

		for (int i = 0; i < NumDataPoint; i++) {
			var time = currentTime - TimeSpan.FromMinutes (i * 30);
			var path = GetFilePathForDate (basePath, time);
			if (path == null)
				break;
			GatherDataPointsFromFile (path, dataPoints);
			times.Add (time);
		}

		var output = Path.Combine (baseOutPath, "history.json");
		var lines = new List<string> ();
		lines.Add (string.Join ("|", times.Select (t => t.ToBinary ().ToString ()).Reverse ()));
		lines.AddRange (dataPoints.Select (dp => dp.Key + "|" + string.Join ("|", dp.Value.Select (v => v.ToString ()).Reverse ())));
		File.WriteAllLines (output, lines);

		Console.WriteLine ("Success");
	}

	static string GetFilePathForDate (string basePath, DateTime date)
	{
		var baseName = string.Format ("bikeStations_{0:yyyy-MM-dd_HH-mm}-", date);
		return Enumerable.Range (0, 59)
			.Select (i => Path.Combine (basePath, baseName + i.ToString ("D2") + ".json"))
			.FirstOrDefault (p => File.Exists (p));
	}

	static void GatherDataPointsFromFile (string filepath, Dictionary<string, List<int>> dataPoints)
	{
		try{
			var info = File.ReadAllText (filepath);

			var stationsRoot = Newtonsoft.Json.JsonConvert.DeserializeObject<ProntoRootObject> (info);

			
				foreach (var station in stationsRoot.Stations) {
				var name = station.Id.ToString();
				List<int> list;
				if (!dataPoints.TryGetValue (name, out list))
					dataPoints[name] = list = new List<int> (NumDataPoint);
					list.Add (station.BikesAvailable);
			
				}
		}
		catch(Exception ex){
		}
	}
}