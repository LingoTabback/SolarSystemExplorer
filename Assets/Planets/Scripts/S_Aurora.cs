using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class S_Aurora : MonoBehaviour
{
	private MeshRenderer m_MeshRenderer;

	// Start is called before the first frame update
	void Start()
	{
		m_MeshRenderer = GetComponent<MeshRenderer>();
		RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
	}

	void OnDestroy()
	{
		RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
	}

	private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
	{
		const float eps = 0.001f;
		Vector3 camForward = camera.transform.forward;

		Bounds adjustedBounds = m_MeshRenderer.bounds;
		adjustedBounds.center = m_MeshRenderer.transform.position - camForward * (eps * 3);
		adjustedBounds.extents = Vector3.Scale(m_MeshRenderer.localBounds.extents,
			m_MeshRenderer.transform.lossyScale) * 1.05f;
		m_MeshRenderer.bounds = adjustedBounds;
	}
}
