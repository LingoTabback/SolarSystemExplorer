using CustomMath;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class S_Sun : S_CelestialBody
{
	[SerializeField]
	private float m_Radius = 69550;
	[SerializeField]
	private float m_ScaleToSize = 2;

	//public override CelestialBodyType Type => CelestialBodyType.Star;
	public override double ScaledRadius => CMath.KMtoAU(m_Radius * 10) * m_ScaleToSize * 0.5;
	public override double Radius => m_Radius * 10;

	private GameObject m_SunObject;

	public override void SetScale(float scale) => m_SunObject.transform.localScale = Vector3.one * scale;
	public override void SetSpin(in dQuaternion spin) => m_SunObject.transform.localRotation = (Quaternion)spin;
	public override void SetSunDirection(float3 direction) {}

	// Start is called before the first frame update
	void Start()
	{
		InitObjects();
	}

	void OnValidate()
	{
		InitObjects();
	}

	private void InitObjects()
	{
		m_SunObject = gameObject.transform.Find("SolarSurface").gameObject;
		m_SunObject.TryGetComponent<S_LandmarkManager>(out m_LandmarksManager);
	}
}
