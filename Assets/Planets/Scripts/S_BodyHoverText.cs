using Animation;
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
	private Animator<TextProperties> m_Animatior = Animator<TextProperties>.CreateDone(new(0, false), new(0, false), s_AnimationLength, EasingType.EaseOutQuad);

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
		m_Animatior.Update(Time.deltaTime);
		doneThisFrame &= m_Animatior.IsDone;
		if (!m_Animatior.IsDone || doneThisFrame)
		{
			var color = m_TextMesh.color;
			color.a = math.pow(m_Animatior.Current.Alpha, 2.2f);
			m_TextMesh.color = color;
			m_MeshRenderer.enabled = m_Animatior.Current.Enabled;
		}
	}

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		if (!m_MeshRenderer.enabled)
			return;

		transform.position = transform.parent.position +  m_LocalPosition * (float)m_Body.ScaledRadiusInSolarSystem;
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

	public void OnHoverStart() => m_Animatior.Reset(new(1, true));

	public void OnHoverEnd() => m_Animatior.Reset(new(0, false));

	private struct TextProperties : IAnimatable<TextProperties>
	{
		public float Alpha;
		public bool Enabled;

		public TextProperties(float alpha, bool enabled)
		{
			Alpha = alpha;
			Enabled = enabled;
		}

		public TextProperties Lerp(in TextProperties to, float alpha)
			=> new(math.lerp(Alpha, to.Alpha, alpha), !Enabled | alpha >= 1 ? to.Enabled : Enabled);
	}
}
