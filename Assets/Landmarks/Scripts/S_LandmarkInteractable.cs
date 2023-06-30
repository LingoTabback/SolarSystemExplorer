using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_LandmarkInteractable : XRBaseInteractable
{
	[SerializeField]
	private float m_ColliderSize = 0.05f;
	[SerializeField]
	private S_LandmarkMarker m_Marker;
	private SphereCollider m_Collider;

	private int m_NumHovers = 0;

	void Start()
	{
		m_Collider = GetComponent<SphereCollider>();
	}

	// Update is called once per frame
	void Update()
	{
		float distance = math.abs(Vector3.Dot(transform.position - Camera.main.transform.position, Camera.main.transform.forward));
		m_Collider.radius = distance * m_ColliderSize * 0.25f;
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);
		if (++m_NumHovers == 1)
			m_Marker.OnHoverStart();
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);
		if (--m_NumHovers == 0)
			m_Marker.OnHoverEnd();
	}

}
