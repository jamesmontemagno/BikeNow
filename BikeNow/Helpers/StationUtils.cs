using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace BikeNow
{
	public static class StationUtils
	{
		public static string CutStationName (string rawStationName)
		{
			string dummy;
			return CutStationName (rawStationName, out dummy);
		}

		public static string CutStationName (string rawStationName, out string secondPart)
		{
			secondPart = string.Empty;
			var nameParts = rawStationName.Split (new string[] { "-", " at " , "/ ", " / " }, StringSplitOptions.RemoveEmptyEntries);
			if (nameParts.Length > 1)
				secondPart = string.Join (", ", nameParts.Skip (1)).Trim ();
			else
				secondPart = nameParts [0].Trim ();
			return nameParts [0].Trim ();
		}

		public static void DumpStations (IList<Station> stations, Stream stream)
		{
			using (var writer = new BinaryWriter (stream, System.Text.Encoding.UTF8, true)) {
				writer.Write (stations.Count);
				foreach (var s in stations) {
					writer.Write (s.Id);
					writer.Write (s.Street);
					writer.Write (s.Name);
					writer.Write (s.StationType);
					writer.Write (s.b);
					writer.Write (s.su);
					writer.Write (s.t);
					writer.Write (s.bk);
					writer.Write (s.bl);
					writer.Write (s.Latitude);
					writer.Write (s.Longitude);
					writer.Write (s.EmptySlotCount);
					writer.Write (s.dx);
					writer.Write (s.BikeCount);
					writer.Write (s.bx);
				}
			}
		}

		public static IList<Station> ParseStations (Stream stream)
		{
			using (var reader = new BinaryReader (stream, System.Text.Encoding.UTF8, true)) {
				var count = reader.ReadInt32 ();
				var stations = new Station[count];
				for (int i = 0; i < count; i++) {
					stations [i] = new Station {
						Id = reader.ReadInt32 (),
						Street = reader.ReadString (),
						Name = reader.ReadString(),
						StationType = reader.ReadInt32(),
						b = reader.ReadBoolean(),
						su = reader.ReadBoolean(),
						t = reader.ReadBoolean(),
						bk = reader.ReadBoolean(),
						bl = reader.ReadBoolean(),
						Latitude = reader.ReadDouble(),
						Longitude = reader.ReadDouble(),
						EmptySlotCount = reader.ReadInt32(),
						dx = reader.ReadInt32(),
						BikeCount = reader.ReadInt32(),
						bx = reader.ReadInt32()
					};
				}

				return stations;
			}
		}
	}
}

