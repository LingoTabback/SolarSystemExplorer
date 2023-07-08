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

	[SerializeField]
	CanvasGroup m_CanvasGroup;
	[SerializeField]
	VerticalLayoutGroup m_LayoutGroup;
	[SerializeField]
	Image m_ImageComponent;
	[SerializeField]
	TextMeshProUGUI m_CaptionComponent;
	[SerializeField]
	TextMeshProUGUI m_TitleComponent;
	[SerializeField]
	TextMeshProUGUI m_TextComponent;

	[SerializeField]
	S_LandmarkInfoSettings m_Settings;

	[SerializeField]
	Animator<FloatAnimatable> m_Animator = Animator<FloatAnimatable>.Create(0, 1, 1, EasingType.EaseOutSine);

	// Start is called before the first frame update
	private void Start()
	{
		UpdateSettings();
	}

	// Update is called once per frame
	private void Update()
	{
		m_LayoutGroup.SetLayoutVertical();
		m_Animator.Update(Time.deltaTime);
		m_CanvasGroup.alpha = math.pow(m_Animator.Current, 2.2f);
	}

	private void UpdateSettings()
	{
		m_ImageComponent.sprite = m_Settings.Image;
		m_ImageComponent.GetComponent<S_ImageScaler>().ComputeBounds();
		m_CaptionComponent.text = m_Settings.Caption;
		m_TitleComponent.text = m_Settings.Title;
		m_TextComponent.text = m_Settings.Description;
	}
}
