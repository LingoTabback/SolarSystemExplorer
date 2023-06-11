using UnityEngine;
using UnityEngine.InputSystem;

public class S_SolarSystemInputHandler : MonoBehaviour
{
	[SerializeField]
	private InputActionReference m_SelectAction;
	[SerializeField]
	private S_SolarSystem m_SolarSystem;

	void Awake()
	{
		m_SelectAction.action.performed += Teleport;
	}

	void OnDestroy()
	{
		m_SelectAction.action.performed -= Teleport;
	}

	public void Teleport(InputAction.CallbackContext context)
	{
		m_SolarSystem.SetFocus(OrbitID.Invalid);
	}
}