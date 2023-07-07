using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class S_AtmosphereCompositionTool : XRDirectInteractor
{
	[SerializeField]
	private TextMeshPro m_TextMesh;

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

		m_TextMesh.text = body.AtmosphereComposition;
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

		//m_TextMesh.text = "None";
	}
}
