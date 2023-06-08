using CustomMath;
using Ephemeris;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum CelestialBodyType : byte
{
	Planet = 0,
	Moon
}

public abstract class S_CelestialBody : MonoBehaviour
{
	public abstract CelestialBodyType Type { get; }
	public abstract double ScaledRadius { get; }
	public double ScaledRadiusInSolarSystem => ScaledRadius / (ParentSystem != null ? ParentSystem.CurrentReferenceTransform.Scale : 1);
	public abstract void SetSpin(in dQuaternion spin);
	public abstract void SetSunDirection(float3 direction);
	public virtual void SetShadowSpheres(float4[] spheres) { }
	public OrbitType CurrentOrbit { get; set; } = OrbitType.None; // only changed in S_SolarSystem
	public S_SolarSystem ParentSystem { get; set; } = null; // only changed in S_SolarSystem
	public void Focus() => ParentSystem.SetFocus(CurrentOrbit);

}
