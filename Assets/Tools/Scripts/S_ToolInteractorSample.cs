using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_ToolInteractorSample : XRDirectInteractor
{
	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = S_CelestialBody.GetCelestialBodyComponent(args.interactableObject.transform.gameObject);
		if (body == null)
			return;

		Debug.Log($"Entering {body.BodyName}");
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = S_CelestialBody.GetCelestialBodyComponent(args.interactableObject.transform.gameObject);
		if (body == null)
			return;

		Debug.Log($"Exiting {body.BodyName}");
	}
}