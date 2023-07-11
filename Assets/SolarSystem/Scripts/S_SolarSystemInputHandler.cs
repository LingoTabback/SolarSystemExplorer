using AstroTime;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(S_SolarSystem))]
public class S_SolarSystemInputHandler : MonoBehaviour
{
	[SerializeField]
	private InputActionReference m_SelectAction;
	[SerializeField]
	private InputActionReference m_TimeSpeedAction;
	//[SerializeField]
	//private InputActionReference m_TimeSpeedUpAction;
	//[SerializeField]
	//private InputActionReference m_TimeSlowDownAction;

	[Serializable]
	private struct TimeSpeed
	{
		public double Speed;
		public AstroTimeUnit Unit;

		public TimeSpeed(double speed, AstroTimeUnit unit)
		{
			Speed = speed;
			Unit = unit;
		}

		public static implicit operator TimeSpeed((double speed, AstroTimeUnit unit) value) => new(value.speed, value.unit);
	}

	[SerializeField]
	private TimeSpeed[] m_TimeSpeedValues = {
		(1, AstroTimeUnit.Seconds), (1, AstroTimeUnit.Minutes), (1, AstroTimeUnit.Hours), (1, AstroTimeUnit.Days),
		(1, AstroTimeUnit.Weeks), (1, AstroTimeUnit.Months), (1, AstroTimeUnit.Years)
	};
	private int m_TimeSpeedIndex = 0;
	private S_SolarSystem m_SolarSystem;
	private double m_LastTimeSpeedPressTime = -double.MaxValue * 0.5;
	[SerializeField]
	private float m_MultitapMaxDelay = 0.2f;
	private int m_MultitapCount = 0;

	private void Start()
	{
		m_SolarSystem = GetComponent<S_SolarSystem>();
		m_SolarSystem.TimeScale = TimeUtil.ConvertToDays(m_TimeSpeedValues[m_TimeSpeedIndex].Speed, m_TimeSpeedValues[m_TimeSpeedIndex].Unit);
	}

	void Awake()
	{
		m_SelectAction.action.performed += Teleport;
		m_TimeSpeedAction.action.performed += OnTimeSpeedPressed;
		//m_TimeSpeedUpAction.action.performed += SpeedUpTime;
		//m_TimeSlowDownAction.action.performed += SlowDownTime;
	}

	void OnDestroy()
	{
		m_SelectAction.action.performed -= Teleport;
		m_TimeSpeedAction.action.performed -= OnTimeSpeedPressed;
		//m_TimeSpeedUpAction.action.performed -= SpeedUpTime;
		//m_TimeSlowDownAction.action.performed -= SlowDownTime;
	}

	private void FixedUpdate()
	{
		if (m_MultitapCount > 0 && Time.timeAsDouble - m_LastTimeSpeedPressTime > m_MultitapMaxDelay)
		{
			if (m_MultitapCount == 1)
				ToggleTimePaused();
			else if (m_MultitapCount == 2)
				SpeedUpTime();
			else if (m_MultitapCount == 3)
				SlowDownTime();

			m_MultitapCount = 0;
		}
	}

	private void Teleport(InputAction.CallbackContext context) => m_SolarSystem.SetFocus(OrbitID.Invalid);

	private void OnTimeSpeedPressed(InputAction.CallbackContext context)
	{
		if (Time.timeAsDouble - m_LastTimeSpeedPressTime <= m_MultitapMaxDelay)
			m_MultitapCount++;
		else
			m_MultitapCount = 1;

		m_LastTimeSpeedPressTime = Time.timeAsDouble;
	}

	private void ToggleTimePaused() => m_SolarSystem.Paused = !m_SolarSystem.Paused;

	private void SpeedUpTime()
	{
		m_TimeSpeedIndex = Math.Min(m_TimeSpeedIndex + 1, m_TimeSpeedValues.Length - 1);
		m_SolarSystem.TimeScale = TimeUtil.ConvertToDays(m_TimeSpeedValues[m_TimeSpeedIndex].Speed, m_TimeSpeedValues[m_TimeSpeedIndex].Unit);
	}

	private void SlowDownTime()
	{
		m_TimeSpeedIndex = Math.Max(m_TimeSpeedIndex - 1, 0);
		m_SolarSystem.TimeScale = TimeUtil.ConvertToDays(m_TimeSpeedValues[m_TimeSpeedIndex].Speed, m_TimeSpeedValues[m_TimeSpeedIndex].Unit);
	}
}