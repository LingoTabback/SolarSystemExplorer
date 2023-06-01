using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CustomMath
{

	[Serializable]
	public struct dQuaternion : IEquatable<dQuaternion>, IFormattable
	{
		public double4 value;

		public static readonly dQuaternion identity = new(0d, 0d, 0d, 1d);

		public static explicit operator Quaternion(dQuaternion q)
		{
			return new Quaternion((float)q.value.x, (float)q.value.y, (float)q.value.z, (float)q.value.w);
		}

		public static explicit operator dQuaternion(Quaternion q)
		{
			return new dQuaternion(q.x, q.y, q.z, q.w);
		}

		public static explicit operator quaternion(dQuaternion q)
		{
			return new quaternion((float4)q.value);
		}

		public static explicit operator dQuaternion(quaternion q)
		{
			return new dQuaternion(q.value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public dQuaternion(double x, double y, double z, double w)
		{
			value.x = x;
			value.y = y;
			value.z = z;
			value.w = w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public dQuaternion(double4 value)
		{
			this.value = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator dQuaternion(double4 v)
		{
			return new dQuaternion(v);
		}

		//public quaternion(float3x3 m)
		//{
		//	float3 c = m.c0;
		//	float3 c2 = m.c1;
		//	float3 c3 = m.c2;
		//	uint num = math.asuint(c.x) & 0x80000000u;
		//	float x = c2.y + math.asfloat(math.asuint(c3.z) ^ num);
		//	uint4 uint5 = math.uint4((int)num >> 31);
		//	uint4 uint6 = math.uint4(math.asint(x) >> 31);
		//	float x2 = 1f + math.abs(c.x);
		//	uint4 uint7 = math.uint4(0u, 2147483648u, 2147483648u, 2147483648u) ^ (uint5 & math.uint4(0u, 2147483648u, 0u, 2147483648u)) ^ (uint6 & math.uint4(2147483648u, 2147483648u, 2147483648u, 0u));
		//	value = math.float4(x2, c.y, c3.x, c2.z) + math.asfloat(math.asuint(math.float4(x, c2.x, c.z, c3.y)) ^ uint7);
		//	value = math.asfloat((math.asuint(value) & ~uint5) | (math.asuint(value.zwxy) & uint5));
		//	value = math.asfloat((math.asuint(value.wzyx) & ~uint6) | (math.asuint(value) & uint6));
		//	value = math.normalize(value);
		//}

		//public quaternion(float4x4 m)
		//{
		//	float4 c = m.c0;
		//	float4 c2 = m.c1;
		//	float4 c3 = m.c2;
		//	uint num = math.asuint(c.x) & 0x80000000u;
		//	float x = c2.y + math.asfloat(math.asuint(c3.z) ^ num);
		//	uint4 uint5 = math.uint4((int)num >> 31);
		//	uint4 uint6 = math.uint4(math.asint(x) >> 31);
		//	float x2 = 1f + math.abs(c.x);
		//	uint4 uint7 = math.uint4(0u, 2147483648u, 2147483648u, 2147483648u) ^ (uint5 & math.uint4(0u, 2147483648u, 0u, 2147483648u)) ^ (uint6 & math.uint4(2147483648u, 2147483648u, 2147483648u, 0u));
		//	value = math.float4(x2, c.y, c3.x, c2.z) + math.asfloat(math.asuint(math.float4(x, c2.x, c.z, c3.y)) ^ uint7);
		//	value = math.asfloat((math.asuint(value) & ~uint5) | (math.asuint(value.zwxy) & uint5));
		//	value = math.asfloat((math.asuint(value.wzyx) & ~uint6) | (math.asuint(value) & uint6));
		//	value = math.normalize(value);
		//}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion AxisAngle(double3 axis, double angle)
		{
			math.sincos(0.5 * angle, out var s, out var c);
			return new dQuaternion(math.double4(axis * s, c));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerXYZ(double3 xyz)
		{
			math.sincos(0.5 * xyz, out var s, out var c);
			return new dQuaternion(math.double4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * math.double4(c.xyz, s.x) * math.double4(-1d, 1d, -1d, 1d));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerXZY(double3 xyz)
		{
			math.sincos(0.5f * xyz, out var s, out var c);
			return new dQuaternion(math.double4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * math.double4(c.xyz, s.x) * math.double4(1d, 1d, -1d, -1d));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerYXZ(double3 xyz)
		{
			math.sincos(0.5 * xyz, out var s, out var c);
			return new dQuaternion(math.double4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * math.double4(c.xyz, s.x) * math.double4(-1f, 1f, 1f, -1f));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerYZX(double3 xyz)
		{
			math.sincos(0.5 * xyz, out var s, out var c);
			return new dQuaternion(math.double4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * math.double4(c.xyz, s.x) * math.double4(-1d, -1d, 1d, 1d));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerZXY(double3 xyz)
		{
			math.sincos(0.5 * xyz, out var s, out var c);
			return new dQuaternion(math.double4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * math.double4(c.xyz, s.x) * math.double4(1d, -1d, -1d, 1d));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerZYX(double3 xyz)
		{
			math.sincos(0.5 * xyz, out var s, out var c);
			return new dQuaternion(math.double4(s.xyz, c.x) * c.yxxy * c.zzyz + s.yxxy * s.zzyz * math.double4(c.xyz, s.x) * math.double4(1d, -1d, 1d, -1d));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerXYZ(double x, double y, double z)
		{
			return EulerXYZ(math.double3(x, y, z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerXZY(double x, double y, double z)
		{
			return EulerXZY(math.double3(x, y, z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerYXZ(double x, double y, double z)
		{
			return EulerYXZ(math.double3(x, y, z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerYZX(double x, double y, double z)
		{
			return EulerYZX(math.double3(x, y, z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerZXY(double x, double y, double z)
		{
			return EulerZXY(math.double3(x, y, z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerZYX(double x, double y, double z)
		{
			return EulerZYX(math.double3(x, y, z));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion Euler(double3 xyz, math.RotationOrder order = math.RotationOrder.ZXY)
		{
			return order switch
			{
				math.RotationOrder.XYZ => EulerXYZ(xyz),
				math.RotationOrder.XZY => EulerXZY(xyz),
				math.RotationOrder.YXZ => EulerYXZ(xyz),
				math.RotationOrder.YZX => EulerYZX(xyz),
				math.RotationOrder.ZXY => EulerZXY(xyz),
				math.RotationOrder.ZYX => EulerZYX(xyz),
				_ => identity,
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion Euler(double x, double y, double z, math.RotationOrder order = math.RotationOrder.ZXY)
		{
			return Euler(math.double3(x, y, z), order);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion RotateX(double angle)
		{
			math.sincos(0.5 * angle, out var s, out var c);
			return new dQuaternion(s, 0d, 0d, c);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion RotateY(double angle)
		{
			math.sincos(0.5 * angle, out var s, out var c);
			return new dQuaternion(0d, s, 0d, c);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion RotateZ(double angle)
		{
			math.sincos(0.5 * angle, out var s, out var c);
			return new dQuaternion(0d, 0d, s, c);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion LookRotation(double3 forward, double3 up)
		{
			double3 float5 = math.normalize(math.cross(up, forward));
			return (dQuaternion)math.quaternion(math.float3x3((float3)float5, (float3)math.cross(forward, float5), (float3)forward));
		}

		public static dQuaternion LookRotationSafe(double3 forward, double3 up)
		{
			return (dQuaternion)quaternion.LookRotationSafe((float3)forward, (float3)up);
			//double x = math.dot(forward, forward);
			//double num = math.dot(up, up);
			//forward *= math.rsqrt(x);
			//up *= math.rsqrt(num);
			//double3 float5 = math.cross(up, forward);
			//double num2 = math.dot(float5, float5);
			//float5 *= math.rsqrt(num2);
			//double num3 = math.min(math.min(x, num), num2);
			//double num4 = math.max(math.max(x, num), num2);
			//bool c = num3 > 1E-35f && num4 < 1E+35f && math.isfinite(x) && math.isfinite(num) && math.isfinite(num2);
			//return math.quaternion(math.select(math.float4(0f, 0f, 0f, 1f), math.quaternion(math.float3x3(float5, math.cross(forward, float5), forward)).value, c));
		}

		public static dQuaternion FromTo(double3 from, double3 to)
		{
			double k_cos_theta = math.dot(from, to);
			double k = math.sqrt(math.lengthsq(from) * math.lengthsq(to));

			//if (k_cos_theta / k == -1)
			//{
			//	// 180 degree rotation around any orthogonal vector
			//	return dQuaternion(normalize(orthogonal(from)), 0);
			//}

			return normalize(new dQuaternion(math.double4(math.cross(from, to), k_cos_theta + k)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(dQuaternion x)
		{
			if (value.x == x.value.x && value.y == x.value.y && value.z == x.value.z)
			{
				return value.w == x.value.w;
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object x)
		{
			if (x is dQuaternion)
			{
				dQuaternion x2 = (dQuaternion)x;
				return Equals(x2);
			}

			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return (int)math.hash(this.value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
		{
			return $"dQuaternion({value.x}d, {value.y}d, {value.z}d, {value.w}d)";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string format, IFormatProvider formatProvider)
		{
			return $"dQuaternion({value.x.ToString(format, formatProvider)}d, {value.y.ToString(format, formatProvider)}d, {value.z.ToString(format, formatProvider)}d, {value.w.ToString(format, formatProvider)}d)";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion mul(dQuaternion a, dQuaternion b)
		{
			return new dQuaternion(a.value.wwww * b.value + (a.value.xyzx * b.value.wwwx + a.value.yzxy * b.value.zxyy) * math.double4(1d, 1d, 1d, -1d) - a.value.zxyz * b.value.yzxz);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double3 mul(dQuaternion q, double3 v)
		{
			double3 t = 2 * math.cross(q.value.xyz, v);
			return v + q.value.w * t + math.cross(q.value.xyz, t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double dot(dQuaternion a, dQuaternion b)
		{
			return math.dot(a.value, b.value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion normalize(dQuaternion q)
		{
			double4 x = q.value;
			return new dQuaternion(math.rsqrt(dot(x, x)) * x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion nlerp(dQuaternion q1, dQuaternion q2, double t)
		{
			double dt = dot(q1, q2);
			if (dt < 0d)
				q2.value = -q2.value;

			return normalize(new dQuaternion(math.lerp(q1.value, q2.value, t)));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion slerp(dQuaternion q1, dQuaternion q2, double t)
		{
			double dt = dot(q1, q2);
			if (dt < 0d)
			{
				dt = -dt;
				q2.value = -q2.value;
			}

			if (dt < 0.9995d)
			{
				double angle = math.acos(dt);
				double s = math.rsqrt(1.0f - dt * dt);    // 1.0f / sin(angle)
				double w1 = math.sin(angle * (1.0f - t)) * s;
				double w2 = math.sin(angle * t) * s;
				return new dQuaternion(q1.value * w1 + q2.value * w2);
			}
			else
			{
				// if the angle is small, use linear interpolation
				return nlerp(q1, q2, t);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion inverse(dQuaternion q)
		{
			double4 x = q.value;
			return new dQuaternion(math.rcp(dot(x, x)) * x * new double4(-1.0f, -1.0f, -1.0f, 1.0f));
		}
	}

}

public class S_Orbit : MonoBehaviour
{
	[Header("Orbital Elements")]
	public double MeanDistance = 1;
	[Range(0f, 1f)]
	public double Eccentricity = 0.01673;
	public float Inclination = 0;
	public float LongitudeOfPeriapsis = 102.93f;
	public float LongitudeOfAscendingNode = 0;
	public double MeanLongitudeAtEpoch = 100.47;
	public double OrbitalPeriod = 1;

	[Header("Time")]
	public bool UseDate = true;
	public int Year = 2000;
	[Range(1, 12)]
	public int Month = 1;
	[Range(1, 31)]
	public int Days = 1;
	[Range(0, 23)]
	public int Hours = 0;
	[Range(0, 59)]
	public int Minutes = 0;
	[Range(0, 59.999f)]
	public double Seconds = 0;
	public double TimeInDays = 0;

	[Header("Display Settings")]
	public Color DisplayColor = new(1, 1, 1, 0.25f);

	private DateTime m_Date = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	// Start is called before the first frame update
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	private void OnValidate()
	{
		if (!TryGetComponent<LineRenderer>(out var line))
			line = gameObject.AddComponent<LineRenderer>();
		line.loop = true;
		const int count = 200;
		line.positionCount = count;

		double eccentricity = math.clamp(Eccentricity, 0.0, 0.999);
		double minExtent = 0;
		double maxExtent = 0;

		if (UseDate)
			SetTime(Year, Month, Days, Hours, Minutes, Seconds);
		else
			SetTime(TimeInDays);

		var span = m_Date - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		double curTimeOffset = span.TotalDays / OrbitalPeriod;

		for (int i = 0; i < count; ++i)
		{
			double time = i / (double)count + curTimeOffset;
			double3 point = GetPositionInOrbit(MeanDistance, eccentricity, time);
			line.SetPosition(i, new Vector3((float)point.x, (float)point.y, (float)point.z));
			minExtent = math.min(point.x, minExtent);
			maxExtent = math.max(point.x, maxExtent);
		}

		line.widthMultiplier = 30f * 0.005f;

		GradientColorKey[] colorKeys = new GradientColorKey[1];
		GradientAlphaKey[] alphaKeys = new GradientAlphaKey[1];
		colorKeys[0] = new GradientColorKey(DisplayColor.linear, 0);
		alphaKeys[0] = new GradientAlphaKey(DisplayColor.a, 0);

		Gradient gradient = new()
		{
			mode = GradientMode.Blend
		};
		gradient.SetKeys(colorKeys, alphaKeys);
		line.colorGradient = gradient;

		CustomMath.dQuaternion rotation = CustomMath.dQuaternion.mul(CustomMath.dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-LongitudeOfAscendingNode)),
			CustomMath.dQuaternion.mul(CustomMath.dQuaternion.AxisAngle(new double3(1, 0, 0), math.radians(-Inclination)),
			CustomMath.dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-LongitudeOfPeriapsis + LongitudeOfAscendingNode))));
		transform.localRotation = (Quaternion)rotation;
	}

	void SetTime(double timeInDays)
	{
		m_Date = new DateTime(TicksFromDate(2000, 1, 1, 0, 0, timeInDays * 24 * 60 * 60), DateTimeKind.Utc);
		Year = m_Date.Year;
		Month = m_Date.Month;
		Days = m_Date.Day;
		Hours = m_Date.Hour;
		Minutes = m_Date.Minute;
		Seconds = (m_Date - new DateTime(m_Date.Year, m_Date.Month, m_Date.Day, m_Date.Hour, m_Date.Minute, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	void SetTime(int year, int month, int day, int hours, int minutes, double seconds)
	{
		m_Date = new DateTime(TicksFromDate(year, month, day, hours, minutes, seconds), DateTimeKind.Utc);
		Year = m_Date.Year;
		Month = m_Date.Month;
		Days = m_Date.Day;
		Hours = m_Date.Hour;
		Minutes = m_Date.Minute;
		Seconds = (m_Date - new DateTime(m_Date.Year, m_Date.Month, m_Date.Day, m_Date.Hour, m_Date.Minute, 0, DateTimeKind.Utc)).TotalSeconds;
		TimeInDays = (m_Date - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
	}

	private double GetSpeedInOrbit(double a, double e, double time, double epsilon)
	{
		return math.distance(GetPositionInOrbit(a, e, time - epsilon), GetPositionInOrbit(a, e, time + epsilon));
	}

	private double3 GetPositionInOrbit(double a, double e, double time)
	{
		double M = time * math.PI_DBL * 2 + math.radians(MeanLongitudeAtEpoch - LongitudeOfPeriapsis);
		int q;
		double c1, c2, E, V, r;
		c1 = math.sqrt((1.0 + e) / (1.0 - e));  // some helper constants computation
		c2 = a * (1 - e * e);

		for (E = M, q = 0; q < 20; ++q)
			E = M + e * math.sin(E); // Kepler's equation

		V = 2.0 * math.atan(c1 * math.tan(E / 2.0));
		r = c2 / (1.0 + e * math.cos(V));
		return new double3(math.cos(V), 0, math.sin(V)) * r;
	}

	private long TicksFromDate(int year, int month, int day, int hours, int minutes, double seconds)
	{
		long ticks = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
		ticks += (day - 1) * 24L * 60L * 60L * 10000000L;
		ticks += hours * 60L * 60L * 10000000L;
		ticks += minutes * 60L * 10000000L;
		ticks += (long)(seconds * 10000000.0 + 0.5);
		return ticks;
	}
}
