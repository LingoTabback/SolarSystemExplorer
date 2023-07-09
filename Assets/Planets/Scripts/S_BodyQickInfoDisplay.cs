using Animation;
using AstroTime;
using CustomMath;
using Ephemeris;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(S_InfoDisplayAnchor))]
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

	private static readonly float s_UIFocusAnimationLength = 2;
	private Animator<FloatAnimatable> m_UIFocusAnimator = Animator<FloatAnimatable>.CreateDone(0, 0, s_UIFocusAnimationLength, 0.5f);

	private S_InfoDisplayAnchor m_DisplayAnchor;

	// Start is called before the first frame update
	private void Start()
	{
		System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

		m_DisplayAnchor = GetComponent<S_InfoDisplayAnchor>();
		m_DisplayAnchor.enabled = false;

		InitItems();

		m_Body.FocusGained += OnFocusGained;
		m_Body.FocusLoosing += OnFocusLoosing;
	}

	private void OnDestroy()
	{
		if (m_Body != null)
		{
			m_Body.FocusGained -= OnFocusGained;
			m_Body.FocusLoosing -= OnFocusLoosing;
		}
	}

	// Update is called once per frame
	private void Update()
	{
		m_UIFocusAnimator.Update(Time.deltaTime);
	}

	private void FixedUpdate()
	{
		UpdateItems();
	}

	private void InitItems()
	{
		m_Items.Clear();

		foreach (ItemType type in m_ItemTypes)
		{
			QickInfoItem item = type switch
			{
				ItemType.DistanceToSun => new("distance from sun", "n/a", GetDistanceToSunValue, m_ItemTemplate, m_LabelColor, true),
				ItemType.OneWayLightTimeToSun => new("one way light time to the sun", "n/a", GetOneWayLightTimeToSun, m_ItemTemplate, m_LabelColor, true),
				ItemType.LengthOfYear => new("length of year", "n/a", GetYearLengthValue, m_ItemTemplate, m_LabelColor),
				ItemType.PlanetType => new("planet type", "n/a", GetBodyTypeValue, m_ItemTemplate, m_LabelColor),
				ItemType.Description => new(m_Body.BodyName, "n/a", GetDescriptionValue, m_DescriptionTemplate, Color.white),
				ItemType.Age => new("age", "n/a", GetAgeValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToGalacticCenter => new("distance from galactic center", "n/a", GetDistFromGalacticCenterValue, m_ItemTemplate, m_LabelColor),
				ItemType.StarType => new("star-type", "n/a", GetBodyTypeValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToEarth => new("distance from earth", "n/a", GetDistanceFromEarthValue, m_ItemTemplate, m_LabelColor, true),
				ItemType.HumanVisitors => new("human visitors", "n/a", GetHumanVisitorsValue, m_ItemTemplate, m_LabelColor),
				ItemType.Moonwalkers => new("moonwalkers", "n/a", GetMoonwalkersValue, m_ItemTemplate, m_LabelColor),
				ItemType.RoboticVisits => new("robotic visits", "n/a", GetRobotikVisitsValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToJupiter => new("distance from jupiter", "n/a", GetDistanceFromJupiterValue, m_ItemTemplate, m_LabelColor, true),
				ItemType.OneWayLightTimeToEarth => new("one way light time to earth", "n/a", GetOneWayLightTimeToEarth, m_ItemTemplate, m_LabelColor, true),
				ItemType.Discovered => new("discovered", "n/a", GetDiscoveredValue, m_ItemTemplate, m_LabelColor),
				ItemType.DistanceToSaturn => new("distance from saturn", "n/a", GetDistanceFromSaturnValue, m_ItemTemplate, m_LabelColor, true),
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

	private void OnFocusGained()
	{
		m_UIFocusAnimator.Reset(1);

		foreach (var item in m_Items)
			item.GameObject.SetActive(true);

		m_DisplayAnchor.enabled = true;
	}

	private void OnFocusLoosing()
	{
		m_UIFocusAnimator.Reset(0);

		foreach (var item in m_Items)
			item.GameObject.SetActive(false);

		m_DisplayAnchor.enabled = false;
	}

	private void UpdateItems()
	{
		transform.localScale = Vector3.one;

		float totalHeight = 0;
		foreach (var item in m_Items)
		{
			item.AlphaScale = m_UIFocusAnimator.Current;
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
		return $"{distance / CMath.SpeedOfLight / 60:n3} mins";
	}
	private static string GetYearLengthValue(S_CelestialBody body) => $"{body.OrbitalPeriod:n0} Earth Days";
	private static string GetBodyTypeValue(S_CelestialBody body) => CelestialBodyTypes.ToStringEN(body.Type);
	private static string GetAgeValue(S_CelestialBody body) => "~4.5 billion years";
	private static string GetDistFromGalacticCenterValue(S_CelestialBody body) => "26,000 light years";
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
		return $"{distance / CMath.SpeedOfLight / 60:n3} mins";
	}
	private static string GetDiscoveredValue(S_CelestialBody body)
		=> body.DiscoveryDate == S_CelestialBody.InvalidDiscoveryDate ? "n/a" : body.DiscoveryDate.ToString(Date.Format.US, true);

	private static string GetDistanceFromSaturnValue(S_CelestialBody body)
	{
		double distance = CMath.AUtoKM(math.length(body.PositionInSystem - body.ParentSystem.GetBodyPositionInSystem(OrbitType.Saturn)));
		return $"{distance:n0} km";
	}

	private static string GetDescriptionValueDE(S_CelestialBody body)
	{
		return body.BodyIndex switch
		{
			OrbitType.Mercury => "Von Merkurs Oberfläche aus würde die Sonne mehr als dreimal " +
							"so groß erscheinen wie von der Erde aus betrachtet, und das Sonnenlicht " +
							"wäre bis zu 11-mal heller.",

			OrbitType.Venus => "Ähnlich in Struktur und Größe wie die Erde, fängt die dichte Atmosphäre von " +
							"Venus Wärme in einem sich verstärkenden Treibhauseffekt ein und macht sie damit zum " +
							"heißesten Planeten in unserem Sonnensystem.",

			OrbitType.Earth => "Die Erde - unser Heimatplanet - ist bisher der einzige uns bekannte Ort, " +
							"der von Lebewesen bewohnt wird. Sie ist auch der einzige Planet in unserem " +
							"Sonnensystem mit flüssigem Wasser auf der Oberfläche.",

			OrbitType.Mars => "Mars ist eine staubige, kalte Wüstenwelt mit einer sehr dünnen Atmosphäre. " +
							"Es gibt starke Hinweise darauf, dass der Mars vor Milliarden von Jahren feuchter " +
							"und wärmer war und eine dickere Atmosphäre hatte.",

			OrbitType.Jupiter => "Jupiter ist mehr als doppelt so groß wie alle anderen Planeten unseres Sonnensystems zusammen. " +
							"Der riesige rote Fleck des Planeten ist ein jahrhundertealter Sturm, der größer ist als die Erde.",

			OrbitType.Saturn => "Geschmückt mit einem atemberaubenden, komplexen System aus eisigen Ringen ist Saturn " +
							"einzigartig in unserem Sonnensystem. Die anderen Gasplaneten haben zwar auch Ringe, " +
							"aber keine sind so spektakulär wie die Ringe Saturns.",

			OrbitType.Uranus => "Uranus - der siebte Planet von der Sonne aus gesehen - dreht sich fast im 90-Grad-Winkel " +
							"zur Ebene seiner Umlaufbahn. Diese einzigartige Neigung lässt Uranus erscheinen, " +
							"als ob er seitlich rotieren würde.",

			OrbitType.Neptune => "Neptun - der achte und am weitesten entfernte große Planet, der unsere Sonne umkreist - ist dunkel, " +
							"kalt und von Überschallwinden gepeitscht. Er wurde als erster Planet durch mathematische Berechnungen lokalisiert.",

			OrbitType.Lunar => "Der fünftgrößte Mond im Sonnensystem, der Mond der Erde, ist der einzige Ort außerhalb der Erde, " +
							"auf dem Menschen ihren Fuß gesetzt haben.",

			OrbitType.Io => "Io ist der vulkanisch aktivste Körper in unserem Sonnensystem.",

			OrbitType.Europa => "Unter Europas eisiger Oberfläche befindet sich ein Ozean aus salzigem Wasser, " +
							"der möglicherweise eine Umgebung Leben bieten könnte.",

			OrbitType.Ganymede => "Ganymed ist der größte Satellit in unserem Sonnensystem. " +
							"Er ist größer als Merkur und Pluto und hat drei Viertel der Größe von Mars.",

			OrbitType.Callisto => "Callisto ist das am stärksten verkraterte Objekt in unserem Sonnensystem.",

			OrbitType.Titan => "Der größte Mond des Saturns, Titan, hat einen erdähnlichen Zyklus von Flüssigkeiten, " +
							"die über seine Oberfläche fließen. Er ist der einzige Mond mit einer dicken Atmosphäre.",

			OrbitType.Sun => "Die Sonne hält das Sonnensystem zusammen und hält alles - " +
							"von den größten Planeten bis zu den kleinsten Trümmern - in ihrer Umlaufbahn.",

			_ => "n/a"
		};
	}

	private static string GetDescriptionValue(S_CelestialBody body)
	{
		return body.BodyIndex switch
		{
			OrbitType.Mercury => "From the surface of Mercury, the Sun would appear more " +
							"than three times as large as it does when viewed from " +
							"Earth, and the sunlight would be as much as 11 times brighter.",

			OrbitType.Venus => "Similar in structure and size to Earth, Venus's thick " +
								"atmosphere traps heat in a runaway greenhouse effect, " +
								"making it the hottest planet in our solar system.",

			OrbitType.Earth => "Earth—our home planet—is the only place we know of so " +
								"far that’s inhabited by living things. It's also the only " +
								"planet in our solar system with liquid water on the surface.",

			OrbitType.Mars => "Mars is a dusty, cold, desert world with a very thin " +
								"atmosphere. There is strong evidence Mars was – billions " +
								"of years ago – wetter and warmer, with a thicker atmosphere.",

			OrbitType.Jupiter => "Jupiter is more than twice as massive than the other " +
								"planets of our solar system combined. The giant planet's " +
								"Great Red Spot is a centuries-old storm bigger than Earth.",

			OrbitType.Saturn => "Adorned with a dazzling, complex system of icy rings, " +
								"Saturn is unique in our solar system. The other giant " +
								"planets have rings, but none are as spectacular as Saturn's.",

			OrbitType.Uranus => "Uranus—seventh planet from the Sun—rotates at a nearly " +
								"90-degree angle from the plane of its orbit. This unique " +
								"tilt makes Uranus appear to spin on its side.",

			OrbitType.Neptune => "Neptune—the eighth and most distant major planet " +
								"orbiting our Sun—is dark, cold and whipped by " +
								"supersonic winds. It was the first planet located through " +
								"mathematical calculations.",

			OrbitType.Lunar => "The fifth largest moon in the solar system, Earth's moon " +
								"is the only place beyond Earth where humans have set foot.",

			OrbitType.Io => "Io is the most volcanically active body in the solar system.",

			OrbitType.Europa => "Europa may be one of the best places to look for " +
								"environments where life could exist beyond Earth.",

			OrbitType.Ganymede => "Ganymede is the largest satellite in our solar system. It is " +
								"larger than Mercury and Pluto, and three-quarters the size of Mars.",

			OrbitType.Callisto => "Callisto is the most heavily cratered object in our solar system.",

			OrbitType.Titan => "Saturn’s largest moon, Titan has an earthlike cycle of " +
								"liquids flowing across its surface. It is the only moon with " +
								"a thick atmosphere.",

			OrbitType.Sun => "The Sun holds the solar system together, keeping " +
							"everything – from the biggest planets to the smallest " +
							"debris – in its orbit.",

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
		private string m_Value = "n/a";

		public Vector3 Position
		{
			get => m_Position;
			set { m_Position = value; if (m_Object != null) m_Object.transform.localPosition = m_Position; }
		}
		private Vector3 m_Position = Vector3.zero;

		public float Height => math.abs(m_LabelMesh.bounds.size.y + m_ValueMesh.bounds.size.y);
		public GameObject GameObject => m_Object;
		public float AlphaScale { get; set; } = 1;

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
			m_LabelMesh.alpha = AlphaScale;
			m_ValueMesh.alpha = AlphaScale;

			if (!m_IsRealtime & m_IsInitialized)
				return;
			Value = m_ValueFunc(body);
			m_IsInitialized = true;
		}
	}
}
