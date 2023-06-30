using Animation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class S_BodyHighlight : MonoBehaviour
{
	private static readonly float s_Size = 0.3f;
	private static readonly float s_SelectedScale = 2;
	private static readonly float s_Brightness = 2;
	private static readonly float s_BrightnessScale = 4;
	private static readonly float s_Halo = 0;
	private static readonly float s_SelectedHalo = 0.75f;
	private static readonly float s_AnimationLength = 0.5f;

	private Animator<Highlight> m_Animatior = Animator<Highlight>.CreateDone(new(1, 1, s_Halo), new(1, 1, s_Halo), s_AnimationLength, EasingType.EaseOutBack);
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
		var curState = m_Animatior.Current;
		m_Material.SetFloat("_Size", s_Size * curState.Scale);
		m_Material.SetFloat("_Brightness", s_Brightness * curState.Brightness);
		m_Material.SetFloat("_Halo", curState.Halo);

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
		m_Animatior.Update(Time.deltaTime);
		doneThisFrame &= m_Animatior.IsDone;
		if (!m_Animatior.IsDone || doneThisFrame)
		{
			var curState = m_Animatior.Current;
			m_Material.SetFloat("_Size", s_Size * curState.Scale);
			m_Material.SetFloat("_Brightness", s_Brightness * curState.Brightness);
			m_Material.SetFloat("_Halo", curState.Halo);
		}
	}

	public void SetActive(bool active) => m_MeshRenderer.enabled = active;

	public void OnHoverStart() => m_Animatior.Reset(m_Animatior.Current, new(s_SelectedScale, s_BrightnessScale, s_SelectedHalo));

	public void OnHoverEnd() => m_Animatior.Reset(m_Animatior.Current, new(1, 1, s_Halo));

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		Bounds adjustedBounds = m_MeshRenderer.bounds;
		adjustedBounds.center = camera.transform.position
			+ ((camera.farClipPlane - camera.nearClipPlane) * 0.5f * camera.transform.forward);
		m_MeshRenderer.bounds = adjustedBounds;
	}

	private struct Highlight : IAnimatable<Highlight>
	{
		public float Scale;
		public float Brightness;
		public float Halo;

		public Highlight(float scale, float brightness, float halo)
		{
			Scale = scale;
			Brightness = brightness;
			Halo = halo;
		}

		public Highlight Lerp(Highlight to, float alpha)
			=> new(math.lerp(Scale, to.Scale, alpha), math.lerp(Brightness, to.Brightness, alpha), math.lerp(Halo, to.Halo, alpha));
	}
}
