using Animation;
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

	public void OnSelectStart()
	{
		Manager.OnLandmarkSeleced(this);
	}

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		//transform.localPosition = Vector3.zero;
		Vector3 camPos = camera.transform.position;
		Vector3 camForward = camera.transform.forward;
		Vector3 relPos = transform.position - camPos;
		float dist = Vector3.Dot(relPos, camForward);

		Vector3 planetPosition = transform.parent.parent.position;
		if (dist > 0)
			transform.localScale = m_ScreenSize * dist * m_Animator.Current * Vector3.one;
		else
			transform.localScale = Vector3.one * 0.01f;
		transform.rotation = camera.transform.rotation;

		Vector3 landmarkPosition = transform.parent.position;
		Vector3 normal = Vector3.Normalize(landmarkPosition - planetPosition);
		Vector3 viewDirection = Vector3.Normalize(landmarkPosition - camPos);

		float markerAlpha = m_IsHovered ? 1 : math.smoothstep(-0.2f, 0.1f, -Vector3.Dot(normal, viewDirection));
		markerAlpha *= Manager.MarkersAlpha;
		markerAlpha = math.pow(markerAlpha, 2.2f);
		m_MarkerMaterial.SetFloat("_MarkerAlpha", markerAlpha);
		m_LabelMesh.alpha = markerAlpha;
	}
}
