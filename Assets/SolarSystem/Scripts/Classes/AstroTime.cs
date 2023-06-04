using System;
using Unity.Mathematics;

namespace AstroTime
{
	[Serializable]
	public struct Date
	{
		public int Year => m_Year;
		public int Month => m_Month;
		public int Day => m_Day;
		public int Hour => m_Hour;
		public int Minute => m_Minute;
		public double Seconds => m_Seconds;
		public int UtcOffset => m_UtcOffset;
		public int WeekDay => m_WDay;
		public string TimeZoneName => m_TZName;

		public Date(int y, int m, int d)
		{
			m_Year = y;
			m_Month = m;
			m_Day = d;
			m_Hour = 0;
			m_Minute = 0;
			m_Seconds = 0;
			m_WDay = 0;
			m_UtcOffset = 0;
			m_TZName = "UTC";
		}

		public Date(int y, int mon, int d, int h, int min, double s)
		{
			m_Year = y;
			m_Month = mon;
			m_Day = d;
			m_Hour = h;
			m_Minute = min;
			m_Seconds = s;
			m_WDay = 0;
			m_UtcOffset = 0;
			m_TZName = "UTC";
		}

		public Date(double jd)
		{
			var a = (long)math.floor(jd + 0.5);
			m_WDay = (int)((a + 1) % 7);
			double c;
			if (a < 2299161)
				c = (double)(a + 1524);
			else
			{
				double b = (double)((long)math.floor((a - 1867216.25) / 36524.25));
				c = a + b - (long)math.floor(b / 4) + 1525;
			}

			var d = (long)math.floor((c - 122.1) / 365.25);
			var e = (long)math.floor(365.25 * d);
			var f = (long)math.floor((c - e) / 30.6001);

			double dday = c - e - (long)math.floor(30.6001 * f) + ((jd + 0.5) - a);

			m_Month = (int)(f - 1 - 12 * (long)(f / 14));
			m_Year = (int)(d - 4715 - (long)((7.0 + m_Month) / 10.0));
			m_Day = (int)dday;

			double dhour = (dday - m_Day) * 24;
			m_Hour = (int)dhour;

			double dminute = (dhour - m_Hour) * 60;
			m_Minute = (int)dminute;

			m_Seconds = (dminute - m_Minute) * 60;
			m_UtcOffset = 0;
			m_TZName = "UTC";
		}

		public static Date WithLeapSeconds(double jd, int leapSeconds)
		{
			Date date = new(jd);
			date.m_Seconds += leapSeconds;
			return date;
		}

		// Convert a calendar date to a Julian date
		public double Julian
		{
			get
			{
				int y = m_Year, m = m_Month;
				if (m_Month <= 2)
				{
					y = m_Year - 1;
					m = m_Month + 12;
				}

				// Correct for the lost days in Oct 1582 when the Gregorian calendar
				// replaced the Julian calendar.
				int B = -2;
				if (m_Year > 1582 || (m_Year == 1582 && (m_Month > 10 || (m_Month == 10 && m_Day >= 15))))
					B = y / 400 - y / 100;

				return (math.floor(365.25 * y) + math.floor(30.6001 * (m + 1)) + B + 1720996.5 +
					m_Day + m_Hour / s_HoursPerDay + m_Minute / s_MinutesPerDay + m_Seconds / s_SecondsPerDay);
			}
		}

		public static Date Now
		{
			get
			{
				Date date = new(0, 0, 0);
				var dateTime = DateTime.UtcNow;
				date.m_Year = dateTime.Year;
				date.m_Month = dateTime.Month;
				date.m_Day = dateTime.Day;
				date.m_Hour = dateTime.Hour;
				date.m_Minute = dateTime.Minute;
				date.m_Seconds = dateTime.Second;
				return date;
			}
		}

		private int m_Year;
		private int m_Month;
		private int m_Day;
		private int m_Hour;
		private int m_Minute;
		private int m_WDay;      // week day, 0 Sunday to 6 Saturday
		private int m_UtcOffset; // offset from utc in seconds
		private string m_TZName; // timezone name
		private double m_Seconds;

		// Julian year
		//private static readonly double s_DaysPerYear = 365.25;
		private static readonly double s_SecondsPerDay = 86400.0;
		private static readonly double s_MinutesPerDay = 1440.0;
		private static readonly double s_HoursPerDay = 24.0;
	}

	public static class TimeUtil
	{
		// Time scale conversions
		// UTC - Coordinated Universal Time
		// TAI - International Atomic Time
		// TT  - Terrestrial Time
		// TCB - Barycentric Coordinate Time
		// TDB - Barycentric Dynamical Time

		public static readonly double J2000 = 2451545.0;
		public static readonly double J1900 = 2415020.0;

		public static double SecsToDays(double s)
		{
			return s * (1.0 / s_SecondsPerDay);
		}

		public static double DaysToSecs(double d)
		{
			return d * s_SecondsPerDay;
		}

		public static double GetJulianCentury(double jd) => jd / 36525.0;
		public static double GetJulianMillenium(double jd) => jd / 365250.0;

		/********* Time scale conversion functions ***********/

		// Convert from Atomic Time to UTC
		public static Date TAItoUTC(double tai)
		{
			double dAT = s_LeapSeconds[0].Seconds;
			int extraSecs = 0;

			for (int i = s_LeapSeconds.Length - 1; i > 0; --i)
			{
				if (tai - SecsToDays(s_LeapSeconds[i].Seconds) >= s_LeapSeconds[i].T)
				{
					dAT = s_LeapSeconds[i].Seconds;
					break;
				}
				if (tai - SecsToDays(s_LeapSeconds[i - 1].Seconds) >= s_LeapSeconds[i].T)
				{
					dAT = s_LeapSeconds[i].Seconds;
					extraSecs = s_LeapSeconds[i].Seconds - s_LeapSeconds[i - 1].Seconds;
					break;
				}
			}

			return Date.WithLeapSeconds(tai - SecsToDays(dAT), extraSecs);
		}

		// Convert from UTC to Atomic Time
		public static double UTCtoTAI(in Date utc)
		{
			double dAT = s_LeapSeconds[0].Seconds;
			double utcjd = new Date(utc.Year, utc.Month, utc.Day).Julian;

			for (int i = s_LeapSeconds.Length - 1; i > 0;--i)
			{
				if (utcjd >= s_LeapSeconds[i].T)
				{
					dAT = s_LeapSeconds[i].Seconds;
					break;
				}
			}

			double tai = utcjd + SecsToDays(utc.Hour * 3600.0 + utc.Minute * 60.0 + utc.Seconds + dAT);

			return tai;
		}

		// Convert from Terrestrial Time to Atomic Time
		public static double TTtoTAI(double tt) => tt - SecsToDays(s_DTA);
		// Convert from Atomic Time to Terrestrial TIme
		public static double TAItoTT(double tai) => tai + SecsToDays(s_DTA);

		// Convert from Terrestrial Time to Barycentric Dynamical Time
		public static double TTtoTDB(double tt) => tt + SecsToDays(TDBCorrection(tt));
		// Convert from Barycentric Dynamical Time to Terrestrial Time
		public static double TDBtoTT(double tdb) => tdb - SecsToDays(TDBCorrection(tdb));

		// Convert from Barycentric Dynamical Time to Coordinated Universal Time
		public static Date TDBtoUTC(double tdb) => TAItoUTC(TTtoTAI(TDBtoTT(tdb)));
		// Convert from UTC to Barycentric Dynamical Time
		public static double UTCtoTDB(in Date utc) => TTtoTDB(TAItoTT(UTCtoTAI(utc)));

		// Convert from TAI to Julian Date UTC. The Julian Date UTC functions should
		// generally be avoided because there's no provision for dealing with leap
		// seconds.
		public static double JDUTCtoTAI(double utc)
		{
			double dAT = s_LeapSeconds[0].Seconds;

			for (int i = s_LeapSeconds.Length - 1; i > 0; --i)
			{
				if (utc > s_LeapSeconds[i].T)
				{
					dAT = s_LeapSeconds[i].Seconds;
					break;
				}
			}

			return utc + SecsToDays(dAT);
		}

		// Convert from Julian Date UTC to TAI
		public static double TAItoJDUTC(double tai)
		{
			double dAT = s_LeapSeconds[0].Seconds;

			for (int i = s_LeapSeconds.Length - 1; i > 0; --i)
			{
				if (tai - SecsToDays(s_LeapSeconds[i - 1].Seconds) > s_LeapSeconds[i].T)
				{
					dAT = s_LeapSeconds[i].Seconds;
					break;
				}
			}

			return tai - SecsToDays(dAT);
		}

		// Correction for converting from Terrestrial Time to Barycentric Dynamical
		// Time. Constants and algorithm from "Time Routines in CSPICE"
		private static readonly double s_K = 1.657e-3;
		private static readonly double s_EB = 1.671e-2;
		private static readonly double s_M0 = 6.239996;
		private static readonly double s_M1 = 1.99096871e-7;

		private static readonly double s_SecondsPerDay = 86400.0;
		//private static readonly double s_MinutesPerDay = 1440.0;
		//private static readonly double s_HoursPerDay = 24.0;

		// Input is a TDB Julian Date; result is in seconds
		private static double TDBCorrection(double tdb)
		{
			// t is seconds from J2000.0
			double t = DaysToSecs(tdb - J2000);
			// Approximate calculation of Earth's mean anomaly
			double M = s_M0 + s_M1 * t;
			// Compute the eccentric anomaly
			double E = M + s_EB * math.sin(M);

			return s_K * math.sin(E);
		}

		// Difference in seconds between Terrestrial Time and International Atomic Time
		private static readonly double s_DTA = 32.184;

		private struct LeapSecondRecord
		{
			public int Seconds;
			public double T;
			public LeapSecondRecord(int s, double t) { Seconds = s; T = t; }
		}

		private static readonly LeapSecondRecord[] s_LeapSeconds =
		{
			new LeapSecondRecord(10, 2441317.5), // 1 Jan 1972
			new LeapSecondRecord(11, 2441499.5), // 1 Jul 1972
			new LeapSecondRecord(12, 2441683.5), // 1 Jan 1973
			new LeapSecondRecord(13, 2442048.5), // 1 Jan 1974
			new LeapSecondRecord(14, 2442413.5), // 1 Jan 1975
			new LeapSecondRecord(15, 2442778.5), // 1 Jan 1976
			new LeapSecondRecord(16, 2443144.5), // 1 Jan 1977
			new LeapSecondRecord(17, 2443509.5), // 1 Jan 1978
			new LeapSecondRecord(18, 2443874.5), // 1 Jan 1979
			new LeapSecondRecord(19, 2444239.5), // 1 Jan 1980
			new LeapSecondRecord(20, 2444786.5), // 1 Jul 1981
			new LeapSecondRecord(21, 2445151.5), // 1 Jul 1982
			new LeapSecondRecord(22, 2445516.5), // 1 Jul 1983
			new LeapSecondRecord(23, 2446247.5), // 1 Jul 1985
			new LeapSecondRecord(24, 2447161.5), // 1 Jan 1988
			new LeapSecondRecord(25, 2447892.5), // 1 Jan 1990
			new LeapSecondRecord(26, 2448257.5), // 1 Jan 1991
			new LeapSecondRecord(27, 2448804.5), // 1 Jul 1992
			new LeapSecondRecord(28, 2449169.5), // 1 Jul 1993
			new LeapSecondRecord(29, 2449534.5), // 1 Jul 1994
			new LeapSecondRecord(30, 2450083.5), // 1 Jan 1996
			new LeapSecondRecord(31, 2450630.5), // 1 Jul 1997
			new LeapSecondRecord(32, 2451179.5), // 1 Jan 1999
			new LeapSecondRecord(33, 2453736.5), // 1 Jan 2006
			new LeapSecondRecord(34, 2454832.5), // 1 Jan 2009
			new LeapSecondRecord(35, 2456109.5), // 1 Jul 2012
			new LeapSecondRecord(36, 2457204.5), // 1 Jul 2015
			new LeapSecondRecord(37, 2457754.5), // 1 Jan 2017
		};
	}

	/*[Serializable]
	public struct JulianDate
	{
		private long m_Ticks;
		private double m_JulianDay;
		private double m_Epoch;

		public double DayNumber => GetDayNumber(m_JulianDay);
		public double JulianDay => m_JulianDay;
		public double Epoch => m_Epoch;
		public long Ticks => m_Ticks;

		public static readonly double J2000 = 2451545.0;
		public static readonly double J1900 = 2415020.0;

		public static JulianDate FromDays(double jd, double epoch)
		{
			JulianDate date = new()
			{
				m_JulianDay = jd,
				m_Epoch = epoch,
				m_Ticks = JulianToDate(jd).Ticks
			};
			return date;
		}

		public static JulianDate FromTicks(long ticks, double epoch)
		{
			double jd = MillisToJulian(ticks / 10000.0);

			JulianDate date = new()
			{
				m_JulianDay = jd,
				m_Epoch = epoch,
				m_Ticks = JulianToDate(jd).Ticks
			};
			return date;
		}

		public static double DateToJulian(in DateTime date) => DateToJulian(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond);

		public static double DateToJulian(int year, int month, int day, int hour, int minute, int second, double millisecond, int tz = 0)
		{
			double dayDecimal, julianDay, a;

			dayDecimal = day + (hour - tz + (minute + second / 60.0 + millisecond / 1000 / 60) / 60.0) / 24.0;

			if (month < 3)
			{
				month += 12;
				year--;
			}

			julianDay = math.floor(365.25 * (year + 4716.0)) + math.floor(30.6001 * (month + 1)) + dayDecimal - 1524.5;
			if (julianDay > 2299160.0)
			{
				a = math.floor(year / 100.0);
				julianDay += (2 - a + math.floor(a / 4));
			}

			return julianDay;
		}

		public static double MillisToJulian(double millis)
		{
			DateTime date = new((long)(millis * 10000), DateTimeKind.Utc);
			return DateToJulian(date);
		}

		public static double JulianNow(bool zeroTime = false)
		{
			var d = DateTime.UtcNow;

			int h = (zeroTime == true) ? 0 : d.Hour;
			int m = (zeroTime == true) ? 0 : d.Minute;
			int s = (zeroTime == true) ? 0 : d.Second;
			int ms = (zeroTime == true) ? 0 : d.Millisecond;

			return DateToJulian(d.Year,
								d.Month,
								d.Day,
								h,
								m,
								s,
								ms);
		}

		public static DateTime JulianToDate(double jd)
		{
			jd += 0.5;
			double z = math.floor(jd);
			double f = jd - z;
			double A = 0;
			if (z < 2299161)
				A = z;
			else
			{
				double omega = math.floor((z - 1867216.25) / 36524.25);
				A = z + 1 + omega - math.floor(omega / 4);
			}
			double B = A + 1524;
			double C = math.floor((B - 122.1) / 365.25);
			double D = math.floor(365.25 * C);
			double Epsilon = math.floor((B - D) / 30.6001);
			double dayGreg = B - D - math.floor(30.6001 * Epsilon) + f;
			double monthGreg, yearGreg;
			if (Epsilon < 14)
				monthGreg = Epsilon - 1;
			else
				monthGreg = Epsilon - 13;
			if (monthGreg > 2)
				yearGreg = C - 4716;
			else
				yearGreg = C - 4715;

			var year = yearGreg;
			var month = monthGreg;
			var day = math.floor(dayGreg);

			var dayMinutes = ((dayGreg - day) * 1440.0);
			var hour = math.floor(dayMinutes / 60.0);
			var minute = math.floor(dayMinutes - (hour * 60.0));
			var second = math.round(60.0 * (dayMinutes - (hour * 60.0) - minute));
			//var millisecond = 0.0;//(1000.0 * (60.0 * (dayMinutes - (hour * 60.0) -minute)- second) );

			return new DateTime(TicksFromDate((int)year, (int)month, (int)day, (int)hour, (int)minute, second), DateTimeKind.Utc);
		}

		private static int Div(int a, int b) => ((a - a % b) / b);

		public static double GetDayNumber(double jd)
		{
			var date = JulianToDate(jd);
			var dd = date.Day;
			var mm = date.Month;
			var yyyy = date.Year;
			var hh = date.Hour;
			var min = date.Minute;
			var sec = date.Second;
			var ms = date.Millisecond;

			double d = 367.0 * yyyy - Div(7 * (yyyy + Div(mm + 9, 12)), 4) + Div(275 * mm, 9) + dd - 730530.0;
			d = d + hh / 24.0 + min / (60.0 * 24.0) + sec / (24.0 * 60.0 * 60.0);

			return d;
		}

		public static double GetJulianCentury(double jd) => jd / 36525.0;
		public static double GetJulianMillenium(double jd) => jd / 365250.0;

		private static long TicksFromDate(int year, int month, int day, int hours, int minutes, double seconds)
		{
			long ticks = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
			ticks += (day - 1) * 24L * 60L * 60L * 10000000L;
			ticks += hours * 60L * 60L * 10000000L;
			ticks += minutes * 60L * 10000000L;
			ticks += (long)(seconds * 10000000.0 + 0.5);
			return ticks;
		}
	}*/
}