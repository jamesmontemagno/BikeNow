using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BikeNow
{
	public class ProntoRootObject
	{
		[JsonProperty("timestamp")]
		public long Timestamp { get; set; }
		[JsonProperty("schemeSuspended")]
		public bool SchemeSuspended { get; set; }
		[JsonProperty("stations")]
		public List<Station> Stations { get; set; }
	}
}

