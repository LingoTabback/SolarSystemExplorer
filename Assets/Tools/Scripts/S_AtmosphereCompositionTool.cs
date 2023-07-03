using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class S_AtmosphereCompositionTool : XRDirectInteractor
{
	[SerializeField]
	private string m_AtmosphereComposition;

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;
		else
			m_AtmosphereComposition = body.AtmosphereComposition;

		Debug.Log($"Atmosphere Composition: {m_AtmosphereComposition}");
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
