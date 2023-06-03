using System;
using Unity.Mathematics;

namespace JulianTime
{
	[Serializable]
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
	}
}