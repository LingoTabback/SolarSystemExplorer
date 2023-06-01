using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "O_NewOrbitSettings", menuName = "Solar System/Orbit Settings")]
public class S_OrbitSettings : ScriptableObject
{
	[Serializable]
	public enum Epochs
	{
		J1900 = 0,
		J2000
	}

	public string Name = "Unnamed Orbit";

	[Header("Orbital Elements")]
	public double MeanDistance = 1;
	[Range(0f, 1f)]
	public double Eccentricity = 0.01673;
	public float Inclination = 0;
	public float LongitudeOfPeriapsis = 102.93f;
	public float LongitudeOfAscendingNode = 0;
	public double MeanLongitudeAtEpoch = 100.47;
	public double OrbitalPeriod = 365.2;
	public Epochs Epoch = Epochs.J2000;

	[Header("New Settings")]
	public int EpochYear = 0;
	public double EpochDay = 0;
	public double DerivativeOfMeanMotion = 0;
	public double DragTerm = 0;
	public double RightAscension = 0;
	public double ArgOfPeriapsis = 0;
	public double MeanAnomalyAtEpoch = 0;
	public double MeanMotion = 0;

	public GameObject OrbitingObject;
	public S_OrbitSettings[] SatelliteOrbits;

	[Header("Display Settings")]
	public Color DisplayColor = new(1, 1, 1, 0.25f);
}
