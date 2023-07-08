using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class S_LandmarkInfoDisplay : MonoBehaviour
{
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
	Sprite m_Image;
	[SerializeField]
	string m_Caption;
	[SerializeField]
	string m_Title;
	[SerializeField]
	string m_Text;

	// Start is called before the first frame update
	private void Start()
	{
		m_ImageComponent.sprite = m_Image;
		m_ImageComponent.GetComponent<S_ImageScaler>().ComputeBounds();
		m_CaptionComponent.text = m_Caption;
		m_TitleComponent.text = m_Title;
		m_TextComponent.text = m_Text;
	}

	// Update is called once per frame
	private void Update()
	{
		m_LayoutGroup.SetLayoutVertical();
	}
}
