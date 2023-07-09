using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

	public static S_LandmarkInfoSettings Default
	{
		get
		{
			if (s_Default != null)
				return s_Default;

			var result = CreateInstance<S_LandmarkInfoSettings>();
			result.m_Title = "Lorem Ipsum Dolor";
			result.m_Caption = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";
			result.m_Description = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua.";
			result.m_Image = Addressables.LoadAssetAsync<Sprite>("T_UVGrid_Sprite").WaitForCompletion();

			s_Default = result;
			return s_Default;
		}
	}
	private static S_LandmarkInfoSettings s_Default;
}
