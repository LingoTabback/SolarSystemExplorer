#define ROTATION_1
//#define ROTATION_2
//#define ROTATION_3
//#define ROTATION_4

using UnityEngine;
using CustomMath;
using System;
using Unity.Mathematics;
using AstroTime;
using Ephemeris;
using System.Collections.Generic;
using UnityEngine.Animations;

public struct OrbitID
{
	public OrbitID(int id) => m_ID = id;
	public static readonly OrbitID Invalid = -1;
	public bool Valid => m_ID >= 0;

	public static implicit operator OrbitID(int id) => new(id);
	public static explicit operator int(OrbitID id) => id.m_ID;
	public static bool operator ==(OrbitID l, OrbitID r) => l.m_ID == r.m_ID;
	public static bool operator !=(OrbitID l, OrbitID r) => l.m_ID != r.m_ID;

	public override bool Equals(object obj) => obj is OrbitID id && m_ID == id.m_ID;
	public override int GetHashCode() => HashCode.Combine(m_ID);
	public override string ToString() => m_ID.ToString();

	private int m_ID;
}

public class S_SolarSystem : MonoBehaviour
{
	public bool Paused { get => m_Paused; set => m_Paused = value; }
	public double TimeScale { get => m_TimeScale; set => m_TimeScale = value; }
	public Date Date => TimeUtil.TDBtoUTC(m_BarycentricDynamicalTime);
	public OrbitID FocusedOrbit => m_FocusedOrbit;
	public ReferenceTransform CurrentReferenceTransform => m_ReferenceTransform;

	[SerializeField]
	private S_OrbitSettings[] m_OrbitsSettingsObjects;
	[SerializeField]
	private GameObject m_SunPrefab;
	[SerializeField]
	private float m_SunRadius = 69550;

	[Header("Transitions")]
	[SerializeField]
	private float m_TransitionTime = 5;

	[Header("Rendering")]
	[SerializeField]
	private double m_PlanetScale = 1;
	[SerializeField]
	private Material m_LineMaterial;
	
	[Header("Time")]
	[SerializeField]
	private bool m_Paused = true;

	private double m_TimeScale = TimeUtil.SecsToDays(1);
	private double m_BarycentricDynamicalTime = TimeUtil.J2000;

	private static readonly int s_LinePointCount = 200;
	private static double s_PlanetScale = 1;

	private Orbit m_SunOrbit;
	private OrbitWrapper[] m_PlanetOrbits;
	private List<OrbitWrapper> m_AllOrbits;
	private OrbitID[] m_OrbitDict;

	private OrbitID m_FocusedOrbit = OrbitID.Invalid;
	private bool m_FocusChanged = false;

	private GameObject m_SunObject;
	private S_Sun m_SunScript;
	private double3 m_SunPosition = 0;
	private double m_SunRadiusInAU = 1;
	private bool m_SunFocusable = true;

	private TransformAnimator m_Animator = new();
	private ReferenceTransform m_ReferenceTransform = ReferenceTransform.Identity;

	public void SetTime(double tdb) => m_BarycentricDynamicalTime = tdb;
	public void SetTime(in Date date) => m_BarycentricDynamicalTime = TimeUtil.UTCtoTDB(date);

	public void SetFocus(OrbitID id)
	{
		OrbitID newID = m_AllOrbits != null && (int)id < m_AllOrbits.Count && (int)id >= 0 ? id : OrbitID.Invalid;
		if (newID != m_FocusedOrbit)
		{
			m_FocusedOrbit = newID;
			m_FocusChanged = true;
			MarkFocusableOrbits();
		}
	}

	public void SetFocus(OrbitType type)
	{
		OrbitID newID = m_AllOrbits != null && (int)type >= 0 && (int)type <= (int)OrbitType.Sun ? m_OrbitDict[(int)type] : OrbitID.Invalid;
		if (newID != m_FocusedOrbit)
		{
			m_FocusedOrbit = newID;
			m_FocusChanged = true;
			MarkFocusableOrbits();
		}
	}

	public bool IsOrbitFocusable(OrbitID id) => (int)id < 0 | (int)id >= m_AllOrbits.Count ? false : ((int)id == 0 ? m_SunFocusable : m_AllOrbits[(int)id].Focusable);

	// Start is called before the first frame update
	void Start()
	{
		s_PlanetScale = m_PlanetScale;

		m_SunOrbit = Orbit.Create(OrbitType.Sun);
		m_SunObject = Instantiate(m_SunPrefab, transform, false);
		m_SunScript = m_SunObject.GetComponent<S_Sun>();
		m_SunScript.ID = 0;
		m_SunScript.ParentSystem = this;
		m_SunRadiusInAU = GetScaleFromPlanet(m_SunScript.ScaledRadius);

		m_PlanetOrbits = new OrbitWrapper[m_OrbitsSettingsObjects.Length];
		for (int i = 0; i < m_OrbitsSettingsObjects.Length; ++i)
			m_PlanetOrbits[i] = OrbitWrapper.Create(m_OrbitsSettingsObjects[i], transform, m_LineMaterial);

		m_AllOrbits = new() { null };
		m_OrbitDict = new OrbitID[(int)OrbitType.Sun + 1];
		Array.Fill(m_OrbitDict, OrbitID.Invalid);
		m_OrbitDict[(int)OrbitType.Sun] = 0;

		foreach (OrbitWrapper orbit in m_PlanetOrbits)
			orbit.CollectFocusableOrbits(m_AllOrbits, m_OrbitDict);

		InitOrbits();

		foreach (var orbit in m_PlanetOrbits)
			orbit.SpawnPrefab(transform, this);

		MarkFocusableOrbits();
	}

	// Update is called once per frame
	void Update()
	{
		SetTime(m_BarycentricDynamicalTime + Time.deltaTime * (m_Paused ? 0.0 : m_TimeScale));
		UpdateOrbits();

		if (m_FocusChanged)
		{
			if (!m_FocusedOrbit.Valid)
				m_Animator = new TransformAnimator(m_ReferenceTransform, ReferenceTransform.Identity, m_Animator.EndID, m_FocusedOrbit, m_TransitionTime);
			else if (m_AllOrbits[(int)m_FocusedOrbit] == null)
			{
				ReferenceTransform target = new()
				{
					Position = 0,
					Rotation = dQuaternion.identity,
					Scale = m_SunRadiusInAU
				};
				m_Animator = new TransformAnimator(m_ReferenceTransform, target, m_Animator.EndID, m_FocusedOrbit, m_TransitionTime);
			}
			else
			{
				var orbit = m_AllOrbits[(int)m_FocusedOrbit];
				double3 pos = orbit.BodyPositionWorld;
				dQuaternion rotation = dQuaternion.identity;

				ReferenceTransform target = new()
				{
					Position = pos,
					Rotation = rotation,
					Scale = orbit.BodyRadiusInAU
				};
				if (m_Animator.StartID.Valid)
					m_AllOrbits[(int)m_Animator.StartID]?.LineMaterial.SetFloat("_FadeAmount", 0f);
				m_Animator = new TransformAnimator(m_ReferenceTransform, target, m_Animator.EndID, m_FocusedOrbit, m_TransitionTime);
			}

			m_FocusChanged = false;
		}

		bool doneThisFrame = !m_Animator.IsDone;
		m_Animator.Update();
		doneThisFrame &= m_Animator.IsDone;
		if (!m_Animator.IsDone || doneThisFrame)
			m_ReferenceTransform = m_Animator.TransformCurrent;
		else if (m_Animator.CurrentID.Valid && m_AllOrbits[(int)m_Animator.CurrentID] != null)
		{
			var orbit = m_AllOrbits[(int)m_Animator.CurrentID];

			m_ReferenceTransform.Position = orbit.BodyPositionWorld;
			/*
#if ROTATION_1
			double angle = math.atan2(orbit.PlanetPositionRelative.z, orbit.PlanetPositionRelative.x);
			dQuaternion qaut = dQuaternion.RotateY(-angle);
			m_ReferenceTransform.Rotation = dQuaternion.mul(orbit.OrbitRotation, qaut);
#endif*/
		}

		if (m_Animator.StartID.Valid)
			m_AllOrbits[(int)m_Animator.StartID]?.LineMaterial.SetFloat("_FadeAmount", 1 - math.smoothstep(0.25f, 0.75f, m_Animator.Progress));
		if (m_Animator.EndID.Valid)
			m_AllOrbits[(int)m_Animator.EndID]?.LineMaterial.SetFloat("_FadeAmount", math.smoothstep(0.25f, 0.75f, m_Animator.Progress));

		m_SunObject.transform.localPosition = (float3)((m_SunPosition - m_ReferenceTransform.Position) / m_ReferenceTransform.Scale);
		m_SunScript.SetScale((float)(m_SunRadiusInAU / m_ReferenceTransform.Scale));

		foreach (var orbit in m_PlanetOrbits)
			orbit.UpdateTransforms(m_ReferenceTransform, m_SunPosition);

		transform.localRotation = (Quaternion)dQuaternion.inverse(m_ReferenceTransform.Rotation);
	}

	private void MarkFocusableOrbits()
	{
		m_SunFocusable = m_FocusedOrbit != 0;
		foreach (var orbit in m_PlanetOrbits)
			orbit.MarkFocusable(m_FocusedOrbit, true);
	}

	private void InitOrbits()
	{
		foreach (var orbit in m_PlanetOrbits)
			orbit.Init(m_BarycentricDynamicalTime);
	}

	private void UpdateOrbits()
	{
		m_SunPosition = dQuaternion.mul(OrbitWrapper.s_SunOrientationQuat, m_SunOrbit.PositionAtTime(m_BarycentricDynamicalTime)) * 0;

		foreach (var orbit in m_PlanetOrbits)
			orbit.Update(m_BarycentricDynamicalTime);
	}

	public static double GetScaleFromPlanet(double scaledRadius) => scaledRadius * s_PlanetScale;

	public struct ReferenceTransform
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
		public ReferenceTransform TransformCurrent { get => m_TransformCurrent; }
		private ReferenceTransform m_TransformCurrent = ReferenceTransform.Identity;

		public OrbitID StartID { get; private set; } = OrbitID.Invalid;
		public OrbitID EndID { get; private set; } = OrbitID.Invalid;
		public OrbitID CurrentID { get; private set; } = OrbitID.Invalid;
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

		public TransformAnimator(in ReferenceTransform start, in ReferenceTransform end, OrbitID indexStart, OrbitID indexEnd, float length)
		{
			TransformStart = start;
			TransformEnd = end;
			m_TransformCurrent = start;
			StartID = indexStart;
			EndID = indexEnd;
			CurrentID = indexStart;
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
				CurrentID = EndID;
				IsDone = true;
			}

			Progress = EaseOutSine(m_Time / Length);
			m_TransformCurrent.Position = math.lerp(TransformStart.Position, TransformEnd.Position, Progress);
			m_TransformCurrent.Rotation = dQuaternion.slerp(TransformStart.Rotation, TransformEnd.Rotation, Progress);
			m_TransformCurrent.Scale = math.lerp(TransformStart.Scale, TransformEnd.Scale, Progress);
		}

		private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
		private static float EaseOutSine(float x) => math.sin(x * math.PI * 0.5f);
	}

	private class OrbitWrapper
	{
		public S_OrbitSettings Settings;
		public GameObject OrbitLineObject;
		public GameObject OrbitingObject;
		public S_CelestialBody Script;
		public Mesh LineMesh;
		public Material LineMaterial;

		public double3 BodyPositionRelative = 0;
		public double3 BodyPositionWorld = 0;
		public dQuaternion EquatorOrientationRelative = dQuaternion.identity;
		public dQuaternion EquatorOrientationWorld = dQuaternion.identity;
		public dQuaternion Spin = dQuaternion.identity;
		public double BodyRadiusInAU = 1;

		public bool Focusable { get; private set; } = false;

		public double3 ParentPosition => m_Parent != null ? m_Parent.BodyPositionWorld : double3.zero;
		public dQuaternion ParentOrientation => m_Parent != null ? m_Parent.EquatorOrientationWorld : dQuaternion.identity;

		private Orbit m_Orbit;
		private RotationModel m_RotationModel;
		private OrbitWrapper m_Parent;
		private OrbitWrapper[] m_Satellites;

		private float4[] m_SatelliteShadowSpheres;
		private float4 m_ShadowSphere = 0;

		private double m_LineStartJulian = 0;
		private double m_LineEndJulian = 0;
		private Vector3[] m_Vertices;
		private Vector2[] m_UVs;

		private OrbitID m_ID = OrbitID.Invalid;

		public OrbitWrapper(Orbit orbit, RotationModel frame, S_OrbitSettings settings, Transform systemTransform, Material lineMaterial, OrbitWrapper parent = null)
		{
			m_Parent = parent;
			m_Orbit = orbit;
			m_RotationModel = frame;

			Settings = settings;
			OrbitLineObject = new GameObject(settings.OrbitName);
			OrbitLineObject.transform.SetParent(systemTransform);
			LineMaterial = new Material(lineMaterial);
			LineMaterial.SetColor("_Color", settings.DisplayColor.linear);
			LineMaterial.SetFloat("_FadeAmount", 0f);

			LineMesh = new Mesh();
			OrbitLineObject.AddComponent<MeshFilter>().sharedMesh = LineMesh;
			OrbitLineObject.AddComponent<MeshRenderer>().sharedMaterial = LineMaterial;

			if (settings.OrbitingObject.TryGetComponent(out S_Planet planetScript))
				BodyRadiusInAU = GetScaleFromPlanet(planetScript.ScaledRadius);
			else if (settings.OrbitingObject.TryGetComponent(out S_Moon moonScript))
				BodyRadiusInAU = GetScaleFromPlanet(moonScript.ScaledRadius);

			m_Satellites = new OrbitWrapper[settings.SatelliteOrbits.Length];
			for (int i = 0; i < m_Satellites.Length; ++i)
				m_Satellites[i] = Create(settings.SatelliteOrbits[i], systemTransform, lineMaterial, this);

			m_SatelliteShadowSpheres = new float4[m_Parent == null ? m_Satellites.Length : (m_Satellites.Length + 1)];
		}

		public void CollectFocusableOrbits(List<OrbitWrapper> allOrbits, OrbitID[] orbitDict)
		{
			m_ID = allOrbits.Count;
			allOrbits.Add(this);
			if ((int)Settings.OrbitType >= 0 && (int)Settings.OrbitType < (int)OrbitType.Sun)
				orbitDict[(int)Settings.OrbitType] = m_ID;

			foreach (var satellite in m_Satellites)
				satellite.CollectFocusableOrbits(allOrbits, orbitDict);
		}

		public void SpawnPrefab(Transform systemTransform, S_SolarSystem system)
		{
			DestroyPrefab();

			OrbitingObject = Instantiate(Settings.OrbitingObject);
			OrbitingObject.transform.SetParent(systemTransform, false);
			OrbitingObject.transform.localRotation = (Quaternion)EquatorOrientationWorld;

			Script = S_CelestialBody.GetCelestialBodyComponent(OrbitingObject);
			Script.ID = m_ID;
			Script.ParentSystem = system;

			foreach (var satellite in m_Satellites)
				satellite.SpawnPrefab(systemTransform, system);
		}

		public void DestroyPrefab()
		{
			if (OrbitingObject != null)
				Destroy(OrbitingObject);

			OrbitingObject = null;

			foreach (var satellite in m_Satellites)
				satellite.DestroyPrefab();
		}

		public void Init(double t)
		{
			EquatorOrientationRelative = m_RotationModel.ComputeEquatorOrientation(t);
			EquatorOrientationWorld = dQuaternion.mul(ParentOrientation, EquatorOrientationRelative);
			Spin = m_RotationModel.ComputeSpin(t);
			BodyPositionRelative = m_Orbit.PositionAtTime(t);
			BodyPositionWorld = dQuaternion.mul(ParentOrientation, BodyPositionRelative) + ParentPosition;

			LineMaterial.SetFloat("_PositionInOrbit", 0);
			m_Vertices = new Vector3[s_LinePointCount + 1];
			m_UVs = new Vector2[s_LinePointCount + 1];

			InitOrbitLine(t);

			foreach (var satellite in m_Satellites)
				satellite.Init(t);
		}

		private void InitOrbitLine(double t)
		{
			m_LineStartJulian = t - m_Orbit.Period;
			m_LineEndJulian = t + m_Orbit.Period / (s_LinePointCount - 1);

			int[] indices = new int[s_LinePointCount * 2];

			for (int i = 0; i < s_LinePointCount; ++i)
			{
				indices[i * 2] = i;
				indices[i * 2 + 1] = i + 1;
			}

			for (int i = 0; i < s_LinePointCount + 1; ++i)
			{
				double time = i / (double)(s_LinePointCount - 1) * m_Orbit.Period + m_LineStartJulian;
				double3 point = m_Orbit.PositionAtTime(time);
				m_Vertices[i] = (float3)point;
				m_UVs[i] = new Vector2(i / (float)(s_LinePointCount - 1), 0);
			}

			LineMesh.vertices = m_Vertices;
			LineMesh.uv = m_UVs;
			LineMesh.SetIndices(indices, MeshTopology.Lines, 0, true);
		}

		public void MarkFocusable(OrbitID currentFocus, bool parentFocused)
		{
			bool focused = currentFocus == m_ID;
			Focusable = !focused & parentFocused;
			foreach (var orbit in m_Satellites)
				orbit.MarkFocusable(currentFocus, focused);
		}

		private void UpdateOrbitLine(double t)
		{
			double segmentTime = m_Orbit.Period / (s_LinePointCount - 1);

			double dateDiffEnd = t - m_LineEndJulian;
			double dateDiffStart = m_LineEndJulian - t - segmentTime;

			if (dateDiffEnd > 0.0)
			{
				int numVerts = 1 + (int)(dateDiffEnd / segmentTime);

				for (int vId = 0; vId < s_LinePointCount + 1 - numVerts; ++vId)
					m_Vertices[vId] = m_Vertices[vId + numVerts];

				int i = 1 + math.max(numVerts - s_LinePointCount - 1, 0);
				for (int vId = math.max(s_LinePointCount + 1 - numVerts, 0); vId < s_LinePointCount + 1; ++vId)
				{
					m_Vertices[vId] = (float3)m_Orbit.PositionAtTime(m_LineEndJulian + segmentTime * i);
					++i;
				}

				m_LineStartJulian += segmentTime * numVerts;
				m_LineEndJulian += segmentTime * numVerts;

				LineMesh.SetVertices(m_Vertices);
			}
			else if (dateDiffStart > 0.0)
			{
				int numVerts = 1 + (int)(dateDiffStart / segmentTime);

				for (int vId = s_LinePointCount; vId >= numVerts; --vId)
					m_Vertices[vId] = m_Vertices[vId - numVerts];

				
				int iters = math.min(numVerts, s_LinePointCount + 1);
				int i = numVerts - 1;
				for (int vId = 0; vId < iters; ++vId)
				{
					m_Vertices[vId] = (float3)m_Orbit.PositionAtTime(m_LineStartJulian - segmentTime * i);
					--i;
				}

				m_LineStartJulian -= segmentTime * numVerts;
				m_LineEndJulian -= segmentTime * numVerts;

				LineMesh.SetVertices(m_Vertices);
			}
		}

		public void Update(double t)
		{
			EquatorOrientationRelative = m_RotationModel.ComputeEquatorOrientation(t);
			EquatorOrientationWorld = dQuaternion.mul(ParentOrientation, EquatorOrientationRelative);
			Spin = m_RotationModel.ComputeSpin(t);
			BodyPositionRelative = m_Orbit.PositionAtTime(t);
			if (m_Parent == null)
			{
				if (Settings.RotationModelType == RotationModelType.Earth)
					EquatorOrientationWorld = dQuaternion.mul(s_SunOrientationQuat, EquatorOrientationRelative);
				BodyPositionWorld = dQuaternion.mul(s_SunOrientationQuat, BodyPositionRelative) + ParentPosition;
			}
			else
				BodyPositionWorld = dQuaternion.mul(ParentOrientation, BodyPositionRelative) + ParentPosition;

			UpdateOrbitLine(t);
			LineMaterial.SetFloat("_PositionInOrbit", (float)((t - m_LineStartJulian) / m_Orbit.Period));

			foreach (var satellite in m_Satellites)
				satellite.Update(t);
		}

		public void UpdateTransforms(in ReferenceTransform referenceTransform, double3 sunPosition)
		{
			double3 finalPosition = (BodyPositionWorld - referenceTransform.Position) / referenceTransform.Scale;
			LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)finalPosition);
			OrbitLineObject.transform.localPosition = (float3)((ParentPosition - referenceTransform.Position) / referenceTransform.Scale);
			OrbitLineObject.transform.localScale = Vector3.one * (float)(1d / referenceTransform.Scale);

			m_ShadowSphere = new float4((float3)dQuaternion.mul(dQuaternion.inverse(referenceTransform.Rotation), finalPosition), (float)(BodyRadiusInAU / referenceTransform.Scale));

			if (m_Parent == null)
				OrbitLineObject.transform.localRotation = (Quaternion)s_SunOrientationQuat;
			else
				OrbitLineObject.transform.localRotation = (Quaternion)ParentOrientation;

			if (OrbitingObject != null)
			{
				OrbitingObject.transform.localPosition = (float3)((BodyPositionWorld - referenceTransform.Position) / referenceTransform.Scale);
				//OrbitingObject.transform.localScale = Vector3.one * (float)(BodyRadiusInAU / referenceTransform.Scale);
				OrbitingObject.transform.localRotation = (Quaternion)EquatorOrientationWorld;
			}

			if (Script != null)
			{
				Script.SetSpin(Spin);
				Script.SetScale((float)(BodyRadiusInAU / referenceTransform.Scale));
				Script.SetSunDirection((float3)dQuaternion.mul(dQuaternion.inverse(referenceTransform.Rotation), math.normalize(BodyPositionWorld - sunPosition)));
			}

			foreach (var satellite in m_Satellites)
				satellite.UpdateTransforms(referenceTransform, sunPosition);

			int shadowIndexOffset = m_Parent == null ? 0 : 1;
			for (int i = 0; i < m_Satellites.Length; ++i)
				m_SatelliteShadowSpheres[i + shadowIndexOffset] = m_Satellites[i].m_ShadowSphere;
			if (m_Parent != null)
				m_SatelliteShadowSpheres[0] = m_Parent.m_ShadowSphere;

			if (Script != null)
				Script.SetShadowSpheres(m_SatelliteShadowSpheres);
		}

		public static OrbitWrapper Create(S_OrbitSettings settings, Transform systemTransform, Material lineMaterial, OrbitWrapper parent = null)
		{
			return new OrbitWrapper(Orbit.Create(settings.OrbitType), RotationModel.Create(settings.RotationModelType), settings, systemTransform, lineMaterial, parent);
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
}
