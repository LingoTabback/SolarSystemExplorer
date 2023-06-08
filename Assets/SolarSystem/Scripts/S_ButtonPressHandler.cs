using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Ephemeris;

public class S_ButtonPressHandler : MonoBehaviour
{ 
	public InputActionReference ToggleReference = null;
	public S_SolarSystem SolarSystem;

	void Awake()
	{
		ToggleReference.action.started += Teleport;
	}

	void OnDestroy()
	{
		ToggleReference.action.started -= Teleport;
	}

	public void Teleport(InputAction.CallbackContext context)
	{
		SolarSystem.SetFocus(OrbitID.Invalid);
	}
}