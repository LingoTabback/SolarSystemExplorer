using AstroTime;
using Ephemeris;
using System;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(S_SolarSystem))]
public class S_SolarSystemInspector : Editor
{
	private static readonly GUIContent[] s_DateSubLabels = new GUIContent[]
	{
		new("Year"), new("Month"), new("Day")
	};
	private static readonly GUIContent[] s_TimeSubLabels = new GUIContent[]
	{
		new("Hour"), new("Minute"), new("Second")
	};

	private AstroTimeUnit m_TimeUnit = AstroTimeUnit.Seconds;

	private bool m_SolarEclipseDatesFoldout = false;
	private OrbitType m_FocusSelection = OrbitType.None;
	private Date m_Date = TimeUtil.TDBtoUTC(TimeUtil.J2000);

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		S_SolarSystem script = (S_SolarSystem)target;

		{
			EditorGUILayout.BeginHorizontal();
			double timeScale = TimeUtil.ConvertUnit(script.TimeScale, AstroTimeUnit.Days, m_TimeUnit);
			timeScale = EditorGUILayout.DoubleField("Time Scale", timeScale);
			script.TimeScale = TimeUtil.ConvertUnit(timeScale, m_TimeUnit, AstroTimeUnit.Days);
			m_TimeUnit = (AstroTimeUnit)EditorGUILayout.EnumPopup(m_TimeUnit, GUILayout.MaxWidth(150));
			EditorGUILayout.EndHorizontal();
		}

		EditorGUILayout.SelectableLabel($"Simulation Time:  {script.Date.ToString(Date.Format.US)}");
		{
			int[] newDate = new int[] { m_Date.Year, m_Date.Month, m_Date.Day };
			EditorGUI.MultiIntField(EditorGUILayout.GetControlRect(false, 18f, EditorStyles.numberField, GUILayout.MaxWidth(300)), s_DateSubLabels, newDate);

			int[] newTime = new int[] { m_Date.Hour, m_Date.Minute, (int)m_Date.Seconds };
			EditorGUI.MultiIntField(EditorGUILayout.GetControlRect(false, 18f, EditorStyles.numberField, GUILayout.MaxWidth(300)), s_TimeSubLabels, newTime);

			if (newDate[0] != m_Date.Year || newDate[1] != m_Date.Month || newDate[2] != m_Date.Day
				|| newTime[0] != m_Date.Hour || newTime[1] != m_Date.Minute || newTime[2] != (int)m_Date.Seconds)
				m_Date = new Date(newDate[0], newDate[1], newDate[2], newTime[0], newTime[1], newTime[2]);
		}

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Set Time"))
			script.SetTime(m_Date);
		TimeJumpButton(script, "Jump to Now", DateTime.UtcNow);
		TimeJumpButton(script, "Jump to J2000.0", TimeUtil.J2000);
		EditorGUILayout.EndHorizontal();

		m_SolarEclipseDatesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SolarEclipseDatesFoldout, "Solar Eclipse Dates");
		if (m_SolarEclipseDatesFoldout)
		{
			const Date.Format format = Date.Format.US;
			// Solar Eclipse Dates
			TimeJumpButton(script, new Date(2001,  6, 21, 12,  4, 0), format);
			TimeJumpButton(script, new Date(2002, 12,  4,  7, 32, 0), format);
			TimeJumpButton(script, new Date(2003, 11, 23, 22, 50, 0), format);
			TimeJumpButton(script, new Date(2006,  3, 29, 10, 12, 0), format);
			TimeJumpButton(script, new Date(2010,  7, 11, 19, 34, 0), format);
			TimeJumpButton(script, new Date(2012, 11, 13, 22, 12, 0), format);
			TimeJumpButton(script, new Date(2016,  3,  9,  1, 58, 0), format);
			TimeJumpButton(script, new Date(2017,  8, 21, 18, 26, 0), format);
			TimeJumpButton(script, new Date(2020, 12, 14, 16, 14, 0), format);
			TimeJumpButton(script, new Date(2021, 12,  4,  6, 34, 0), format);
			TimeJumpButton(script, new Date(2024,  4,  8, 18, 18, 0), format);
			TimeJumpButton(script, new Date(-100,  4,  8, 18, 18, 0), format);

			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		EditorGUILayout.Separator();
		EditorGUILayout.BeginHorizontal();
		m_FocusSelection = (OrbitType)EditorGUILayout.EnumPopup("Focus on", m_FocusSelection);
		if (GUILayout.Button("Set Focus"))
			script.SetFocus(m_FocusSelection);
		EditorGUILayout.EndHorizontal();
	}

	private bool TimeJumpButton(S_SolarSystem script, in Date date, Date.Format format = Date.Format.ISO8601) => TimeJumpButton(script, date.ToString(format), date);
	private bool TimeJumpButton(S_SolarSystem script, string label, in Date date)
	{
		if (!GUILayout.Button(label))
			return false;
		script.SetTime(date);
		return true;
	}
	private bool TimeJumpButton(S_SolarSystem script, string label, DateTime date) => TimeJumpButton(script, label, new Date(date));
	private bool TimeJumpButton(S_SolarSystem script, string label, double t)
	{
		if (!GUILayout.Button(label))
			return false;
		script.SetTime(t);
		return true;
	}
}
