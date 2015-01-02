using System;

namespace BikeNow
{
	public interface IProntoSection
	{
		string Name { get; }
		string Title { get; }
		void RefreshData ();
	}
}

