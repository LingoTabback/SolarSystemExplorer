using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class S_LandmarkManager : MonoBehaviour
{
	[Serializable]
	public class Landmark
	{
		public float Latitude = 0;
		public float Longitude = 0;
		public string Name = "Unnamed";
		public int ColorIndex = 0;
		[TextArea(5, 10)]
		public string InfoText = "n/a";
	}

	[SerializeField]
	[ColorUsage(true, true)]
	private Color[] m_MarkerColors;
	//[SerializeField]
	//[ColorUsage(true, true)]
	//private Color m_MarkerColorDefault = Color.white;
	//[SerializeField]
	//[ColorUsage(true, true)]
	//private Color m_MarkerColorManMade = Color.white;
	[SerializeField]
	private Landmark[] m_Landsmarks;
	[SerializeField]
	private GameObject m_LandmarkPrefab;

	private GameObject[] m_LandmarkObjects;

	// Start is called before the first frame update
	private void Start()
	{
		m_LandmarkObjects = new GameObject[m_Landsmarks.Length];

		for (int i = 0; i < m_Landsmarks.Length; ++i)
		{
			var landmark = m_Landsmarks[i];
			var landmarkObject = Instantiate(m_LandmarkPrefab, transform);
			landmarkObject.transform.localPosition = LatLongToDirection(math.radians(landmark.Latitude), math.radians(landmark.Longitude));

			var marker = landmarkObject.transform.GetChild(0).gameObject.GetComponent<S_LandmarkMarker>();
			marker.Label = landmark.Name;
			marker.MarkerColor = m_MarkerColors[math.clamp(landmark.ColorIndex, 0, m_MarkerColors.Length - 1)];

			landmarkObject.SetActive(false);
			m_LandmarkObjects[i] = landmarkObject;
		}
	}

	public void OnSelectStart()
	{
		foreach (var landmark in m_LandmarkObjects)
			landmark.SetActive(true);
	}

	public void OnSelectEnd()
	{
		foreach (var landmark in m_LandmarkObjects)
			landmark.SetActive(false);
	}

	private static Vector3 LatLongToDirection(float lat, float lon)
	{
		math.sincos(lat, out float latSin, out float latCos);
		math.sincos(lon, out float lonSin, out float lonCos);
		return new Vector3(-lonCos * latCos, latSin, lonSin * latCos);
	}
}
