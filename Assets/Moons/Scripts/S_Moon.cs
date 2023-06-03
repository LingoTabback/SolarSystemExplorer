using CustomMath;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

public class S_Moon : MonoBehaviour
{
	public float Radius = 173.74f;
	public float MaxElevation = 1.8f;
	public float ElevationScale = 2;

	[Range(0f, 360f)]
	public float Meridian = 0;

	public float ScaleToSize = 0.5f;

	[Header("Sun")]
	public Vector3 SunDirection = -Vector3.right;
	[ColorUsage(false, true)]
	public Color SunColor = Color.white;
	public float SunBrightness = 4;

	public bool HasAtmosphere = false;
	[Serializable]
	public class AtmosphereSettings
	{
		public float AtmosphereHeight = 6;
		public float AtmosphereScale = 2;

		[Header("Rayleigh")]
		[ColorUsage(false, true)]
		public Color RayleighScattering = new(0.175f, 0.409f, 1);
		public float RayleighScale = 0.331f;
		public float RayleighExponent = 0.8f;

		[Header("Mie")]
		[ColorUsage(false, true)]
		public Color MieScattering = new(1, 1, 1);
		public float MieScatteringScale = 0.03996f;
		[ColorUsage(false, true)]
		public Color MieExtinction = new(1, 1, 1);
		public float MieExtinctionScale = 1.1f;
		public float MieAnisotropy = 0.8f;
		public float MieExponent = 0.12f;

		[Header("Absorption")]
		[ColorUsage(false, true)]
		public Color Absorption = new(0.345f, 1.0f, 0.045f);
		public float AbsorptionScale = 0.01881f;
		public float TipAltitude = 2.5f;
		public float TipWidth = 1.5f;
		public float TipValue = 0.75f;

		[Header("Precomputation")]
		[Range(90f, 180f)]
		public float MaxSunAngle = 105;
	}
	public AtmosphereSettings Atmosphere;

	public Material MoonMaterial;
	public Material AtmosphereAbsorptionMaterial;
	public Material AtmosphereScatteringMaterial;

	private GameObject m_MoonObject;
	private GameObject m_AtmosphereAbsorptionObject;
	private GameObject m_AtmosphereScatteringObject;

	private Material m_AtmosphereAbsorptionMaterial;
	private Material m_AtmosphereScatteringMaterial;
	private Material m_MoonMaterial;

	private RenderTexture m_GroundTransmittanceTexture;
	private RenderTexture m_AtmosphereTransmittanceTexture;
	private RenderTexture m_AtmosphereScatteringTexture;
	private static ComputeShader s_AtmospherePrecompShader;

	// Start is called before the first frame update
	void Start()
	{
		LoadShaders();
		InitObjects();
		InitAtmosphere();
		InitMaterials();
	}

	private void OnValidate()
	{
		LoadShaders();
		InitObjects();
		InitAtmosphere();
		InitMaterials();
	}

	// Update is called once per frame
	void Update()
	{
		float rotation = Meridian / 360;
		m_MoonMaterial.SetFloat("_MoonRotation", rotation);

		UpdateLight();
	}

	public void SetSpin(in dQuaternion spin)
	{
		m_MoonObject.transform.localRotation = (Quaternion)spin;
	}

	private void InitObjects()
	{
		m_MoonObject = gameObject.transform.Find("SurfaceMesh").gameObject;
		m_MoonObject.transform.localScale = Vector3.one * (ScaleToSize * 0.5f);

		m_AtmosphereAbsorptionObject = m_MoonObject.transform.Find("AtmosphereAbsorptionMesh").gameObject;
		m_AtmosphereScatteringObject = m_MoonObject.transform.Find("AtmosphereScatteringMesh").gameObject;

		m_AtmosphereAbsorptionObject.transform.localScale = Vector3.one * (1 + Atmosphere.AtmosphereHeight * Atmosphere.AtmosphereScale / Radius);
		m_AtmosphereScatteringObject.transform.localScale = Vector3.one * (1 + Atmosphere.AtmosphereHeight * Atmosphere.AtmosphereScale / Radius);
		m_AtmosphereAbsorptionObject.transform.localPosition = Vector3.zero;
		m_AtmosphereScatteringObject.transform.localPosition = Vector3.zero;

		m_AtmosphereAbsorptionObject.SetActive(HasAtmosphere);
		m_AtmosphereScatteringObject.SetActive(HasAtmosphere);
	}

	private void InitMaterials()
	{
		m_MoonMaterial = new(MoonMaterial);
		m_AtmosphereAbsorptionMaterial = new(AtmosphereAbsorptionMaterial);
		m_AtmosphereScatteringMaterial = new(AtmosphereScatteringMaterial);

		m_MoonObject.GetComponent<MeshRenderer>().sharedMaterial = m_MoonMaterial;
		m_AtmosphereAbsorptionObject.GetComponent<MeshRenderer>().sharedMaterial = m_AtmosphereAbsorptionMaterial;
		m_AtmosphereScatteringObject.GetComponent<MeshRenderer>().sharedMaterial = m_AtmosphereScatteringMaterial;

		Vector3 ambientColor = HasAtmosphere ? Vector3.Normalize(new(Atmosphere.RayleighScattering.r, Atmosphere.RayleighScattering.g, Atmosphere.RayleighScattering.b)) * 0.01f : Vector3.zero;
		float rotation = Meridian / 360;

		m_MoonMaterial.SetFloat("_MoonRadius", Radius);
		m_MoonMaterial.SetFloat("_MoonRotation", rotation);
		m_MoonMaterial.SetFloat("_MaxElevation", MaxElevation);
		m_MoonMaterial.SetFloat("_ElevationScale", ElevationScale);
		m_MoonMaterial.SetVector("_AmbientColor", ambientColor);

		if (HasAtmosphere)
		{
			m_MoonMaterial.SetTexture("_GroundTransmittanceTexture", m_GroundTransmittanceTexture);

			float cosMaxSunAngle = math.cos(math.radians(Atmosphere.MaxSunAngle));
			m_AtmosphereAbsorptionMaterial.SetTexture("_TransmittanceTexture", m_AtmosphereTransmittanceTexture);
			m_AtmosphereAbsorptionMaterial.SetFloat("_BottomRadius", Radius);
			m_AtmosphereAbsorptionMaterial.SetFloat("_TopRadius", Radius + Atmosphere.AtmosphereHeight * Atmosphere.AtmosphereScale);
			m_AtmosphereAbsorptionMaterial.SetFloat("_CosineMaxSunAngle", cosMaxSunAngle);

			m_AtmosphereScatteringMaterial.SetTexture("_AtmosphereDepthTexture", m_GroundTransmittanceTexture);
			m_AtmosphereScatteringMaterial.SetTexture("_TransmittanceTexture", m_AtmosphereTransmittanceTexture);
			m_AtmosphereScatteringMaterial.SetTexture("_ScatteringTexture", m_AtmosphereScatteringTexture);
			m_AtmosphereScatteringMaterial.SetFloat("_BottomRadius", Radius);
			float atmosphereTopRadius = Radius + Atmosphere.AtmosphereHeight * Atmosphere.AtmosphereScale;
			m_AtmosphereScatteringMaterial.SetFloat("_TopRadius", atmosphereTopRadius);
			m_AtmosphereScatteringMaterial.SetVector("_RayleighScattering", Atmosphere.RayleighScattering * Atmosphere.RayleighScale / Atmosphere.AtmosphereScale);
			m_AtmosphereScatteringMaterial.SetVector("_MieScattering", Atmosphere.MieScattering * Atmosphere.MieScatteringScale / Atmosphere.AtmosphereScale);
			m_AtmosphereScatteringMaterial.SetFloat("_MiePhaseFunctionG", Atmosphere.MieAnisotropy);
			m_AtmosphereScatteringMaterial.SetFloat("_CosineMaxSunAngle", cosMaxSunAngle);
		}

		UpdateLight();
	}

	private void UpdateLight()
	{
		Vector3 sunDirection = Vector3.Normalize(SunDirection);
		Color sunColor = SunColor * SunBrightness;

		m_MoonMaterial.SetVector("_SunDirection", sunDirection);
		m_MoonMaterial.SetVector("_SunColor", sunColor);

		if (HasAtmosphere)
		{
			m_AtmosphereScatteringMaterial.SetVector("_SunDirection", sunDirection);
			m_AtmosphereScatteringMaterial.SetVector("_SunColor", sunColor);
		}
	}

	private void InitAtmosphere()
	{
		if (m_AtmosphereScatteringTexture != null)
			m_AtmosphereScatteringTexture.Release();
		if (m_GroundTransmittanceTexture != null)
			m_GroundTransmittanceTexture.Release();
		if (m_AtmosphereTransmittanceTexture != null)
			m_AtmosphereTransmittanceTexture.Release();

		if (!HasAtmosphere)
			return;
		
		m_GroundTransmittanceTexture = new(128, 128, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
		{
			enableRandomWrite = true,
			useMipMap = false,
			autoGenerateMips = false,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp,
		};
		m_GroundTransmittanceTexture.Create();

		
		m_AtmosphereTransmittanceTexture = new(256, 64, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
		{
			enableRandomWrite = true,
			useMipMap = false,
			autoGenerateMips = false,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp,
		};
		m_AtmosphereTransmittanceTexture.Create();

		const int SCATTERING_TEXTURE_R_SIZE = 32;
		const int SCATTERING_TEXTURE_MU_SIZE = 128;
		const int SCATTERING_TEXTURE_MU_S_SIZE = 32;
		const int SCATTERING_TEXTURE_NU_SIZE = 8;

		const int SCATTERING_TEXTURE_WIDTH = SCATTERING_TEXTURE_NU_SIZE * SCATTERING_TEXTURE_MU_S_SIZE;
		const int SCATTERING_TEXTURE_HEIGHT = SCATTERING_TEXTURE_MU_SIZE;
		const int SCATTERING_TEXTURE_DEPTH = SCATTERING_TEXTURE_R_SIZE;

		m_AtmosphereScatteringTexture = new(SCATTERING_TEXTURE_WIDTH, SCATTERING_TEXTURE_HEIGHT, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
		{
			dimension = TextureDimension.Tex3D,
			volumeDepth = SCATTERING_TEXTURE_DEPTH,
			enableRandomWrite = true,
			useMipMap = false,
			autoGenerateMips = false,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp,
		};
		m_AtmosphereScatteringTexture.Create();

		s_AtmospherePrecompShader.SetFloat("u_BottomRadius", Radius);
		float cosMaxSunAngle = math.cos(math.radians(Atmosphere.MaxSunAngle));
		s_AtmospherePrecompShader.SetFloat("u_CosineMaxSunAngle", cosMaxSunAngle);

		// No scaling for accurate ground lighting
		s_AtmospherePrecompShader.SetFloat("u_TopRadius", Radius + Atmosphere.AtmosphereHeight);
		s_AtmospherePrecompShader.SetFloat("u_RayleighExpScale", -1.0f / Atmosphere.RayleighExponent);
		s_AtmospherePrecompShader.SetVector("u_RayleighScattering", Atmosphere.RayleighScattering * Atmosphere.RayleighScale);
		s_AtmospherePrecompShader.SetFloat("u_MieExpScale", -1.0f / Atmosphere.MieExponent);
		s_AtmospherePrecompShader.SetVector("u_MieScattering", Atmosphere.MieScattering * Atmosphere.MieScatteringScale);
		s_AtmospherePrecompShader.SetVector("u_MieExtinction", Atmosphere.MieExtinction * Atmosphere.MieExtinctionScale);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipAltitude", Atmosphere.TipAltitude);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipWidth", Atmosphere.TipWidth);
		s_AtmospherePrecompShader.SetVector("u_AbsorptionExtinction", Atmosphere.Absorption * Atmosphere.AbsorptionScale * Atmosphere.TipValue);

		int groundTransmittanceKernel = s_AtmospherePrecompShader.FindKernel("CSPrecompGroundTransmittance");
		s_AtmospherePrecompShader.SetTexture(groundTransmittanceKernel, "u_OutGroundTransmittanceTexture", m_GroundTransmittanceTexture, 0);
		s_AtmospherePrecompShader.SetVector("u_InvResolution", new(1f / m_GroundTransmittanceTexture.width, 1f / m_GroundTransmittanceTexture.height, 0, 0));
		DispatchCompute(s_AtmospherePrecompShader, groundTransmittanceKernel, m_GroundTransmittanceTexture.width, m_GroundTransmittanceTexture.height);

		s_AtmospherePrecompShader.SetFloat("u_TopRadius", Radius + Atmosphere.AtmosphereHeight * Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_RayleighExpScale", -1.0f / (Atmosphere.RayleighExponent * Atmosphere.AtmosphereScale));
		s_AtmospherePrecompShader.SetVector("u_RayleighScattering", Atmosphere.RayleighScattering * Atmosphere.RayleighScale / Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_MieExpScale", -1.0f / (Atmosphere.MieExponent * Atmosphere.AtmosphereScale));
		s_AtmospherePrecompShader.SetVector("u_MieScattering", Atmosphere.MieScattering * Atmosphere.MieScatteringScale / Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetVector("u_MieExtinction", Atmosphere.MieExtinction * Atmosphere.MieExtinctionScale / Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipAltitude", Atmosphere.TipAltitude * Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipWidth", Atmosphere.TipWidth * Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetVector("u_AbsorptionExtinction", Atmosphere.Absorption * Atmosphere.AbsorptionScale * Atmosphere.TipValue / Atmosphere.AtmosphereScale);

		int transmittanceKernel = s_AtmospherePrecompShader.FindKernel("CSPrecompTransmittance");
		s_AtmospherePrecompShader.SetTexture(transmittanceKernel, "u_OutTransmittanceTexture", m_AtmosphereTransmittanceTexture, 0);
		DispatchCompute(s_AtmospherePrecompShader, transmittanceKernel, m_AtmosphereTransmittanceTexture.width, m_AtmosphereTransmittanceTexture.height);

		int scatteringKernel = s_AtmospherePrecompShader.FindKernel("CSPrecompScattering");
		s_AtmospherePrecompShader.SetTexture(scatteringKernel, "u_OutScatteringTexture", m_AtmosphereScatteringTexture, 0);
		s_AtmospherePrecompShader.SetTexture(scatteringKernel, "u_TransmittanceTexture", m_AtmosphereTransmittanceTexture);
		DispatchCompute(s_AtmospherePrecompShader, scatteringKernel, m_AtmosphereScatteringTexture.width, m_AtmosphereScatteringTexture.height, m_AtmosphereScatteringTexture.volumeDepth);
	}

	private static void LoadShaders()
	{
		s_AtmospherePrecompShader = Addressables.LoadAssetAsync<ComputeShader>("CS_AtmospherePrecomp").WaitForCompletion();
	}

	private static void DispatchCompute(ComputeShader shader, int kernelIndex, int threadsX, int threadsY, int threadsZ = 1)
	{
		shader.GetKernelThreadGroupSizes(kernelIndex, out uint numThreadsX, out uint numThreadsY, out uint numThreadsZ);
		int nX = (threadsX + (int)numThreadsX - 1) / (int)numThreadsX;
		int nY = (threadsY + (int)numThreadsY - 1) / (int)numThreadsY;
		int nZ = (threadsZ + (int)numThreadsZ - 1) / (int)numThreadsZ;
		shader.Dispatch(kernelIndex, nX, nY, nZ);
	}
}
