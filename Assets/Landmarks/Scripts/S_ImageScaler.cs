using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class S_ImageScaler : MonoBehaviour
{
	[SerializeField]
	private Vector2 m_Size;
	[SerializeField]
	private bool m_LockX;
	[SerializeField]
	private bool m_LockY;

	// Start is called before the first frame update
	private void Start()
	{
		ComputeBounds();
	}

	private void OnValidate()
	{
		ComputeBounds();
	}

	public void ComputeBounds()
	{
		var imageComponent = GetComponent<Image>();
		if (imageComponent == null)
			return;

		var rectTransform = imageComponent.gameObject.GetComponent<RectTransform>();

		if (m_LockX & !m_LockY)
		{
			float height = imageComponent.sprite.texture.height / (float)imageComponent.sprite.texture.width;
			rectTransform.sizeDelta = new Vector2(m_Size.x, m_Size.x * height);
		}
		else if (!m_LockX & m_LockY)
		{
			float width = imageComponent.sprite.texture.width / (float)imageComponent.sprite.texture.height;
			rectTransform.sizeDelta = new Vector2(m_Size.y * width, m_Size.y);
		}
		else
		{
			rectTransform.sizeDelta = m_Size;
		}
	}
}
