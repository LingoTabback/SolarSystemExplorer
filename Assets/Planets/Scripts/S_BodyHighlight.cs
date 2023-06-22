using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class S_BodyHighlight : MonoBehaviour
{
	private static float s_Size = 0.3f;
	private static float s_SelectedScale = 2;
	private static float s_Brightness = 2;
	private static float s_BrightnessScale = 4;
	private static float s_Halo = 0;
	private static float s_SelectedHalo = 0.75f;
	private static float s_AnimationLength = 0.5f;

	private HighlightAnimator m_Animatior = HighlightAnimator.CreateDone(1, 1, 1, 1, s_Halo, s_Halo, 1);
	[SerializeField]
	private Material m_MaterialTemplate;
	private Material m_Material;
	private MeshRenderer m_MeshRenderer;

	// Start is called before the first frame update
	void Start()
	{
		TryGetComponent(out m_MeshRenderer);
		m_Material = new(m_MaterialTemplate);
		m_MeshRenderer.sharedMaterial = m_Material;
		m_Material.SetFloat("_Size", s_Size * m_Animatior.ScaleCurrent);
		m_Material.SetFloat("_Brightness", s_Brightness * m_Animatior.BrightnessCurrent);
		m_Material.SetFloat("_Halo", m_Animatior.HaloCurrent);

		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
	}

	void OnDestroy()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
	}

	// Update is called once per frame
	void Update()
	{
		bool doneThisFrame = !m_Animatior.IsDone;
		m_Animatior.Update();
		doneThisFrame &= m_Animatior.IsDone;
		if (!m_Animatior.IsDone || doneThisFrame)
		{
			m_Material.SetFloat("_Size", s_Size * m_Animatior.ScaleCurrent);
			m_Material.SetFloat("_Brightness", s_Brightness * m_Animatior.BrightnessCurrent);
			m_Material.SetFloat("_Halo", m_Animatior.HaloCurrent);
		}
	}

	public void SetActive(bool active) => m_MeshRenderer.enabled = active;

	public void OnHoverStart()
	{
		m_Animatior = new(m_Animatior.ScaleCurrent, s_SelectedScale, m_Animatior.BrightnessCurrent, s_BrightnessScale, m_Animatior.HaloCurrent, s_SelectedHalo, s_AnimationLength);
	}

	public void OnHoverEnd()
	{
		m_Animatior = new(m_Animatior.ScaleCurrent, 1, m_Animatior.BrightnessCurrent, 1, m_Animatior.HaloCurrent, s_Halo, s_AnimationLength);
	}

	public void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		Bounds adjustedBounds = m_MeshRenderer.bounds;
		adjustedBounds.center = camera.transform.position
			+ ((camera.farClipPlane - camera.nearClipPlane) * 0.5f * camera.transform.forward);
		m_MeshRenderer.bounds = adjustedBounds;
	}

	private class HighlightAnimator
	{
		public float ScaleStart { get; private set; } = 1;
		public float ScaleEnd { get; private set; } = 1;
		public float ScaleCurrent { get; private set; } = 1;

		public float BrightnessStart { get; private set; } = 1;
		public float BrightnessEnd { get; private set; } = 1;
		public float BrightnessCurrent { get; private set; } = 1;

		public float HaloStart { get; private set; } = 0;
		public float HaloEnd { get; private set; } = 0;
		public float HaloCurrent { get; private set; } = 0;

		public float Length { get; private set; } = 1;
		public float Progress { get; private set; } = 0;
		public bool IsDone { get; private set; } = false;
		private float m_Time = 0;

		public HighlightAnimator()
		{
			Progress = 1;
			m_Time = 1;
			IsDone = true;
		}

		public HighlightAnimator(float sStart, float sEnd, float bStart, float bEnd, float hStart, float hEnd, float length)
		{
			ScaleStart = sStart;
			ScaleEnd = sEnd;
			ScaleCurrent = sStart;
			BrightnessStart = bStart;
			BrightnessEnd = bEnd;
			BrightnessCurrent = bStart;
			HaloStart = hStart;
			HaloEnd = hEnd;
			HaloCurrent = hStart;
			Length = length;
		}

		public void Update()
		{
			if (IsDone)
				return;

			m_Time += Time.deltaTime;
			if (m_Time > Length)
			{
				m_Time = Length;
				IsDone = true;
			}

			Progress = EaseOutBack(m_Time / Length);
			ScaleCurrent = math.lerp(ScaleStart, ScaleEnd, Progress);
			BrightnessCurrent = math.lerp(BrightnessStart, BrightnessEnd, Progress);
			HaloCurrent = math.lerp(HaloStart, HaloEnd, Progress);
		}

		private static float EaseOutBack(float x)
		{
			const float c1 = 1.70158f;
			const float c3 = c1 + 1;
			float x2 = (x - 1) * (x - 1);

			return 1 + c3 * x2 * (x - 1) + c1 * x2;
		}

		public static HighlightAnimator CreateDone(float sStart, float sEnd, float bStart, float bEnd, float hStart, float hEnd, float length)
		{
			return new HighlightAnimator(sStart, sEnd, bStart, bEnd, hStart, hEnd, length)
			{
				ScaleCurrent = sEnd,
				BrightnessCurrent = bEnd,
				HaloCurrent = hEnd,
				Progress = 1,
				m_Time = length,
				IsDone = true
			};
		}
	}
}
