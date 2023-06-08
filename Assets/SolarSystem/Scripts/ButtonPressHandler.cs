using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Ephemeris;

public class ButtonPressHandler : MonoBehaviour
{ 
    public InputActionReference toggleReference = null;
    public S_SolarSystem solarSystem;

    void Awake()
    {
    toggleReference.action.started += Teleport;
    }

    void onDestroy()
    {
        toggleReference.action.started -= Teleport;
    }

    public void Teleport(InputAction.CallbackContext context)
    {
        Ephemeris.OrbitType orbitType = Ephemeris.OrbitType.None;
        solarSystem.SetFocus(orbitType);
    }
}