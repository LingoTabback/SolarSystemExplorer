using Animation;
using CustomMath;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class S_LandmarkMarker : MonoBehaviour
{
	public string Label { get => m_LabelMesh.text; set => m_LabelMesh.text = value; }
	public Color MarkerColor
	{
		get => m_MarkerColor;
		set { m_MarkerColor = value; if (m_MarkerMaterial != null) m_MarkerMaterial.SetColor("_RingColor", m_MarkerColor); }
	}
	public S_LandmarkInfoSettings Settings { get => m_Settings; set => m_Settings = value != null ? value : S_LandmarkInfoSettings.Default; }
	public S_LandmarkManager Manager { get; set; }
	public int Index { get; set; } = -2; // only set by S_LandmarkManager!!

	private S_LandmarkInfoSettings m_Settings;

	[SerializeField]
	private float m_ScreenSize = 0.05f;
	[SerializeField]
	[ColorUsage(true, true)]
	private Color m_MarkerColor = Color.white;
	[SerializeField]
	private TextMeshPro m_LabelMesh;

	private Material m_MarkerMaterial;
	private static readonly Color s_MarkerCenterColor = new(0, 0, 0, 0.35f);
	private static readonly Color s_MarkerCenterColorHovered = new(1, 1, 1, 0.35f);

	private bool m_IsHovered = false;
	private static readonly float s_HoveredScale = 1.2f;
	private static readonly float s_AnimationLength = 0.5f;
	private Animator<FloatAnimatable> m_Animator = Animator<FloatAnimatable>.CreateDone(1, 1, s_AnimationLength, EasingType.EaseOutBack);

	void Start()
	{
		var meshRenderer = GetComponent<MeshRenderer>();
		m_MarkerMaterial = new(meshRenderer.material);
		meshRenderer.sharedMaterial = m_MarkerMaterial;
		m_MarkerMaterial.SetColor("_RingColor", m_MarkerColor);
		m_MarkerMaterial.SetColor("_CenterColor", s_MarkerCenterColor);

		float markerAlpha = Manager.MarkersAlpha;
		markerAlpha = math.pow(markerAlpha, 2.2f);
		m_MarkerMaterial.SetFloat("_MarkerAlpha", markerAlpha);
		m_LabelMesh.alpha = markerAlpha;

		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
	}

	void Update()
	{
		m_Animator.Update(Time.deltaTime);
	}

	void OnDestroy()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
	}

	public void OnHoverStart()
	{
		m_MarkerMaterial.SetColor("_CenterColor", s_MarkerCenterColorHovered);
		m_IsHovered = true;
		m_Animator.Reset(s_HoveredScale);
	}

	public void OnHoverEnd()
	{
		m_MarkerMaterial.SetColor("_CenterColor", s_MarkerCenterColor);
		m_IsHovered = false;
		m_Animator.Reset(1);
	}

	public void OnSelectStart() => Manager.OnLandmarkSeleced(this);

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		//transform.localPosition = Vector3.zero;
		float3 camPos = camera.transform.position;
		float3 camForward = camera.transform.forward;
		float3 relPos = (float3)transform.position - camPos;
		float dist = math.dot(relPos, camForward);

		if (dist > 0)
			transform.localScale = m_ScreenSize * dist * m_Animator.Current * Vector3.one;
		else
			transform.localScale = Vector3.one * 0.01f;

		transform.rotation = Quaternion.Euler(0.0f, camera.transform.rotation.eulerAngles.y, 0.0f);
		//transform.rotation = camera.transform.rotation;

		float3 planetPosition = transform.parent.parent.position;
		float3 landmarkPosition = transform.parent.position;
		float3 normal = math.normalize(landmarkPosition - planetPosition);
		float3 viewDirection = math.normalize(landmarkPosition - camPos);
		float3 rayOrigin = landmarkPosition + normal * 0.01f;

		float markerAlpha = Manager.NoneSelectedAlpha;
		if (CMath.RaySphereIntersection(rayOrigin, -viewDirection, planetPosition, 1, out float interDist))
		{
			float3 interPosition = rayOrigin - viewDirection * interDist;
			float3 interNormal = math.normalize(interPosition - planetPosition);
			markerAlpha *= math.smoothstep(-0.3f, 0.0f, -math.dot(interNormal, viewDirection));
		}

		markerAlpha = m_IsHovered | (Index == Manager.SelectedMarker) ? 1 : markerAlpha;

		markerAlpha *= Manager.MarkersAlpha;
		markerAlpha = math.pow(markerAlpha, 2.2f);
		m_MarkerMaterial.SetFloat("_MarkerAlpha", markerAlpha);
		m_LabelMesh.alpha = markerAlpha;
	}
}
