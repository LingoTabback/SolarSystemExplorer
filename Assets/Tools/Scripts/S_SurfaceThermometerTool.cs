using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using UnityEngine.InputSystem;

public class S_SurfaceThermometerTool : XRDirectInteractor
{
	[SerializeField]
	private double m_SurfaceTemperature;
	[SerializeField] 
	private TextMeshPro m_TextMesh;
	[SerializeField] 
	private float m_DefaultTemperature = -273.15f;
	[SerializeField]
	private InputActionReference m_UseAction;

	private S_CelestialBody m_HoveredBody = null;

	protected override void Start()
	{
		base.Start();
		m_TextMesh.text = $"{m_DefaultTemperature} °C";
	}

	protected override void Awake()
	{
		base.Awake();
		m_UseAction.action.performed += OnUse;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_UseAction.action.performed -= OnUse;
	}

	private void OnUse(InputAction.CallbackContext context)
	{
		if (m_HoveredBody == null)
			m_TextMesh.text = $"{m_DefaultTemperature} °C";
		else
			m_TextMesh.text = $"{m_HoveredBody.SurfaceTemparature} °C";
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);
		args.interactableObject.transform.gameObject.TryGetComponent(out m_HoveredBody);
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		if (!args.interactableObject.transform.gameObject.TryGetComponent<S_CelestialBody>(out _))
			return;
		m_HoveredBody = null;
	}
}
