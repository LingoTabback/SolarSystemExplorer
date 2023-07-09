using Animation;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class S_LandmarkInfoDisplay : MonoBehaviour
{
	public S_LandmarkInfoSettings Settings
	{
		get => m_Settings;
		set { m_Settings = value; UpdateSettings(); }
	}
	public bool IsClosing { get; private set; } = false;

	[SerializeField]
	private Canvas m_Canvas;
	[SerializeField]
	private CanvasGroup m_CanvasGroup;
	[SerializeField]
	private VerticalLayoutGroup m_LayoutGroup;
	[SerializeField]
	private Image m_ImageComponent;
	[SerializeField]
	private TextMeshProUGUI m_CaptionComponent;
	[SerializeField]
	private TextMeshProUGUI m_TitleComponent;
	[SerializeField]
	private TextMeshProUGUI m_TextComponent;

	[SerializeField]
	private S_LandmarkInfoSettings m_Settings;

	private Animator<FloatAnimatable> m_Animator = Animator<FloatAnimatable>.Create(0, 1, 0.75f, EasingType.EaseOutQuad);
	private Vector3 m_LocalPosition;

	// Start is called before the first frame update
	private void Start()
	{
		m_CanvasGroup.alpha = math.pow(m_Animator.Current, 2.2f);
		UpdateSettings();
		m_LocalPosition = m_Canvas.transform.localPosition;
		m_Canvas.transform.localPosition = Vector3.Lerp(Vector3.zero, m_LocalPosition, m_Animator.Current);
	}

	// Update is called once per frame
	private void Update()
	{
		m_LayoutGroup.SetLayoutVertical();
		m_Animator.Update(Time.deltaTime);
		m_CanvasGroup.alpha = math.pow(m_Animator.Current, 2.2f);

		m_Canvas.transform.localPosition = Vector3.Lerp(Vector3.zero, m_LocalPosition, m_Animator.Current);
	}

	private void UpdateSettings()
	{
		m_ImageComponent.sprite = m_Settings.Image;
		m_ImageComponent.GetComponent<S_ImageScaler>().ComputeBounds();
		m_CaptionComponent.text = m_Settings.Caption;
		m_TitleComponent.text = m_Settings.Title;
		m_TextComponent.text = m_Settings.Description;
	}

	public void OnClose()
	{
		if (IsClosing)
			return;
		m_Animator.Reset(0);
		Destroy(gameObject, m_Animator.Delay + m_Animator.Length);
		IsClosing = true;
	}
}
