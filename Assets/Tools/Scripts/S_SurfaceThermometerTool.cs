using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class S_SurfaceThermometerTool : XRDirectInteractor
{
	[SerializeField]
	private double m_SurfaceTemperature;

	[SerializeField] 
	private TextMeshPro m_TextMesh;
	
	[SerializeField] 
	private float m_DefaultTemperature = -273.15f;
	
	protected override void Start()
	{
		base.Start();
		m_TextMesh.text = $"{m_DefaultTemperature} °C";
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

		m_TextMesh.text = $"{body.SurfaceTemparature} °C";
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;
		
		m_TextMesh.text = $"{m_DefaultTemperature} °C";
	}
}
