using AstroTime;
using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class S_CalendarTool : MonoBehaviour
{
	[SerializeField]
	private InputActionReference m_UseAction;

	[SerializeField]
	private TextMeshPro m_DayMonthMesh;
	[SerializeField]
	private TextMeshPro m_YearMesh;
	[SerializeField]
	private GameObject m_LabelBC;
	[SerializeField]
	private TextMeshPro m_TimeMesh;
	[SerializeField]
	private TextMeshPro m_UTCMesh;
	[SerializeField]
	private TextMeshPro m_TimeSpeedMesh;
	[SerializeField]
	private TextMeshPro m_TimeSpeedUnitMesh;

	[SerializeField]
	private Color m_TimeColor = Color.black;

	private S_SolarSystem m_SolarSystem;
	private TimeSpeed[] m_TimeSpeedValues = {
		(-1, AstroTimeUnit.Years), (-1, AstroTimeUnit.Months), (-1, AstroTimeUnit.Weeks), (-1, AstroTimeUnit.Days),
		(-1, AstroTimeUnit.Hours), (-1, AstroTimeUnit.Minutes), (-1, AstroTimeUnit.Seconds),
		(1, AstroTimeUnit.Seconds), (1, AstroTimeUnit.Minutes), (1, AstroTimeUnit.Hours), (1, AstroTimeUnit.Days),
		(1, AstroTimeUnit.Weeks), (1, AstroTimeUnit.Months), (1, AstroTimeUnit.Years)
	};
	private int m_TimeSpeedIndex = 7;
	private double m_LastTimeSpeedPressTime = -double.MaxValue * 0.5;
	[SerializeField]
	private float m_MultitapMaxDelay = 0.3f;
	private int m_MultitapCount = 0;


	// Start is called before the first frame update
	private void Start()
	{
		m_TimeMesh.color = m_TimeColor.linear;
		m_UTCMesh.color = m_TimeColor.linear;
		m_SolarSystem = FindAnyObjectByType<S_SolarSystem>();
		m_LabelBC.SetActive(false);
	}

	private void Awake() => m_UseAction.action.performed += OnUse;
	private void OnDestroy() => m_UseAction.action.performed -= OnUse;

	private void FixedUpdate()
	{
		Date date = m_SolarSystem.Date;
		string monthName = Date.GetMonthAbbreviationEN(date.Month);
		bool isBeforeChrist = date.Year <= 0;
		int year = isBeforeChrist ? 1 - date.Year : date.Year;
		string yearName = year.ToString("D4");

		m_DayMonthMesh.text = $"{monthName}\n{date.Day}";
		m_YearMesh.text = $"{yearName[..^2]}\n{yearName[^2..]}";
		m_TimeMesh.text = $"{date.Hour:00}:{date.Minute:00}:{(int)date.Seconds:00}";

		m_LabelBC.SetActive(isBeforeChrist);

		double timeScale = m_SolarSystem.TimeScale;
		AstroTimeUnit unitToDisplay = AstroTimeUnit.Seconds;

		for (byte i = 0; i <= (byte)AstroTimeUnit.Years; ++i)
		{
			if (math.abs(TimeUtil.ConvertFromDays(timeScale, (AstroTimeUnit)i)) < 1)
				break;
			unitToDisplay = (AstroTimeUnit)i;
		}

		timeScale = TimeUtil.ConvertFromDays(timeScale, unitToDisplay);
		m_TimeSpeedMesh.text = m_SolarSystem.Paused ? (timeScale < 0 ? "-0" : "0") : timeScale.ToString();
		m_TimeSpeedUnitMesh.text = unitToDisplay switch
		{
			AstroTimeUnit.Minutes => "Min",
			AstroTimeUnit.Hours => "Hour",
			AstroTimeUnit.Days => "Day",
			AstroTimeUnit.Weeks => "Week",
			AstroTimeUnit.Months => "Mon",
			AstroTimeUnit.Years => "Year",
			_ => "Sec"
		};

		UpdateMultitapInputs();
	}

	private void UpdateMultitapInputs()
	{
		if (m_MultitapCount > 0 && Time.realtimeSinceStartupAsDouble - m_LastTimeSpeedPressTime > m_MultitapMaxDelay)
		{
			if (m_MultitapCount == 1)
				SpeedUpTime();
			else if (m_MultitapCount == 2)
				SlowDownTime();

			m_MultitapCount = 0;
		}
	}

	private void SpeedUpTime()
	{
		m_TimeSpeedIndex = math.min(m_TimeSpeedIndex + 1, m_TimeSpeedValues.Length - 1);
		m_SolarSystem.TimeScale = TimeUtil.ConvertToDays(m_TimeSpeedValues[m_TimeSpeedIndex].Speed, m_TimeSpeedValues[m_TimeSpeedIndex].Unit);
	}

	private void SlowDownTime()
	{
		m_TimeSpeedIndex = math.max(m_TimeSpeedIndex - 1, 0);
		m_SolarSystem.TimeScale = TimeUtil.ConvertToDays(m_TimeSpeedValues[m_TimeSpeedIndex].Speed, m_TimeSpeedValues[m_TimeSpeedIndex].Unit);
	}

	private void OnUse(InputAction.CallbackContext context)
	{
		if (Time.realtimeSinceStartupAsDouble - m_LastTimeSpeedPressTime <= m_MultitapMaxDelay)
			++m_MultitapCount;
		else
			m_MultitapCount = 1;

		m_LastTimeSpeedPressTime = Time.realtimeSinceStartupAsDouble;
	}

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
}