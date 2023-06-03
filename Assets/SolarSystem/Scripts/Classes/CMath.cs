using Unity.Mathematics;

namespace CustomMath
{
	public static class CMath
	{
		public static double Square(double x) => x * x;

		// This function is like fmod except that it always returns
		// a positive value in the range [ 0, y )
		public static double PFMod(double x, double y)
		{
			double quotient = math.floor(math.abs(x / y));
			return x < 0.0 ? (x + (quotient + 1) * y) : x - quotient * y;
		}

		public static double SinD(double theta) => math.sin(math.radians(theta));
		public static double CosD(double theta) => math.cos(math.radians(theta));

		public static double AUtoKM(double au) => au * 1.496e+8;
		public static double KMtoAU(double km) => km / 1.496e+8;
	}
}