using AstroTime;
using CustomMath;
using Ephemeris;
using System;
using Unity.Mathematics;
using UnityEngine;

//public enum CelestialBodyType : byte
//{
//	Planet = 0,
//	Moon,
//	Star
//}

public abstract class S_CelestialBody : MonoBehaviour
{
	public CelestialBodyType Type => m_Type;
	public OrbitType BodyIndex => m_BodyIndex;
	public abstract double Radius { get; }
	public int HumanVisitors => m_HumanVisitors;
	public int Moonwalkers => m_Moonwalkers;
	public int RoboticVisits => m_RoboticVisits;
	public Date DiscoveryDate => new(m_DiscoveryDate.Year, m_DiscoveryDate.Month, m_DiscoveryDate.Day);
	public static Date InvalidDiscoveryDate => new(0, 1, 2);
	public abstract double ScaledRadius { get; }
	public double ScaledRadiusInSolarSystem => ScaledRadius / (ParentSystem != null ? ParentSystem.CurrentReferenceTransform.Scale : 1);
	public abstract void SetSpin(in dQuaternion spin);
	public abstract void SetScale(float scale);
	public abstract void SetSunDirection(float3 direction);
	public virtual void SetShadowSpheres(float4[] spheres) { }
	public OrbitID ID { get; set; } = OrbitID.Invalid; // only changed in S_SolarSystem!
	public S_SolarSystem ParentSystem { get; set; } = null; // only changed in S_SolarSystem!
	public void Focus() => ParentSystem.SetFocus(ID);
	public string BodyName => m_BodyName;
	public double3 PositionInSystem => ParentSystem.GetBodyPositionInSystem(ID);
	public double OrbitalPeriod => ParentSystem.GetOrbitalPeriod(ID);
	public bool IsFocused => ParentSystem.FocusedOrbit == ID & ID != OrbitID.Invalid;
	public virtual double SurfaceTemparature => 0;
	public virtual string AtmosphereComposition => "Gas 1, Gas 2, Gas 3.";

	[SerializeField]
	private CelestialBodyType m_Type = CelestialBodyType.Unknown;
	[SerializeField]
	protected string m_BodyName = "Unnamed";
	[SerializeField]
	private OrbitType m_BodyIndex = OrbitType.None;
	[SerializeField]
	private int m_HumanVisitors = -1;
	[SerializeField]
	private int m_Moonwalkers = -1;
	[SerializeField]
	private int m_RoboticVisits = -1;

	[Serializable]
	private class DiscoveryDateClass
	{
		public int Year = InvalidDiscoveryDate.Year;
		public int Month = InvalidDiscoveryDate.Month;
		public int Day = InvalidDiscoveryDate.Day;
	}
	[SerializeField]
	private DiscoveryDateClass m_DiscoveryDate = new();

	public static S_CelestialBody GetCelestialBodyComponent(GameObject obj)
	{
		if (obj.TryGetComponent(out S_Planet planetScript))
			return planetScript;
		else if (obj.TryGetComponent(out S_Moon moonScript))
			return moonScript;
		else if (obj.TryGetComponent(out S_Sun sunScript))
			return sunScript;
		return null;
	}

}
