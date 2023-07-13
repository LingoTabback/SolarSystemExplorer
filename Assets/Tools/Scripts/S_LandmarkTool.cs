using UnityEngine;
using UnityEngine.InputSystem;

public class S_LandmarkTool : MonoBehaviour
{
	[SerializeField]
	private InputActionReference m_UseAction;

	private S_SolarSystem m_SolarSystem;

	// Start is called before the first frame update
	private void Start() => m_SolarSystem = FindAnyObjectByType<S_SolarSystem>();
	private void Awake() => m_UseAction.action.performed += OnUse;
	private void OnDestroy() => m_UseAction.action.performed -= OnUse;

	private void OnUse(InputAction.CallbackContext context)
	{
		if (m_SolarSystem == null)
			return;

		var focusedBody = m_SolarSystem.FocusedBody;
		if (focusedBody == null)
			return;

		focusedBody.LandmarksVisible = !focusedBody.LandmarksVisible;
	}

}
