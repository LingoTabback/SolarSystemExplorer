using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Object = System.Object;

public class S_ToolController : MonoBehaviour
{
	[SerializeField]
	private InputActionReference m_SelectAction;

	[SerializeField]
	private GameObject[] m_ToolPrefabs;
	[SerializeField]
	private int m_CurrentToolIndex = 0;

	private GameObject m_CurrentToolObject;
	
	// Start is called before the first frame update
	void Start()
	{
		m_CurrentToolIndex %= m_ToolPrefabs.Length;
	}

	private bool m_IsSetUp = false;
	// Update is called once per frame
	// Has to be called after Start (initializes before referenced objects) 
	void Update()
	{
		if (!m_IsSetUp)
		{
			Destroy(transform.GetChild(transform.childCount - 1).gameObject); // delete pre spawned prefab
			CreateTool(m_CurrentToolIndex);
			m_IsSetUp = true;
		}
	}
	
	void Awake()
	{
		m_SelectAction.action.performed += SwitchToNext;
	}

	void OnDestroy()
	{
		m_SelectAction.action.performed -= SwitchToNext;
	}

	private void SwitchToNext(InputAction.CallbackContext context)
	{
		//It ain`t much, but it`s honest work.
		DestroyCurrentTool();
		m_CurrentToolIndex = (m_CurrentToolIndex + 1) % m_ToolPrefabs.Length;
		CreateTool(m_CurrentToolIndex);
	}

	// destroy all tools here
	private void DestroyCurrentTool()
	{
		if (m_CurrentToolObject != null)
			Destroy(m_CurrentToolObject);
	}

	// create tools here
	private void CreateTool(int toolIndex)
	{
		m_CurrentToolObject = Instantiate(m_ToolPrefabs[toolIndex], transform);
		//m_CurrentToolObject.transform.localPosition = Vector3.zero;
		//m_CurrentToolObject.transform.localScale = Vector3.one * 0.1f;
	}
}
