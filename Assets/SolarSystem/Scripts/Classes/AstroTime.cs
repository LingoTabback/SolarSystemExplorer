using System;
using Unity.Mathematics;

namespace AstroTime
{
	[Serializable]
	public struct Date
	{
		public enum Format : byte
		{
			ISO8601,
			US, // but with 24h Time Of Day
			DE
		}

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

		public Date(in DateTime date)
		{
			DateTime utc = date.ToUniversalTime();
			m_Year = utc.Year;
			m_Month = utc.Month;
			m_Day = utc.Day;
			m_Hour = utc.Hour;
			m_Minute = utc.Minute;
			m_Seconds = utc.Second;
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

		public static implicit operator Date(double jd) => new(jd);
		public static implicit operator Date(in DateTime date) => new(date);
		public static bool operator ==(in Date a, in Date b)
			=> a.m_Year == b.m_Year & a.m_Month == b.m_Month & a.m_Day == b.m_Day & a.m_Minute == b.m_Minute & a.m_Seconds == b.m_Seconds;
		public static bool operator !=(in Date a, in Date b) => !(a == b);

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

		private static readonly string[] s_MonthAbbreviationsUS =
		{
			"Jan", "Feb", "Mar", "Apr", "May", "Jun",
			"Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
		};
		private static readonly string[] s_MonthAbbreviationsDE =
		{
			"Jan", "Feb", "Mär", "Apr", "Mai", "Jun",
			"Jul", "Aug", "Sep", "Okt", "Nov", "Dez"
		};

		public override string ToString() => ToString(Format.ISO8601);
		public string ToString(bool dateOnly) => ToString(Format.ISO8601, dateOnly);
		public string ToString(Format format, bool dateOnly = false)
		{
			if (m_Month < 1 | m_Month > 12 | m_Day < 1 | m_Hour < 0 | m_Minute < 0 | m_Seconds < 0)
				return "NOT A VALID DATE";

			if (dateOnly)
			{
				return format switch
				{
					Format.ISO8601 => $"{m_Year:0000}-{m_Month:00}-{m_Day:00}",
					Format.US => $"{s_MonthAbbreviationsUS[m_Month - 1]} {m_Day:00}, {m_Year:0000}",
					Format.DE => $"{m_Day:00}.{s_MonthAbbreviationsDE[m_Month - 1]} {m_Year:0000}",
					_ => "NOT A VALID DATE",
				};
			}
			return format switch
			{
				Format.ISO8601 => $"{m_Year:0000}-{m_Month:00}-{m_Day:00} {m_Hour:00}:{m_Minute:00}:{(int)m_Seconds:00} {m_TZName}",
				Format.US => $"{s_MonthAbbreviationsUS[m_Month - 1]} {m_Day:00}, {m_Year:0000} {m_Hour:00}:{m_Minute:00}:{(int)m_Seconds:00} {m_TZName}",
				Format.DE => $"{m_Day:00}.{s_MonthAbbreviationsDE[m_Month - 1]} {m_Year:0000} {m_Hour:00}:{m_Minute:00}:{(int)m_Seconds:00} {m_TZName}",
				_ => "NOT A VALID DATE",
			};
		}

		public override bool Equals(object obj)
		{
			return obj is Date date&&
				   Year==date.Year&&
				   Month==date.Month&&
				   Day==date.Day&&
				   Hour==date.Hour&&
				   Minute==date.Minute&&
				   Seconds==date.Seconds&&
				   UtcOffset==date.UtcOffset&&
				   WeekDay==date.WeekDay&&
				   TimeZoneName==date.TimeZoneName&&
				   Julian==date.Julian&&
				   m_Year==date.m_Year&&
				   m_Month==date.m_Month&&
				   m_Day==date.m_Day&&
				   m_Hour==date.m_Hour&&
				   m_Minute==date.m_Minute&&
				   m_WDay==date.m_WDay&&
				   m_UtcOffset==date.m_UtcOffset&&
				   m_TZName==date.m_TZName&&
				   m_Seconds==date.m_Seconds;
		}

		public override int GetHashCode()
		{
			HashCode hash = new();
			hash.Add(Year);
			hash.Add(Month);
			hash.Add(Day);
			hash.Add(Hour);
			hash.Add(Minute);
			hash.Add(Seconds);
			hash.Add(UtcOffset);
			hash.Add(WeekDay);
			hash.Add(TimeZoneName);
			hash.Add(Julian);
			hash.Add(m_Year);
			hash.Add(m_Month);
			hash.Add(m_Day);
			hash.Add(m_Hour);
			hash.Add(m_Minute);
			hash.Add(m_WDay);
			hash.Add(m_UtcOffset);
			hash.Add(m_TZName);
			hash.Add(m_Seconds);
			return hash.ToHashCode();
		}
	}

	public enum AstroTimeUnit : byte
	{
		Seconds,
		Minutes,
		Hours,
		Days,
		Weeks,
		Years
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

		public static double ConvertToDays(double t, AstroTimeUnit from)
		{
			return from switch
			{
				AstroTimeUnit.Seconds => t * s_DaysPerSecond,
				AstroTimeUnit.Minutes => t * s_DaysPerMinute,
				AstroTimeUnit.Hours => t * s_DaysPerHour,
				AstroTimeUnit.Weeks => t * s_DaysPerWeek,
				AstroTimeUnit.Years => t * s_DaysPerYear,
				_ => t
			};
		}

		public static double ConvertFromDays(double t, AstroTimeUnit to)
		{
			return to switch
			{
				AstroTimeUnit.Seconds => t * s_SecondsPerDay,
				AstroTimeUnit.Minutes => t * s_MinutesPerDay,
				AstroTimeUnit.Hours => t * s_HoursPerDay,
				AstroTimeUnit.Weeks => t * s_WeeksPerDay,
				AstroTimeUnit.Years => t * s_YearsPerDay,
				_ => t
			};
		}

		public static double ConvertUnit(double t, AstroTimeUnit from, AstroTimeUnit to) => ConvertFromDays(ConvertToDays(t, from), to);

		public static double SecsToDays(double s) => s * s_DaysPerSecond;
		public static double DaysToSecs(double d) => d * s_SecondsPerDay;

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
		private static readonly double s_MinutesPerDay = 1440.0;
		private static readonly double s_HoursPerDay = 24.0;
		private static readonly double s_WeeksPerDay = 1.0 / 7.0;
		private static readonly double s_YearsPerDay = 1.0 / 365.25;

		private static readonly double s_DaysPerSecond = 1.0 / 86400.0;
		private static readonly double s_DaysPerMinute = 1.0 / 1440.0;
		private static readonly double s_DaysPerHour = 1.0 / 24.0;
		private static readonly double s_DaysPerWeek = 7.0;
		private static readonly double s_DaysPerYear = 365.25;

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
			public static implicit operator LeapSecondRecord((int s, double t) value) => new(value.s, value.t);
		}

		private static readonly LeapSecondRecord[] s_LeapSeconds =
		{
			(10, 2441317.5), // 1 Jan 1972
			(11, 2441499.5), // 1 Jul 1972
			(12, 2441683.5), // 1 Jan 1973
			(13, 2442048.5), // 1 Jan 1974
			(14, 2442413.5), // 1 Jan 1975
			(15, 2442778.5), // 1 Jan 1976
			(16, 2443144.5), // 1 Jan 1977
			(17, 2443509.5), // 1 Jan 1978
			(18, 2443874.5), // 1 Jan 1979
			(19, 2444239.5), // 1 Jan 1980
			(20, 2444786.5), // 1 Jul 1981
			(21, 2445151.5), // 1 Jul 1982
			(22, 2445516.5), // 1 Jul 1983
			(23, 2446247.5), // 1 Jul 1985
			(24, 2447161.5), // 1 Jan 1988
			(25, 2447892.5), // 1 Jan 1990
			(26, 2448257.5), // 1 Jan 1991
			(27, 2448804.5), // 1 Jul 1992
			(28, 2449169.5), // 1 Jul 1993
			(29, 2449534.5), // 1 Jul 1994
			(30, 2450083.5), // 1 Jan 1996
			(31, 2450630.5), // 1 Jul 1997
			(32, 2451179.5), // 1 Jan 1999
			(33, 2453736.5), // 1 Jan 2006
			(34, 2454832.5), // 1 Jan 2009
			(35, 2456109.5), // 1 Jul 2012
			(36, 2457204.5), // 1 Jul 2015
			(37, 2457754.5), // 1 Jan 2017
		};
	}
}