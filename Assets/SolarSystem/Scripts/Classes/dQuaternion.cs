using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace CustomMath
{

	[Serializable]
	public struct dQuaternion : IEquatable<dQuaternion>, IFormattable
	{
		public double4 value;

		public static readonly dQuaternion identity = new(0d, 0d, 0d, 1d);

		public static explicit operator Quaternion(dQuaternion q)
			=> new((float)q.value.x, (float)q.value.y, (float)q.value.z, (float)q.value.w);
		public static explicit operator dQuaternion(Quaternion q) => new(q.x, q.y, q.z, q.w);
		public static explicit operator quaternion(dQuaternion q) => new((float4)q.value);
		public static explicit operator dQuaternion(quaternion q) => new(q.value);
		public static implicit operator dQuaternion((double x, double y, double z, double w) value)
			=> new(value.x, value.y, value.z, value.w);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public dQuaternion(double x, double y, double z, double w)
		{
			value.x = x;
			value.y = y;
			value.z = z;
			value.w = w;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public dQuaternion(double4 value) => this.value = value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator dQuaternion(double4 v) => new(v);
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
		public static dQuaternion EulerXYZ(double x, double y, double z) => EulerXYZ(math.double3(x, y, z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerXZY(double x, double y, double z) => EulerXZY(math.double3(x, y, z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerYXZ(double x, double y, double z) => EulerYXZ(math.double3(x, y, z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerYZX(double x, double y, double z) => EulerYZX(math.double3(x, y, z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerZXY(double x, double y, double z) => EulerZXY(math.double3(x, y, z));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion EulerZYX(double x, double y, double z) => EulerZYX(math.double3(x, y, z));

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
			=> Euler(math.double3(x, y, z), order);

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
			=> (dQuaternion)quaternion.LookRotationSafe((float3)forward, (float3)up);

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
				return value.w == x.value.w;
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
		public override int GetHashCode() => (int)math.hash(this.value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString() => $"dQuaternion({value.x}d, {value.y}d, {value.z}d, {value.w}d)";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string format, IFormatProvider formatProvider)
			=> $"dQuaternion({value.x.ToString(format, formatProvider)}d, {value.y.ToString(format, formatProvider)}d, {value.z.ToString(format, formatProvider)}d, {value.w.ToString(format, formatProvider)}d)";

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion mul(dQuaternion a, dQuaternion b)
			=> new dQuaternion(a.value.wwww * b.value + (a.value.xyzx * b.value.wwwx + a.value.yzxy * b.value.zxyy) * math.double4(1d, 1d, 1d, -1d) - a.value.zxyz * b.value.yzxz);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double3 mul(dQuaternion q, double3 v)
		{
			double3 t = 2 * math.cross(q.value.xyz, v);
			return v + q.value.w * t + math.cross(q.value.xyz, t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double dot(dQuaternion a, dQuaternion b) => math.dot(a.value, b.value);

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static dQuaternion conjugate(dQuaternion q) => q.value * new double4(-1, -1, -1, 1);
	}

}