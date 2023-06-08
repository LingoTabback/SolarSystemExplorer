using CustomMath;
using AstroTime;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Ephemeris
{

	[Serializable]
	public enum OrbitType : byte
	{
		Mercury = 0,
		Venus,
		Earth,
		Mars,
		Jupiter,
		Saturn,
		Unranus,
		Neptune,
		Lunar,
		Io,
		Europa,
		Ganymede,
		Callisto,
		Titan,
		Sun,
		None
	}

	public abstract class Orbit
	{
		protected double m_Period = 0;
		public double Period => m_Period;

		public abstract double3 PositionAtTime(double t);

		public virtual double3 VelocityAtTime(double t)
		{
			double3 p0 = PositionAtTime(t);
			double3 p1 = PositionAtTime(t + s_OrbitalVelocityDiffDelta);
			return (p1 - p0) * (1.0 / s_OrbitalVelocityDiffDelta);
		}

		public static Orbit Create(OrbitType type)
		{
			return type switch
			{
				OrbitType.Mercury => new VSOP87Orbit(VSOPSeries.MercuryL, VSOPSeries.MercuryB, VSOPSeries.MercuryR, 87.9522),
				OrbitType.Venus => new VSOP87Orbit(VSOPSeries.VenusL, VSOPSeries.VenusB, VSOPSeries.VenusR, 224.7018),
				OrbitType.Earth => new VSOP87Orbit(VSOPSeries.EarthL, VSOPSeries.EarthB, VSOPSeries.EarthR, 365.25),
				OrbitType.Mars => new VSOP87Orbit(VSOPSeries.MarsL, VSOPSeries.MarsB, VSOPSeries.MarsR, 689.998725),
				OrbitType.Jupiter => new VSOP87Orbit(VSOPSeries.JupiterL, VSOPSeries.JupiterB, VSOPSeries.JupiterR, 4332.66855),
				OrbitType.Saturn => new VSOP87Orbit(VSOPSeries.SaturnL, VSOPSeries.SaturnB, VSOPSeries.SaturnR, 10759.42493),
				OrbitType.Unranus => new VSOP87Orbit(VSOPSeries.UranusL, VSOPSeries.UranusB, VSOPSeries.UranusR, 30686.07698),
				OrbitType.Neptune => new VSOP87Orbit(VSOPSeries.NeptuneL, VSOPSeries.NeptuneB, VSOPSeries.NeptuneR, 60190.64325),
				OrbitType.Lunar => new LunarOrbit(),
				OrbitType.Io => new IoOrbit(),
				OrbitType.Europa => new EuropaOrbit(),
				OrbitType.Ganymede => new GanymedeOrbit(),
				OrbitType.Callisto => new CallistoOrbit(),
				OrbitType.Titan => new TitanOrbit(),
				OrbitType.Sun => new VSOP87OrbitRect(VSOPSeries.SunX, VSOPSeries.SunY, VSOPSeries.SunZ, 0),
				_ => null
			};
		}

		protected static readonly double s_OrbitalVelocityDiffDelta = 1.0 / 1440.0;

		protected static readonly double LPEJ = 0.23509484; // Longitude of perihelion of Jupiter
		protected static readonly double JupAscendingNode = math.radians(22.203);

		protected static readonly double SatAscendingNode = 168.8112;
		protected static readonly double SatTilt = 28.0817;

		// These are required because the orbits of the Jovian and Saturnian
		// satellites are computed in units of their parent planets' radii.
		protected static readonly double JupiterRadius = 71398.0;
		protected static readonly double SaturnRadius = 60330.0;

		protected static void ComputeGalileanElements(double t,
							 out double l1, out double l2, out double l3, out double l4,
							 out double p1, out double p2, out double p3, out double p4,
							 out double w1, out double w2, out double w3, out double w4,
							 out double gamma, out double phi, out double psi,
							 out double G, out double Gp)
		{
			// Parameter t is Julian days, epoch 1950.0.
			l1 = 1.8513962 + 3.551552269981 * t;
			l2 = 3.0670952 + 1.769322724929 * t;
			l3 = 2.1041485 + 0.87820795239 * t;
			l4 = 1.473836 + 0.37648621522 * t;

			p1 = 1.69451 + 2.8167146e-3 * t;
			p2 = 2.702927 + 8.248962e-4 * t;
			p3 = 3.28443 + 1.24396e-4 * t;
			p4 = 5.851859 + 3.21e-5 * t;

			w1 = 5.451267 - 2.3176901e-3 * t;
			w2 = 1.753028 - 5.695121e-4 * t;
			w3 = 2.080331 - 1.25263e-4 * t;
			w4 = 5.630757 - 3.07063e-5 * t;

			gamma = 5.7653e-3 * math.sin(2.85674 + 1.8347e-5 * t) + 6.002e-4 * math.sin(0.60189 - 2.82274e-4 * t);
			phi = 3.485014 + 3.033241e-3 * t;
			psi = 5.524285 - 3.63e-8 * t;
			G = 0.527745 + 1.45023893e-3 * t + gamma;
			Gp = 0.5581306 + 5.83982523e-4 * t;
		}

		// Calculations for the orbits of Mimas, Enceladus, Tethys, Dione, Rhea,
		// Titan, Hyperion, and Iapetus are from Jean Meeus's Astronomical Algorithms,
		// and were originally derived by Gerard Dourneau.

		protected static void ComputeSaturnianElements(double t,
									  out double t1, out double t2, out double t3,
									  out double t4, out double t5, out double t6,
									  out double t7, out double t8, out double t9,
									  out double t10, out double t11,
									  out double W0, out double W1, out double W2,
									  out double W3, out double W4, out double W5,
									  out double W6, out double W7, out double W8)
		{
			t1 = t - 2411093.0;
			t2 = t1 / 365.25;
			t3 = (t - 2433282.423) / 365.25 + 1950.0;
			t4 = t - 2411368.0;
			t5 = t4 / 365.25;
			t6 = t - 2415020.0;
			t7 = t6 / 36525;
			t8 = t6 / 365.25;
			t9 = (t - 2442000.5) / 365.25;
			t10 = t - 2409786.0;
			t11 = t10 / 36525;

			W0 = 5.095 * (t3 - 1866.39);
			W1 = 74.4 + 32.39 * t2;
			W2 = 134.3 + 92.62 * t2;
			W3 = 42.0 - 0.5118 * t5;
			W4 = 276.59 + 0.5118 * t5;
			W5 = 267.2635 + 1222.1136 * t7;
			W6 = 175.4762 + 1221.5515 * t7;
			W7 = 2.4891 + 0.002435 * t7;
			W8 = 113.35 - 0.2597 * t7;
		}

		protected static void OuterSaturnMoonParams(double a, double e, double i,
						   double Om_, double M, double lam_,
						   out double lam, out double gam,
						   out double r, out double w)
		{
			double s1 = CMath.SinD(SatTilt);
			double c1 = CMath.CosD(SatTilt);
			double e_2 = e * e;
			double e_3 = e_2 * e;
			double e_4 = e_3 * e;
			double e_5 = e_4 * e;
			double C = (2 * e - 0.25 * e_3 + 0.0520833333 * e_5) * CMath.SinD(M) +
				(1.25 * e_2 - 0.458333333 * e_4) * CMath.SinD(2 * M) +
				(1.083333333 * e_3 - 0.671875 * e_5) * CMath.SinD(3 * M) +
				1.072917 * e_4 * CMath.SinD(4 * M) + 1.142708 * e_5 * CMath.SinD(5 * M);
			double g = Om_ - SatAscendingNode;
			double a1 = CMath.SinD(i) * CMath.SinD(g);
			double a2 = c1 * CMath.SinD(i) * CMath.CosD(g) - s1 * CMath.CosD(i);
			double u = math.degrees(math.atan2(a1, a2));
			double h = c1 * CMath.SinD(i) - s1 * CMath.CosD(i) * CMath.CosD(g);
			double psi = math.degrees(math.atan2(s1 * CMath.SinD(g), h));

			C = math.degrees(C);
			lam = lam_ + C + u - g - psi;
			gam = math.degrees(math.asin(math.sqrt(CMath.Square(a1) + CMath.Square(a2))));
			r = a * (1 - e * e) / (1 + e * CMath.CosD(M + C));
			w = SatAscendingNode + u;
		}

		protected static double3 SaturnMoonPosition(double lam, double gam, double Om, double r)
		{
			double u = lam - Om;
			double w = Om - SatAscendingNode;

			u = math.radians(u);
			w = math.radians(w);
			gam = -math.radians(gam);
			r *= SaturnRadius;

			// Corrections for coordinate system
			u = -u;
			w = -w;

			math.sincos(u, out double su, out double cu);
			math.sincos(w, out double sw, out double cw);
			math.sincos(gam, out double sgam, out double cgam);

			double x = r * (cu * cw - su * sw * cgam);
			double y = r * su * sgam;
			double z = r * (su * cw * cgam + cu * sw);

			return new double3(CMath.KMtoAU(x), CMath.KMtoAU(y), -CMath.KMtoAU(z));
		}

		protected static double Obliquity(double t)
		{
			// Parameter t represents the Julian centuries elapsed since 1900.
			// In other words, t = (jd - 2415020.0) / 36525.0

			return math.radians(2.345229444e1 - ((((-1.81e-3 * t) + 5.9e-3) * t + 4.6845e1) * t) / 3600.0);
		}

		protected static void Nutation(double t, out double deps, out double dpsi)
		{
			// Parameter t represents the Julian centuries elapsed since 1900.
			// In other words, t = (jd - 2415020.0) / 36525.0

			double ls, ld;    // sun's mean longitude, moon's mean longitude
			double ms, md;    // sun's mean anomaly, moon's mean anomaly
			double nm;        // longitude of moon's ascending node
			double t2;
			double tls, tnm, tld;    // twice above
			double a, b;

			t2 = t * t;

			a = 100.0021358 * t;
			b = 360.0 * (a - (int)a);
			ls = 279.697 + .000303 * t2 + b;

			a = 1336.855231 * t;
			b = 360.0 * (a - (int)a);
			ld = 270.434 - .001133 * t2 + b;

			a = 99.99736056000026 * t;
			b = 360.0 * (a - (int)a);
			ms = 358.476 - .00015 * t2 + b;

			a = 13255523.59 * t;
			b = 360.0 * (a - (int)a);
			md = 296.105 + .009192 * t2 + b;

			a = 5.372616667 * t;
			b = 360.0 * (a - (int)a);
			nm = 259.183 + .002078 * t2 - b;

			//convert to radian forms for use with trig functions.
			tls = 2 * math.radians(ls);
			nm = math.radians(nm);
			tnm = 2 * math.radians(nm);
			ms = math.radians(ms);
			tld = 2 * math.radians(ld);
			md = math.radians(md);

			// find delta psi and eps, in arcseconds.
			dpsi = (-17.2327 - .01737 * t) * math.sin(nm) + (-1.2729 - .00013 * t) * math.sin(tls)
				+ .2088 * math.sin(tnm) - .2037 * math.sin(tld) + (.1261 - .00031 * t) * math.sin(ms)
				+ .0675 * math.sin(md) - (.0497 - .00012 * t) * math.sin(tls + ms)
				- .0342 * math.sin(tld - nm) - .0261 * math.sin(tld + md) + .0214 * math.sin(tls - ms)
				- .0149 * math.sin(tls - tld + md) + .0124 * math.sin(tls - nm) + .0114 * math.sin(tld - md);
			deps = (9.21 + .00091 * t) * math.cos(nm) + (.5522 - .00029 * t) * math.cos(tls)
				- .0904 * math.cos(tnm) + .0884 * math.cos(tld) + .0216 * math.cos(tls + ms)
				+ .0183 * math.cos(tld - nm) + .0113 * math.cos(tld + md) - .0093 * math.cos(tls - ms)
				- .0066 * math.cos(tls - nm);

			// convert to radians.
			dpsi = math.radians(dpsi / 3600);
			deps = math.radians(deps / 3600);
		}

		protected static void EclipticToEquatorial(double fEclLat, double fEclLon,
						  out double RA, out double dec)
		{
			// Parameter t represents the Julian centuries elapsed since 1900.
			// In other words, t = (jd - 2415020.0) / 36525.0

			//    t = (astro::J2000 - 2415020.0) / 36525.0;
			double t = 0;
			double eps = Obliquity(t);        // mean obliquity for date
			Nutation(t, out double deps, out double dpsi);
			eps += deps;
			math.sincos(eps, out double seps, out double ceps);

			math.sincos(fEclLat, out double sy, out double cy /* always non-negative*/);
			if (math.abs(cy) < 1e-20)
				cy = 1e-20;        // insure > 0
			double ty = sy / cy;
			math.sincos(fEclLon, out double sx, out double cx);
			dec = math.asin((sy * ceps) + (cy * seps * sx));
			RA = math.atan(((sx * ceps) - (ty * seps)) / cx);
			if (cx < 0)
				RA += math.PI_DBL; // account for atan quad ambiguity
			RA = CMath.PFMod(RA, math.PI_DBL * 2);
		}

		// Convert equatorial coordinates from one epoch to another.  Method is from
		// Chapter 21 of Meeus's _Astronomical Algorithms_
		protected void EpochConvert(double jdFrom, double jdTo,
						  double a0, double d0,
						  out double a, out double d)
		{
			double T = TimeUtil.GetJulianCentury(jdFrom - TimeUtil.J2000);
			double t = TimeUtil.GetJulianCentury(jdTo - jdFrom);

			double zeta = (2306.2181 + 1.39656 * T - 0.000139 * T * T) * t +
				(0.30188 - 0.000344 * T) * t * t + 0.017998 * t * t * t;
			double z = (2306.2181 + 1.39656 * T - 0.000139 * T * T) * t +
				(1.09468 + 0.000066 * T) * t * t + 0.018203 * t * t * t;
			double theta = (2004.3109 - 0.85330 * T - 0.000217 * T * T) * t -
				(0.42665 + 0.000217 * T) * t * t - 0.041833 * t * t * t;
			zeta = math.radians(zeta / 3600.0);
			z = math.radians(z / 3600.0);
			theta = math.radians(theta / 3600.0);

			double A = math.cos(d0) * math.sin(a0 + zeta);
			double B = math.cos(theta) * math.cos(d0) * math.cos(a0 + zeta) -
				math.sin(theta) * math.sin(d0);
			double C = math.sin(theta) * math.cos(d0) * math.cos(a0 + zeta) +
				math.cos(theta) * math.sin(d0);

			a = math.atan2(A, B) + z;
			d = math.asin(C);
		}

		protected double SumSeries(double[,] series, double t)
		{
			double x = 0.0;
			for (int i = 0; i < series.GetLength(0); ++i)
				x += series[i, 0] * math.cos(series[i, 1] + series[i, 2] * t);
			return x;
		}
}

	public class VSOP87Orbit : Orbit
	{
		private readonly double[][,] m_L;
		private readonly double[][,] m_B;
		private readonly double[][,] m_R;

		public VSOP87Orbit(double[][,] l, double[][,] b, double[][,] r, double period)
		{
			m_L = l;
			m_B = b;
			m_R = r;
			m_Period = period;
		}

		public override double3 PositionAtTime(double jd)
		{
			double t = TimeUtil.GetJulianMillenium(jd - TimeUtil.J2000);

			// Heliocentric coordinates
			double l = 0.0; // longitude
			double b = 0.0; // latitude
			double r = 0.0; // radius

			double T = 1;
			for (int i = 0; i < m_L.Length; ++i)
			{
				double s = 0;
				for (int j = 0; j < m_L[i].GetLength(0); ++j)
					s += m_L[i][j, 0] * math.cos(m_L[i][j, 1] + m_L[i][j, 2] * t);

				l += s * T;
				T = t * T;
			}

			T = 1;
			for (int i = 0; i < m_B.Length; ++i)
			{
				double s = 0;
				for (var j = 0; j < m_B[i].GetLength(0); j++)
					s += m_B[i][j, 0] * math.cos(m_B[i][j, 1] + m_B[i][j, 2] * t);

				b += s * T;
				T = t * T;
			}

			T = 1;
			for (int i = 0; i < m_R.Length; ++i)
			{
				double s = 0;
				for (var j = 0; j < m_R[i].GetLength(0); j++)
					s += m_R[i][j, 0] * math.cos(m_R[i][j, 1] + m_R[i][j, 2] * t);

				r += s * T;
				T = t * T;
			}

			// Corrections for internal coordinate system
			b -= math.PI_DBL * 0.5;
			l += math.PI_DBL;

			double x = math.cos(l) * math.sin(b) * r;
			double y = math.cos(b) * r;
			double z = math.sin(l) * math.sin(b) * r;

			return new double3(x, y, z);
		}
	}

	public class VSOP87OrbitRect : Orbit
	{
		private readonly double[][,] m_X;
		private readonly double[][,] m_Y;
		private readonly double[][,] m_Z;

		public VSOP87OrbitRect(double[][,] x, double[][,] y, double[][,] z, double period)
		{
			m_X = x;
			m_Y = y;
			m_Z = z;
			m_Period = period;
		}

		public override double3 PositionAtTime(double jd)
		{
			double t = TimeUtil.GetJulianMillenium(jd - TimeUtil.J2000);
			double3 v = 0;

			double T = 1;
			for (int i = 0; i < m_X.Length; ++i)
			{
				v.x += SumSeries(m_X[i], t) * T;
				T = t * T;
			}

			T = 1;
			for (int i = 0; i < m_Y.Length; ++i)
			{
				v.y += SumSeries(m_Y[i], t) * T;
				T = t * T;
			}

			T = 1;
			for (int i = 0; i < m_Z.Length; ++i)
			{
				v.z += SumSeries(m_Z[i], t) * T;
				T = t * T;
			}

			return new double3(v.x, v.z, v.y);
		}
	}

	public class LunarOrbit : Orbit
	{
		public LunarOrbit() { m_Period = 27.321661; }

		public override double3 PositionAtTime(double jd)
		{
			// Computation requires an abbreviated Julian day:
			// epoch January 0.5, 1900.
			double jd19 = jd - 2415020.0;
			double t = TimeUtil.GetJulianCentury(jd19);
			double t2 = t * t;

			double m1 = jd19 / 27.32158213;
			m1 = 360.0 * (m1 - (int)m1);
			double m2 = jd19 / 365.2596407;
			m2 = 360.0 * (m2 - (int)m2);
			double m3 = jd19 / 27.55455094;
			m3 = 360.0 * (m3 - (int)m3);
			double m4 = jd19 / 29.53058868;
			m4 = 360.0 * (m4 - (int)m4);
			double m5 = jd19 / 27.21222039;
			m5 = 360.0 * (m5 - (int)m5);
			double m6 = jd19 / 6798.363307;
			m6 = 360.0 * (m6 - (int)m6);

			double ld = 270.434164 + m1 - (.001133 - .0000019 * t) * t2;
			double ms = 358.475833 + m2 - (.00015 + .0000033 * t) * t2;
			double md = 296.104608 + m3 + (.009192 + .0000144 * t) * t2;
			double de = 350.737486 + m4 - (.001436 - .0000019 * t) * t2;
			double f = 11.250889 + m5 - (.003211 + .0000003 * t) * t2;
			double n = 259.183275 - m6 + (.002078 + .000022 * t) * t2;

			double a = math.radians(51.2 + 20.2 * t);
			double sa = math.sin(a);
			double sn = math.sin(math.radians(n));
			double b = 346.56 + (132.87 - .0091731 * t) * t;
			double sb = .003964 * math.sin(math.radians(b));
			double c = math.radians(n + 275.05 - 2.3 * t);
			double sc = math.sin(c);
			ld = ld + .000233 * sa + sb + .001964 * sn;
			ms = ms - .001778 * sa;
			md = md + .000817 * sa + sb + .002541 * sn;
			f = f + sb - .024691 * sn - .004328 * sc;
			de = de + .002011 * sa + sb + .001964 * sn;
			double e = 1 - (.002495 + 7.52e-06 * t) * t;
			double e2 = e * e;

			ld = math.radians(ld);
			ms = math.radians(ms);
			n = math.radians(n);
			de = math.radians(de);
			f = math.radians(f);
			md = math.radians(md);

			double l = 6.28875 * math.sin(md) + 1.27402 * math.sin(2 * de - md) + .658309 * math.sin(2 * de) +
				.213616 * math.sin(2 * md) - e * .185596 * math.sin(ms) - .114336 * math.sin(2 * f) +
				.058793 * math.sin(2 * (de - md)) + .057212 * e * math.sin(2 * de - ms - md) +
				.05332 * math.sin(2 * de + md) + .045874 * e * math.sin(2 * de - ms) + .041024 * e * math.sin(md - ms);
			l = l - .034718 * math.sin(de) - e * .030465 * math.sin(ms + md) + .015326 * math.sin(2 * (de - f)) -
				.012528 * math.sin(2 * f + md) - .01098 * math.sin(2 * f - md) + .010674 * math.sin(4 * de - md) +
				.010034 * math.sin(3 * md) + .008548 * math.sin(4 * de - 2 * md) - e * .00791 * math.sin(ms - md + 2 * de) -
				e * .006783 * math.sin(2 * de + ms);
			l = l + .005162 * math.sin(md - de) + e * .005 * math.sin(ms + de) + .003862 * math.sin(4 * de) +
				e * .004049 * math.sin(md - ms + 2 * de) + .003996 * math.sin(2 * (md + de)) +
				.003665 * math.sin(2 * de - 3 * md) + e * .002695 * math.sin(2 * md - ms) +
				.002602 * math.sin(md - 2 * (f + de)) + e * .002396 * math.sin(2 * (de - md) - ms) -
				.002349 * math.sin(md + de);
			l = l + e2 * .002249 * math.sin(2 * (de - ms)) - e * .002125 * math.sin(2 * md + ms) -
				e2 * .002079 * math.sin(2 * ms) + e2 * .002059 * math.sin(2 * (de - ms) - md) -
				.001773 * math.sin(md + 2 * (de - f)) - .001595 * math.sin(2 * (f + de)) +
				e * .00122 * math.sin(4 * de - ms - md) - .00111 * math.sin(2 * (md + f)) + .000892 * math.sin(md - 3 * de);
			l = l - e * .000811 * math.sin(ms + md + 2 * de) + e * .000761 * math.sin(4 * de - ms - 2 * md) +
				e2 * .000704 * math.sin(md - 2 * (ms + de)) + e * .000693 * math.sin(ms - 2 * (md - de)) +
				e * .000598 * math.sin(2 * (de - f) - ms) + .00055 * math.sin(md + 4 * de) + .000538 * math.sin(4 * md) +
				e * .000521 * math.sin(4 * de - ms) + .000486 * math.sin(2 * md - de);
			l = l + e2 * .000717 * math.sin(md - 2 * ms);
			double eclLon = ld + math.radians(l);
			eclLon = CMath.PFMod(eclLon, math.PI_DBL * 2);

			double g = 5.12819 * math.sin(f) + .280606 * math.sin(md + f) + .277693 * math.sin(md - f) +
				.173238 * math.sin(2 * de - f) + .055413 * math.sin(2 * de + f - md) + .046272 * math.sin(2 * de - f - md) +
				.032573 * math.sin(2 * de + f) + .017198 * math.sin(2 * md + f) + .009267 * math.sin(2 * de + md - f) +
				.008823 * math.sin(2 * md - f) + e * .008247 * math.sin(2 * de - ms - f);
			g = g + .004323 * math.sin(2 * (de - md) - f) + .0042 * math.sin(2 * de + f + md) +
				e * .003372 * math.sin(f - ms - 2 * de) + e * .002472 * math.sin(2 * de + f - ms - md) +
				e * .002222 * math.sin(2 * de + f - ms) + e * .002072 * math.sin(2 * de - f - ms - md) +
				e * .001877 * math.sin(f - ms + md) + .001828 * math.sin(4 * de - f - md) - e * .001803 * math.sin(f + ms) -
				.00175 * math.sin(3 * f);
			g = g + e * .00157 * math.sin(md - ms - f) - .001487 * math.sin(f + de) - e * .001481 * math.sin(f + ms + md) +
				e * .001417 * math.sin(f - ms - md) + e * .00135 * math.sin(f - ms) + .00133 * math.sin(f - de) +
				.001106 * math.sin(f + 3 * md) + .00102 * math.sin(4 * de - f) + .000833 * math.sin(f + 4 * de - md) +
				.000781 * math.sin(md - 3 * f) + .00067 * math.sin(f + 4 * de - 2 * md);
			g = g + .000606 * math.sin(2 * de - 3 * f) + .000597 * math.sin(2 * (de + md) - f) +
				e * .000492 * math.sin(2 * de + md - ms - f) + .00045 * math.sin(2 * (md - de) - f) +
				.000439 * math.sin(3 * md - f) + .000423 * math.sin(f + 2 * (de + md)) +
				.000422 * math.sin(2 * de - f - 3 * md) - e * .000367 * math.sin(ms + f + 2 * de - md) -
				e * .000353 * math.sin(ms + f + 2 * de) + .000331 * math.sin(f + 4 * de);
			g = g + e * .000317 * math.sin(2 * de + f - ms + md) + e2 * .000306 * math.sin(2 * (de - ms) - f) -
				.000283 * math.sin(md + 3 * f);
			double w1 = .0004664 * math.cos(n);
			double w2 = .0000754 * math.cos(c);
			double eclLat = math.radians(g) * (1 - w1 - w2);

			double hp = .950724 + .051818 * math.cos(md) + .009531 * math.cos(2 * de - md) + .007843 * math.cos(2 * de) +
				 .002824 * math.cos(2 * md) + .000857 * math.cos(2 * de + md) + e * .000533 * math.cos(2 * de - ms) +
				 e * .000401 * math.cos(2 * de - md - ms) + e * .00032 * math.cos(md - ms) - .000271 * math.cos(de) -
				 e * .000264 * math.cos(ms + md) - .000198 * math.cos(2 * f - md);
			hp = hp + .000173 * math.cos(3 * md) + .000167 * math.cos(4 * de - md) - e * .000111 * math.cos(ms) +
				 .000103 * math.cos(4 * de - 2 * md) - .000084 * math.cos(2 * md - 2 * de) -
				 e * .000083 * math.cos(2 * de + ms) + .000079 * math.cos(2 * de + 2 * md) + .000072 * math.cos(4 * de) +
				 e * .000064 * math.cos(2 * de - ms + md) - e * .000063 * math.cos(2 * de + ms - md) +
				 e * .000041 * math.cos(ms + de);
			hp = hp + e * .000035 * math.cos(2 * md - ms) - .000033 * math.cos(3 * md - 2 * de) -
				 .00003 * math.cos(md + de) - .000029 * math.cos(2 * (f - de)) - e * .000029 * math.cos(2 * md + ms) +
				 e2 * .000026 * math.cos(2 * (de - ms)) - .000023 * math.cos(2 * (f - de) + md) +
				 e * .000019 * math.cos(4 * de - ms - md);
			double horzPar = math.radians(hp);

			// At this point we have values of ecliptic longitude, latitude and
			// horizontal parallax (eclLong, eclLat, horzPar) in radians.

			// Now compute distance using horizontal parallax.
			double distance = 6378.14 / math.sin(horzPar);

			// Finally convert eclLat, eclLon to RA, Dec.
			EclipticToEquatorial(eclLat, eclLon, out double RA, out double dec);

			// RA and Dec are referred to the equinox of date; we want to use
			// the J2000 equinox instead.  A better idea would be to directly
			// compute the position of the Moon in this coordinate system, but
			// this was easier.
			EpochConvert(jd, TimeUtil.J2000, RA, dec, out RA, out dec);

			// Corrections for internal coordinate system
			dec -= math.PI_DBL * 0.5;
			RA += math.PI_DBL;

			double x = distance * math.cos(eclLat) * math.cos(eclLon);
			double y = distance * math.sin(eclLat);
			double z = distance * math.cos(eclLat) * math.sin(eclLon);

			//double x = math.cos(RA) * math.sin(dec) * distance;
			//double y = math.cos(dec) * distance;
			//double z = math.sin(RA) * math.sin(dec) * distance;

			// The position already has Earths Orientation baked in, so we must 'unrotate' it.
			// This is a BIG MAYBE i dont actually know why this seems to work!!!!
			double3 position = new(CMath.KMtoAU(x), CMath.KMtoAU(y), CMath.KMtoAU(z));
			position = dQuaternion.mul(dQuaternion.inverse(s_EarthRotationModel.ComputeEquatorOrientation(jd)), position);
			return position;
		}

		private static RotationModel s_EarthRotationModel = RotationModel.Create(RotationModelType.Earth);
	}

	public class IoOrbit : Orbit
	{
		public IoOrbit() { m_Period = 1.769138; }

		public override double3 PositionAtTime(double jd)
		{
			// Epoch for Galilean satellites is 1976.0 Aug 10
			double t = jd - 2443000.5;

			ComputeGalileanElements(t,
							 out double l1, out double l2, out double l3, out double l4,
							 out double p1, out double p2, out double p3, out double p4,
							 out double w1, out double w2, out double w3, out double w4,
							 out double gamma, out double phi, out double psi,
							 out double G, out double Gp);

			// Calculate periodic terms for longitude
			double sigma = 0.47259 * math.sin(2 * (l1 - l2)) - 0.03478 * math.sin(p3 - p4)
				  + 0.01081 * math.sin(l2 - 2 * l3 + p3) + 7.38e-3 * math.sin(phi)
				  + 7.13e-3 * math.sin(l2 - 2 * l3 + p2) - 6.74e-3 * math.sin(p1 + p3 - 2 * LPEJ - 2 * G)
				  + 6.66e-3 * math.sin(l2 - 2 * l3 + p4) + 4.45e-3 * math.sin(l1 - p3)
				  - 3.54e-3 * math.sin(l1 - l2) - 3.17e-3 * math.sin(2 * (psi - LPEJ))
				  + 2.65e-3 * math.sin(l1 - p4) - 1.86e-3 * math.sin(G)
				  + 1.62e-3 * math.sin(p2 - p3) + 1.58e-3 * math.sin(4 * (l1 - l2))
				  - 1.55e-3 * math.sin(l1 - l3) - 1.38e-3 * math.sin(psi + w3 - 2 * LPEJ - 2 * G)
				  - 1.15e-3 * math.sin(2 * (l1 - 2 * l2 + w2)) + 8.9e-4 * math.sin(p2 - p4)
				  + 8.5e-4 * math.sin(l1 + p3 - 2 * LPEJ - 2 * G) + 8.3e-4 * math.sin(w2 - w3)
				  + 5.3e-4 * math.sin(psi - w2);
			sigma = CMath.PFMod(sigma, 360.0);
			sigma = math.radians(sigma);
			double L = l1 + sigma;

			// Calculate periodic terms for the tangent of the latitude
			double B = 6.393e-4 * math.sin(L - w1) + 1.825e-4 * math.sin(L - w2)
			  + 3.29e-5 * math.sin(L - w3) - 3.11e-5 * math.sin(L - psi)
			  + 9.3e-6 * math.sin(L - w4) + 7.5e-6 * math.sin(3 * L - 4 * l2 - 1.9927 * sigma + w2)
			  + 4.6e-6 * math.sin(L + psi - 2 * LPEJ - 2 * G);
			B = math.atan(B);

			// Calculate the periodic terms for distance
			double R = -4.1339e-3 * math.cos(2 * (l1 - l2)) - 3.87e-5 * math.cos(l1 - p3)
			  - 2.14e-5 * math.cos(l1 - p4) + 1.7e-5 * math.cos(l1 - l2)
			  - 1.31e-5 * math.cos(4 * (l1 - l2)) + 1.06e-5 * math.cos(l1 - l3)
			  - 6.6e-6 * math.cos(l1 + p3 - 2 * LPEJ - 2 * G);
			R = 5.90569 * JupiterRadius * (1 + R);

			double T = (jd - 2433282.423) / 36525.0;
			double P = 1.3966626 * T + 3.088e-4 * T * T;
			L += math.radians(P);

			L += JupAscendingNode;

			// Corrections for internal coordinate system
			B -= math.PI_DBL * 0.5;
			L += math.PI_DBL;

			double x = math.cos(L) * math.sin(B) * R;
			double y = math.cos(B) * R;
			double z = math.sin(L) * math.sin(B) * R;

			return new double3(CMath.KMtoAU(x), CMath.KMtoAU(y), CMath.KMtoAU(z));
		}
	}

	public class EuropaOrbit : Orbit
	{
		public EuropaOrbit() { m_Period = 3.5511810791; }

		public override double3 PositionAtTime(double jd)
		{
			// Epoch for Galilean satellites is 1976.0 Aug 10
			double t = jd - 2443000.5;

			ComputeGalileanElements(t,
						 out double l1, out double l2, out double l3, out double l4,
						 out double p1, out double p2, out double p3, out double p4,
						 out double w1, out double w2, out double w3, out double w4,
						 out double gamma, out double phi, out double psi,
						 out double G, out double Gp);

			// Calculate periodic terms for longitude
			double sigma = 1.06476 * math.sin(2 * (l2 - l3)) + 0.04256 * math.sin(l1 - 2 * l2 + p3)
				  + 0.03581 * math.sin(l2 - p3) + 0.02395 * math.sin(l1 - 2 * l2 + p4)
				  + 0.01984 * math.sin(l2 - p4) - 0.01778 * math.sin(phi)
				  + 0.01654 * math.sin(l2 - p2) + 0.01334 * math.sin(l2 - 2 * l3 + p2)
				  + 0.01294 * math.sin(p3 - p4) - 0.01142 * math.sin(l2 - l3)
				  - 0.01057 * math.sin(G) - 7.75e-3 * math.sin(2 * (psi - LPEJ))
				  + 5.24e-3 * math.sin(2 * (l1 - l2)) - 4.6e-3 * math.sin(l1 - l3)
				  + 3.16e-3 * math.sin(psi - 2 * G + w3 - 2 * LPEJ) - 2.03e-3 * math.sin(p1 + p3 - 2 * LPEJ - 2 * G)
				  + 1.46e-3 * math.sin(psi - w3) - 1.45e-3 * math.sin(2 * G)
				  + 1.25e-3 * math.sin(psi - w4) - 1.15e-3 * math.sin(l1 - 2 * l3 + p3)
				  - 9.4e-4 * math.sin(2 * (l2 - w2)) + 8.6e-4 * math.sin(2 * (l1 - 2 * l2 + w2))
				  - 8.6e-4 * math.sin(5 * Gp - 2 * G + 0.9115) - 7.8e-4 * math.sin(l2 - l4)
				  - 6.4e-4 * math.sin(3 * l3 - 7 * l4 + 4 * p4) + 6.4e-4 * math.sin(p1 - p4)
				  - 6.3e-4 * math.sin(l1 - 2 * l3 + p4) + 5.8e-4 * math.sin(w3 - w4)
				  + 5.6e-4 * math.sin(2 * (psi - LPEJ - G)) + 5.6e-4 * math.sin(2 * (l2 - l4))
				  + 5.5e-4 * math.sin(2 * (l1 - l3)) + 5.2e-4 * math.sin(3 * l3 - 7 * l4 + p3 + 3 * p4)
				  - 4.3e-4 * math.sin(l1 - p3) + 4.1e-4 * math.sin(5 * (l2 - l3))
				  + 4.1e-4 * math.sin(p4 - LPEJ) + 3.2e-4 * math.sin(w2 - w3)
				  + 3.2e-4 * math.sin(2 * (l3 - G - LPEJ));
			sigma = CMath.PFMod(sigma, 360.0);
			sigma = math.radians(sigma);
			double L = l2 + sigma;

			// Calculate periodic terms for the tangent of the latitude
			double B = 8.1004e-3 * math.sin(L - w2) + 4.512e-4 * math.sin(L - w3)
			  - 3.284e-4 * math.sin(L - psi) + 1.160e-4 * math.sin(L - w4)
			  + 2.72e-5 * math.sin(l1 - 2 * l3 + 1.0146 * sigma + w2) - 1.44e-5 * math.sin(L - w1)
			  + 1.43e-5 * math.sin(L + psi - 2 * LPEJ - 2 * G) + 3.5e-6 * math.sin(L - psi + G)
			  - 2.8e-6 * math.sin(l1 - 2 * l3 + 1.0146 * sigma + w3);
			B = math.atan(B);

			// Calculate the periodic terms for distance
			double R = 9.3848e-3 * math.cos(l1 - l2) - 3.116e-4 * math.cos(l2 - p3)
			  - 1.744e-4 * math.cos(l2 - p4) - 1.442e-4 * math.cos(l2 - p2)
			  + 5.53e-5 * math.cos(l2 - l3) + 5.23e-5 * math.cos(l1 - l3)
			  - 2.9e-5 * math.cos(2 * (l1 - l2)) + 1.64e-5 * math.cos(2 * (l2 - w2))
			  + 1.07e-5 * math.cos(l1 - 2 * l3 + p3) - 1.02e-5 * math.cos(l2 - p1)
			  - 9.1e-6 * math.cos(2 * (l1 - l3));
			R = 9.39657 * JupiterRadius * (1 + R);

			double T = (jd - 2433282.423) / 36525.0;
			double P = 1.3966626 * T + 3.088e-4 * T * T;
			L += math.radians(P);

			L += JupAscendingNode;

			// Corrections for internal coordinate system
			B -= math.PI_DBL * 0.5;
			L += math.PI_DBL;

			double x = math.cos(L) * math.sin(B) * R;
			double y = math.cos(B) * R;
			double z = math.sin(L) * math.sin(B) * R;

			return new double3(CMath.KMtoAU(x), CMath.KMtoAU(y), CMath.KMtoAU(z));
		}
	}

	public class GanymedeOrbit : Orbit
	{
		public GanymedeOrbit() { m_Period = 7.154553; }

		public override double3 PositionAtTime(double jd)
		{
			// Epoch for Galilean satellites is 1976.0 Aug 10
			double t = jd - 2443000.5;

			ComputeGalileanElements(t,
						 out double l1, out double l2, out double l3, out double l4,
						 out double p1, out double p2, out double p3, out double p4,
						 out double w1, out double w2, out double w3, out double w4,
						 out double gamma, out double phi, out double psi,
						 out double G, out double Gp);

			//Calculate periodic terms for longitude
			double sigma = 0.1649 * math.sin(l3 - p3) + 0.09081 * math.sin(l3 - p4)
				  - 0.06907 * math.sin(l2 - l3) + 0.03784 * math.sin(p3 - p4)
				  + 0.01846 * math.sin(2 * (l3 - l4)) - 0.01340 * math.sin(G)
				  - 0.01014 * math.sin(2 * (psi - LPEJ)) + 7.04e-3 * math.sin(l2 - 2 * l3 + p3)
				  - 6.2e-3 * math.sin(l2 - 2 * l3 + p2) - 5.41e-3 * math.sin(l3 - l4)
				  + 3.81e-3 * math.sin(l2 - 2 * l3 + p4) + 2.35e-3 * math.sin(psi - w3)
				  + 1.98e-3 * math.sin(psi - w4) + 1.76e-3 * math.sin(phi)
				  + 1.3e-3 * math.sin(3 * (l3 - l4)) + 1.25e-3 * math.sin(l1 - l3)
				  - 1.19e-3 * math.sin(5 * Gp - 2 * G + 0.9115) + 1.09e-3 * math.sin(l1 - l2)
				  - 1.0e-3 * math.sin(3 * l3 - 7 * l4 + 4 * p4) + 9.1e-4 * math.sin(w3 - w4)
				  + 8.0e-4 * math.sin(3 * l3 - 7 * l4 + p3 + 3 * p4) - 7.5e-4 * math.sin(2 * l2 - 3 * l3 + p3)
				  + 7.2e-4 * math.sin(p1 + p3 - 2 * LPEJ - 2 * G) + 6.9e-4 * math.sin(p4 - LPEJ)
				  - 5.8e-4 * math.sin(2 * l3 - 3 * l4 + p4) - 5.7e-4 * math.sin(l3 - 2 * l4 + p4)
				  + 5.6e-4 * math.sin(l3 + p3 - 2 * LPEJ - 2 * G) - 5.2e-4 * math.sin(l2 - 2 * l3 + p1)
				  - 5.0e-4 * math.sin(p2 - p3) + 4.8e-4 * math.sin(l3 - 2 * l4 + p3)
				  - 4.5e-4 * math.sin(2 * l2 - 3 * l3 + p4) - 4.1e-4 * math.sin(p2 - p4)
				  - 3.8e-4 * math.sin(2 * G) - 3.7e-4 * math.sin(p3 - p4 + w3 - w4)
				  - 3.2e-4 * math.sin(3 * l3 - 7 * l4 + 2 * p3 + 2 * p4) + 3.0e-4 * math.sin(4 * (l3 - l4))
				  + 2.9e-4 * math.sin(l3 + p4 - 2 * LPEJ - 2 * G) - 2.8e-4 * math.sin(w3 + psi - 2 * LPEJ - 2 * G)
				  + 2.6e-4 * math.sin(l3 - LPEJ - G) + 2.4e-4 * math.sin(l2 - 3 * l3 + 2 * l4)
				  + 2.1e-4 * math.sin(2 * (l3 - LPEJ - G)) - 2.1e-4 * math.sin(l3 - p2)
				  + 1.7e-4 * math.sin(l3 - p3);
			sigma = CMath.PFMod(sigma, 360.0);
			sigma = math.radians(sigma);
			double L = l3 + sigma;

			//Calculate periodic terms for the tangent of the latitude
			double B = 3.2402e-3 * math.sin(L - w3) - 1.6911e-3 * math.sin(L - psi)
			  + 6.847e-4 * math.sin(L - w4) - 2.797e-4 * math.sin(L - w2)
			  + 3.21e-5 * math.sin(L + psi - 2 * LPEJ - 2 * G) + 5.1e-6 * math.sin(L - psi + G)
			  - 4.5e-6 * math.sin(L - psi - G) - 4.5e-6 * math.sin(L + psi - 2 * LPEJ)
			  + 3.7e-6 * math.sin(L + psi - 2 * LPEJ - 3 * G) + 3.0e-6 * math.sin(2 * l2 - 3 * L + 4.03 * sigma + w2)
			  - 2.1e-6 * math.sin(2 * l2 - 3 * L + 4.03 * sigma + w3);
			B = math.atan(B);

			//Calculate the periodic terms for distance
			double R = -1.4388e-3 * math.cos(l3 - p3) - 7.919e-4 * math.cos(l3 - p4)
			  + 6.342e-4 * math.cos(l2 - l3) - 1.761e-4 * math.cos(2 * (l3 - l4))
			  + 2.94e-5 * math.cos(l3 - l4) - 1.56e-5 * math.cos(3 * (l3 - l4))
			  + 1.56e-5 * math.cos(l1 - l3) - 1.53e-5 * math.cos(l1 - l2)
			  + 7.0e-6 * math.cos(2 * l2 - 3 * l3 + p3) - 5.1e-6 * math.cos(l3 + p3 - 2 * LPEJ - 2 * G);
			R = 14.98832 * JupiterRadius * (1 + R);

			double T = (jd - 2433282.423) / 36525.0;
			double P = 1.3966626 * T + 3.088e-4 * T * T;
			L += math.radians(P);

			L += JupAscendingNode;

			// Corrections for internal coordinate system
			B -= math.PI_DBL * 0.5;
			L += math.PI_DBL;

			double x = math.cos(L) * math.sin(B) * R;
			double y = math.cos(B) * R;
			double z = math.sin(L) * math.sin(B) * R;

			return new double3(CMath.KMtoAU(x), CMath.KMtoAU(y), CMath.KMtoAU(z));
		}
	}

	public class CallistoOrbit : Orbit
	{
		public CallistoOrbit() { m_Period = 16.689018; }

		public override double3 PositionAtTime(double jd)
		{
			// Epoch for Galilean satellites is 1976.0 Aug 10
			double t = jd - 2443000.5;

			ComputeGalileanElements(t,
						 out double l1, out double l2, out double l3, out double l4,
						 out double p1, out double p2, out double p3, out double p4,
						 out double w1, out double w2, out double w3, out double w4,
						 out double gamma, out double phi, out double psi,
						 out double G, out double Gp);

			//Calculate periodic terms for longitude
			double sigma =
				0.84287 * math.sin(l4 - p4)
				+ 0.03431 * math.sin(p4 - p3)
				- 0.03305 * math.sin(2 * (psi - LPEJ))
				- 0.03211 * math.sin(G)
				- 0.01862 * math.sin(l4 - p3)
				+ 0.01186 * math.sin(psi - w4)
				+ 6.23e-3 * math.sin(l4 + p4 - 2 * G - 2 * LPEJ)
				+ 3.87e-3 * math.sin(2 * (l4 - p4))
				- 2.84e-3 * math.sin(5 * Gp - 2 * G + 0.9115)
				- 2.34e-3 * math.sin(2 * (psi - p4))
				- 2.23e-3 * math.sin(l3 - l4)
				- 2.08e-3 * math.sin(l4 - LPEJ)
				+ 1.78e-3 * math.sin(psi + w4 - 2 * p4)
				+ 1.34e-3 * math.sin(p4 - LPEJ)
				+ 1.25e-3 * math.sin(2 * (l4 - G - LPEJ))
				- 1.17e-3 * math.sin(2 * G)
				- 1.12e-3 * math.sin(2 * (l3 - l4))
				+ 1.07e-3 * math.sin(3 * l3 - 7 * l4 + 4 * p4)
				+ 1.02e-3 * math.sin(l4 - G - LPEJ)
				+ 9.6e-4 * math.sin(2 * l4 - psi - w4)
				+ 8.7e-4 * math.sin(2 * (psi - w4))
				- 8.5e-4 * math.sin(3 * l3 - 7 * l4 + p3 + 3 * p4)
				+ 8.5e-4 * math.sin(l3 - 2 * l4 + p4)
				- 8.1e-4 * math.sin(2 * (l4 - psi))
				+ 7.1e-4 * math.sin(l4 + p4 - 2 * LPEJ - 3 * G)
				+ 6.1e-4 * math.sin(l1 - l4)
				- 5.6e-4 * math.sin(psi - w3)
				- 5.4e-4 * math.sin(l3 - 2 * l4 + p3)
				+ 5.1e-4 * math.sin(l2 - l4)
				+ 4.2e-4 * math.sin(2 * (psi - G - LPEJ))
				+ 3.9e-4 * math.sin(2 * (p4 - w4))
				+ 3.6e-4 * math.sin(psi + LPEJ - p4 - w4)
				+ 3.5e-4 * math.sin(2 * Gp - G + 3.2877)
				- 3.5e-4 * math.sin(l4 - p4 + 2 * LPEJ - 2 * psi)
				- 3.2e-4 * math.sin(l4 + p4 - 2 * LPEJ - G)
				+ 3.0e-4 * math.sin(2 * Gp - 2 * G + 2.6032)
				+ 2.9e-4 * math.sin(3 * l3 - 7 * l4 + 2 * p3 + 2 * p4)
				+ 2.8e-4 * math.sin(l4 - p4 + 2 * psi - 2 * LPEJ)
				- 2.8e-4 * math.sin(2 * (l4 - w4))
				- 2.7e-4 * math.sin(p3 - p4 + w3 - w4)
				- 2.6e-4 * math.sin(5 * Gp - 3 * G + 3.2877)
				+ 2.5e-4 * math.sin(w4 - w3)
				- 2.5e-4 * math.sin(l2 - 3 * l3 + 2 * l4)
				- 2.3e-4 * math.sin(3 * (l3 - l4))
				+ 2.1e-4 * math.sin(2 * l4 - 2 * LPEJ - 3 * G)
				- 2.1e-4 * math.sin(2 * l3 - 3 * l4 + p4)
				+ 1.9e-4 * math.sin(l4 - p4 - G)
				- 1.9e-4 * math.sin(2 * l4 - p3 - p4)
				- 1.8e-4 * math.sin(l4 - p4 + G)
				- 1.6e-4 * math.sin(l4 + p3 - 2 * LPEJ - 2 * G);
			sigma = CMath.PFMod(sigma, 360.0);
			sigma = math.radians(sigma);
			double L = l4 + sigma;

			//Calculate periodic terms for the tangent of the latitude
			double B =
				-7.6579e-3 * math.sin(L - psi)
				+ 4.4134e-3 * math.sin(L - w4)
				- 5.112e-4 * math.sin(L - w3)
				+ 7.73e-5 * math.sin(L + psi - 2 * LPEJ - 2 * G)
				+ 1.04e-5 * math.sin(L - psi + G)
				- 1.02e-5 * math.sin(L - psi - G)
				+ 8.8e-6 * math.sin(L + psi - 2 * LPEJ - 3 * G)
				- 3.8e-6 * math.sin(L + psi - 2 * LPEJ - G);
			B = math.atan(B);

			//Calculate the periodic terms for distance
			double R =
				-7.3546e-3 * math.cos(l4 - p4)
				+ 1.621e-4 * math.cos(l4 - p3)
				+ 9.74e-5 * math.cos(l3 - l4)
				- 5.43e-5 * math.cos(l4 + p4 - 2 * LPEJ - 2 * G)
				- 2.71e-5 * math.cos(2 * (l4 - p4))
				+ 1.82e-5 * math.cos(l4 - LPEJ)
				+ 1.77e-5 * math.cos(2 * (l3 - l4))
				- 1.67e-5 * math.cos(2 * l4 - psi - w4)
				+ 1.67e-5 * math.cos(psi - w4)
				- 1.55e-5 * math.cos(2 * (l4 - LPEJ - G))
				+ 1.42e-5 * math.cos(2 * (l4 - psi))
				+ 1.05e-5 * math.cos(l1 - l4)
				+ 9.2e-6 * math.cos(l2 - l4)
				- 8.9e-6 * math.cos(l4 - LPEJ - G)
				- 6.2e-6 * math.cos(l4 + p4 - 2 * LPEJ - 3 * G)
				+ 4.8e-6 * math.cos(2 * (l4 - w4));

			R = 26.36273 * JupiterRadius * (1 + R);

			double T = (jd - 2433282.423) / 36525.0;
			double P = 1.3966626 * T + 3.088e-4 * T * T;
			L += math.radians(P);

			L += JupAscendingNode;

			// Corrections for internal coordinate system
			B -= math.PI_DBL * 0.5;
			L += math.PI_DBL;

			double x = math.cos(L) * math.sin(B) * R;
			double y = math.cos(B) * R;
			double z = math.sin(L) * math.sin(B) * R;

			return new double3(CMath.KMtoAU(x), CMath.KMtoAU(y), CMath.KMtoAU(z));
		}
	}

	public class TitanOrbit : Orbit
	{
		public TitanOrbit() { m_Period = 15.94544758; }

		public override double3 PositionAtTime(double jd)
		{
			ComputeSaturnianElements(jd,
									  out double t1, out double t2, out double t3,
									  out double t4, out double t5, out double t6,
									  out double t7, out double t8, out double t9,
									  out double t10, out double t11,
									  out double W0, out double W1, out double W2,
									  out double W3, out double W4, out double W5,
									  out double W6, out double W7, out double W8);
			double e1 = 0.05589 - 0.000346 * t7;

			double L = 261.1582 + 22.57697855 * t4 + 0.074025 * CMath.SinD(W3);
			double i_ = 27.45141 + 0.295999 * CMath.CosD(W3);
			double Om_ = 168.66925 + 0.628808 * CMath.SinD(W3);
			double a1 = CMath.SinD(W7) * CMath.SinD(Om_ - W8);
			double a2 = CMath.CosD(W7) * CMath.SinD(i_) - CMath.SinD(W7) * CMath.CosD(i_) * CMath.CosD(Om_ - W8);
			double g0 = 102.8623;
			double psi = math.degrees(math.atan2(a1, a2));
			double s = math.sqrt(CMath.Square(a1) + CMath.Square(a2));
			double g = W4 - Om_ - psi;

			// Three successive approximations will always be enough
			double om = 0.0;
			for (int n = 0; n < 3; n++)
			{
				om = W4 + 0.37515 * (CMath.SinD(2 * g) - CMath.SinD(2 * g0));
				g = om - Om_ - psi;
			}

			double e_ = 0.029092 + 0.00019048 * (CMath.CosD(2 * g) - CMath.CosD(2 * g0));
			double q = 2 * (W5 - om);
			double b1 = CMath.SinD(i_) * CMath.SinD(Om_ - W8);
			double b2 = CMath.CosD(W7) * CMath.SinD(i_) * CMath.CosD(Om_ - W8) - CMath.SinD(W7) * CMath.CosD(i_);
			double theta = math.degrees(math.atan2(b1, b2)) + W8;
			double e = e_ + 0.002778797 * e_ * CMath.CosD(q);
			double p = om + 0.159215 * CMath.SinD(q);
			double u = 2 * W5 - 2 * theta + psi;
			double h = 0.9375 * CMath.Square(e_) * CMath.SinD(q) + 0.1875 * CMath.Square(s) * CMath.SinD(2 * (W5 - theta));
			double lam_ = L - 0.254744 * (e1 * CMath.SinD(W6) + 0.75 * CMath.Square(e1) * CMath.SinD(2 * W6) + h);
			double i = i_ + 0.031843 * s * CMath.CosD(u);
			double Om = Om_ + (0.031843 * s * CMath.SinD(u)) / CMath.SinD(i_);
			double a = 20.216193;

			OuterSaturnMoonParams(a, e, i, Om, lam_ - p, lam_,
							  out double lam, out double gam, out double r, out double w);

			return SaturnMoonPosition(lam, gam, w, r);
		}
	}

}