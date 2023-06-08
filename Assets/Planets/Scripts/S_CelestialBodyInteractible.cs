using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_CelestialBodyInteractible : XRBaseInteractable
{
	public float ColliderRadiusFocused = 1;
	public float ColliderRadiusUnfocused = 0.05f;

	private S_CelestialBody m_Body;
	private SphereCollider m_Collider;

	// Start is called before the first frame update
	void Start()
	{
		if (TryGetComponent(out S_Planet planetScript))
			m_Body = planetScript;
		else if (TryGetComponent(out S_Moon moonScript))
			m_Body = moonScript;

		TryGetComponent(out m_Collider);
		m_Collider.radius = 0.001f;

		colliders.Add(m_Collider);
	}

	private void Update()
	{
		Camera cam = Camera.main;
		Vector3 diff = transform.position - cam.transform.position;
		float distance = diff.magnitude;
		if (m_Body.CurrentOrbit != m_Body.ParentSystem.FocusedOrbit)
			m_Collider.radius = (float)(distance / m_Body.ScaledRadiusInSolarSystem * ColliderRadiusUnfocused);
		else
			m_Collider.radius = ColliderRadiusFocused;
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);
		Debug.Log($"Entering {m_Body.CurrentOrbit}");
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);
		Debug.Log($"Exiting {m_Body.CurrentOrbit}");
	}
}
