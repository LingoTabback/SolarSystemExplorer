using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_CelestialBodyInteractable : XRBaseInteractable
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
	[SerializeField]
	private S_LandmarkManager m_LandmarkManager;

	[SerializeField]
	private InteractionLayerMask m_FocusedInterationLayer;
	private InteractionLayerMask m_UnfocusedInterationLayer;

	private int m_NumHovers = 0;
	private bool m_LastFocused = false;

	// Start is called before the first frame update
	void Start()
	{
		m_UnfocusedInterationLayer = interactionLayers;

		colliders.Add(m_Collider);
		m_Collider.radius = 0.001f;
	}

	private void Update()
	{
		bool focusable = false;
		bool isFocused = false;

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
			isFocused = m_CelestialBody.IsFocused;
		}
		m_Collider.enabled = focusable | isFocused;
		interactionLayers = isFocused ? m_FocusedInterationLayer : m_UnfocusedInterationLayer;
		m_Highlight.SetActive(focusable);

		if (m_LastFocused & !isFocused)
			m_LandmarkManager.OnSelectEnd();

		m_LastFocused = isFocused;
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);
		if (++m_NumHovers == 1)
		{
			m_Highlight.OnHoverStart();
			m_HoverText.OnHoverStart();
		}
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);
		if (--m_NumHovers == 0)
		{
			m_Highlight.OnHoverEnd();
			m_HoverText.OnHoverEnd();
		}
	}

	protected override void OnSelectEntered(SelectEnterEventArgs args)
	{
		base.OnSelectEntered(args);
		m_CelestialBody.Focus();
		m_LandmarkManager.OnSelectStart();
	}
}