using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class S_SurfaceThermometerTool : XRDirectInteractor
{
	[SerializeField]
	private double m_SurfaceTemperature;

	[SerializeField] 
	private GameObject m_Template;
	
	private int m_UpdateSteps = 0;
	private double m_UpdateWidth = 0;
	private double m_UpdateStart = 0;
	protected void Update()
	{
		if (m_UpdateSteps > 0)
		{
			var updateValue = 
			m_UpdateSteps--;
		}
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null && m_UpdateSteps > 0)
			return;
		
		
		Debug.Log($"Temperature {m_SurfaceTemperature}");
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;
		
		var textMash = m_Template.GetComponent<TextMeshPro>();
		textMash.text = "-272,15°C";
	}
}
