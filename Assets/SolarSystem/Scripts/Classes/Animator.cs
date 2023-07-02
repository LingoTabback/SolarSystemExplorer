using System;
using Unity.Mathematics;

namespace Animation
{

	public interface IAnimatable<T>
	{
		public abstract T Lerp(in T to, float alpha);
	}

	public enum EasingType : byte
	{
		Linear = 0,
		EaseOutQuad,
		EaseOutSine,
		EaseOutBack
	}

	public struct FloatAnimatable : IAnimatable<FloatAnimatable>
	{
		public FloatAnimatable(float value) => m_Value = value;

		public static implicit operator FloatAnimatable(float value) => new(value);
		public static implicit operator float(FloatAnimatable value) => value.m_Value;

		public static bool operator ==(FloatAnimatable l, FloatAnimatable r) => l.m_Value == r.m_Value;
		public static bool operator !=(FloatAnimatable l, FloatAnimatable r) => l.m_Value != r.m_Value;

		public override bool Equals(object obj) => obj is FloatAnimatable id && this == id;
		public override int GetHashCode() => HashCode.Combine(m_Value);
		public override string ToString() => m_Value.ToString();

		public FloatAnimatable Lerp(in FloatAnimatable to, float alpha) => new(math.lerp(m_Value, to.m_Value, alpha));

		private float m_Value;
	}

	public class Animator<T> where T : IAnimatable<T>
	{
		public T Start { get; private set; }
		public T End { get; private set; }
		public T Current { get; private set; }

		public float Length { get; private set; } = 1;
		public float Progress { get; private set; } = 0;
		public bool IsDone { get; private set; } = false;
		private float m_Time = 0;

		private readonly Func<float, float> m_EasingFunc;

		public static Animator<T> Create(in T start, in T end, float length, EasingType easingType = EasingType.Linear)
		{
			return easingType switch
			{
				EasingType.EaseOutQuad => new(start, end, length, EaseOutQuad),
				EasingType.EaseOutSine => new(start, end, length, EaseOutSine),
				EasingType.EaseOutBack => new(start, end, length, EaseOutBack),
				_ => new(start, end, length, Linear),
			};
		}

		public static Animator<T> CreateDone(in T start, in T end, float length, EasingType easingType = EasingType.Linear)
		{
			Animator<T> result =  easingType switch
			{
				EasingType.EaseOutQuad => new(start, end, length, EaseOutQuad),
				EasingType.EaseOutSine => new(start, end, length, EaseOutSine),
				EasingType.EaseOutBack => new(start, end, length, EaseOutBack),
				_ => new(start, end, length, Linear),
			};
			result.SkipToEnd();
			return result;
		}

		public Animator(in T start, in T end, float length, Func<float, float> easingFunc)
		{
			Start = start;
			End = end;
			Current = start;
			Length = length;
			m_EasingFunc = easingFunc;
		}

		public void Update(float dt)
		{
			if (IsDone)
				return;

			m_Time += dt;
			if (m_Time > Length)
			{
				m_Time = Length;
				IsDone = true;
			}

			Progress = m_EasingFunc(m_Time / Length);
			Current = Start.Lerp(End, Progress);
		}

		public void SkipToEnd()
		{
			m_Time = Length;
			Update(0);
		}

		public void Reset(in T start, in T end, float length)
		{
			Start = start;
			End = end;
			Current = start;
			m_Time = 0;
			IsDone = false;
			Progress = 0;
			Length = length;
		}

		public void Reset(in T start, in T end) => Reset(start, end, Length);
		public void Reset(in T end) => Reset(Current, end, Length);

		private static float Linear(float x) => x;
		private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
		private static float EaseOutSine(float x) => math.sin(x * math.PI * 0.5f);
		private static float EaseOutBack(float x)
		{
			const float c1 = 1.70158f;
			const float c3 = c1 + 1;
			float x2 = (x - 1) * (x - 1);

			return 1 + c3 * x2 * (x - 1) + c1 * x2;
		}
	}
}