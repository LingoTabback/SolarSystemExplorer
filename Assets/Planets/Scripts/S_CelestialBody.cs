using Animation;
using AstroTime;
using CustomMath;
using Ephemeris;
using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
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
	public double ScaledRadiusInSolarSystem => ScaledRadius * (ParentSystem != null ? ParentSystem.CurrentReferenceTransform.InvScale : 1);
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

	public Action FocusGained;
	public Action FocusLoosing;

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
	private DiscoveryDateClass m_DiscoveryDate = new();

	public void OnFocusGained() => FocusGained?.Invoke();
	public void OnFocusLoosing() => FocusLoosing?.Invoke();

}
