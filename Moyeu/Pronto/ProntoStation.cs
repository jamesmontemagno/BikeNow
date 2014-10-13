using System;
using Newtonsoft.Json;

namespace Moyeu.Pronto
{
	public class ProntoStation
	{
			[JsonProperty("id")]
			public int Id { get; set; }
			[JsonProperty("s")]
			public string Streets { get; set; }
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
			public int DocksAvailable { get; set; }
			public int dx { get; set; }
			[JsonProperty("ba")]
			public int BikesAvailable { get; set; }
			public int bx { get; set; }

		[JsonIgnore]
		public bool Planned 
		{
			get { return StationType == 2; }
		}
		[JsonIgnore]
		public bool OutOfService
		{
			get { return !Planned && (b || t || (DocksAvailable == 0 && BikesAvailable == 0)); }
		}
	}
}

