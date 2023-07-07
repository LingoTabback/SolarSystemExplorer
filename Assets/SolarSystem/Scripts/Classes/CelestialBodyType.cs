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
		public static string ToStringDE(CelestialBodyType type)
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

		public static string ToStringEN(CelestialBodyType type)
		{
			return type switch
			{
				CelestialBodyType.Terrestrial => "Terrestrial",
				CelestialBodyType.GasGiant => "Gas Giant",
				CelestialBodyType.IceGiant => "Ice Giant",
				CelestialBodyType.YellowDwarf => "Yellow Dwarf",
				_ => "Unknown",
			};
		}
	}
}