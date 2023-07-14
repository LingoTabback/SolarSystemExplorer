using AstroTime;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(S_SolarSystem))]
public class S_SolarSystemInputHandler : MonoBehaviour
{
	[SerializeField]
	private InputActionReference m_RightStickAction;
	[SerializeField]
	private InputActionReference m_FasterTimeMoveAction;
	[SerializeField]
	private float m_Deadzone = 0.75f;
	[SerializeField]
	private double m_RotationScale = 0.25;
	[SerializeField]
	[Range(0.001f, 1)]
	private float m_AnimationSmoothing = 0.1f;

	private float m_PrevAngle = 0;
	private float m_CurAngleDiff = 0;
	private float m_NextAngleDiff = 0;
	private bool m_FirstFrameOfInput = true;

	[SerializeField]
	private InputActionReference m_SelectAction;
	[SerializeField]
	private InputActionReference m_TimePauseAction;

	private S_SolarSystem m_SolarSystem;

	private void Start()
	{
		m_SolarSystem = GetComponent<S_SolarSystem>();
	}

	void Awake()
	{
		m_SelectAction.action.performed += Teleport;
		m_TimePauseAction.action.performed += OnTimePausePressed;
	}

	void OnDestroy()
	{
		m_SelectAction.action.performed -= Teleport;
		m_TimePauseAction.action.performed -= OnTimePausePressed;
	}

	private void Update()
	{
		float blend = math.pow(0.5f, Time.deltaTime / m_AnimationSmoothing);
		m_CurAngleDiff = math.lerp(m_NextAngleDiff, m_CurAngleDiff, blend);
		if (math.abs(m_CurAngleDiff) < 0.0001f)
			m_CurAngleDiff = 0;

		double timeScale = m_FasterTimeMoveAction.action.ReadValue<float>() > 0.9f ? m_SolarSystem.CurrentOrbitalPeriod : m_SolarSystem.CurrentRotationalPeriod;
		m_SolarSystem.SetTime(m_SolarSystem.BarycentricDynamicalTime - m_CurAngleDiff / math.PI_DBL * 0.5 * m_RotationScale * timeScale);
	}

	private void FixedUpdate()
	{
		float2 stickPosition = m_RightStickAction.action.ReadValue<Vector2>();
		if (math.lengthsq(stickPosition) < m_Deadzone * m_Deadzone)
		{
			m_FirstFrameOfInput = true;
			m_NextAngleDiff = 0;
			return;
		}

		float2 stickDirection = math.normalize(stickPosition);
		float angle = math.atan2(stickDirection.y, stickDirection.x);

		if (!m_FirstFrameOfInput)
		{
			float angleDiff = angle - m_PrevAngle;
			if (angleDiff > math.PI)
				angleDiff = -math.PI * 2 + angleDiff;
			else if (angleDiff < -math.PI)
				angleDiff = math.PI * 2 + angleDiff;

			m_NextAngleDiff = angleDiff;
		}

		m_PrevAngle = angle;
		m_FirstFrameOfInput = false;
	}

	private void Teleport(InputAction.CallbackContext context) => m_SolarSystem.SetFocus(OrbitID.Invalid);

	private void OnTimePausePressed(InputAction.CallbackContext context)
	{
		m_SolarSystem.Paused = !m_SolarSystem.Paused;
	}
}