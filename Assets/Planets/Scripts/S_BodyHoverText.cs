using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class S_BodyHoverText : MonoBehaviour
{
	[SerializeField]
	private S_CelestialBody m_Body;

	private MeshRenderer m_MeshRenderer;
	private TextMeshPro m_TextMesh;
	private Vector3 m_LocalPosition;
	private Vector3 m_LocalScale;

	private static readonly float s_AnimationLength = 0.5f;
	private TextAnimator m_Animatior = TextAnimator.CreateDone(0, 0, false, false, s_AnimationLength);

	// Start is called before the first frame update
	void Start()
	{
		TryGetComponent(out m_MeshRenderer);
		TryGetComponent(out m_TextMesh);
		m_LocalPosition = transform.localPosition;
		m_LocalScale = transform.localScale;

		m_MeshRenderer.enabled = false;
		m_TextMesh.text = m_Body.BodyName;
		m_TextMesh.color = new Color(1, 1, 1, 0);

		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
	}

	void OnDestroy()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
	}

	// Update is called once per frame
	void Update()
	{
		bool doneThisFrame = !m_Animatior.IsDone;
		m_Animatior.Update();
		doneThisFrame &= m_Animatior.IsDone;
		if (!m_Animatior.IsDone || doneThisFrame)
		{
			var color = m_TextMesh.color;
			color.a = math.pow(m_Animatior.AlphaCurrent, 2.2f);
			m_TextMesh.color = color;
			m_MeshRenderer.enabled = m_Animatior.EnabledCurrent;
		}
	}

	public void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		if (!m_MeshRenderer.enabled)
			return;

		transform.localPosition = m_LocalPosition * (float)m_Body.ScaledRadiusInSolarSystem;
		Vector3 camPos = camera.transform.position;
		Vector3 camForward = camera.transform.forward;
		Vector3 relPos = transform.position - camPos;
		float dist = Vector3.Dot(relPos, camForward);
		relPos -= camForward * dist;

		float correctedDist = math.min(0.95f * camera.farClipPlane, dist);
		transform.position = camera.transform.position + relPos * (correctedDist / dist) + camForward * correctedDist;
		transform.localScale = m_LocalScale * correctedDist;
		transform.rotation = camera.transform.rotation;
	}

	public void OnHoverStart()
	{
		m_Animatior = new(m_Animatior.AlphaCurrent, 1, m_Animatior.EnabledCurrent, true, s_AnimationLength);
	}

	public void OnHoverEnd()
	{
		m_Animatior = new(m_Animatior.AlphaCurrent, 0, m_Animatior.EnabledCurrent, false, s_AnimationLength);
	}

	private class TextAnimator
	{

		public float AlphaStart { get; private set; } = 0;
		public float AlphaEnd { get; private set; } = 0;
		public float AlphaCurrent { get; private set; } = 0;

		public bool EnabledStart { get; private set; } = false;
		public bool EnabledEnd { get; private set; } = false;
		public bool EnabledCurrent { get; private set; } = false;

		public float Length { get; private set; } = 1;
		public float Progress { get; private set; } = 0;
		public bool IsDone { get; private set; } = false;
		private float m_Time = 0;

		public TextAnimator()
		{
			Progress = 1;
			m_Time = 1;
			IsDone = true;
		}

		public TextAnimator(float aStart, float aEnd, bool eStart, bool eEnd, float length)
		{
			AlphaStart = aStart;
			AlphaEnd = aEnd;
			AlphaCurrent = aStart;
			EnabledStart = eStart;
			EnabledEnd = eEnd;
			EnabledCurrent = eStart;
			Length = length;
		}

		public void Update()
		{
			if (IsDone)
				return;

			m_Time += Time.deltaTime;
			if (m_Time > Length)
			{
				m_Time = Length;
				IsDone = true;
			}

			if (!EnabledStart)
				EnabledCurrent = EnabledEnd;
			else if (IsDone)
				EnabledCurrent = EnabledEnd;

			Progress = EaseOutQuad(m_Time / Length);
			AlphaCurrent = math.lerp(AlphaStart, AlphaEnd, Progress);
		}

		private static float EaseOutQuad(float x)
		{
			return 1f - (1f - x) * (1f - x);
		}

		public static TextAnimator CreateDone(float aStart, float aEnd, bool eStart, bool eEnd, float length)
		{
			return new TextAnimator(aStart, aEnd, eStart, eEnd, length)
			{
				AlphaCurrent = aEnd,
				EnabledCurrent = eEnd,
				Progress = 1,
				m_Time = length,
				IsDone = true
			};
		}
	}
}
