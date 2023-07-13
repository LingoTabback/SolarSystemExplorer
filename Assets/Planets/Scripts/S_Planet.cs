using CustomMath;
using System;
using Unity.Mathematics;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;

public class S_Planet : S_CelestialBody
{
	[SerializeField]
	private float m_Radius = 636;
	[SerializeField]
	private float m_MaxElevation = 0.88f;
	[SerializeField]
	private float m_ElevationScale = 2;
	[SerializeField]
	private float m_CloudsHeight = 1.2f;
	[ColorUsage(false, true)]
	[SerializeField]
	private Color m_EmissiveColor = new(1, 0.7f, 0.4f);
	[SerializeField]
	private float m_EmissiveBrightness = 2;

	[Range(0f, 1f)]
	[SerializeField]
	private float m_CloudsRotation = 0;

	[Range(0f, 360f)]
	[SerializeField]
	private float m_Meridian = 0;

	[SerializeField]
	private float m_ScaleToSize = 2;

	[Serializable]
	public class RingSettings
	{
		public bool Enabled = false;
		public float InnerRadius = 6690;
		public float OuterRadius = 13678;
		public float LightingEccentricity = 0;
	}
	[SerializeField]
	private RingSettings m_Rings;

	[Header("Sun")]
	[SerializeField]
	private Vector3 m_SunDirection = -Vector3.right;
	[ColorUsage(false, true)]
	[SerializeField]
	private Color m_SunColor = Color.white;
	[SerializeField]
	private float m_SunBrightness = 16;

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
	[SerializeField]
	private AtmosphereSettings m_Atmosphere;

	[SerializeField]
	private Material m_PlanetMaterialTemplate;
	[SerializeField]
	private Material m_CloudsMaterialTemplate;
	[SerializeField]
	private Material m_AtmosphereAbsorptionMaterialTemplate;
	[SerializeField]
	private Material m_AtmosphereScatteringMaterialTemplate;
	[SerializeField]
	private Material m_RingsMaterialTemplate;

	private GameObject m_PlanetObject;
	private GameObject m_CloudsObject;
	private GameObject m_AtmosphereAbsorptionObject;
	private GameObject m_AtmosphereScatteringObject;
	private GameObject m_RingsObject;

	private MeshRenderer m_AtmosphereAbsorptionRenderer;
	private MeshRenderer m_AtmosphereScatteringRenderer;
	private MeshRenderer m_RingsRenderer;

	private Material m_PlanetMaterial;
	private Material m_CloudsMaterial;
	private Material m_AtmosphereAbsorptionMaterial;
	private Material m_AtmosphereScatteringMaterial;
	private Material m_RingsMaterial;

	private RenderTexture m_GroundTransmittanceTexture;
	private RenderTexture m_AtmosphereTransmittanceTexture;
	private RenderTexture m_AtmosphereScatteringTexture;
	private static ComputeShader s_AtmospherePrecompShader;

	//public override CelestialBodyType Type => CelestialBodyType.Planet;
	public override double ScaledRadius => CMath.KMtoAU(m_Radius * 10) * m_ScaleToSize * 0.5;
	public override double Radius => m_Radius * 10;

	// Start is called before the first frame update
	void Start()
	{
		LoadShaders();
		InitObjects();
		InitAtmosphere();
		InitMaterials();

		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
	}

	void OnDestroy()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
	}

	void OnValidate()
	{
		LoadShaders();
		InitObjects();
		InitAtmosphere();
		InitMaterials();

		m_CloudsMaterial.renderQueue = (int)RenderQueue.Transparent - 3;
		m_AtmosphereAbsorptionMaterial.renderQueue = (int)RenderQueue.Transparent - 2;
		m_AtmosphereScatteringMaterial.renderQueue = (int)RenderQueue.Transparent - 1;
		m_RingsMaterial.renderQueue = (int)RenderQueue.Transparent;
	}

	// Update is called once per frame
	private void Update()
	{
		float rotation = m_Meridian / 360;
		m_PlanetMaterial.SetFloat("_PlanetRotation", rotation);
		m_PlanetMaterial.SetFloat("_CloudsRotation", rotation + m_CloudsRotation);
		m_CloudsMaterial.SetFloat("_CloudsRotation", rotation + m_CloudsRotation);

		UpdateLight();
	}

	public override void SetSpin(in dQuaternion spin) => m_PlanetObject.transform.localRotation = (Quaternion)spin;
	public override void SetScale(float scale) => m_PlanetObject.transform.localScale = Vector3.one * scale;
	public override void SetSunDirection(float3 direction) => m_SunDirection = direction;
	public override void SetShadowSpheres(float4[] spheres)
	{
		if (spheres == null)
		{
			m_PlanetMaterial.SetVector("_ShadowSphereAlphas", Vector4.zero);
			return;
		}
		
		int iters = math.min(spheres.Length, 4);
		float4 alphas = 0;
		
		for (int i = 0; i < iters; ++i)
		{
			m_PlanetMaterial.SetVector("_ShadowSphere" + i, spheres[i]);
			m_CloudsMaterial.SetVector("_ShadowSphere" + i, spheres[i]);
			m_AtmosphereScatteringMaterial.SetVector("_ShadowSphere" + i, spheres[i]);
			m_RingsMaterial.SetVector("_ShadowSphere" + i, spheres[i]);
			alphas[i] = 1;
		}

		m_PlanetMaterial.SetVector("_ShadowSphereAlphas", alphas);
		m_CloudsMaterial.SetVector("_ShadowSphereAlphas", alphas);
		m_AtmosphereScatteringMaterial.SetVector("_ShadowSphereAlphas", alphas);
		m_RingsMaterial.SetVector("_ShadowSphereAlphas", alphas);
	}

	private void InitObjects()
	{
		m_PlanetObject = gameObject.transform.Find("SurfaceMesh").gameObject;
		m_CloudsObject = m_PlanetObject.transform.Find("CloudsMesh").gameObject;
		m_AtmosphereAbsorptionObject = m_PlanetObject.transform.Find("AtmosphereAbsorptionMesh").gameObject;
		m_AtmosphereScatteringObject = m_PlanetObject.transform.Find("AtmosphereScatteringMesh").gameObject;
		m_RingsObject = m_PlanetObject.transform.Find("Rings").gameObject;

		m_PlanetObject.TryGetComponent<S_LandmarkManager>(out m_LandmarksManager);

		m_AtmosphereAbsorptionObject.TryGetComponent(out m_AtmosphereAbsorptionRenderer);
		m_AtmosphereScatteringObject.TryGetComponent(out m_AtmosphereScatteringRenderer);
		m_RingsObject.TryGetComponent(out m_RingsRenderer);

		m_PlanetObject.transform.localScale = Vector3.one * (m_ScaleToSize * 0.5f);
		m_PlanetObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
		m_CloudsObject.transform.localScale = Vector3.one * (1 + m_CloudsHeight / m_Radius);
		m_CloudsObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		m_AtmosphereAbsorptionObject.transform.localScale = Vector3.one * (1 + m_Atmosphere.AtmosphereHeight * m_Atmosphere.AtmosphereScale / m_Radius);
		m_AtmosphereScatteringObject.transform.localScale = Vector3.one * (1 + m_Atmosphere.AtmosphereHeight * m_Atmosphere.AtmosphereScale / m_Radius);
		m_AtmosphereAbsorptionObject.transform.localPosition = Vector3.zero;
		m_AtmosphereScatteringObject.transform.localPosition = Vector3.zero;

		m_RingsObject.SetActive(m_Rings.Enabled);
		m_RingsObject.transform.localScale = Vector3.one * (m_Rings.OuterRadius / m_Radius);
	}

	private void InitMaterials()
	{
		m_PlanetMaterial = new(m_PlanetMaterialTemplate);
		m_CloudsMaterial = new(m_CloudsMaterialTemplate);
		m_AtmosphereAbsorptionMaterial = new(m_AtmosphereAbsorptionMaterialTemplate);
		m_AtmosphereScatteringMaterial = new(m_AtmosphereScatteringMaterialTemplate);
		m_RingsMaterial = new(m_RingsMaterialTemplate);

		m_PlanetObject.GetComponent<MeshRenderer>().sharedMaterial = m_PlanetMaterial;
		m_CloudsObject.GetComponent<MeshRenderer>().sharedMaterial = m_CloudsMaterial;
		m_AtmosphereAbsorptionObject.GetComponent<MeshRenderer>().sharedMaterial = m_AtmosphereAbsorptionMaterial;
		m_AtmosphereScatteringObject.GetComponent<MeshRenderer>().sharedMaterial = m_AtmosphereScatteringMaterial;
		m_RingsObject.GetComponent<MeshRenderer>().sharedMaterial = m_RingsMaterial;

		Vector3 ambientColor = Vector3.Normalize(new Vector3(m_Atmosphere.RayleighScattering.r, m_Atmosphere.RayleighScattering.g, m_Atmosphere.RayleighScattering.b)) * 0.01f;
		float rotation = m_Meridian / 360;

		m_PlanetMaterial.SetFloat("_PlanetRadius", m_Radius);
		m_PlanetMaterial.SetFloat("_PlanetRotation", rotation);
		m_PlanetMaterial.SetFloat("_MaxElevation", m_MaxElevation);
		m_PlanetMaterial.SetFloat("_ElevationScale", m_ElevationScale);
		m_PlanetMaterial.SetFloat("_CloudsHeight", m_CloudsHeight);
		m_PlanetMaterial.SetFloat("_CloudsRotation", rotation + m_CloudsRotation);
		m_PlanetMaterial.SetVector("_AmbientColor", ambientColor);
		m_PlanetMaterial.SetVector("_EmissiveColor", m_EmissiveColor * m_EmissiveBrightness);
		m_PlanetMaterial.SetTexture("_GroundTransmittanceTexture", m_GroundTransmittanceTexture);
		m_PlanetMaterial.SetVector("_RingPositionRelative", m_RingsObject.transform.localPosition);
		m_PlanetMaterial.SetVector("_RingNormalRelative", m_RingsObject.transform.localRotation * Vector3.up);
		m_PlanetMaterial.SetFloat("_RingInnerRadiusRelative", m_Rings.InnerRadius / m_Radius);
		m_PlanetMaterial.SetFloat("_RingOuterRadiusRelative", m_Rings.OuterRadius / m_Radius);

		m_CloudsMaterial.SetFloat("_CloudsRotation", rotation + m_CloudsRotation);
		m_CloudsMaterial.SetFloat("_GroundTransmittanceCoordY", m_CloudsHeight / m_Atmosphere.AtmosphereHeight * 0.1f);
		m_CloudsMaterial.SetVector("_AmbientColor", ambientColor);
		m_CloudsMaterial.SetTexture("_GroundTransmittanceTexture", m_GroundTransmittanceTexture);

		float cosMaxSunAngle = math.cos(math.radians(m_Atmosphere.MaxSunAngle));
		m_AtmosphereAbsorptionMaterial.SetTexture("_TransmittanceTexture", m_AtmosphereTransmittanceTexture);
		m_AtmosphereAbsorptionMaterial.SetFloat("_BottomRadius", m_Radius);
		m_AtmosphereAbsorptionMaterial.SetFloat("_TopRadius", m_Radius + m_Atmosphere.AtmosphereHeight * m_Atmosphere.AtmosphereScale);
		m_AtmosphereAbsorptionMaterial.SetFloat("_CosineMaxSunAngle", cosMaxSunAngle);

		m_AtmosphereScatteringMaterial.SetTexture("_AtmosphereDepthTexture", m_GroundTransmittanceTexture);
		m_AtmosphereScatteringMaterial.SetTexture("_TransmittanceTexture", m_AtmosphereTransmittanceTexture);
		m_AtmosphereScatteringMaterial.SetTexture("_ScatteringTexture", m_AtmosphereScatteringTexture);
		m_AtmosphereScatteringMaterial.SetFloat("_BottomRadius", m_Radius);
		float atmosphereTopRadius = m_Radius + m_Atmosphere.AtmosphereHeight * m_Atmosphere.AtmosphereScale;
		m_AtmosphereScatteringMaterial.SetFloat("_TopRadius", atmosphereTopRadius);
		m_AtmosphereScatteringMaterial.SetVector("_RayleighScattering", m_Atmosphere.RayleighScattering * m_Atmosphere.RayleighScale / m_Atmosphere.AtmosphereScale);
		m_AtmosphereScatteringMaterial.SetVector("_MieScattering", m_Atmosphere.MieScattering * m_Atmosphere.MieScatteringScale / m_Atmosphere.AtmosphereScale);
		m_AtmosphereScatteringMaterial.SetFloat("_MiePhaseFunctionG", m_Atmosphere.MieAnisotropy);
		m_AtmosphereScatteringMaterial.SetFloat("_CosineMaxSunAngle", cosMaxSunAngle);
		m_AtmosphereScatteringMaterial.SetVector("_RingPositionRelative", m_RingsObject.transform.localPosition);
		m_AtmosphereScatteringMaterial.SetVector("_RingNormalRelative", m_RingsObject.transform.localRotation * Vector3.up);
		m_AtmosphereScatteringMaterial.SetFloat("_RingInnerRadiusRelative", m_Rings.InnerRadius / atmosphereTopRadius);
		m_AtmosphereScatteringMaterial.SetFloat("_RingOuterRadiusRelative", m_Rings.OuterRadius / atmosphereTopRadius);

		m_RingsMaterial.SetFloat("_InnerRadius", m_Rings.InnerRadius / m_Rings.OuterRadius);
		m_RingsMaterial.SetFloat("_LightingEccentricity", m_Rings.LightingEccentricity);
		m_RingsMaterial.SetVector("_PlanetPositionRelative", Vector3.zero);
		m_RingsMaterial.SetFloat("_PlanetRadiusRelative", m_Radius / m_Rings.OuterRadius);

		SetShadowSpheres(null);
		UpdateLight();
	}

	private void UpdateLight()
	{
		Vector3 sunDirection = Vector3.Normalize(m_SunDirection);
		Color sunColor = m_SunColor * m_SunBrightness;

		m_PlanetMaterial.SetVector("_SunDirection", sunDirection);
		m_PlanetMaterial.SetVector("_SunColor", sunColor);

		m_CloudsMaterial.SetVector("_SunDirection", sunDirection);
		m_CloudsMaterial.SetVector("_SunColor", sunColor);

		m_AtmosphereScatteringMaterial.SetVector("_SunDirection", sunDirection);
		m_AtmosphereScatteringMaterial.SetVector("_SunColor", sunColor);

		m_RingsMaterial.SetVector("_SunDirection", sunDirection);
		m_RingsMaterial.SetVector("_SunColor", sunColor);
	}

	private void InitAtmosphere()
	{
		if (m_GroundTransmittanceTexture != null)
			m_GroundTransmittanceTexture.Release();
		m_GroundTransmittanceTexture = new(128, 128, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
		{
			enableRandomWrite = true,
			useMipMap = false,
			autoGenerateMips = false,
			filterMode = FilterMode.Bilinear,
			wrapMode = TextureWrapMode.Clamp,
		};
		m_GroundTransmittanceTexture.Create();

		if (m_AtmosphereTransmittanceTexture != null)
			m_AtmosphereTransmittanceTexture.Release();
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

		if (m_AtmosphereScatteringTexture != null)
			m_AtmosphereScatteringTexture.Release();
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

		s_AtmospherePrecompShader.SetFloat("u_BottomRadius", m_Radius);
		float cosMaxSunAngle = math.cos(math.radians(m_Atmosphere.MaxSunAngle));
		s_AtmospherePrecompShader.SetFloat("u_CosineMaxSunAngle", cosMaxSunAngle);

		// No scaling for accurate ground lighting
		s_AtmospherePrecompShader.SetFloat("u_TopRadius", m_Radius + m_Atmosphere.AtmosphereHeight);
		s_AtmospherePrecompShader.SetFloat("u_RayleighExpScale", -1.0f / m_Atmosphere.RayleighExponent);
		s_AtmospherePrecompShader.SetVector("u_RayleighScattering", m_Atmosphere.RayleighScattering * m_Atmosphere.RayleighScale);
		s_AtmospherePrecompShader.SetFloat("u_MieExpScale", -1.0f / m_Atmosphere.MieExponent);
		s_AtmospherePrecompShader.SetVector("u_MieScattering", m_Atmosphere.MieScattering * m_Atmosphere.MieScatteringScale);
		s_AtmospherePrecompShader.SetVector("u_MieExtinction", m_Atmosphere.MieExtinction * m_Atmosphere.MieExtinctionScale);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipAltitude", m_Atmosphere.TipAltitude);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipWidth", m_Atmosphere.TipWidth);
		s_AtmospherePrecompShader.SetVector("u_AbsorptionExtinction", m_Atmosphere.Absorption * m_Atmosphere.AbsorptionScale * m_Atmosphere.TipValue);

		int groundTransmittanceKernel = s_AtmospherePrecompShader.FindKernel("CSPrecompGroundTransmittance");
		s_AtmospherePrecompShader.SetTexture(groundTransmittanceKernel, "u_OutGroundTransmittanceTexture", m_GroundTransmittanceTexture, 0);
		s_AtmospherePrecompShader.SetVector("u_InvResolution", new(1f / m_GroundTransmittanceTexture.width, 1f / m_GroundTransmittanceTexture.height, 0, 0));
		DispatchCompute(s_AtmospherePrecompShader, groundTransmittanceKernel, m_GroundTransmittanceTexture.width, m_GroundTransmittanceTexture.height);

		s_AtmospherePrecompShader.SetFloat("u_TopRadius", m_Radius + m_Atmosphere.AtmosphereHeight * m_Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_RayleighExpScale", -1.0f / (m_Atmosphere.RayleighExponent * m_Atmosphere.AtmosphereScale));
		s_AtmospherePrecompShader.SetVector("u_RayleighScattering", m_Atmosphere.RayleighScattering * m_Atmosphere.RayleighScale / m_Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_MieExpScale", -1.0f / (m_Atmosphere.MieExponent * m_Atmosphere.AtmosphereScale));
		s_AtmospherePrecompShader.SetVector("u_MieScattering", m_Atmosphere.MieScattering * m_Atmosphere.MieScatteringScale / m_Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetVector("u_MieExtinction", m_Atmosphere.MieExtinction * m_Atmosphere.MieExtinctionScale / m_Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipAltitude", m_Atmosphere.TipAltitude * m_Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetFloat("u_AbsorptionTipWidth", m_Atmosphere.TipWidth * m_Atmosphere.AtmosphereScale);
		s_AtmospherePrecompShader.SetVector("u_AbsorptionExtinction", m_Atmosphere.Absorption * m_Atmosphere.AbsorptionScale * m_Atmosphere.TipValue / m_Atmosphere.AtmosphereScale);

		int transmittanceKernel = s_AtmospherePrecompShader.FindKernel("CSPrecompTransmittance");
		s_AtmospherePrecompShader.SetTexture(transmittanceKernel, "u_OutTransmittanceTexture", m_AtmosphereTransmittanceTexture, 0);
		DispatchCompute(s_AtmospherePrecompShader, transmittanceKernel, m_AtmosphereTransmittanceTexture.width, m_AtmosphereTransmittanceTexture.height);

		int scatteringKernel = s_AtmospherePrecompShader.FindKernel("CSPrecompScattering");
		s_AtmospherePrecompShader.SetTexture(scatteringKernel, "u_OutScatteringTexture", m_AtmosphereScatteringTexture, 0);
		s_AtmospherePrecompShader.SetTexture(scatteringKernel, "u_TransmittanceTexture", m_AtmosphereTransmittanceTexture);
		DispatchCompute(s_AtmospherePrecompShader, scatteringKernel, m_AtmosphereScatteringTexture.width, m_AtmosphereScatteringTexture.height, m_AtmosphereScatteringTexture.volumeDepth);
	}

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		const float eps = 0.001f;
		Vector3 camForward = camera.transform.forward;

		Bounds adjustedBounds = m_AtmosphereAbsorptionRenderer.bounds;
		adjustedBounds.center = m_AtmosphereAbsorptionRenderer.transform.position - camForward * eps;
		adjustedBounds.extents = Vector3.Scale(m_AtmosphereAbsorptionRenderer.localBounds.extents,
			m_AtmosphereAbsorptionRenderer.transform.lossyScale) * 1.05f;
		m_AtmosphereAbsorptionRenderer.bounds = adjustedBounds;

		adjustedBounds = m_AtmosphereScatteringRenderer.bounds;
		adjustedBounds.center = m_AtmosphereScatteringRenderer.transform.position - camForward * (eps * 2);
		adjustedBounds.extents = Vector3.Scale(m_AtmosphereScatteringRenderer.localBounds.extents,
			m_AtmosphereScatteringRenderer.transform.lossyScale) * 1.05f;
		m_AtmosphereScatteringRenderer.bounds = adjustedBounds;

		adjustedBounds = m_RingsRenderer.bounds;
		adjustedBounds.center = m_RingsRenderer.transform.position - camForward * (eps * 4);
		adjustedBounds.extents = Vector3.Scale(m_RingsRenderer.localBounds.extents,
			m_RingsRenderer.transform.lossyScale) * 1.05f;
		m_RingsRenderer.bounds = adjustedBounds;
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
