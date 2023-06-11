using Ephemeris;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "O_NewOrbitSettings", menuName = "Solar System/Orbit Settings")]
public class S_OrbitSettings : ScriptableObject
{
	public string OrbitName => m_OrbitName;
	public OrbitType OrbitType => m_OrbitType;
	public RotationModelType RotationModelType => m_RotationModelType;
	public GameObject OrbitingObject => m_OrbitingObject;
	public S_OrbitSettings[] SatelliteOrbits => m_SatelliteOrbits;
	public Color DisplayColor => m_DisplayColor;

	[SerializeField]
	private string m_OrbitName = "Unnamed Orbit";

	[SerializeField]
	private OrbitType m_OrbitType = OrbitType.Earth;
	[SerializeField]
	private RotationModelType m_RotationModelType = RotationModelType.Earth;

	[SerializeField]
	private GameObject m_OrbitingObject;
	[SerializeField]
	private S_OrbitSettings[] m_SatelliteOrbits;

	[Header("Display Settings")]
	[SerializeField]
	private Color m_DisplayColor = new(1, 1, 1, 0.25f);
}
