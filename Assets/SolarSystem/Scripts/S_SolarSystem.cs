#define ROTATION_1
//#define ROTATION_2
//#define ROTATION_3
//#define ROTATION_4

using UnityEngine;
using CustomMath;
using System;
using Unity.Mathematics;
using UnityEditor.Build.Pipeline.Interfaces;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using JulianTime;
using Ephemeris;

public class S_SolarSystem : MonoBehaviour
{
	public S_OrbitSettings[] Orbits;
	public GameObject SunPrefab;
	public float SunRadius = 69550;

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
	public double TimeScale = 1;

	[Header("Transitions")]
	public float TransitionTime = 5;

	[Header("Rendering")]
	public double PlanetScale = 10;
	public Material LineMaterial;

	private static readonly int s_LinePointCount = 200;
	private static double s_PlanetScale = 1;

	private OrbitWrapperPlanet[] m_PlanetOrbits;
	private GameObject m_SunObject;
	private double m_SunRadiusInAU = 1;

	private struct ReferenceTransform
	{
		public double3 Position;
		public dQuaternion Rotation;
		public double Scale;

		public static readonly ReferenceTransform Identity = new() { Position = 0, Rotation = dQuaternion.identity, Scale = 1 };
	}

	private class TransformAnimator
	{
		public ReferenceTransform TransformStart { get; private set; } = ReferenceTransform.Identity;
		public ReferenceTransform TransformEnd { get; private set; } = ReferenceTransform.Identity;
		public ReferenceTransform TransformCurrent { get => m_TransformCurrent; private set => m_TransformCurrent = value; }
		private ReferenceTransform m_TransformCurrent = ReferenceTransform.Identity;

		public int IndexStart { get; private set; } = -1;
		public int IndexEnd { get; private set; } = -1;
		public int IndexCurrrent { get; private set; } = -1;
		public float Length { get; private set; } = 1;
		public float Progress { get; private set; } = 0;
		public bool IsDone { get; private set; } = false;
		private float m_Time = 0;

		public TransformAnimator()
		{
			Progress = 1;
			m_Time = 1;
			IsDone = true;
		}

		public TransformAnimator(in ReferenceTransform start, in ReferenceTransform end, int indexStart, int indexEnd, float length)
		{
			TransformStart = start;
			TransformEnd = end;
			TransformCurrent = start;
			IndexStart = indexStart;
			IndexEnd = indexEnd;
			IndexCurrrent = indexStart;
			Length = length;
		}

		public void Update()
		{
			if (IsDone)
				return;

			m_Time += Time.deltaTime;
			if (m_Time > Length)
			{
				m_Time = Length;
				IndexCurrrent = IndexEnd;
				IsDone = true;
			}

			Progress = EaseOutSine(m_Time / Length);
			m_TransformCurrent.Position = math.lerp(TransformStart.Position, TransformEnd.Position, Progress);
			m_TransformCurrent.Rotation = dQuaternion.slerp(TransformStart.Rotation, TransformEnd.Rotation, Progress);
			m_TransformCurrent.Scale = math.lerp(TransformStart.Scale, TransformEnd.Scale, Progress);
		}

		private float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
		private float EaseOutSine(float x) => math.sin(x * math.PI * 0.5f);
	}

	private TransformAnimator m_Animator = new();
	private ReferenceTransform m_ReferenceTransform = ReferenceTransform.Identity;

	// Start is called before the first frame update
	void Start()
	{
		s_PlanetScale = PlanetScale;

		SetTime(Year, Month, Days, Hours, Minutes, Seconds);

		m_SunObject = Instantiate(SunPrefab, transform, false);
		m_SunRadiusInAU = CMath.KMtoAU(SunRadius * 10);

		m_PlanetOrbits = new OrbitWrapperPlanet[Orbits.Length];
		for (int i = 0; i < Orbits.Length; ++i)
		{
			var orbitSettings = Orbits[i];
			m_PlanetOrbits[i] = OrbitWrapperPlanet.Create(orbitSettings, transform, LineMaterial);
		}

		InitOrbits();

		foreach (var orbit in m_PlanetOrbits)
			orbit.SpawnPrefab(transform);
	}

	// Update is called once per frame
	void Update()
	{
		SetTime(TimeInDays + Time.deltaTime * TimeScale);
		UpdateOrbits();

		int nextOrbit = -2;
		if (Input.GetKeyUp(KeyCode.Alpha0)) nextOrbit = -1;
		if (Input.GetKeyUp(KeyCode.Alpha1)) nextOrbit = 0;
		if (Input.GetKeyUp(KeyCode.Alpha2)) nextOrbit = 1;
		if (Input.GetKeyUp(KeyCode.Alpha3)) nextOrbit = 2;
		if (Input.GetKeyUp(KeyCode.Alpha4)) nextOrbit = 3;
		if (Input.GetKeyUp(KeyCode.Alpha5)) nextOrbit = 4;
		if (Input.GetKeyUp(KeyCode.Alpha6)) nextOrbit = 5;
		if (Input.GetKeyUp(KeyCode.Alpha7)) nextOrbit = 6;
		if (Input.GetKeyUp(KeyCode.Alpha8)) nextOrbit = 7;

		if (nextOrbit == -1)
		{
			ReferenceTransform target = new()
			{
				Position = 0,
				Rotation = dQuaternion.identity,
				Scale = m_SunRadiusInAU
			};
			m_Animator = new TransformAnimator(m_ReferenceTransform, target, m_Animator.IndexEnd, nextOrbit, TransitionTime);
		}
		else if (nextOrbit > -1)
		{
			var orbitSettings = Orbits[nextOrbit];
			var orbit = m_PlanetOrbits[nextOrbit];

			/*
			double3 relPos = GetPositionInOrbit(orbitSettings, TimeInDays + TimeScale * TransitionTime);
			double3 pos = dQuaternion.mul(orbit.OrbitRotation, relPos);

#if ROTATION_1
			double angle = math.atan2(relPos.z, relPos.x);
			dQuaternion rotation = dQuaternion.mul(orbit.OrbitRotation, dQuaternion.RotateY(-angle));
#elif ROTATION_2
			dQuaternion rotation = orbit.OrbitRotation;
#elif ROTATION_3
			dQuaternion rotation = dQuaternion.identity;
#elif ROTATION_4
			double3 orbitUp = dQuaternion.mul(orbit.OrbitRotation, new double3(0, 1, 0));
			dQuaternion rotation = dQuaternion.FromTo(new double3(0, 1, 0), orbitUp);
#endif
			*/
			double3 pos = orbit.PlanetPositionWorld;
			dQuaternion rotation = dQuaternion.identity;

			ReferenceTransform target = new()
			{
				Position = pos,
				Rotation = rotation,
				Scale = orbit.RadiusInAU
			};
			if (m_Animator.IndexStart >= 0)
				m_PlanetOrbits[m_Animator.IndexStart].LineMaterial.SetFloat("_FadeAmount", 0f);
			m_Animator = new TransformAnimator(m_ReferenceTransform, target, m_Animator.IndexEnd, nextOrbit, TransitionTime);

			//if (m_Animator.IndexStart >= 0)
			//	m_Orbits[m_Animator.IndexStart].DestroyPrefab();
			//if (m_Animator.IndexEnd >= 0)
			//	m_Orbits[m_Animator.IndexEnd].SpawnPrefab(transform);
		}

		bool doneThisFrame = !m_Animator.IsDone;
		m_Animator.Update();
		doneThisFrame &= m_Animator.IsDone;
		if (!m_Animator.IsDone || doneThisFrame)
		{
			m_ReferenceTransform = m_Animator.TransformCurrent;
		}
		else if (m_Animator.IndexCurrrent >= 0)
		{
			var orbit = m_PlanetOrbits[m_Animator.IndexCurrrent];

			m_ReferenceTransform.Position = orbit.PlanetPositionWorld;
			/*
#if ROTATION_1
			double angle = math.atan2(orbit.PlanetPositionRelative.z, orbit.PlanetPositionRelative.x);
			dQuaternion qaut = dQuaternion.RotateY(-angle);
			m_ReferenceTransform.Rotation = dQuaternion.mul(orbit.OrbitRotation, qaut);
#endif*/
		}

		if (m_Animator.IndexStart >= 0)
			m_PlanetOrbits[m_Animator.IndexStart].LineMaterial.SetFloat("_FadeAmount", 1 - math.smoothstep(0.25f, 0.75f, m_Animator.Progress));
		if (m_Animator.IndexEnd >= 0)
			m_PlanetOrbits[m_Animator.IndexEnd].LineMaterial.SetFloat("_FadeAmount", math.smoothstep(0.25f, 0.75f, m_Animator.Progress));

		m_SunObject.transform.localPosition = -(float3)(m_ReferenceTransform.Position / m_ReferenceTransform.Scale);
		m_SunObject.transform.localScale = Vector3.one * (float)(m_SunRadiusInAU / m_ReferenceTransform.Scale);

		//foreach (var orbit in m_PlanetOrbits)
		//{
		//	orbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
		//	orbit.OrbitLineObject.transform.localPosition = (float3)((orbit.ParentPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
		//	orbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);
		//	if (orbit.OrbitingObject != null)
		//	{
		//		orbit.OrbitingObject.transform.localPosition = (float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
		//		orbit.OrbitingObject.transform.localScale = Vector3.one * (float)(orbit.RadiusInAU / m_ReferenceTransform.Scale);

		//		if (orbit.Script != null)
		//			orbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(orbit.PlanetPositionWorld));
		//	}

		//	foreach (var moonOrbit in orbit.MoonOrbits)
		//	{
		//		moonOrbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((moonOrbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
		//		moonOrbit.OrbitLineObject.transform.localPosition = (float3)((moonOrbit.ParentPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
		//		moonOrbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);

		//		if (moonOrbit.OrbitingObject != null)
		//		{
		//			moonOrbit.OrbitingObject.transform.localPosition = (float3)((moonOrbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
		//			moonOrbit.OrbitingObject.transform.localScale = Vector3.one * (float)(moonOrbit.RadiusInAU / m_ReferenceTransform.Scale);

		//			if (moonOrbit.Script != null)
		//				moonOrbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(moonOrbit.PlanetPositionWorld));
		//		}
		//	}
		//}

		foreach (var orbit in m_PlanetOrbits)
		{
			orbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
			orbit.OrbitLineObject.transform.localPosition = (float3)((-m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
			orbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);
			if (orbit.OrbitingObject != null)
			{
				orbit.OrbitingObject.transform.localPosition = (float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
				orbit.OrbitingObject.transform.localScale = Vector3.one * (float)(orbit.RadiusInAU / m_ReferenceTransform.Scale);
				orbit.OrbitingObject.transform.localRotation = (Quaternion)orbit.EquatorOrientation;
				orbit.Script.SetSpin(orbit.Spin);

				if (orbit.Script != null)
					orbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(orbit.PlanetPositionWorld));
			}

			foreach (var moon in orbit.Moons)
			{
				moon.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((moon.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
				moon.OrbitLineObject.transform.localPosition = (float3)((moon.Parent.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
				moon.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);
				moon.OrbitLineObject.transform.localRotation = (Quaternion)moon.Parent.EquatorOrientation;
			
				if (moon.OrbitingObject != null)
				{
					moon.OrbitingObject.transform.localPosition = (float3)((moon.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
					moon.OrbitingObject.transform.localScale = Vector3.one * (float)(moon.RadiusInAU / m_ReferenceTransform.Scale);
					moon.OrbitingObject.transform.localRotation = (Quaternion)orbit.EquatorOrientation;
					moon.Script.SetSpin(moon.Spin);

					if (moon.Script != null)
						moon.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(moon.PlanetPositionWorld));
				}
			}
		}

		transform.localRotation = (Quaternion)dQuaternion.inverse(m_ReferenceTransform.Rotation);
	}

	private void InitOrbits()
	{
		var date = JulianDate.FromTicks(new DateTime(TicksFromDate(2000, 1, 1, 0, 0, TimeInDays * 24 * 60 * 60), DateTimeKind.Utc).Ticks, JulianDate.J2000);
		foreach (var orbit in m_PlanetOrbits)
			orbit.Init(date);
	}

	private void UpdateOrbits()
	{
		var date = JulianDate.FromTicks(new DateTime(TicksFromDate(2000, 1, 1, 0, 0, TimeInDays * 24 * 60 * 60), DateTimeKind.Utc).Ticks, JulianDate.J2000);
		foreach (var orbit in m_PlanetOrbits)
			orbit.Update(date);
	}

	public void SetTime(double timeInDays)
	{
		DateTime date = new(TicksFromDate(2000, 1, 1, 0, 0, timeInDays * 24 * 60 * 60), DateTimeKind.Utc);
		Year = date.Year;
		Month = date.Month;
		Days = date.Day;
		Hours = date.Hour;
		Minutes = date.Minute;
		Seconds = (date - new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc)).TotalSeconds;
		TimeInDays = timeInDays;
	}

	public void SetTime(int year, int month, int day, int hours, int minutes, double seconds)
	{
		DateTime date = new(TicksFromDate(year, month, day, hours, minutes, seconds), DateTimeKind.Utc);
		Year = date.Year;
		Month = date.Month;
		Days = date.Day;
		Hours = date.Hour;
		Minutes = date.Minute;
		Seconds = (date - new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0, DateTimeKind.Utc)).TotalSeconds;
		TimeInDays = (date - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
	}

	private static long TicksFromDate(int year, int month, int day, int hours, int minutes, double seconds)
	{
		long ticks = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
		ticks += (day - 1) * 24L * 60L * 60L * 10000000L;
		ticks += hours * 60L * 60L * 10000000L;
		ticks += minutes * 60L * 10000000L;
		ticks += (long)(seconds * 10000000.0 + 0.5);
		return ticks;
	}
	public static double GetScaleFromPlanet(float radius, float scaleToSize) => CMath.KMtoAU(radius * 10) * scaleToSize * 0.5 * s_PlanetScale;

	private abstract class OrbitWrapper<ScriptType> where ScriptType : MonoBehaviour
	{
		public S_OrbitSettings Settings;
		public GameObject OrbitLineObject;
		public GameObject OrbitingObject;
		public ScriptType Script;
		public Mesh LineMesh;
		public Material LineMaterial;

		public double3 PlanetPositionRelative = 0;
		public double3 PlanetPositionWorld = 0;
		public dQuaternion EquatorOrientation = dQuaternion.identity;
		public dQuaternion Spin = dQuaternion.identity;
		public double RadiusInAU = 1;

		protected double m_StartJulianDay = 0;

		protected Orbit m_Orbit;
		protected RotationModel m_RotationModel;

		public OrbitWrapper(Orbit orbit, RotationModel frame, S_OrbitSettings settings, Transform systemTransform, Material lineMaterial)
		{
			m_Orbit = orbit;
			m_RotationModel = frame;

			Settings = settings;
			OrbitLineObject = new GameObject(settings.Name);
			OrbitLineObject.transform.SetParent(systemTransform);
			LineMaterial = new Material(lineMaterial);
			LineMaterial.SetColor("_Color", settings.DisplayColor.linear);
			LineMaterial.SetFloat("_FadeAmount", 0f);

			LineMesh = new Mesh();
			OrbitLineObject.AddComponent<MeshFilter>().sharedMesh = LineMesh;
			OrbitLineObject.AddComponent<MeshRenderer>().sharedMaterial = LineMaterial;
		}

		public virtual void SpawnPrefab(Transform systemTransform)
		{
			DestroyPrefab();

			OrbitingObject = Instantiate(Settings.OrbitingObject);
			OrbitingObject.transform.SetParent(systemTransform, false);
			OrbitingObject.transform.localScale = Vector3.one * 0.001f;
			OrbitingObject.transform.localRotation = (Quaternion)EquatorOrientation;
			OrbitingObject.TryGetComponent(out Script);
		}

		public virtual void DestroyPrefab()
		{
			if (OrbitingObject != null)
				Destroy(OrbitingObject);

			OrbitingObject = null;
		}

		public virtual void Init(in JulianDate date)
		{
			m_StartJulianDay = date.JulianDay;
			EquatorOrientation = dQuaternion.mul(s_SunOrientationQuatInv, m_RotationModel.ComputeEquatorOrientation(date));
			Spin = m_RotationModel.ComputeSpin(date);
			PlanetPositionWorld = m_Orbit.PositionAtTime(date.JulianDay);
			LineMaterial.SetFloat("_PositionInOrbit", 0);
			InitOrbitLine(date);
		}

		private void InitOrbitLine(in JulianDate date)
		{
			Vector3[] vertices = new Vector3[s_LinePointCount + 1];
			Vector2[] uvs = new Vector2[s_LinePointCount + 1];

			int[] indices = new int[s_LinePointCount * 2];

			for (int i = 0; i < s_LinePointCount; ++i)
			{
				double time = i / (double)s_LinePointCount * m_Orbit.Period + date.JulianDay;
				double3 point = m_Orbit.PositionAtTime(time);
				vertices[i] = (float3)point;
				uvs[i] = new Vector2(i / (float)s_LinePointCount, 0);

				indices[i * 2] = i;
				indices[i * 2 + 1] = i + 1;
			}
			vertices[s_LinePointCount] = vertices[0];
			uvs[s_LinePointCount] = new Vector2(1, 0);

			LineMesh.vertices = vertices;
			LineMesh.uv = uvs;
			LineMesh.SetIndices(indices, MeshTopology.Lines, 0, true);
		}

		private void UpdateOrbitLine(in JulianDate date)
		{
			Vector3[] vertices = new Vector3[s_LinePointCount + 1];
			Vector2[] uvs = new Vector2[s_LinePointCount + 1];

			for (int i = 0; i < s_LinePointCount; ++i)
			{
				double time = i / (double)s_LinePointCount * m_Orbit.Period + date.JulianDay;
				double3 point = m_Orbit.PositionAtTime(time);
				vertices[i] = (float3)point;
				uvs[i] = new Vector2(i / (float)s_LinePointCount, 0);
			}
			vertices[s_LinePointCount] = vertices[0];
			uvs[s_LinePointCount] = new Vector2(1, 0);

			LineMesh.vertices = vertices;
			LineMesh.uv = uvs;
		}

		public virtual void Update(in JulianDate date)
		{
			PlanetPositionRelative = m_Orbit.PositionAtTime(date.JulianDay);
			EquatorOrientation = dQuaternion.mul(s_SunOrientationQuatInv, m_RotationModel.ComputeEquatorOrientation(date));
			Spin = m_RotationModel.ComputeSpin(date);
			// UpdateOrbitLine(date); // TODO: bad perf, needs other solution
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac((date.JulianDay - m_StartJulianDay) / m_Orbit.Period));
		}

		public struct ReferenceOrientation
		{
			public double Ra;
			public double Dec;
			public double Node;
			public double Inclination;

			public dQuaternion GetRotation() => dQuaternion.mul(dQuaternion.RotateY(-math.radians(Node)), dQuaternion.RotateX(-math.radians(Inclination)));
		}

		public static readonly ReferenceOrientation s_SunOrientation = new() { Ra = 286.13, Dec = 63.87, Node = 286.13 + 90, Inclination = 90 - 63.87 };
		public static readonly dQuaternion s_SunOrientationQuat = s_SunOrientation.GetRotation();
		public static readonly dQuaternion s_SunOrientationQuatInv = dQuaternion.inverse(s_SunOrientationQuat);
	}

	private class OrbitWrapperPlanet : OrbitWrapper<S_Planet>
	{
		public OrbitWrapperMoon[] Moons;

		public OrbitWrapperPlanet(Orbit orbit, RotationModel frame,
			S_OrbitSettings settings, Transform systemTransform, Material lineMaterial) : base(orbit, frame, settings, systemTransform, lineMaterial)
		{
			if (settings.OrbitingObject.TryGetComponent(out S_Planet planetScript))
				RadiusInAU = GetScaleFromPlanet(planetScript.Radius, planetScript.ScaleToSize);

			Moons = new OrbitWrapperMoon[settings.SatelliteOrbits.Length];
			for (int i = 0; i < Moons.Length; ++i)
				Moons[i] = OrbitWrapperMoon.Create(this, settings.SatelliteOrbits[i], systemTransform, lineMaterial);
		}

		public override void Init(in JulianDate date)
		{
			base.Init(date);
			PlanetPositionWorld = PlanetPositionRelative;

			foreach (var moon in Moons)
				moon.Init(date);
		}

		public override void Update(in JulianDate date)
		{
			base.Update(date);
			PlanetPositionWorld = PlanetPositionRelative;

			foreach (var moon in Moons)
				moon.Update(date);
		}

		public override void SpawnPrefab(Transform systemTransform)
		{
			base.SpawnPrefab(systemTransform);
			foreach (var orbit in Moons)
				orbit.SpawnPrefab(systemTransform);
		}

		public override void DestroyPrefab()
		{
			base.DestroyPrefab();
			foreach (var orbit in Moons)
				orbit.DestroyPrefab();
		}

		public static OrbitWrapperPlanet Create(S_OrbitSettings settings, Transform systemTransform, Material lineMaterial)
		{
			return new OrbitWrapperPlanet(Orbit.Create(settings.OrbitType), RotationModel.Create(settings.RotationModelType), settings, systemTransform, lineMaterial);
		}
		
	}

	private class OrbitWrapperMoon : OrbitWrapper<S_Moon>
	{
		public OrbitWrapperPlanet Parent;

		public OrbitWrapperMoon(OrbitWrapperPlanet parent, Orbit orbit, RotationModel frame, S_OrbitSettings settings, Transform systemTransform, Material lineMaterial)
			: base(orbit, frame, settings, systemTransform, lineMaterial)
		{
			if (settings.OrbitingObject.TryGetComponent(out S_Moon moonScript))
				RadiusInAU = GetScaleFromPlanet(moonScript.Radius, moonScript.ScaleToSize);

			Parent = parent;
		}

		public override void Init(in JulianDate date)
		{
			base.Init(date);
			PlanetPositionWorld = dQuaternion.mul(Parent.EquatorOrientation, PlanetPositionRelative) + Parent.PlanetPositionWorld;
		}

		public override void Update(in JulianDate date)
		{
			base.Update(date);
			PlanetPositionWorld = dQuaternion.mul(Parent.EquatorOrientation, PlanetPositionRelative) + Parent.PlanetPositionWorld;
		}

		public static OrbitWrapperMoon Create(OrbitWrapperPlanet parent, S_OrbitSettings settings, Transform systemTransform, Material lineMaterial)
		{
			return new OrbitWrapperMoon(parent, Orbit.Create(settings.OrbitType), RotationModel.Create(settings.RotationModelType), settings, systemTransform, lineMaterial);
		}
	}
}
