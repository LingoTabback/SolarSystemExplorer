using Unity.Mathematics;
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
	[SerializeField]
	private S_BodyHoverText m_HoverText;
	[SerializeField]
	private S_CelestialBody m_CelestialBody;
	private int m_NumHovers = 0;

	// Start is called before the first frame update
	void Start()
	{
		//m_CelestialBody = S_CelestialBody.GetCelestialBodyComponent(gameObject);

		colliders.Add(m_Collider);
		m_Collider.radius = 0.001f;
	}

	private void Update()
	{
		bool focusable = false;

		if (m_CelestialBody.ParentSystem != null)
		{
			if (m_CelestialBody.ID != m_CelestialBody.ParentSystem.FocusedOrbit)
			{
				Camera cam = Camera.main;
				Vector3 diff = transform.parent.position - cam.transform.position;
				float distance = math.min(diff.magnitude, 100);
				transform.position = cam.transform.position + Vector3.Normalize(diff) * distance;
				m_Collider.radius = (float)math.max(distance * m_ColliderRadiusUnfocused, m_CelestialBody.ScaledRadiusInSolarSystem / diff.magnitude);
			}
			else
			{
				m_Collider.radius = m_ColliderRadiusFocused * (float)m_CelestialBody.ScaledRadiusInSolarSystem;
				transform.localPosition = Vector3.zero;
			}

			focusable = m_CelestialBody.ParentSystem.IsOrbitFocusable(m_CelestialBody.ID);
		}
		m_Collider.enabled = focusable;
		m_Highlight.SetActive(focusable);
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);
		if (++m_NumHovers > 0)
		{
			m_Highlight.OnHoverStart();
			m_HoverText.OnHoverStart();
		}
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);
		if (--m_NumHovers <= 0)
		{
			m_Highlight.OnHoverEnd();
			m_HoverText.OnHoverEnd();
		}
	}

	protected override void OnSelectEntered(SelectEnterEventArgs args)
	{
		base.OnSelectEntered(args);
		m_CelestialBody.Focus();
	}
}
