using AstroTime;
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(S_SolarSystem))]
public class S_SolarSystemInspector : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		S_SolarSystem script = (S_SolarSystem)target;

		if (GUILayout.Button("Jump to Now"))
		{
			var date = DateTime.UtcNow;
			script.SetTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
		}
		if (GUILayout.Button("Jump to J2000.0"))
			script.SetTime(TimeUtil.J2000);
	}
}
