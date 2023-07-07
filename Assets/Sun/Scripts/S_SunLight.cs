using Ephemeris;
using Unity.Mathematics;
using UnityEngine;

public class S_SunLight : MonoBehaviour
{
	private S_SolarSystem m_SolarSystem;

	// Start is called before the first frame update
	private void Start()
	{
		m_SolarSystem = FindAnyObjectByType<S_SolarSystem>();
	}

	// Update is called once per frame
	private void Update()
	{
		double3 sunPosition = m_SolarSystem.GetBodyPositionInScene(OrbitType.Sun);
		double3 sunDirection = math.normalize((float3)Camera.main.transform.position - sunPosition);
		transform.rotation = Quaternion.FromToRotation(Vector3.forward, (float3)sunDirection);
	}
}
