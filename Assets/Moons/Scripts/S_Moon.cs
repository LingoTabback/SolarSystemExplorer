using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class S_Moon : MonoBehaviour
{
	public float Radius = 173.74f;
	public float MaxElevation = 1.8f;
	public float ElevationScale = 2;
	[Range(0f, 1f)]
	public float Rotation = 0;
	public float ScaleToSize = 0.5f;

	[Header("Sun")]
	public Vector3 SunDirection = -Vector3.right;
	[ColorUsage(false, true)]
	public Color SunColor = Color.white;
	public float SunBrightness = 4;

	public Material MoonMaterial;
	private GameObject m_MoonObject;
	private Material m_MoonMaterial;

	// Start is called before the first frame update
	void Start()
	{
		InitObjects();
		InitMaterials();
	}

	private void OnValidate()
	{
		InitObjects();
		InitMaterials();
	}

	// Update is called once per frame
	void Update()
	{
		m_MoonMaterial.SetFloat("_MoonRotation", Rotation);

		UpdateLight();
	}

	private void InitObjects()
	{
		m_MoonObject = gameObject;
		m_MoonObject.transform.localScale = Vector3.one * (ScaleToSize * 0.5f);
	}

	private void InitMaterials()
	{
		m_MoonMaterial = new(MoonMaterial);
		m_MoonObject.GetComponent<MeshRenderer>().sharedMaterial = m_MoonMaterial;

		Vector3 ambientColor = Vector3.zero;

		m_MoonMaterial.SetFloat("_MoonRadius", Radius);
		m_MoonMaterial.SetFloat("_MoonRotation", Rotation);
		m_MoonMaterial.SetFloat("_MaxElevation", MaxElevation);
		m_MoonMaterial.SetFloat("_ElevationScale", ElevationScale);
		m_MoonMaterial.SetVector("_AmbientColor", ambientColor);

		UpdateLight();
	}

	private void UpdateLight()
	{
		Vector3 sunDirection = Vector3.Normalize(SunDirection);
		Color sunColor = SunColor * SunBrightness;

		m_MoonMaterial.SetVector("_SunDirection", sunDirection);
		m_MoonMaterial.SetVector("_SunColor", sunColor);
	}
}
