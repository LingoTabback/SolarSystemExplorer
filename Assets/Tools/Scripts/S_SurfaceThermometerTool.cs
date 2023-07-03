using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_SurfaceThermometerTool : XRDirectInteractor
{
	[SerializeField]
	private double m_SurfaceTemperature;

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;
		else
			m_SurfaceTemperature = body.SurfaceTemparature;

		Debug.Log($"Temperature {m_SurfaceTemperature}");
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

        Debug.Log($"");
    }
}
