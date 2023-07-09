using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class S_InfoDisplayAnchor : MonoBehaviour
{
	[SerializeField]
	private float m_ReferencePointOffset = 1;

	// Update is called once per frame
	void Update()
	{
		float3 camPos = Camera.main.transform.position;
		float3 pos = transform.position;
		float3 relativePos = camPos - pos;

		float3 right = math.normalize(math.cross(relativePos, new float3(0, 1, 0)));
		relativePos -= right * m_ReferencePointOffset;
		if (math.dot(relativePos, relativePos) > 0.001f)
		{
			float angle = math.atan2(relativePos.z, relativePos.x);
			transform.eulerAngles = new(0, -math.degrees(angle) - 90, 0);
		}
	}
}
