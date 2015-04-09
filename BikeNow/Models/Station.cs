using System;
using Newtonsoft.Json;

namespace BikeNow
{
	public class Station
	{
			[JsonProperty("id")]
			public int Id { get; set; }
			[JsonProperty("s")]
			public string Street { get; set; }
			[JsonProperty("n")]
			public string Name { get; set; }
			[JsonProperty("st")]
			public int StationType { get; set; }
			[JsonProperty("b")]
			public bool b { get; set; }
			[JsonProperty("su")]
			public bool su { get; set; }
			[JsonProperty("t")]
			public bool t { get; set; }
			[JsonProperty("bk")]
			public bool bk { get; set; }
			[JsonProperty("bl")]
			public bool bl { get; set; }
			[JsonProperty("la")]
			public double Latitude { get; set; }
			[JsonProperty("lo")]
			public double Longitude { get; set; }

			[JsonProperty("da")]
			public int EmptySlotCount { get; set; }
			[JsonProperty("dx")]	
			public int dx { get; set; }
			[JsonProperty("ba")]
			public int BikeCount { get; set; }
			[JsonProperty("bx")]
			public int bx { get; set; }



		private GeoPoint geoPoint;
		[JsonIgnore]
		public GeoPoint Location
		{
			get {
				return geoPoint.Init ? geoPoint : 
					geoPoint = new GeoPoint{Lat = Latitude, Lon = Longitude, Init = true};
			}
		}

		[JsonIgnore]
		public int Capacity { get { return BikeCount + EmptySlotCount; } }




		[JsonIgnore]
		public bool Installed
		{
			get { return !Temporary; }
		}
		[JsonIgnore]
		public bool Temporary 
		{
			get { return StationType == 2; }
		}
		[JsonIgnore]
		public bool Locked
		{
			get { return !Temporary && (b || (EmptySlotCount == 0 && BikeCount == 0)); }
		}

		public override bool Equals (object obj)
		{
			return obj is Station && Equals ((Station)obj);
		}

		public bool Equals (Station other)
		{
			return other.Id == Id;
		}

		public override int GetHashCode ()
		{
			return Id;
		}

		[JsonIgnore]
		public string GeoUrl {
			get {
				var pos = Location.Lat + "," + Location.Lon;
				var location = "geo:" + pos + "?q=" + pos + "(" + Name.Replace (' ', '+') + ")";
				return location;
			}
		}
	}
}

