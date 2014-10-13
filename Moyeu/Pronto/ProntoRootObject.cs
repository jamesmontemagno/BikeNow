using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Moyeu.Pronto
{
	public class ProntoRootObject
	{
		[JsonProperty("timestamp")]
		public long Timestamp { get; set; }
		[JsonProperty("schemeSuspended")]
		public bool SchemeSuspended { get; set; }
		[JsonProperty("stations")]
		public List<ProntoStation> Stations { get; set; }
	}
}

