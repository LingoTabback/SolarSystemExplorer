using AstroTime;
using CustomMath;
using Ephemeris;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class S_BodyQickInfoDisplay : MonoBehaviour
{
	public enum ItemType : byte
	{
		DistanceToSun = 0,
		OneWayLightTimeToSun,
		LengthOfYear,
		PlanetType,
		Description,
		Age,
		DistanceToGalacticCenter,
		StarType,
		DistanceToEarth,
		HumanVisitors,
		Moonwalkers,
		RoboticVisits,
		DistanceToJupiter,
		OneWayLightTimeToEarth,
		Discovered,
		DistanceToSaturn,
	}

	[SerializeField]
	private S_CelestialBody m_Body;
	[SerializeField]
	private ItemType[] m_ItemTypes;
	[SerializeField]
	private Color m_LabelColor = Color.white;
	[SerializeField]
	private float m_HorizontalOffset = 1.1f;
	[SerializeField]
	private float m_VerticalSpacing = 0.25f;
	[SerializeField]
	private GameObject m_DescriptionTemplate;
	[SerializeField]
	private GameObject m_ItemTemplate;
	
	private List<QickInfoItem> m_Items = new();
	private bool m_IsVisible = true;

	// Start is called before the first frame update
	void Start()
	{
		InitItems();
	}

	// Update is called once per frame
	void Update()
	{
		bool isFocused = m_Body.IsFocused;
		if (m_IsVisible != isFocused)
		{
			int numChildren = transform.childCount;
			for (int i = 0; i < numChildren; ++i)
				transform.GetChild(i).gameObject.SetActive(isFocused);

			m_IsVisible = isFocused;
		}

		if (!m_IsVisible)
			return;

		float3 camPos = Camera.main.transform.position;
		float3 pos = transform.position;
		float3 relativePos = camPos - pos;

		float3 right = math.normalize(math.cross(relativePos, new float3(0, 1, 0)));
		relativePos -= right;
		if (math.dot(relativePos, relativePos) > 0.001f)
		{
			float angle = math.atan2(relativePos.z, relativePos.x);
			transform.eulerAngles = new(0, -math.degrees(angle) - 90, 0);
		}

		UpdateItems();
	}

	private void InitItems()
	{
		m_Items.Clear();

		foreach (ItemType type in m_ItemTypes)
		{
			QickInfoItem item = type switch
			{
				ItemType.DistanceToSun => new("Distanz zu Sonne", "n/a", GetDistanceToSunValue, m_ItemTemplate, m_LabelColor, true),
				ItemType.OneWayLightTimeToSun => new("Lichtzeit zu Sonne", "n/a", GetOneWayLightTimeToSun, m_ItemTemplate, m_LabelColor, true),
				ItemType.LengthOfYear => new("Jahresl�nge", "n/a", GetYearLengthValue, m_ItemTemplate, m_LabelColor),
				ItemType.PlanetType => new("Planetenart", "n/a", GetBodyTypeValue, m_ItemTemplate, m_LabelColor),
				ItemType.Description => new(m_Body.BodyName, "n/a", GetDescriptionValue, m_DescriptionTemplate, Color.white),
				ItemType.Age => new("Alter", "n/a", GetAgeValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToGalacticCenter => new("Distanz zu galaktischem Mittelpunkt", "n/a", GetDistFromGalacticCenterValue, m_ItemTemplate, m_LabelColor),
				ItemType.StarType => new("Sternart", "n/a", GetBodyTypeValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToEarth => new("Distanz zu Erde", "n/a", GetDistanceFromEarthValue, m_ItemTemplate, m_LabelColor, true),
				ItemType.HumanVisitors => new("Menschliche Besucher", "n/a", GetHumanVisitorsValue, m_ItemTemplate, m_LabelColor),
				ItemType.Moonwalkers => new("Mondl�ufer", "n/a", GetMoonwalkersValue, m_ItemTemplate, m_LabelColor),
				ItemType.RoboticVisits => new("Robotische Besuche", "n/a", GetRobotikVisitsValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToJupiter => new("Distanz zu Jupiter", "n/a", GetDistanceFromJupiterValue, m_ItemTemplate, m_LabelColor, true),
				ItemType.OneWayLightTimeToEarth => new("Lichtzeit zu Erde", "n/a", GetOneWayLightTimeToEarth, m_ItemTemplate, m_LabelColor, true),
				ItemType.Discovered => new("Entdeckung", "n/a", GetDiscoveredValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToSaturn => new("Distanz zu Saturn", "n/a", GetDistanceFromSaturnValue, m_ItemTemplate, m_LabelColor, true),
				_ => null
			};

			if (item != null)
				m_Items.Add(item);
		}

		Vector3 nextPosition = new(m_HorizontalOffset, m_VerticalSpacing * m_Items.Count * 0.5f, 0);
		foreach (var item in m_Items)
		{
			item.Position = nextPosition;
			item.Init(gameObject);
			nextPosition.y -= m_VerticalSpacing;
		}
	}

	private void UpdateItems()
	{
		transform.localScale = Vector3.one;

		float totalHeight = 0;
		foreach (var item in m_Items)
		{
			item.Update(m_Body);
			totalHeight += item.Height;
		}
		Vector3 nextPosition = new(m_HorizontalOffset, totalHeight * 0.5f, 0);
		foreach (var item in m_Items)
		{
			item.Position = nextPosition;
			nextPosition.y -= item.Height;
		}

		transform.localScale = Vector3.one * (float)m_Body.ScaledRadiusInSolarSystem;
	}

	private static string GetDistanceToSunValue(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Sun)));
		distance = math.max(distance - body.Radius, 0);
		return $"{distance:n0} km";
	}
	private static string GetOneWayLightTimeToSun(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Sun)));
		distance = math.max(distance - body.Radius, 0);
		return $"{distance / CMath.SpeedOfLight / 60:n3} Minuten";
	}
	private static string GetYearLengthValue(S_CelestialBody body) => $"{body.OrbitalPeriod:n0} Tage";
	private static string GetBodyTypeValue(S_CelestialBody body) => CelestialBodyTypes.ToStringGerman(body.Type);
	private static string GetAgeValue(S_CelestialBody body) => "~4,5 Mrd. Jahre";
	private static string GetDistFromGalacticCenterValue(S_CelestialBody body) => "26.000 Lichtjahre";
	private static string GetDistanceFromEarthValue(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Earth)));
		return $"{distance:n0} km";
	}
	private static string GetHumanVisitorsValue(S_CelestialBody body) => body.HumanVisitors < 0 ? "n/a" : $"{body.HumanVisitors:n0}";
	private static string GetMoonwalkersValue(S_CelestialBody body) => body.Moonwalkers < 0 ? "n/a" : $"{body.Moonwalkers:n0}";
	private static string GetRobotikVisitsValue(S_CelestialBody body) => body.RoboticVisits < 0 ? "n/a" : $"{body.RoboticVisits:n0}+";
	private static string GetDistanceFromJupiterValue(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Jupiter)));
		return $"{distance:n0} km";
	}
	private static string GetOneWayLightTimeToEarth(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Earth)));
		distance = math.max(distance - body.Radius, 0);
		return $"{distance / CMath.SpeedOfLight / 60:n3} Minuten";
	}
	private static string GetDiscoveredValue(S_CelestialBody body)
		=> body.DiscoveryDate == S_CelestialBody.InvalidDiscoveryDate ? "n/a" : body.DiscoveryDate.ToString(Date.Format.DE, true);

	private static string GetDistanceFromSaturnValue(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Saturn)));
		return $"{distance:n0} km";
	}

	private static string GetDescriptionValue(S_CelestialBody body)
	{
		return body.BodyIndex switch
		{
			OrbitType.Mercury => "Von Merkurs Oberfl�che aus w�rde die Sonne mehr als dreimal " +
							"so gro� erscheinen wie von der Erde aus betrachtet, und das Sonnenlicht " +
							"w�re bis zu 11-mal heller.",

			OrbitType.Venus => "�hnlich in Struktur und Gr��e wie die Erde, f�ngt die dichte Atmosph�re von " +
							"Venus W�rme in einem sich verst�rkenden Treibhauseffekt ein und macht sie damit zum " +
							"hei�esten Planeten in unserem Sonnensystem.",

			OrbitType.Earth => "Die Erde - unser Heimatplanet - ist bisher der einzige uns bekannte Ort, " +
							"der von Lebewesen bewohnt wird. Sie ist auch der einzige Planet in unserem " +
							"Sonnensystem mit fl�ssigem Wasser auf der Oberfl�che.",

			OrbitType.Mars => "Mars ist eine staubige, kalte W�stenwelt mit einer sehr d�nnen Atmosph�re. " +
							"Es gibt starke Hinweise darauf, dass der Mars vor Milliarden von Jahren feuchter " +
							"und w�rmer war und eine dickere Atmosph�re hatte.",

			OrbitType.Jupiter => "Jupiter ist mehr als doppelt so gro� wie alle anderen Planeten unseres Sonnensystems zusammen. " +
							"Der riesige rote Fleck des Planeten ist ein jahrhundertealter Sturm, der gr��er ist als die Erde.",

			OrbitType.Saturn => "Geschm�ckt mit einem atemberaubenden, komplexen System aus eisigen Ringen ist Saturn " +
							"einzigartig in unserem Sonnensystem. Die anderen Gasplaneten haben zwar auch Ringe, " +
							"aber keine sind so spektakul�r wie die Ringe Saturns.",

			OrbitType.Uranus => "Uranus - der siebte Planet von der Sonne aus gesehen - dreht sich fast im 90-Grad-Winkel " +
							"zur Ebene seiner Umlaufbahn. Diese einzigartige Neigung l�sst Uranus erscheinen, " +
							"als ob er seitlich rotieren w�rde.",

			OrbitType.Neptune => "Neptun - der achte und am weitesten entfernte gro�e Planet, der unsere Sonne umkreist - ist dunkel, " +
							"kalt und von �berschallwinden gepeitscht. Er wurde als erster Planet durch mathematische Berechnungen lokalisiert.",

			OrbitType.Lunar => "Der f�nftgr��te Mond im Sonnensystem, der Mond der Erde, ist der einzige Ort au�erhalb der Erde, " +
							"auf dem Menschen ihren Fu� gesetzt haben.",

			OrbitType.Io => "Io ist der vulkanisch aktivste K�rper in unserem Sonnensystem.",

			OrbitType.Europa => "Unter Europas eisiger Oberfl�che befindet sich ein Ozean aus salzigem Wasser, " +
							"der m�glicherweise eine Umgebung Leben bieten k�nnte.",

			OrbitType.Ganymede => "Ganymed ist der gr��te Satellit in unserem Sonnensystem. " +
							"Er ist gr��er als Merkur und Pluto und hat drei Viertel der Gr��e von Mars.",

			OrbitType.Callisto => "Callisto ist das am st�rksten verkraterte Objekt in unserem Sonnensystem.",

			OrbitType.Titan => "Der gr��te Mond des Saturns, Titan, hat einen erd�hnlichen Zyklus von Fl�ssigkeiten, " +
							"die �ber seine Oberfl�che flie�en. Er ist der einzige Mond mit einer dicken Atmosph�re.",

			OrbitType.Sun => "Die Sonne h�lt das Sonnensystem zusammen und h�lt alles - " +
							"von den gr��ten Planeten bis zu den kleinsten Tr�mmern - in ihrer Umlaufbahn.",

			_ => "n/a"
		};
	}

	public class QickInfoItem
	{
		public string Label
		{
			get => m_Label;
			set { m_Label = value; if (m_ValueMesh != null) m_LabelMesh.text = m_Label; }
		}
		private string m_Label = "Unnamed";
		public string Value
		{
			get => m_Value;
			set { m_Value = value; if (m_ValueMesh != null) m_ValueMesh.text = m_Value; }
		}
		private string m_Value = "NA";

		public Vector3 Position
		{
			get => m_Position;
			set { m_Position = value; if (m_Object != null) m_Object.transform.localPosition = m_Position; }
		}
		private Vector3 m_Position = Vector3.zero;

		public float Height => math.abs(m_LabelMesh.bounds.size.y + m_ValueMesh.bounds.size.y);

		private Func<S_CelestialBody, string> m_ValueFunc;
		private Color m_LabelColor;
		private bool m_IsRealtime;
		private bool m_IsInitialized = false;
		private GameObject m_Template;
		private GameObject m_Object;
		private TextMeshPro m_LabelMesh;
		private TextMeshPro m_ValueMesh;

		public QickInfoItem(string label, string value, Func<S_CelestialBody, string> valueFunc,
			GameObject template, Color labelColor, bool isRealtime = false)
		{
			m_Label = label;
			m_Value = value;
			m_ValueFunc = valueFunc;
			m_Template = template;
			m_LabelColor = labelColor;
			m_IsRealtime = isRealtime;
		}

		public void Init(GameObject parent)
		{
			m_Object = Instantiate(m_Template, parent.transform);
			m_Object.name = m_Label;
			m_Object.transform.localPosition = m_Position;
			m_Object.transform.Find("LabelMesh").gameObject.TryGetComponent(out m_LabelMesh);
			m_Object.transform.Find("ValueMesh").gameObject.TryGetComponent(out m_ValueMesh);

			m_LabelMesh.color = m_LabelColor.linear;
			m_LabelMesh.text = m_Label;
			m_LabelMesh.ClearMesh(true);
			m_ValueMesh.text = m_Value;
			m_ValueMesh.ClearMesh(true);
		}

		public void Update(S_CelestialBody body)
		{
			if (!m_IsRealtime & m_IsInitialized)
				return;
			Value = m_ValueFunc(body);
			m_IsInitialized = true;
		}
	}
}