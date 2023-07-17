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
using Animation;

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
	public double BarycentricDynamicalTime => m_BarycentricDynamicalTime;
	public Date Date => TimeUtil.TDBtoUTC(m_BarycentricDynamicalTime);
	public OrbitID FocusedOrbit => m_FocusedOrbit;
	public ReferenceTransform CurrentReferenceTransform => m_ReferenceTransform;
	public double CurrentOrbitalPeriod
	{
		get
		{
			if (m_FocusedOrbit.Valid && m_AllOrbits != null && m_AllOrbits[(int)m_FocusedOrbit] != null)
				return m_AllOrbits[(int)m_FocusedOrbit].Orbit.Period;
			return 365.25;
		}
	}
	public double CurrentRotationalPeriod
	{
		get
		{
			if (m_FocusedOrbit == m_OrbitDict[(int)OrbitType.Sun])
				return m_SunRotation.Period;
			if (m_FocusedOrbit.Valid && m_AllOrbits != null && m_AllOrbits[(int)m_FocusedOrbit] != null)
				return m_AllOrbits[(int)m_FocusedOrbit].RotationalModel.Period;
			return 1;
		}
	}
	public S_CelestialBody FocusedBody
	{
		get
		{
			if (!m_FocusedOrbit.Valid)
				return null;
			if (m_FocusedOrbit == m_OrbitDict[(int)OrbitType.Sun])
				return m_SunScript;
			if (m_AllOrbits != null && (int)m_FocusedOrbit < m_AllOrbits.Count)
				return m_AllOrbits[(int)m_FocusedOrbit].Script;
			return null;
		}
	}

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
	private bool m_StartAtCurrentTime = false;
	[SerializeField]
	private bool m_Paused = true;

	private double m_TimeScale = TimeUtil.SecsToDays(1);
	private double m_BarycentricDynamicalTime = TimeUtil.J2000;

	private static readonly int s_LinePointCount = 200;
	private static double s_PlanetScale = 1;

	private Orbit m_SunOrbit;
	private RotationModel m_SunRotation;
	private OrbitWrapper[] m_PlanetOrbits;
	private List<OrbitWrapper> m_AllOrbits;
	private OrbitID[] m_OrbitDict;

	private OrbitID m_FocusedOrbit = OrbitID.Invalid;
	private bool m_FocusChanged = false;

	private GameObject m_SunObject;
	private S_Sun m_SunScript;
	private double3 m_SunDisplayPosition = 0;
	private double3 m_SunPosition = 0;
	private double m_SunRadiusInAU = 1;
	private bool m_SunFocusable = true;

	private Animator<ReferenceTransform> m_Animator = Animator<ReferenceTransform>.CreateDone(ReferenceTransform.Identity, ReferenceTransform.Identity, 1, EasingType.EaseOutSine);
	private ReferenceTransform m_ReferenceTransform = ReferenceTransform.Identity;

	public void SetTime(double tdb) => m_BarycentricDynamicalTime = tdb;
	public void SetTime(in Date date) => m_BarycentricDynamicalTime = TimeUtil.UTCtoTDB(date);

	public void SetFocus(OrbitID id)
	{
		OrbitID newID = m_AllOrbits != null && (int)id < m_AllOrbits.Count && (int)id >= 0 ? id : OrbitID.Invalid;
		if (newID != m_FocusedOrbit)
		{
			if (m_FocusedOrbit.Valid)
			{
				if (m_FocusedOrbit == 0)
					m_SunScript.OnFocusLoosing();
				else
					m_AllOrbits[(int)m_FocusedOrbit].Script.OnFocusLoosing();
			}

			m_FocusedOrbit = newID;
			m_FocusChanged = true;
			MarkFocusableOrbits();
		}
	}

	public void SetFocus(OrbitType type)
	{
		OrbitID newID = m_AllOrbits != null && (int)type >= 0 && (int)type <= (int)OrbitType.Sun ? m_OrbitDict[(int)type] : OrbitID.Invalid;
		SetFocus(newID);
	}

	public bool IsOrbitFocusable(OrbitID id) => (int)id >= 0 & (int)id < m_AllOrbits.Count && ((int)id == 0 ? m_SunFocusable : m_AllOrbits[(int)id].Focusable);

	public double3 GetBodyPositionInSystem(OrbitID id)
	{
		if (m_OrbitDict[(int)OrbitType.Sun] == id)
			return m_SunPosition;
		return (int)id < 0 | (int)id >= m_AllOrbits.Count ? 0 : (m_AllOrbits[(int)id] == null ? 0 : m_AllOrbits[(int)id].BodyPositionWorld);
	}
	public double3 GetBodyPositionInSystem(OrbitType type)
	{
		if (type == OrbitType.Sun)
			return m_SunPosition;
		OrbitID id = m_AllOrbits != null && (int)type >= 0 && (int)type <= (int)OrbitType.Sun ? m_OrbitDict[(int)type] : OrbitID.Invalid;
		return (int)id < 0 | (int)id >= m_AllOrbits.Count ? 0 : (m_AllOrbits[(int)id] == null ? 0 : m_AllOrbits[(int)id].BodyPositionWorld);
	}

	public double3 GetBodyPositionInScene(OrbitID id)
	{
		double3 localPosition = GetBodyPositionInSystem(id);
		return dQuaternion.mul((dQuaternion)transform.rotation,
			(localPosition - m_ReferenceTransform.Position)
			* m_ReferenceTransform.InvScale) + (float3)transform.position;
	}
	public double3 GetBodyPositionInScene(OrbitType type)
	{
		double3 localPosition = GetBodyPositionInSystem(type);
		return dQuaternion.mul((dQuaternion)transform.rotation,
			(localPosition - m_ReferenceTransform.Position)
			* m_ReferenceTransform.InvScale) + (float3)transform.position;
	}

	public double GetOrbitalPeriod(OrbitID id) => (int)id < 0 | (int)id >= m_AllOrbits.Count ? 0 : m_AllOrbits[(int)id].Orbit.Period;

	// Start is called before the first frame update
	void Start()
	{
		s_PlanetScale = m_PlanetScale;

		m_SunOrbit = Orbit.Create(OrbitType.Sun);
		m_SunRotation = RotationModel.Create(RotationModelType.Sun);
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

		if (m_StartAtCurrentTime)
			SetTime(Date.Now);
	}

	private static double3 GetOffsetOnCircle(double3 from, double3 to, double radius)
	{
		double3 direction = from - to;
		direction.y = 0;
		direction = math.dot(direction, direction) <= 0 ? new double3(1, 0, 0) : math.normalize(direction);
		return direction * radius;
	}

	private void UpdateFocus()
	{
		if (!m_FocusChanged)
			return;

		if (!m_FocusedOrbit.Valid) // go back to view of complete solar system
		{
			ReferenceTransform from = m_ReferenceTransform;
			from.ID = m_Animator.End.ID;
			m_Animator.Reset(from, ReferenceTransform.Identity, m_TransitionTime);
		}
		else if (m_AllOrbits[(int)m_FocusedOrbit] == null) // focus on sun
		{
			double3 finalPosition = m_SunDisplayPosition - m_ReferenceTransform.Position;
			double3 correctedPosition = dQuaternion.mul(m_ReferenceTransform.InvRotation, finalPosition);
			correctedPosition.y = 0;

			double3 camPosition = (double3)(float3)(Camera.main.transform.position - transform.position);
			camPosition.y = 0;
			double camRadius = math.length(camPosition) * m_SunRadiusInAU;
			camRadius = math.clamp(camRadius, m_SunRadiusInAU * 1.4, m_SunRadiusInAU * 3);
			double3 nextOffset = GetOffsetOnCircle(camPosition * m_ReferenceTransform.Scale, correctedPosition, camRadius);
			nextOffset -= (camPosition + (float3)transform.position) * m_SunRadiusInAU;

			ReferenceTransform target = new(m_SunDisplayPosition, dQuaternion.identity, m_SunRadiusInAU, nextOffset, m_FocusedOrbit);
			ReferenceTransform from = m_ReferenceTransform;
			from.ID = m_Animator.End.ID;
			m_Animator.Reset(from, target, m_TransitionTime);
		}
		else // focus planet or moon
		{
			var orbit = m_AllOrbits[(int)m_FocusedOrbit];

			double3 finalPosition = orbit.BodyPositionWorld - m_ReferenceTransform.Position;
			double3 correctedPosition = dQuaternion.mul(m_ReferenceTransform.InvRotation, finalPosition);
			correctedPosition.y = 0;

			double3 camPosition = (double3)(float3)(Camera.main.transform.position - transform.position);
			camPosition.y = 0;
			double camRadius = math.length(camPosition) * orbit.BodyRadiusInAU;
			camRadius = math.clamp(camRadius, orbit.BodyRadiusInAU * 1.4, orbit.BodyRadiusInAU * 3);
			double3 nextOffset = GetOffsetOnCircle(camPosition * m_ReferenceTransform.Scale, correctedPosition, camRadius);
			nextOffset -= (camPosition + (float3)transform.position) * orbit.BodyRadiusInAU;

			double3 pos = orbit.BodyPositionWorld;
			dQuaternion rotation = dQuaternion.identity;

			ReferenceTransform target = new(pos, rotation, orbit.BodyRadiusInAU, nextOffset, m_FocusedOrbit);
			if (m_Animator.Start.ID.Valid)
				m_AllOrbits[(int)m_Animator.Start.ID]?.LineMaterial.SetFloat("_FadeAmount", 0f);
			ReferenceTransform from = m_ReferenceTransform;
			from.ID = m_Animator.End.ID;
			m_Animator.Reset(from, target, m_TransitionTime);
		}

		m_FocusChanged = false;
	}

	// Update is called once per frame
	void Update()
	{
		SetTime(m_BarycentricDynamicalTime + Time.deltaTime * (m_Paused ? 0.0 : m_TimeScale));
		UpdateOrbits();

		UpdateFocus();

		bool doneThisFrame = !m_Animator.IsDone;
		m_Animator.Update(Time.deltaTime);
		doneThisFrame &= m_Animator.IsDone;
		if (!m_Animator.IsDone || doneThisFrame)
			m_ReferenceTransform = m_Animator.Current;
		else if (m_Animator.Current.ID.Valid && m_AllOrbits[(int)m_Animator.Current.ID] != null)
		{
			var orbit = m_AllOrbits[(int)m_Animator.Current.ID];

			m_ReferenceTransform.Position = orbit.BodyPositionWorld;
			/*
#if ROTATION_1
			double angle = math.atan2(orbit.PlanetPositionRelative.z, orbit.PlanetPositionRelative.x);
			dQuaternion qaut = dQuaternion.RotateY(-angle);
			m_ReferenceTransform.Rotation = dQuaternion.mul(orbit.OrbitRotation, qaut);
#endif*/
		}

		if (doneThisFrame && m_Animator.Current.ID.Valid)
		{
			if (m_Animator.Current.ID == 0)
				m_SunScript.OnFocusGained();
			else
				m_AllOrbits[(int)m_Animator.Current.ID].Script.OnFocusGained();
		}

		if (m_Animator.Start.ID.Valid)
			m_AllOrbits[(int)m_Animator.Start.ID]?.LineMaterial.SetFloat("_FadeAmount", 1 - math.smoothstep(0.25f, 0.75f, m_Animator.Progress));
		if (m_Animator.End.ID.Valid)
			m_AllOrbits[(int)m_Animator.End.ID]?.LineMaterial.SetFloat("_FadeAmount", math.smoothstep(0.25f, 0.75f, m_Animator.Progress));

		m_SunObject.transform.SetLocalPositionAndRotation(
			(float3)((m_SunDisplayPosition - m_ReferenceTransform.Position) * m_ReferenceTransform.InvScale),
			(quaternion)m_SunRotation.ComputeEquatorOrientation(m_BarycentricDynamicalTime));

		m_SunScript.SetSpin(m_SunRotation.ComputeSpin(m_BarycentricDynamicalTime));
		m_SunScript.SetScale((float)(m_SunRadiusInAU * m_ReferenceTransform.InvScale));

		foreach (var orbit in m_PlanetOrbits)
			orbit.UpdateTransforms(m_ReferenceTransform, m_SunDisplayPosition);

		transform.localRotation = (Quaternion)m_ReferenceTransform.InvRotation;
		transform.position = -(float3)(m_ReferenceTransform.WorldOffset * m_ReferenceTransform.InvScale);
	}

	private void MarkFocusableOrbits()
	{
		m_SunFocusable = m_FocusedOrbit != 0;
		foreach (var orbit in m_PlanetOrbits)
			orbit.MarkFocusable(m_FocusedOrbit, true, false);
	}

	private void InitOrbits()
	{
		foreach (var orbit in m_PlanetOrbits)
			orbit.Init(m_BarycentricDynamicalTime);
	}

	private void UpdateOrbits()
	{
		m_SunPosition = 0; // dQuaternion.mul(OrbitWrapper.s_SunOrientationQuat, m_SunOrbit.PositionAtTime(m_BarycentricDynamicalTime));
		m_SunDisplayPosition = 0;

		foreach (var orbit in m_PlanetOrbits)
			orbit.Update(m_BarycentricDynamicalTime);
	}

	public static double GetScaleFromPlanet(double scaledRadius) => scaledRadius * s_PlanetScale;

	public struct ReferenceTransform : IAnimatable<ReferenceTransform>
	{
		public double3 Position;
		public dQuaternion Rotation { get => m_Rotation; set { m_Rotation = value; InvRotation = dQuaternion.inverse(value); } }
		public double Scale { get => m_Scale; set { m_Scale = value; InvScale = 1 / value; } }
		public double3 WorldOffset;
		public OrbitID ID;

		public dQuaternion InvRotation { get; private set; }
		public double InvScale { get; private set; }

		private dQuaternion m_Rotation;
		private double m_Scale;

		public static readonly ReferenceTransform Identity = new(0, dQuaternion.identity, 1, 0, OrbitID.Invalid);

		public ReferenceTransform(in double3 position, in dQuaternion rotation, double scale, in double3 worldOffset, OrbitID id)
		{
			Position = position;
			m_Rotation = rotation;
			m_Scale = scale;
			WorldOffset = worldOffset;
			ID = id;

			InvRotation = dQuaternion.inverse(m_Rotation);
			InvScale = 1 / m_Scale;
		}

		public ReferenceTransform Lerp(in ReferenceTransform to, float alpha)
		{
			return new(
				math.lerp(Position, to.Position, alpha),
				dQuaternion.slerp(Rotation, to.Rotation, alpha),
				math.lerp(Scale, to.Scale, alpha),
				math.lerp(WorldOffset, to.WorldOffset, alpha),
				alpha >= 1 ? to.ID : ID);
		}
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
		public Orbit Orbit => m_Orbit;
		public RotationModel RotationalModel => m_RotationModel;

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

			Script = OrbitingObject.GetComponent<S_CelestialBody>(); ;
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

		public void MarkFocusable(OrbitID currentFocus, bool parentFocused, bool siblingFocused)
		{
			bool focused = currentFocus == m_ID;
			Focusable = !focused & (parentFocused | siblingFocused);
			bool childFocused = false;
			foreach (var orbit in m_Satellites)
				childFocused |= orbit.m_ID == currentFocus;
			foreach (var orbit in m_Satellites)
				orbit.MarkFocusable(currentFocus, focused, childFocused);
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
				int i = numVerts;
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
			double3 finalPosition = (BodyPositionWorld - referenceTransform.Position) * referenceTransform.InvScale;
			LineMaterial.SetVector("_FadeCenter", (Vector3)(float3)finalPosition);
			OrbitLineObject.transform.localPosition = (float3)((ParentPosition - referenceTransform.Position) * referenceTransform.InvScale);
			OrbitLineObject.transform.localScale = Vector3.one * (float)referenceTransform.InvScale;

			m_ShadowSphere = new float4((float3)(dQuaternion.mul(referenceTransform.InvRotation, finalPosition)
				- referenceTransform.WorldOffset * referenceTransform.InvScale), (float)(BodyRadiusInAU * referenceTransform.InvScale));

			if (m_Parent == null)
				OrbitLineObject.transform.localRotation = (Quaternion)s_SunOrientationQuat;
			else
				OrbitLineObject.transform.localRotation = (Quaternion)ParentOrientation;

			if (OrbitingObject != null)
			{
				//OrbitingObject.transform.localScale = Vector3.one * (float)(BodyRadiusInAU / referenceTransform.Scale);
				OrbitingObject.transform.SetLocalPositionAndRotation(
					(float3)((BodyPositionWorld - referenceTransform.Position) * referenceTransform.InvScale),
					(Quaternion)EquatorOrientationWorld);
			}

			if (Script != null)
			{
				Script.SetSpin(Spin);
				Script.SetScale((float)(BodyRadiusInAU * referenceTransform.InvScale));
				Script.SetSunDirection((float3)dQuaternion.mul(referenceTransform.InvRotation, math.normalize(BodyPositionWorld - sunPosition)));
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
