using CustomMath;
using AstroTime;
using System;
using Unity.Mathematics;

namespace Ephemeris
{

	[Serializable]
	public enum RotationModelType : byte
	{
		None = 0,
		Mercury,
		Venus,
		Earth,
		Mars,
		Jupiter,
		Saturn,
		Uranus,
		Neptune,
		Lunar,
		Io,
		Europa,
		Ganymede,
		Callisto,
		Titan,
		Sun
	}

	public class RotationModel
	{
		public double Period => m_Period;
		protected double m_Period;

		public virtual dQuaternion ComputeEquatorOrientation(double t) => dQuaternion.identity;
		public virtual dQuaternion ComputeSpin(double t) => dQuaternion.identity;

		public static RotationModel Create(RotationModelType type)
		{
			return type switch
			{
				RotationModelType.Mercury => new IAUPrecessingRotationModel(281.01, -0.033, 61.45, -0.005, 329.548, 6.1385025),
				RotationModelType.Venus => new IAUPrecessingRotationModel(272.76, 0.0, 67.16, 0.0, 160.20, -1.4813688),
				RotationModelType.Earth => new EarthRotationModel(), //new IAUPrecessingRotationModel(0.0, -0.641, 90.0, -0.557, 190.147, 360.9856235),
				RotationModelType.Mars => new IAUPrecessingRotationModel(317.68143, -0.1061, 52.88650, -0.0609, 176.630, 350.89198226),
				RotationModelType.Jupiter => new IAUPrecessingRotationModel(268.05, -0.009, 64.49, -0.003, 284.95, 870.5366420),
				RotationModelType.Saturn => new IAUPrecessingRotationModel(40.589, -0.036, 83.537, -0.004, 38.90, 810.7939024),
				RotationModelType.Uranus => new IAUPrecessingRotationModel(257.311, 0.0, -15.175, 0.0, 203.81, -501.1600928),
				RotationModelType.Neptune => new IAUNeptuneRotationModel(),
				RotationModelType.Lunar => new IAULunarRotationModel(),
				RotationModelType.Io => new IAUIoRotationModel(),
				RotationModelType.Europa => new IAUEuropaRotationModel(),
				RotationModelType.Ganymede => new IAUGanymedeRotationModel(),
				RotationModelType.Callisto => new IAUCallistoRotationModel(),
				RotationModelType.Titan => new IAUTitanRotationModel(),
				RotationModelType.Sun => new IAUPrecessingRotationModel(0.0, 0.0, 90.0 - 7.25, 0.0, 0.0, 12.8571426), // not based on real data!!!
				_ => new RotationModel()
			};
		}
	}

	class EarthRotationModel : RotationModel
	{
		public EarthRotationModel() { m_Period = 23.9344694 / 24.0; }

		public override dQuaternion ComputeEquatorOrientation(double jd)
		{
			double T = TimeUtil.GetJulianCentury(jd - TimeUtil.J2000);
		
			// Clamp T to the valid time range of the precession theory.
			T = math.clamp(T, -s_P03lpValidCenturies, s_P03lpValidCenturies);
			
			PrecessionAngles prec = Precession.PrecObliquity_P03LP(T);
			EclipticPole pole = Precession.EclipticPrecession_P03LP(T);

			double obliquity = math.radians(prec.epsA / 3600.0);
			double precession = math.radians(prec.pA / 3600.0);

			// Calculate the angles pi and Pi from the ecliptic pole coordinates
			// P and Q:
			//   P = sin(pi) * sin(Pi)
			//   Q = sin(pi) * cos(Pi)
			double P = pole.PA * 2.0 * math.PI_DBL / 1296000.0;
			double Q = pole.QA * 2.0 * math.PI_DBL / 1296000.0;
			double piA = math.asin(math.sqrt(P * P + Q * Q));
			double PiA = math.atan2(P, Q);

			// Calculate the rotation from the J2000 ecliptic to the ecliptic
			// of date.
			dQuaternion RPi = dQuaternion.RotateZ(PiA);
			dQuaternion rpi = dQuaternion.RotateX(piA);
			dQuaternion eclRotation = dQuaternion.mul(dQuaternion.mul(dQuaternion.conjugate(RPi), rpi), RPi);

			dQuaternion q = dQuaternion.mul(dQuaternion.mul(dQuaternion.RotateX(obliquity), dQuaternion.RotateZ(-precession)), dQuaternion.conjugate(eclRotation));

			// convert to internal coordinate system
			return dQuaternion.mul(dQuaternion.mul(dQuaternion.RotateX(math.PI_DBL * 0.5), q), dQuaternion.RotateX(-math.PI_DBL * 0.5));
		}

		public override dQuaternion ComputeSpin(double jd)
		{
			// TODO: Use a more accurate model for sidereal time
			double t = jd - TimeUtil.J2000;
			double theta = 2 * math.PI_DBL * (t * 24.0 / 23.9344694 - 259.853 / 360.0);

			return dQuaternion.RotateY(-theta);
		}

		// The P03 long period precession theory for Earth is valid for a one
		// million year time span centered on J2000. For dates outside far outside
		// that range, the polynomial terms produce absurd results.
		protected static readonly double s_P03lpValidCenturies = 5000.0;
	}

	// All IAU rotation models are in the J2000.0 Earth equatorial frame
	public abstract class IAURotationModel : RotationModel
	{
		protected bool m_Flipped;

		public IAURotationModel(double period, bool flipped = false)
		{
			m_Period = period;
			m_Flipped = flipped;
		}

		public override dQuaternion ComputeEquatorOrientation(double t)
		{
			t -= TimeUtil.J2000;

			ComputePole(t, out double poleRA, out double poleDec);

			double node = poleRA + 90.0;
			double inclination = 90.0 - poleDec;

			return dQuaternion.mul(dQuaternion.RotateY(-math.radians(node)), dQuaternion.RotateX(-math.radians(inclination)));
		}
		public override dQuaternion ComputeSpin(double t)
		{
			double jd = t - TimeUtil.J2000;
			double meridian = ComputeMeridian(jd);
			return dQuaternion.RotateY((m_Flipped ? 1.0 : -1.0) * math.radians(180.0 + meridian));
		}

		protected abstract void ComputePole(double jd, out double ra, out double dec);
		protected abstract double ComputeMeridian(double jd);

		// Clamp secular terms in IAU rotation models to this number of centuries
		// from J2000. Extrapolating much further can lead to ridiculous results,
		// such as planets 'tipping over' Periodic terms are not clamped; their
		// validity over long time ranges is questionable, but extrapolating them
		// doesn't produce obviously absurd results.
		protected static readonly double s_SecularTermValidCenturies = 50;
		protected double ClampCenturies(double t) => math.clamp(t, -s_SecularTermValidCenturies, s_SecularTermValidCenturies);
	}

	public class IAUPrecessingRotationModel : IAURotationModel
	{
		protected double m_PoleRA;
		protected double m_PoleRARate;
		protected double m_PoleDec;
		protected double m_PoleDecRate;
		protected double m_MeridianAtEpoch;
		protected double m_RotationRate;

		public IAUPrecessingRotationModel(double poleRA, double poleRARate, double poleDec, double poleDecRate, double meridianAtEpoch, double rotationRate)
			: base(math.abs(360.0 / rotationRate), rotationRate < 0.0)
		{
			m_PoleRA = poleRA;
			m_PoleRARate = poleRARate;
			m_PoleDec = poleDec;
			m_PoleDecRate = poleDecRate;
			m_MeridianAtEpoch = meridianAtEpoch;
			m_RotationRate = rotationRate;
		}

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double t = TimeUtil.GetJulianCentury(jd);
			ClampCenturies(t);
			ra = m_PoleRA + m_PoleRARate * t;
			dec = m_PoleDec + m_PoleDecRate * t;
		}

		protected override double ComputeMeridian(double jd) => m_MeridianAtEpoch + m_RotationRate * jd;
	}

	public class IAUNeptuneRotationModel : IAURotationModel
	{
		public IAUNeptuneRotationModel() : base(360.0 / 536.3128492) {}

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double t = TimeUtil.GetJulianCentury(jd);
			double N = math.radians(357.85 + 52.316 * t);
			ra = 299.36 + 0.70 * math.sin(N);
			dec = 43.46 - 0.51 * math.cos(N);
		}

		protected override double ComputeMeridian(double jd)
		{
			double t = TimeUtil.GetJulianCentury(jd);
			double N = math.radians(357.85 + 52.316 * t);
			return 253.18 + 536.3128492 * jd - 0.48 * math.sin(N);
		}
	}

	public class IAULunarRotationModel : IAURotationModel
	{
		public IAULunarRotationModel() : base(360.0 / 13.17635815) {}

		private void CalcArgs(double d,
			out double E1, out double E2, out double E3, out double E4, out double E5, out double E6, out double E7,
			out double E8, out double E9, out double E10, out double E11, out double E12, out double E13)
		{
			E1  = math.radians(125.045 -  0.0529921 * d);
			E2  = math.radians(250.089 -  0.1059842 * d);
			E3  = math.radians(260.008 + 13.0120090 * d);
			E4  = math.radians(176.625 + 13.3407154 * d);
			E5  = math.radians(357.529 +  0.9856993 * d);
			E6  = math.radians(311.589 + 26.4057084 * d);
			E7  = math.radians(134.963 + 13.0649930 * d);
			E8  = math.radians(276.617 +  0.3287146 * d);
			E9  = math.radians( 34.226 +  1.7484877 * d);
			E10 = math.radians( 15.134 -  0.1589763 * d);
			E11 = math.radians(119.743 +  0.0036096 * d);
			E12 = math.radians(239.961 +  0.1643573 * d);
			E13 = math.radians( 25.053 + 12.9590088 * d);
		}

	protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			ClampCenturies(T);

			CalcArgs(jd,
				out double E1, out double E2, out double E3, out double E4, out double E5, out double E6, out double E7,
				out double E8, out double E9, out double E10, out double E11, out double E12, out double E13);

			ra = 269.9949
				+ 0.0013 * T
				- 3.8787 * math.sin(E1)
				- 0.1204 * math.sin(E2)
				+ 0.0700 * math.sin(E3)
				- 0.0172 * math.sin(E4)
				+ 0.0072 * math.sin(E6)
				- 0.0052 * math.sin(E10)
				+ 0.0043 * math.sin(E13);

			dec = 66.5392
				+ 0.0130 * T
				+ 1.5419 * math.cos(E1)
				+ 0.0239 * math.cos(E2)
				- 0.0278 * math.cos(E3)
				+ 0.0068 * math.cos(E4)
				- 0.0029 * math.cos(E6)
				+ 0.0009 * math.cos(E7)
				+ 0.0008 * math.cos(E10)
				- 0.0009 * math.cos(E13);
		}

		protected override double ComputeMeridian(double jd)
		{
			CalcArgs(jd,
				out double E1, out double E2, out double E3, out double E4, out double E5, out double E6, out double E7,
				out double E8, out double E9, out double E10, out double E11, out double E12, out double E13);

			// d^2 represents slowing of lunar rotation as the Moon recedes
			// from the Earth. This may need to be clamped at some very large
			// time range (1 Gy?)

			return 38.3213
					+ 13.17635815 * jd
					- 1.4e-12 * jd * jd
					+ 3.5610 * math.sin(E1)
					+ 0.1208 * math.sin(E2)
					- 0.0642 * math.sin(E3)
					+ 0.0158 * math.sin(E4)
					+ 0.0252 * math.sin(E5)
					- 0.0066 * math.sin(E6)
					- 0.0047 * math.sin(E7)
					- 0.0046 * math.sin(E8)
					+ 0.0028 * math.sin(E9)
					+ 0.0052 * math.sin(E10)
					+ 0.0040 * math.sin(E11)
					+ 0.0019 * math.sin(E12)
					- 0.0044 * math.sin(E13);
		}
	}

	public class IAUIoRotationModel : IAURotationModel
	{
		public IAUIoRotationModel() : base(360.0 / 203.4889538) { }

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J3 = math.radians(283.90 + 4850.7 * T);
			double J4 = math.radians(355.80 + 1191.3 * T);
			ClampCenturies(T);
			ra = 268.05 - 0.009 * T + 0.094 * math.sin(J3) + 0.024 * math.sin(J4);
			dec = 64.49 + 0.003 * T + 0.040 * math.cos(J3) + 0.011 * math.cos(J4);
		}

		protected override double ComputeMeridian(double jd)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J3 = math.radians(283.90 + 4850.7 * T);
			double J4 = math.radians(355.80 + 1191.3 * T);
			return 200.39 + 203.4889538 * jd - 0.085 * math.sin(J3) - 0.022 * math.sin(J4);
		}
	}

	public class IAUEuropaRotationModel : IAURotationModel
	{
		public IAUEuropaRotationModel() : base(360.0 / 101.3747235) { }

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J4 = math.radians(355.80 + 1191.3 * T);
			double J5 = math.radians(119.90 + 262.1 * T);
			double J6 = math.radians(229.80 + 64.3 * T);
			double J7 = math.radians(352.35 + 2382.6 * T);
			ClampCenturies(T);
			ra = 268.05 - 0.009 * T + 1.086 * math.sin(J4) + 0.060 * math.sin(J5) + 0.015 * math.sin(J6) + 0.009 * math.sin(J7);
			dec = 64.49 + 0.003 * T + 0.486 * math.cos(J4) + 0.026 * math.cos(J5) + 0.007 * math.cos(J6) + 0.002 * math.cos(J7);
		}

		protected override double ComputeMeridian(double jd)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J4 = math.radians(355.80 + 1191.3 * T);
			double J5 = math.radians(119.90 + 262.1 * T);
			double J6 = math.radians(229.80 + 64.3 * T);
			double J7 = math.radians(352.35 + 2382.6 * T);
			return 36.022 + 101.3747235 * jd - 0.980 * math.sin(J4) - 0.054 * math.sin(J5) - 0.014 * math.sin(J6) - 0.008 * math.sin(J7);
		}
	}

	public class IAUGanymedeRotationModel : IAURotationModel
	{
		public IAUGanymedeRotationModel() : base(360.0 / 50.3176081) { }

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J4 = math.radians(355.80 + 1191.3 * T);
			double J5 = math.radians(119.90 + 262.1 * T);
			double J6 = math.radians(229.80 + 64.3 * T);
			ClampCenturies(T);
			ra = 268.05 - 0.009 * T - 0.037 * math.sin(J4) + 0.431 * math.sin(J5) + 0.091 * math.sin(J6);
			dec = 64.49 + 0.003 * T - 0.016 * math.cos(J4) + 0.186 * math.cos(J5) + 0.039 * math.cos(J6);
		}

		protected override double ComputeMeridian(double jd)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J4 = math.radians(355.80 + 1191.3 * T);
			double J5 = math.radians(119.90 + 262.1 * T);
			double J6 = math.radians(229.80 + 64.3 * T);
			return 44.064 + 50.3176081 * jd + 0.033 * math.sin(J4) - 0.389 * math.sin(J5) - 0.082 * math.sin(J6);
		}
	}

	public class IAUCallistoRotationModel : IAURotationModel
	{
		public IAUCallistoRotationModel() : base(360.0 / 21.5710715) { }

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J5 = math.radians(119.90 + 262.1 * T);
			double J6 = math.radians(229.80 + 64.3 * T);
			double J8 = math.radians(113.35 + 6070.0 * T);
			ClampCenturies(T);
			ra = 268.05 - 0.009 * T - 0.068 * math.sin(J5) + 0.590 * math.sin(J6) + 0.010 * math.sin(J8);
			dec = 64.49 + 0.003 * T - 0.029 * math.cos(J5) + 0.254 * math.cos(J6) - 0.004 * math.cos(J8);
		}

		protected override double ComputeMeridian(double jd)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double J5 = math.radians(119.90 + 262.1 * T);
			double J6 = math.radians(229.80 + 64.3 * T);
			double J8 = math.radians(113.35 + 6070.0 * T);
			return 259.51 + 21.5710715 * jd + 0.061 * math.sin(J5) - 0.533 * math.sin(J6) - 0.009 * math.sin(J8);
		}
	}

	public class IAUTitanRotationModel : IAURotationModel
	{
		public IAUTitanRotationModel() : base(360.0 / 22.5769768) { }

		protected override void ComputePole(double jd, out double ra, out double dec)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double S8 = math.radians(29.80 - 52.1 * T);
			ClampCenturies(T);
			ra = 36.41 - 0.036 * T + 2.66 * math.sin(S8);
			dec = 83.94 - 0.004 * T - 0.30 * math.cos(S8);
		}

		protected override double ComputeMeridian(double jd)
		{
			double T = TimeUtil.GetJulianCentury(jd);
			double S8 = math.radians(29.80 - 52.1 * T);
			return 189.64 + 22.5769768 * jd - 2.64 * math.sin(S8);
		}
	}

}