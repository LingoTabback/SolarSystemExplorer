using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_CelestialBodyInteractible : XRBaseInteractable
{
	[SerializeField]
	private float m_ColliderRadiusFocused = 1;
	[SerializeField]
	private float m_ColliderRadiusUnfocused = 0.05f;

	[SerializeField]
	private SphereCollider m_Collider;
	[SerializeField]
	private S_BodyHighlight m_Highlight;
	private S_CelestialBody m_Body;
	private int m_NumHovers = 0;

	// Start is called before the first frame update
	void Start()
	{
		m_Body = S_CelestialBody.GetCelestialBodyComponent(gameObject);

		colliders.Add(m_Collider);
		m_Collider.radius = 0.001f;
	}

	private void Update()
	{
		Camera cam = Camera.main;
		Vector3 diff = transform.position - cam.transform.position;
		float distance = diff.magnitude;

		bool focusable = false;

		if (m_Body.ParentSystem != null)
		{
			if (m_Body.ID != m_Body.ParentSystem.FocusedOrbit)
				m_Collider.radius = (float)(distance * m_ColliderRadiusUnfocused);
			else
				m_Collider.radius = m_ColliderRadiusFocused;

			focusable = m_Body.ParentSystem.IsOrbitFocusable(m_Body.ID);
		}
		m_Collider.enabled = focusable;
		m_Highlight.SetActive(focusable);
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);
		if (++m_NumHovers > 0)
			m_Highlight.OnHoverStart();
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);
		if (--m_NumHovers <= 0)
			m_Highlight.OnHoverEnd();
	}

	protected override void OnSelectEntered(SelectEnterEventArgs args)
	{
		base.OnSelectEntered(args);
		m_Body.Focus();
	}
}
