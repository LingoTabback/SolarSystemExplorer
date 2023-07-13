using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class S_RulerTool : MonoBehaviour
{
	[SerializeField]
	private TextMeshPro m_TextMesh;
	[SerializeField]
	private string m_DefaultValue = " ";
	[SerializeField]
	private InputActionReference m_UseAction;

	private S_SolarSystem m_SolarSystem;

	// Start is called before the first frame update
	private void Start()
	{
		m_TextMesh.text = m_DefaultValue;
		m_SolarSystem = FindAnyObjectByType<S_SolarSystem>();
	}
	private void Awake() => m_UseAction.action.performed += OnUse;
	private void OnDestroy() => m_UseAction.action.performed -= OnUse;

	private void OnUse(InputAction.CallbackContext context)
	{
		if (m_SolarSystem == null)
			return;

		var focusedBody = m_SolarSystem.FocusedBody;
		if (focusedBody == null)
		{
			m_TextMesh.text = m_DefaultValue;
			return;
		}

		m_TextMesh.text = $"{focusedBody.Radius * 10:n0} km";
	}
}
