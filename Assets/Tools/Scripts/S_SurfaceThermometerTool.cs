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
	
	[SerializeField] 
	private double m_DefaultTemperature = -273.15;
	
	[SerializeField]
	private double m_CurrentTemperature = -273.15;

	private double m_DisplayedTemperature;
	
	protected void Start()
	{
		m_DisplayedTemperature = m_DefaultTemperature;
		var textMash = m_Template.GetComponent<TextMeshPro>();
		textMash.text = m_DisplayedTemperature.ToString() + "°C";
	}
	
	protected void Update()
	{
		double dist = Math.Abs(m_DisplayedTemperature - m_CurrentTemperature);
		if (dist >= 0.01)
		{
			dist = dist / 50;
			if (m_DisplayedTemperature < m_CurrentTemperature)
				m_DisplayedTemperature = m_DisplayedTemperature + dist;
			else
				m_DisplayedTemperature = m_DisplayedTemperature - dist;
		} 
		else 
			m_DisplayedTemperature = m_CurrentTemperature;
		
		var textMash = m_Template.GetComponent<TextMeshPro>();
		textMash.text = Math.Round(m_DisplayedTemperature,2).ToString() + "°C";
		
	}

	protected override void OnHoverEntered(HoverEnterEventArgs args)
	{
		base.OnHoverEntered(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

		m_CurrentTemperature = body.SurfaceTemparature;
		Debug.Log($"Temperature {m_SurfaceTemperature}");
	}

	protected override void OnHoverExited(HoverExitEventArgs args)
	{
		base.OnHoverExited(args);

		var body = args.interactableObject.transform.gameObject.GetComponent<S_CelestialBody>();
		if (body == null)
			return;

		m_CurrentTemperature = m_DefaultTemperature;
	}
}
