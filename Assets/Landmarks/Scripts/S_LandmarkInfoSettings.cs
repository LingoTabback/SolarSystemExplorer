using UnityEngine;

[CreateAssetMenu(fileName = "L_NewLandmarkInfoSettings", menuName = "Solar System/LandmarkInfo Settings")]
public class S_LandmarkInfoSettings : ScriptableObject
{
	public string Title => m_Title;
	public string Caption => m_Caption;
	public string Description => m_Description;
	public Sprite Image => m_Image;

	[SerializeField]
	private string m_Title;
	[SerializeField]
	[TextArea(5, 10)]
	private string m_Caption;
	[SerializeField]
	[TextArea(5, 10)]
	private string m_Description;
	[SerializeField]
	private Sprite m_Image;
}
