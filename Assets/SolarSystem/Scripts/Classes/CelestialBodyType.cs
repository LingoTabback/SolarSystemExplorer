namespace Ephemeris
{
	public enum CelestialBodyType : byte
	{
		Unknown = 0,
		Terrestrial,
		GasGiant,
		IceGiant,
		YellowDwarf
	}

	public static class CelestialBodyTypes
	{
		public static string ToStringGerman(CelestialBodyType type)
		{
			return type switch
			{
				CelestialBodyType.Terrestrial => "Terrestrisch",
				CelestialBodyType.GasGiant => "Gasriese",
				CelestialBodyType.IceGiant => "Eisriese",
				CelestialBodyType.YellowDwarf => "Gelber Zwerg",
				_ => "Unbekannt",
			};
		}
	}
}