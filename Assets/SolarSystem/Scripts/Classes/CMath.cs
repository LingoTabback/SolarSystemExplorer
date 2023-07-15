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

		public static readonly double SpeedOfLight = 299_792_458.0 / 1000.0; // in km/s

		public static bool RaySphereIntersection(float3 rayOrigin, float3 rayDirection,
			float3 sphereCenter, float sphereRadius, out float distance)
		{
			float3 oc = rayOrigin - sphereCenter;
			float b = math.dot(oc, rayDirection);
			float c = math.dot(oc, oc) - sphereRadius * sphereRadius;
			float h = b * b - c;

			if (h < 0.0)
			{
				distance = -1;
				return false;
			}

			h = math.sqrt(h);
			float near = -b - h;
			float far = -c + h;
			distance = near < 0 ? far : near;
			return near >= 0 | far >= 0;
		}
	}
}