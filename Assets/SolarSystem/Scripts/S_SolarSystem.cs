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

	private class OrbitComponents<ScriptType> where ScriptType : MonoBehaviour
	{
		public S_OrbitSettings Settings;
		public GameObject OrbitLineObject;
		public GameObject OrbitingObject;
		public ScriptType Script;
		public Mesh LineMesh;
		public Material LineMaterial;
		public double3 PlanetPositionRelative = 0;
		public double3 PlanetPositionWorld = 0;
		public dQuaternion OrbitRotation = dQuaternion.identity;
		public double3 ParentPosition = 0;
		public dQuaternion ParentRotation = dQuaternion.identity;
		public dQuaternion WorldRotation = dQuaternion.identity;
		public double RadiusInAU = 1;

		public OrbitComponents(S_OrbitSettings settings, GameObject parent, Material lineMaterial)
		{
			Settings = settings;
			OrbitLineObject = parent;
			LineMaterial = new Material(lineMaterial);
			LineMaterial.SetColor("_Color", settings.DisplayColor.linear);
			LineMaterial.SetFloat("_FadeAmount", 0f);
		}

		public virtual void SpawnPrefab(Transform systemTransform)
		{
			DestroyPrefab();

			OrbitingObject = Instantiate(Settings.OrbitingObject);
			OrbitingObject.transform.SetParent(systemTransform, false);
			OrbitingObject.transform.localRotation = (Quaternion)OrbitRotation;
			OrbitingObject.transform.localScale = Vector3.one * 0.01f;
			OrbitingObject.TryGetComponent(out Script);
		}

		public virtual void DestroyPrefab()
		{
			if (OrbitingObject != null)
				Destroy(OrbitingObject);

			OrbitingObject = null;
		}

		public virtual void Init(double timeInDays)
		{
			PlanetPositionRelative = GetPositionInOrbit(Settings, timeInDays);
			PlanetPositionWorld = dQuaternion.mul(WorldRotation, PlanetPositionRelative) + ParentPosition;
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac(timeInDays / Settings.OrbitalPeriod));
			InitOrbitLine(timeInDays);
		}

		private void InitOrbitLine(double timeInDays)
		{
			Vector3[] vertices = new Vector3[s_LinePointCount + 1];
			Vector2[] uvs = new Vector2[s_LinePointCount + 1];

			int[] indices = new int[s_LinePointCount * 2];

			for (int i = 0; i < s_LinePointCount; ++i)
			{
				double time = i / (double)s_LinePointCount * Settings.OrbitalPeriod + timeInDays;
				double3 point = GetPositionInOrbit(Settings, time);
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

		public virtual void Update(double timeInDays)
		{
			PlanetPositionRelative = GetPositionInOrbit(Settings, timeInDays);
			PlanetPositionWorld = dQuaternion.mul(WorldRotation, PlanetPositionRelative) + ParentPosition;
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac(timeInDays / Settings.OrbitalPeriod));
		}
	}

	private class OrbitComponentsMoon : OrbitComponents<S_Moon>
	{
		public OrbitComponentsMoon(S_OrbitSettings settings, GameObject parent, Material lineMaterial) : base(settings, parent, lineMaterial)
		{
			if (settings.OrbitingObject.TryGetComponent(out S_Moon moonScript))
				RadiusInAU = GetScaleFromPlanet(moonScript.Radius, moonScript.ScaleToSize);
		}

		public static OrbitComponentsMoon Create(GameObject parent, S_OrbitSettings settings, Material lineMaterial, OrbitComponentsPlanet planet)
		{
			OrbitComponentsMoon result = new(settings, new GameObject(settings.Name), lineMaterial);

			result.OrbitLineObject.transform.SetParent(parent.transform, false);
			result.OrbitRotation = dQuaternion.mul(dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfAscendingNode)),
				dQuaternion.mul(dQuaternion.AxisAngle(new double3(1, 0, 0), math.radians(-settings.Inclination)),
				dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfPeriapsis + settings.LongitudeOfAscendingNode))));

			result.WorldRotation = dQuaternion.mul(result.ParentRotation, result.OrbitRotation);
			result.OrbitLineObject.transform.SetLocalPositionAndRotation(Vector3.zero, (Quaternion)result.WorldRotation);

			result.LineMesh = new Mesh();
			result.OrbitLineObject.AddComponent<MeshFilter>().sharedMesh = result.LineMesh;
			result.OrbitLineObject.AddComponent<MeshRenderer>().sharedMaterial = result.LineMaterial;

			result.ParentRotation = planet.OrbitRotation;
			result.ParentPosition = planet.PlanetPositionWorld;

			return result;
		}
	}

	private class OrbitComponentsPlanet : OrbitComponents<S_Planet>
	{
		public OrbitComponentsMoon[] MoonOrbits;

		public OrbitComponentsPlanet(S_OrbitSettings settings, GameObject parent, Material lineMaterial) : base(settings, parent, lineMaterial)
		{
			if (settings.OrbitingObject.TryGetComponent(out S_Planet planetScript))
				RadiusInAU = GetScaleFromPlanet(planetScript.Radius, planetScript.ScaleToSize);
		}

		public override void SpawnPrefab(Transform systemTransform)
		{
			base.SpawnPrefab(systemTransform);
			foreach (var orbit in MoonOrbits)
				orbit.SpawnPrefab(systemTransform);
		}

		public override void DestroyPrefab()
		{
			base.DestroyPrefab();
			foreach (var orbit in MoonOrbits)
				orbit.DestroyPrefab();
		}

		public override void Init(double timeInDays)
		{
			base.Init(timeInDays);
			foreach (var orbit in MoonOrbits)
			{
				orbit.ParentPosition = PlanetPositionWorld;
				orbit.Init(timeInDays);
			}
		}

		public override void Update(double timeInDays)
		{
			base.Update(timeInDays);
			foreach (var orbit in MoonOrbits)
			{
				orbit.ParentPosition = PlanetPositionWorld;
				orbit.Update(timeInDays);
			}
		}

		public static OrbitComponentsPlanet Create(GameObject parent, S_OrbitSettings settings, Material lineMaterial)
		{
			OrbitComponentsPlanet result = new(settings, new GameObject(settings.Name), lineMaterial);

			result.OrbitLineObject.transform.SetParent(parent.transform, false);
			result.OrbitRotation = dQuaternion.mul(dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfAscendingNode)),
				dQuaternion.mul(dQuaternion.AxisAngle(new double3(1, 0, 0), math.radians(-settings.Inclination)),
				dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfPeriapsis + settings.LongitudeOfAscendingNode))));

			result.WorldRotation = dQuaternion.mul(result.ParentRotation, result.OrbitRotation);
			result.OrbitLineObject.transform.SetLocalPositionAndRotation(Vector3.zero, (Quaternion)result.WorldRotation);

			result.LineMesh = new Mesh();
			result.OrbitLineObject.AddComponent<MeshFilter>().sharedMesh = result.LineMesh;
			result.OrbitLineObject.AddComponent<MeshRenderer>().sharedMaterial = result.LineMaterial;

			result.MoonOrbits = new OrbitComponentsMoon[settings.SatelliteOrbits.Length];

			for (int i = 0; i < settings.SatelliteOrbits.Length; ++i)
				result.MoonOrbits[i] = OrbitComponentsMoon.Create(parent, settings.SatelliteOrbits[i], lineMaterial, result);

			return result;
		}
	}

	private OrbitComponentsPlanet[] m_PlanetOrbits;
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

			//Progress = math.smoothstep(0, 1, m_Time / Length);
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
		m_SunRadiusInAU = KMtoAU(SunRadius * 10);

		m_PlanetOrbits = new OrbitComponentsPlanet[Orbits.Length];

		for (int i = 0; i < Orbits.Length; ++i)
		{
			var orbitSettings = Orbits[i];
			m_PlanetOrbits[i] = OrbitComponentsPlanet.Create(gameObject, orbitSettings, LineMaterial);
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
#if ROTATION_1
			double angle = math.atan2(orbit.PlanetPositionRelative.z, orbit.PlanetPositionRelative.x);
			dQuaternion qaut = dQuaternion.RotateY(-angle);
			m_ReferenceTransform.Rotation = dQuaternion.mul(orbit.OrbitRotation, qaut);
#endif
		}

		if (m_Animator.IndexStart >= 0)
			m_PlanetOrbits[m_Animator.IndexStart].LineMaterial.SetFloat("_FadeAmount", 1 - math.smoothstep(0.25f, 0.75f, m_Animator.Progress));
		if (m_Animator.IndexEnd >= 0)
			m_PlanetOrbits[m_Animator.IndexEnd].LineMaterial.SetFloat("_FadeAmount", math.smoothstep(0.25f, 0.75f, m_Animator.Progress));

		m_SunObject.transform.localPosition = -(float3)(m_ReferenceTransform.Position / m_ReferenceTransform.Scale);
		m_SunObject.transform.localScale = Vector3.one * (float)(m_SunRadiusInAU / m_ReferenceTransform.Scale);

		foreach (var orbit in m_PlanetOrbits)
		{
			orbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
			orbit.OrbitLineObject.transform.localPosition = (float3)((orbit.ParentPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
			orbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);
			if (orbit.OrbitingObject != null)
			{
				orbit.OrbitingObject.transform.localPosition = (float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
				orbit.OrbitingObject.transform.localScale = Vector3.one * (float)(orbit.RadiusInAU / m_ReferenceTransform.Scale);

				if (orbit.Script != null)
					orbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(orbit.PlanetPositionWorld));
			}

			foreach (var moonOrbit in orbit.MoonOrbits)
			{
				moonOrbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((moonOrbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
				moonOrbit.OrbitLineObject.transform.localPosition = (float3)((moonOrbit.ParentPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
				moonOrbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);

				if (moonOrbit.OrbitingObject != null)
				{
					moonOrbit.OrbitingObject.transform.localPosition = (float3)((moonOrbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
					moonOrbit.OrbitingObject.transform.localScale = Vector3.one * (float)(moonOrbit.RadiusInAU / m_ReferenceTransform.Scale);

					if (moonOrbit.Script != null)
						moonOrbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(moonOrbit.PlanetPositionWorld));
				}
			}
		}

		transform.localRotation = (Quaternion)dQuaternion.inverse(m_ReferenceTransform.Rotation);
	}

	private void InitOrbits()
	{
		foreach (var orbit in m_PlanetOrbits)
			orbit.Init(TimeInDays);
	}

	private void UpdateOrbits()
	{
		foreach (var orbit in m_PlanetOrbits)
			orbit.Update(TimeInDays);
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

	private static double GetSpeedInOrbit(S_OrbitSettings orbit, double time, double epsilon)
	{
		return math.distance(GetPositionInOrbit(orbit, time - epsilon * orbit.OrbitalPeriod), GetPositionInOrbit(orbit, time + epsilon * orbit.OrbitalPeriod));
	}

	public static double3 GetPositionInOrbit(S_OrbitSettings orbit, double time)
	{
		double a = orbit.MeanDistance;
		double e = orbit.Eccentricity;
		time /= orbit.OrbitalPeriod;

		double M = time * math.PI_DBL * 2 + math.radians(orbit.MeanLongitudeAtEpoch - orbit.LongitudeOfPeriapsis);
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

	public static double AUtoKM(double au) => au * (1.496e+8 / s_PlanetScale);
	public static double KMtoAU(double km) => km / (1.496e+8 / s_PlanetScale);
	public static double GetScaleFromPlanet(float radius, float scaleToSize) => KMtoAU(radius * 10) * scaleToSize * 0.5;

	private struct OrbitData
	{
		public int EpochYear;
		public double EpochDay;
		public double EpochStart;
		public double EpochNow;
		public double Epoch;
		public double DerivativeOfMeanMotion;
		public double DragTerm;
		public double Inclination;
		public double RightAscension;
		public double AscendingNode;
		public double Eccentricity;
		public double ArgOfPeriapsis;
		public double MeanAnomalyAtEpoch;
		public double MeanMotion;
		public double PeriodHours;
		public double Period;
		public double SemiMajorAxis;

		public OrbitData(S_OrbitSettings settings)
		{
			EpochYear = settings.EpochYear;
			EpochDay = settings.EpochDay;
			DerivativeOfMeanMotion = settings.DerivativeOfMeanMotion;
			DragTerm = settings.DragTerm;
			Inclination = settings.Inclination;
			RightAscension = settings.RightAscension;
			AscendingNode = RightAscension;
			Eccentricity = settings.Inclination;
			ArgOfPeriapsis = settings.ArgOfPeriapsis;
			MeanAnomalyAtEpoch = settings.MeanAnomalyAtEpoch;
			MeanMotion = settings.MeanMotion;

			EpochStart = DayNumberTle(2000 + EpochYear, EpochDay);
			EpochNow = DayNumberNow();
			Epoch = EpochStart + 2451545 - 1.5;
			PeriodHours = (1440 / ((1 * MeanMotion) + (DerivativeOfMeanMotion * (EpochNow - EpochStart)))) / 60;
			SemiMajorAxis = math.pow((6028.9 * (PeriodHours * 60)), 2.0 / 3.0) / 149597870.700;
			Period = PeriodHours / 24;
		}

		public static double DayNumber(int dd, int mm, int yyyy, int hh, int min, double sec)
		{
			static int Div(int a, int b) => ((a - a % b) / b);

			double d = 367 * yyyy - Div(7 * (yyyy + Div(mm + 9, 12)), 4) + Div(275 * mm, 9) + dd - 730530;
			d = d + hh / 24.0 + min / (60.0 * 24.0) + sec / (24.0 * 60.0 * 60.0);
			return d;
		}

		public static double DayNumberTle(int year, double day) => DayNumber(1, 1, year, 0, 0, 0) + day - 1;

		public static double DayNumberNow()
		{
			var date = DateTime.UtcNow;
			return DayNumber(date.Day, date.Month, date.Year, date.Hour, date.Minute, date.Second);
		}
	}

	private class Orbit<ScriptType> where ScriptType : MonoBehaviour
	{
		public S_OrbitSettings Settings;
		public GameObject OrbitLineObject;
		public GameObject OrbitingObject;
		public ScriptType Script;
		public Mesh LineMesh;
		public Material LineMaterial;
		public double3 PlanetPositionRelative = 0;
		public double3 PlanetPositionWorld = 0;
		public dQuaternion OrbitRotation = dQuaternion.identity;
		public double3 ParentPosition = 0;
		public dQuaternion ParentRotation = dQuaternion.identity;
		public dQuaternion WorldRotation = dQuaternion.identity;
		public double RadiusInAU = 1;

		public OrbitData Data;
		public double Epoch = AstroUtils.AstroDate.J2000;
		public double MeanMotion = 0;
		public double DerivativeOfMeanMotion = 0;
		public double PericenterDistance = 0;
		public dQuaternion OrbitPlaneRotation = dQuaternion.identity;

		public double E = 0;
		public double M = 0;
		public double TrueAnomaly = 0;

		public Orbit(S_OrbitSettings settings, GameObject parent, Material lineMaterial)
		{
			Settings = settings;
			Epoch = Settings.Epoch == S_OrbitSettings.Epochs.J1900 ? AstroUtils.AstroDate.J1900 : AstroUtils.AstroDate.J2000;
			MeanMotion = 1d / Settings.OrbitalPeriod;
			PericenterDistance = Settings.MeanDistance * (1 - Settings.Eccentricity);

			Data = new OrbitData(settings);

			var ascendingNodeRotation = dQuaternion.RotateY(-math.radians(Settings.LongitudeOfAscendingNode));
			var inclinationRotation = dQuaternion.RotateX(-math.radians(Settings.Inclination));
			var argOfPeriapsisRotation = dQuaternion.RotateY(-math.radians(Settings.LongitudeOfPeriapsis - Settings.LongitudeOfAscendingNode));

			OrbitPlaneRotation = dQuaternion.mul(dQuaternion.mul(ascendingNodeRotation, inclinationRotation), argOfPeriapsisRotation);

			OrbitLineObject = parent;
			LineMaterial = new Material(lineMaterial);
			LineMaterial.SetColor("_Color", settings.DisplayColor.linear);
			LineMaterial.SetFloat("_FadeAmount", 0f);
		}

		public virtual void SpawnPrefab(Transform systemTransform)
		{
			DestroyPrefab();

			OrbitingObject = Instantiate(Settings.OrbitingObject);
			OrbitingObject.transform.SetParent(systemTransform, false);
			OrbitingObject.transform.localRotation = (Quaternion)OrbitRotation;
			OrbitingObject.transform.localScale = Vector3.one * 0.01f;
			OrbitingObject.TryGetComponent(out Script);
		}

		public virtual void DestroyPrefab()
		{
			if (OrbitingObject != null)
				Destroy(OrbitingObject);

			OrbitingObject = null;
		}

		public virtual void Init(double timeInDays)
		{
			PlanetPositionRelative = GetPositionInOrbit(Settings, timeInDays);
			PlanetPositionWorld = dQuaternion.mul(WorldRotation, PlanetPositionRelative) + ParentPosition;
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac(timeInDays / Settings.OrbitalPeriod));
			InitOrbitLine(timeInDays);
		}

		private void InitOrbitLine(double timeInDays)
		{
			Vector3[] vertices = new Vector3[s_LinePointCount + 1];
			Vector2[] uvs = new Vector2[s_LinePointCount + 1];

			int[] indices = new int[s_LinePointCount * 2];

			for (int i = 0; i < s_LinePointCount; ++i)
			{
				double time = i / (double)s_LinePointCount * Settings.OrbitalPeriod + timeInDays;
				double3 point = GetPositionInOrbit(Settings, time);
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

		public virtual void Update(double timeInDays)
		{
			PlanetPositionRelative = GetPositionInOrbit(Settings, timeInDays);
			PlanetPositionWorld = dQuaternion.mul(WorldRotation, PlanetPositionRelative) + ParentPosition;
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac(timeInDays / Settings.OrbitalPeriod));
		}

		public double MeanAnomalyAtTime(double t)
		{
			double timeSinceEpoch = t - Epoch;
			double meanAnomaly = Settings.MeanLongitudeAtEpoch + (360 * (MeanMotion * timeSinceEpoch + 0.5 * DerivativeOfMeanMotion * timeSinceEpoch * timeSinceEpoch));
			meanAnomaly = math.radians(meanAnomaly);
			return meanAnomaly;
		}

		private static double SolveKeplerFunc1Fixed(double eccentricity, double M, int maxIter)
		{
			double x = M;
			for (int i = 0; i < maxIter; ++i)
				x = M + eccentricity * math.sin(x);
			return x;
		}

		private static double SolveKeplerFunc2Fixed(double eccentricity, double M, int maxIter)
		{
			double x = M;
			for (int i = 0; i < maxIter; ++i)
				x = x + (M + eccentricity * math.sin(x) - x) / (1 - eccentricity * math.cos(x));
			return x;
		}

		private static double SolveKeplerKeplerLaguerreConwayFixed(double eccentricity, double M, double x0, int maxIter)
		{
			double x = x0;
			for (int i = 0; i < maxIter; ++i)
			{
				var s = eccentricity * math.sin(x);
				var c = eccentricity * math.cos(x);
				var f = x - s - M;
				var f1 = 1 - c;
				var f2 = s;

				x += -5 * f / (f1 + math.sign(f1) * math.sqrt(math.abs(16 * f1 * f1 - 20 * f * f2)));
			}
			return x;
		}

		private double EccentricAnomaly(double M)
		{
			if (Settings.Eccentricity == 0.0 || Settings.Eccentricity == 1.0)
				return M;
			else if (Settings.Eccentricity < 0.2)
				return SolveKeplerFunc1Fixed(Settings.Eccentricity, M, 5);
			else if (Settings.Eccentricity < 0.9)
				return SolveKeplerFunc2Fixed(Settings.Eccentricity, M, 6);
			else if (Settings.Eccentricity < 1.0)
			{
				double E = M + 0.85 * Settings.Eccentricity * ((math.sin(M) >= 0.0) ? 1 : -1);
				return SolveKeplerKeplerLaguerreConwayFixed(Settings.Eccentricity, M, E, 8);
			}
			// TODO: allow Eccentricity > 1
			return M;
		}

		private double3 LocalPositionAtE(double E)
		{
			double x = 0, y = 0;

			if (Settings.Eccentricity < 1.0)
			{
				double a = PericenterDistance / (1.0 - Settings.Eccentricity);
				x = a * (math.cos(E) - Settings.Eccentricity);
				y = a * math.sqrt(1 - Settings.Eccentricity * Settings.Eccentricity) * math.sin(E);
			}
			// TODO: allow Eccentricity > 1

			return new double3(x, 0, y);
		}

		private double3 PositionAtE(double E)
		{
			return dQuaternion.mul(OrbitPlaneRotation, LocalPositionAtE(E));
		}

		public struct OrbitPosition
		{
			public double3 Position;
			public double E;
			public double M;
		}

		public OrbitPosition LocalPositionAtTime(double t)
		{
			double meanAnomaly = MeanAnomalyAtTime(t);
			double E = EccentricAnomaly(meanAnomaly);

			return new OrbitPosition()
			{
				Position = LocalPositionAtE(E),
				E = E,
				M = meanAnomaly
			};
		}

		public OrbitPosition PositionAtTime(double t)
		{
			double meanAnomaly = MeanAnomalyAtTime(t);
			double E = EccentricAnomaly(meanAnomaly);

			return new OrbitPosition()
			{
				Position = PositionAtE(E),
				E = E,
				M = meanAnomaly
			};
		}

		public OrbitPosition CalculateLocalPosition(in AstroUtils.AstroDate date) => LocalPositionAtTime(date.JulianDay);
		public OrbitPosition CalculatePosition(in AstroUtils.AstroDate date) => PositionAtTime(date.JulianDay);

		public struct SattelitePosition
		{
			double3 Position;
			public double E;
			public double M;
			public double TrueAnomaly;
		}

		/*
		public SattelitePosition CalculateSatellitePosition(in AstroUtils.AstroDate date)
		{
			var position = PositionAtTime(date.JulianDay);

			double E = position.E;
			double M = position.M;
			//var trueAnomaly = 0;

			double semiMajorAxis = Settings.MeanDistance;
			double arg_per = Settings.LongitudeOfPeriapsis - Settings.LongitudeOfAscendingNode;
			double RAAN = Settings.RightAscension;
			double i = math.radians(Settings.Inclination);
			double e = Settings.Eccentricity;

			var Epoch_now = date.getDayNumber();
			var Epoch_start = orbit.orbitProperties.epochStart;
			var Earth_equatorial_radius = 6378.135;
		}*/
	}

}

public static class AstroUtils
{
	public static double DateToJulian(in DateTime date)
	{
		int tz = 0;

		double dayDecimal, julianDay, a;

		dayDecimal = date.Day + (date.Hour - tz + (date.Minute + date.Second / 60.0 + date.Millisecond / 1000 / 60) / 60.0) / 24.0;

		int month = date.Month;
		int year = date.Year;
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
		var millisecond = 0.0;//(1000.0 * (60.0 * (dayMinutes - (hour * 60.0) -minute)- second) );

		return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second, (int)millisecond, DateTimeKind.Utc);
	}

	public struct AstroDate
	{
		public long Ticks;
		public double JulianDay;
		public double Epoch;

		public static readonly double J2000 = 2451545.0;
		public static readonly double J1900 = 2415020.0;

		public static AstroDate FromDays(double jd, double epoch)
		{
			AstroDate date = new()
			{
				JulianDay = jd,
				Epoch = epoch,
				Ticks = JulianToDate(jd).Ticks
			};
			return date;
		}

		public static AstroDate FromTicks(long ticks, double epoch)
		{
			double jd = MillisToJulian(ticks / 10000.0);

			AstroDate date = new()
			{
				JulianDay = jd,
				Epoch = epoch,
				Ticks = JulianToDate(jd).Ticks
			};
			return date;
		}
	}
}

/*
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
	public Material LineMaterial;

	private static readonly int s_LinePointCount = 200;

	private class OrbitComponents<ScriptType> where ScriptType : MonoBehaviour
	{
		public S_OrbitSettings Settings;
		public GameObject OrbitLineObject;
		public GameObject OrbitingObject;
		public ScriptType Script;
		public Mesh LineMesh;
		public Material LineMaterial;
		public double3 PlanetPositionRelative = 0;
		public double3 PlanetPositionWorld = 0;
		public dQuaternion OrbitRotation = dQuaternion.identity;
		public double3 ParentPosition = 0;
		public dQuaternion ParentRotation = dQuaternion.identity;
		public dQuaternion WorldRotation = dQuaternion.identity;
		public double RadiusInAU = 1;

		public OrbitComponents(S_OrbitSettings settings, GameObject parent, Material lineMaterial)
		{
			Settings = settings;
			OrbitLineObject = parent;
			LineMaterial = new Material(lineMaterial);
			LineMaterial.SetColor("_Color", settings.DisplayColor.linear);
			LineMaterial.SetFloat("_FadeAmount", 0f);
		}

		public virtual void SpawnPrefab(Transform systemTransform)
		{
			DestroyPrefab();

			OrbitingObject = Instantiate(Settings.OrbitingObject);
			OrbitingObject.transform.SetParent(systemTransform, false);
			OrbitingObject.transform.localRotation = (Quaternion)OrbitRotation;
			OrbitingObject.transform.localScale = Vector3.one * 0.01f;
			OrbitingObject.TryGetComponent(out Script);
		}

		public virtual void DestroyPrefab()
		{
			if (OrbitingObject != null)
				Destroy(OrbitingObject);

			OrbitingObject = null;
		}

		public virtual void Init(double timeInDays)
		{
			PlanetPositionRelative = GetPositionInOrbit(Settings, timeInDays);
			PlanetPositionWorld = dQuaternion.mul(WorldRotation, PlanetPositionRelative) + ParentPosition;
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac(timeInDays / Settings.OrbitalPeriod));
			InitOrbitLine(timeInDays);
		}

		private void InitOrbitLine(double timeInDays)
		{
			Vector3[] vertices = new Vector3[s_LinePointCount + 1];
			Vector2[] uvs = new Vector2[s_LinePointCount + 1];

			int[] indices = new int[s_LinePointCount * 2];

			for (int i = 0; i < s_LinePointCount; ++i)
			{
				double time = i / (double)s_LinePointCount * Settings.OrbitalPeriod + timeInDays;
				double3 point = GetPositionInOrbit(Settings, time);
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

		public virtual void Update(double timeInDays)
		{
			PlanetPositionRelative = GetPositionInOrbit(Settings, timeInDays);
			PlanetPositionWorld = dQuaternion.mul(WorldRotation, PlanetPositionRelative) + ParentPosition;
			LineMaterial.SetFloat("_PositionInOrbit", (float)math.frac(timeInDays / Settings.OrbitalPeriod));
		}
	}

	private class OrbitComponentsMoon : OrbitComponents<S_Moon>
	{
		public OrbitComponentsMoon(S_OrbitSettings settings, GameObject parent, Material lineMaterial) : base(settings, parent, lineMaterial)
		{
			if (settings.OrbitingObject.TryGetComponent(out S_Moon moonScript))
				RadiusInAU = GetScaleFromPlanet(moonScript.Radius, moonScript.ScaleToSize);
		}

		public static OrbitComponentsMoon Create(GameObject parent, S_OrbitSettings settings, Material lineMaterial, OrbitComponentsPlanet planet)
		{
			OrbitComponentsMoon result = new(settings, new GameObject(settings.Name), lineMaterial);

			result.OrbitLineObject.transform.SetParent(parent.transform, false);
			result.OrbitRotation = dQuaternion.mul(dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfAscendingNode)),
				dQuaternion.mul(dQuaternion.AxisAngle(new double3(1, 0, 0), math.radians(-settings.Inclination)),
				dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfPeriapsis + settings.LongitudeOfAscendingNode))));

			result.WorldRotation = dQuaternion.mul(result.ParentRotation, result.OrbitRotation);
			result.OrbitLineObject.transform.SetLocalPositionAndRotation(Vector3.zero, (Quaternion)result.WorldRotation);

			result.LineMesh = new Mesh();
			result.OrbitLineObject.AddComponent<MeshFilter>().sharedMesh = result.LineMesh;
			result.OrbitLineObject.AddComponent<MeshRenderer>().sharedMaterial = result.LineMaterial;

			result.ParentRotation = planet.OrbitRotation;
			result.ParentPosition = planet.PlanetPositionWorld;

			return result;
		}
	}

	private class OrbitComponentsPlanet : OrbitComponents<S_Planet>
	{
		public OrbitComponentsMoon[] MoonOrbits;

		public OrbitComponentsPlanet(S_OrbitSettings settings, GameObject parent, Material lineMaterial) : base(settings, parent, lineMaterial)
		{
			if (settings.OrbitingObject.TryGetComponent(out S_Planet planetScript))
				RadiusInAU = GetScaleFromPlanet(planetScript.Radius, planetScript.ScaleToSize);
		}

		public override void SpawnPrefab(Transform systemTransform)
		{
			base.SpawnPrefab(systemTransform);
			foreach (var orbit in MoonOrbits)
				orbit.SpawnPrefab(systemTransform);
		}

		public override void DestroyPrefab()
		{
			base.DestroyPrefab();
			foreach (var orbit in MoonOrbits)
				orbit.DestroyPrefab();
		}

		public override void Init(double timeInDays)
		{
			base.Init(timeInDays);
			foreach (var orbit in MoonOrbits)
			{
				orbit.ParentPosition = PlanetPositionWorld;
				orbit.Init(timeInDays);
			}
		}

		public override void Update(double timeInDays)
		{
			base.Update(timeInDays);
			foreach (var orbit in MoonOrbits)
			{
				orbit.ParentPosition = PlanetPositionWorld;
				orbit.Update(timeInDays);
			}
		}

		public static OrbitComponentsPlanet Create(GameObject parent, S_OrbitSettings settings, Material lineMaterial)
		{
			OrbitComponentsPlanet result = new(settings, new GameObject(settings.Name), lineMaterial);

			result.OrbitLineObject.transform.SetParent(parent.transform, false);
			result.OrbitRotation = dQuaternion.mul(dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfAscendingNode)),
				dQuaternion.mul(dQuaternion.AxisAngle(new double3(1, 0, 0), math.radians(-settings.Inclination)),
				dQuaternion.AxisAngle(new double3(0, 1, 0), math.radians(-settings.LongitudeOfPeriapsis + settings.LongitudeOfAscendingNode))));

			result.WorldRotation = dQuaternion.mul(result.ParentRotation, result.OrbitRotation);
			result.OrbitLineObject.transform.SetLocalPositionAndRotation(Vector3.zero, (Quaternion)result.WorldRotation);

			result.LineMesh = new Mesh();
			result.OrbitLineObject.AddComponent<MeshFilter>().sharedMesh = result.LineMesh;
			result.OrbitLineObject.AddComponent<MeshRenderer>().sharedMaterial = result.LineMaterial;

			result.MoonOrbits = new OrbitComponentsMoon[settings.SatelliteOrbits.Length];

			for (int i = 0; i < settings.SatelliteOrbits.Length; ++i)
				result.MoonOrbits[i] = OrbitComponentsMoon.Create(parent, settings.SatelliteOrbits[i], lineMaterial, result);

			return result;
		}
	}

	private OrbitComponentsPlanet[] m_PlanetOrbits;
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

			//Progress = math.smoothstep(0, 1, m_Time / Length);
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
		SetTime(Year, Month, Days, Hours, Minutes, Seconds);

		m_SunObject = Instantiate(SunPrefab, transform, false);
		m_SunRadiusInAU = KMtoAU(SunRadius * 10);

		m_PlanetOrbits = new OrbitComponentsPlanet[Orbits.Length];

		for (int i = 0; i < Orbits.Length; ++i)
		{
			var orbitSettings = Orbits[i];
			m_PlanetOrbits[i] = OrbitComponentsPlanet.Create(gameObject, orbitSettings, LineMaterial);
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
#if ROTATION_1
			double angle = math.atan2(orbit.PlanetPositionRelative.z, orbit.PlanetPositionRelative.x);
			dQuaternion qaut = dQuaternion.RotateY(-angle);
			m_ReferenceTransform.Rotation = dQuaternion.mul(orbit.OrbitRotation, qaut);
#endif
		}

		if (m_Animator.IndexStart >= 0)
			m_PlanetOrbits[m_Animator.IndexStart].LineMaterial.SetFloat("_FadeAmount", 1 - math.smoothstep(0.25f, 0.75f, m_Animator.Progress));
		if (m_Animator.IndexEnd >= 0)
			m_PlanetOrbits[m_Animator.IndexEnd].LineMaterial.SetFloat("_FadeAmount", math.smoothstep(0.25f, 0.75f, m_Animator.Progress));

		m_SunObject.transform.localPosition = -(float3)(m_ReferenceTransform.Position / m_ReferenceTransform.Scale);
		m_SunObject.transform.localScale = Vector3.one * (float)(m_SunRadiusInAU / m_ReferenceTransform.Scale);

		foreach (var orbit in m_PlanetOrbits)
		{
			orbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
			orbit.OrbitLineObject.transform.localPosition = (float3)((orbit.ParentPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
			orbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);
			if (orbit.OrbitingObject != null)
			{
				orbit.OrbitingObject.transform.localPosition = (float3)((orbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
				orbit.OrbitingObject.transform.localScale = Vector3.one * (float)(orbit.RadiusInAU / m_ReferenceTransform.Scale);

				if (orbit.Script != null)
					orbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(orbit.PlanetPositionWorld));
			}

			foreach (var moonOrbit in orbit.MoonOrbits)
			{
				moonOrbit.LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)((moonOrbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale));
				moonOrbit.OrbitLineObject.transform.localPosition = (float3)((moonOrbit.ParentPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
				moonOrbit.OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / m_ReferenceTransform.Scale);

				if (moonOrbit.OrbitingObject != null)
				{
					moonOrbit.OrbitingObject.transform.localPosition = (float3)((moonOrbit.PlanetPositionWorld - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
					moonOrbit.OrbitingObject.transform.localScale = Vector3.one * (float)(moonOrbit.RadiusInAU / m_ReferenceTransform.Scale);

					if (moonOrbit.Script != null)
						moonOrbit.Script.SunDirection = (float3)dQuaternion.mul(dQuaternion.inverse(m_ReferenceTransform.Rotation), math.normalize(moonOrbit.PlanetPositionWorld));
				}
			}
		}

		transform.localRotation = (Quaternion)dQuaternion.inverse(m_ReferenceTransform.Rotation);
	}

	private void InitOrbits()
	{
		foreach (var orbit in m_PlanetOrbits)
			orbit.Init(TimeInDays);
	}

	private void UpdateOrbits()
	{
		foreach (var orbit in m_PlanetOrbits)
			orbit.Update(TimeInDays);
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

	private static double GetSpeedInOrbit(S_OrbitSettings orbit, double time, double epsilon)
	{
		return math.distance(GetPositionInOrbit(orbit, time - epsilon * orbit.OrbitalPeriod), GetPositionInOrbit(orbit, time + epsilon * orbit.OrbitalPeriod));
	}

	public static double3 GetPositionInOrbit(S_OrbitSettings orbit, double time)
	{
		double a = orbit.MeanDistance;
		double e = orbit.Eccentricity;
		time /= orbit.OrbitalPeriod;

		double M = time * math.PI_DBL * 2 + math.radians(orbit.MeanLongitudeAtEpoch - orbit.LongitudeOfPeriapsis);
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

	public static double AUtoKM(double au) => au * (1.496e+8 / 10);
	public static double KMtoAU(double km) => km / (1.496e+8 / 10);
	public static double GetScaleFromPlanet(float radius, float scaleToSize) => KMtoAU(radius * 10) * scaleToSize * 0.5;

}*/
