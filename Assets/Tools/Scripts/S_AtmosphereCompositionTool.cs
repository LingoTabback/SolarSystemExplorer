using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class S_AtmosphereCompositionTool : XRDirectInteractor
{
	[SerializeField]
    private GameObject m_Template;

    [SerializeField]
    private string m_AtmosphereComposition;

    private int m_Counter = 0;
	private bool m_IsLoading = true;
	private bool m_HoverEntered = false;

    protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

		m_AtmosphereComposition = body.AtmosphereComposition;

        var textMash = m_Template.GetComponent<TextMeshPro>();
		textMash.text = "Loading...";

		m_HoverEntered = true;
    }

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

        var textMash = m_Template.GetComponent<TextMeshPro>();
        textMash.text = "None";

		m_HoverEntered = false;
		m_Counter = 0;
    }

	protected void Update()
	{
		if (m_HoverEntered)
		{
			m_Counter++;

			if (m_Counter < 200)
			{
				m_IsLoading = true;
			}
			else
			{
				m_IsLoading = false;
				var textMash = m_Template.GetComponent<TextMeshPro>();
				textMash.text = m_AtmosphereComposition;
			}
		}
    }
}
