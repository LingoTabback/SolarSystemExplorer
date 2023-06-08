using Unity.Mathematics;

namespace Ephemeris
{
	// PA and QA are the location of the pole of the ecliptic of date
	// with respect to the fixed ecliptic of J2000.0
	public struct EclipticPole
	{
		public double PA;
		public double QA;
		public EclipticPole(double pa, double qa) { PA = pa; QA = qa; }
	};

	// piA and PiA are angles that transform the J2000 ecliptic to the
	// ecliptic of date. They are related to the ecliptic pole coordinates
	// PA and QA:
	//   PA = sin(piA) * sin(PiA)
	//   QA = sin(piA) * cos(PiA)
	//
	// PiA is the angle along the J2000 ecliptic between the J2000 equinox
	// and the intersection of the J2000 ecliptic and ecliptic of date.
	public struct EclipticAngles
	{
		public double piA;
		public double PiA;
		public EclipticAngles(double piA, double PiA) { this.piA = piA; this.PiA = PiA; }
	};

	// epsA is the angle between the ecliptic and mean equator of date. pA is the
	// general precession: the difference between angles L and PiA. L is the angle
	// along the mean ecliptic of date from the equinox of date to the
	// intersection of the J2000 ecliptic and ecliptic of date.
	public struct PrecessionAngles
	{
		public double pA;     // precession
		public double epsA;   // obliquity
		public PrecessionAngles(double pA, double epsA) { this.pA = pA; this.epsA = epsA; }
	}

	public struct EquatorialPrecessionAngles
	{
		public double zetaA;
		public double zA;
		public double thetaA;
		public EquatorialPrecessionAngles(double zetaA, double zA, double thetaA)
		{
			this.zetaA = zetaA;
			this.zA = zA;
			this.thetaA = thetaA;
		}
	}

	public static class Precession
	{
		// Periodic term for the long-period extension of the P03 precession
		// model.
		private struct EclipticPrecessionTerm
		{
			public double Pc;
			public double Qc;
			public double Ps;
			public double Qs;
			public double Period;

			public EclipticPrecessionTerm(double pc, double qc, double ps, double qs, double period)
			{
				Pc = pc;
				Qc = qc;
				Ps = ps;
				Qs = qs;
				Period = period;
			}
		}

		private static readonly EclipticPrecessionTerm[] s_EclipticPrecessionTerms =
		{
			new EclipticPrecessionTerm(  486.230527, 2559.065245, -2578.462809,   485.116645, 2308.98),
			new EclipticPrecessionTerm( -963.825784,  247.582718,  -237.405076,  -971.375498, 1831.25),
			new EclipticPrecessionTerm(-1868.737098, -957.399054,  1007.593090, -1930.464338,  687.52),
			new EclipticPrecessionTerm(-1589.172175,  493.021354,  -423.035168, -1634.905683,  729.97),
			new EclipticPrecessionTerm(  429.442489, -328.301413,   337.266785,   429.594383,  492.21),
			new EclipticPrecessionTerm(-2244.742029, -339.969833,   221.240093, -2131.745072,  708.13)
		};

		// Periodic term for the long-period extension of the P03 precession
		// model.
		struct PrecessionTerm
		{
			public double pc;
			public double epsc;
			public double ps;
			public double epss;
			public double Period;

			public PrecessionTerm(double pc, double epsc, double ps, double epss, double period)
			{
				this.pc = pc;
				this.epsc = epsc;
				this.ps = ps;
				this.epss = epss;
				Period = period;
			}
		}

		private static readonly PrecessionTerm[] s_PrecessionTerms =
		{
			new PrecessionTerm(-6180.062400,   807.904635, -2434.845716, -2056.455197,  409.90),
			new PrecessionTerm(-2721.869299,  -177.959383,   538.034071,  -912.727303,  396.15),
			new PrecessionTerm( 1460.746498,   371.942696, -1245.689351,   447.710000,  536.91),
			new PrecessionTerm(-1838.488899,  -176.029134,   529.220775,  -611.297411,  402.90),
			new PrecessionTerm(  949.518077,   -89.154030,   277.195375,   315.900626,  417.15),
			new PrecessionTerm(   32.701460,  -336.048179,   945.979710,    12.390157,  288.92),
			new PrecessionTerm(  598.054819,   -17.415730,  -955.163661,   -15.922155, 4042.97),
			new PrecessionTerm( -293.145284,   -28.084479,    93.894079,  -102.870153,  304.90),
			new PrecessionTerm(   66.354942,    21.456146,     0.671968,    24.123484,  281.46),
			new PrecessionTerm(   18.894136,    30.917011,  -184.663935,     2.512708,  204.38)
		};

		// DE405 obliquity of the ecliptic
		private static readonly double s_Eps0 = 84381.40889;

		/*! Compute the precession of the ecliptic, based on a long-period
		 *  extension of the the P03 model, presented in "Long-periodic Precession
		 *  Parameters", J. Vondrak (2006)
		 *  For an explanation of the angles used in the P03 model, see
		 *  "Expressions for IAU2000 precession quantities", N. Capitaine et al,
		 *  Astronomy & Astrophysics, v.412, p.567-586 (2003).
		 *
		 *  Also: "Expressions for the Precession Quantities", J. H. Lieske et al,
		 *  Astronomy & Astrophysics, v.58, p. 1-16 (1977).
		 *
		 *  6 long-periodic terms, plus a cubic polynomial for longer terms.
		 *  The terms are fitted to the P03 model withing 1000 years of J2000.
		 *
		 *  T is the time in centuries since J2000. The angles returned are
		 *  in arcseconds.
		 */
		public static EclipticPole EclipticPrecession_P03LP(double T)
		{
			EclipticPole pole;

			double T2 = T * T;
			double T3 = T2 * T;

			pole.PA = (5750.804069
					   + 0.1948311 * T
					   - 0.00016739 * T2
					   - 4.8e-8 * T3);
			pole.QA = (-1673.999018
					   + 0.3474459 * T
					   + 0.00011243 * T2
					   - 6.4e-8 * T3);

			foreach (EclipticPrecessionTerm p in s_EclipticPrecessionTerms)
	{
				double theta = 2.0 * math.PI_DBL * T / p.Period;
				math.sincos(theta, out double s, out double c);
				pole.PA += p.Pc * c + p.Ps * s;
				pole.QA += p.Qc * c + p.Qs * s;
			}

			return pole;
		}

		/*! Compute the precession of the ecliptic, based on a long-period
		 *  extension of the the P03 model, presented in "Long-periodic Precession
		 *  Parameters", J. Vondrak (2006)
		 *  For an explanation of the angles used in the P03 model, see
		 *  "Expressions for IAU2000 precession quantities", N. Capitaine et al,
		 *  Astronomy & Astrophysics, v.412, p.567-586 (2003).
		 *
		 *  Also: "Expressions for the Precession Quantities", J. H. Lieske et al,
		 *  Astronomy & Astrophysics, v.58, p. 1-16 (1977).
		 *
		 *  6 long-periodic terms, plus a cubic polynomial for longer terms.
		 *  The terms are fitted to the P03 model withing 1000 years of J2000.
		 *
		 *  T is the time in centuries since J2000. The angles returned are
		 *  in arcseconds.
		*/
		public static PrecessionAngles PrecObliquity_P03LP(double T)
		{
			PrecessionAngles angles;

			double T2 = T * T;
			double T3 = T2 * T;

			angles.pA = (7907.295950 + 5044.374034 * T - 0.00713473 * T2 + 6e-9 * T3);
			angles.epsA = (83973.876448 - 0.0425899 * T - 0.00000113 * T2);

			foreach (PrecessionTerm p in s_PrecessionTerms)
			{
				double theta = 2.0 * math.PI_DBL * T / p.Period;
				math.sincos(theta, out double s, out double c);
				angles.pA += p.pc * c + p.ps * s;
				angles.epsA += p.epsc * c + p.epss * s;
			}

			return angles;
		}

		/*! Compute equatorial precession angles z, zeta, and theta using the P03
		 *  precession model.
		 */
		public static EquatorialPrecessionAngles EquatorialPrecessionAngles_P03(double T)
		{
			EquatorialPrecessionAngles prec;
			double T2 = T * T;
			double T3 = T2 * T;
			double T4 = T3 * T;
			double T5 = T4 * T;

			prec.zetaA = (2.650545
						   + 2306.083227 * T
						   + 0.2988499 * T2
						   + 0.01801828 * T3
						   - 0.000005971 * T4
						   - 0.0000003173 * T5);
			prec.zA = (-2.650545
							+ 2306.077181 * T
							+ 1.0927348 * T2
							+ 0.01826837 * T3
							- 0.000028596 * T4
							- 0.0000002904 * T5);
			prec.thetaA = (2004.191903 * T
						   - 0.4294934 * T2
						   - 0.04182264 * T3
						   - 0.000007089 * T4
						   - 0.0000001274 * T5);

			return prec;
		}

		/*! Compute the ecliptic pole coordinates PA and QA using the P03 precession
		 *  model. The quantities PA and QA are coordinates, but they are given in
		 *  units of arcseconds in P03. They should be divided by 1296000/2pi.
		 */
		public static EclipticPole EclipticPrecession_P03(double T)
		{
			EclipticPole pole;
			double T2 = T * T;
			double T3 = T2 * T;
			double T4 = T3 * T;
			double T5 = T4 * T;

			pole.PA = (4.199094 * T
					  + 0.1939873 * T2
					  - 0.00022466 * T3
					  - 0.000000912 * T4
					  + 0.0000000120 * T5);
			pole.QA = (-46.811015 * T
					  + 0.0510283 * T2
					  + 0.00052413 * T3
					  - 0.00000646 * T4
					  - 0.0000000172 * T5);

			return pole;
		}

		/*! Calculate the angles of the ecliptic of date with respect to
		 *  the J2000 ecliptic using the P03 precession model.
		 */
		public static EclipticAngles EclipticPrecessionAngles_P03(double T)
		{
			EclipticAngles ecl;
			double T2 = T * T;
			double T3 = T2 * T;
			double T4 = T3 * T;
			double T5 = T4 * T;

			ecl.piA = (46.998973 * T
					   - 0.0334926 * T2
					   - 0.00012559 * T3
					   + 0.000000113 * T4
					   - 0.0000000022 * T5);
			ecl.PiA = (629546.7936
						- 867.95758 * T
						+ 0.157992 * T2
						- 0.0005371 * T3
						- 0.00004797 * T4
						+ 0.000000072 * T5);

			return ecl;
		}

		/*! Compute the general precession and obliquity using the P03
 *  precession model. See PrecObliquity_P03LP for more details.
 */
		public static PrecessionAngles PrecObliquity_P03(double T)
		{
			PrecessionAngles prec;
			double T2 = T * T;
			double T3 = T2 * T;
			double T4 = T3 * T;
			double T5 = T4 * T;

			prec.epsA = (s_Eps0
						- 46.836769 * T
						- 0.0001831 * T2
						+ 0.00200340 * T3
						- 0.000000576 * T4
						- 0.0000000434 * T5);
			prec.pA = (5028.796195 * T
						+ 1.1054348 * T2
						+ 0.00007964 * T3
						- 0.000023857 * T4
						- 0.0000000383 * T5);
#if false
			prec.chiA = (  10.556403 * T
						-  2.3814292 * T2
						-  0.00121197 * T3
						+  0.000170663 * T4
						-  0.0000000560 * T5);
#endif

			return prec;
		}

	}
}