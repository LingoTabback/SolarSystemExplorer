using Ephemeris;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "O_NewOrbitSettings", menuName = "Solar System/Orbit Settings")]
public class S_OrbitSettings : ScriptableObject
{
	public string Name = "Unnamed Orbit";

	public OrbitType OrbitType = OrbitType.Earth;
	public RotationModelType RotationModelType = RotationModelType.Earth;

	public GameObject OrbitingObject;
	public S_OrbitSettings[] SatelliteOrbits;

	[Header("Display Settings")]
	public Color DisplayColor = new(1, 1, 1, 0.25f);
}
