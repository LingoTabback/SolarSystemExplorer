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
    private enum Tools
    {
        P_Tool_01,
        P_Tool_02,
        P_Tool_03
    }
    
    [SerializeField]
    private InputActionReference m_SelectAction;

    [SerializeField]
    private GameObject rightHandController;

    [Header("Tool Prefabs")]
    
    [SerializeField] 
    private GameObject P_Tool_01;
    
    [SerializeField] 
    private GameObject P_Tool_02;
    
    [SerializeField] 
    private GameObject P_Tool_03;

    [Header("Tool Selection")]
    [SerializeField]
    private Tools currentTool;
    
    // Start is called before the first frame update
    void Start() { }

    private bool settedUp = false;
    // Update is called once per frame
    // Has to be called after Start (initializes before referenced objects) 
    void Update()
    {
        if (!settedUp)
        {
            DestroyTool(Tools.P_Tool_01);
            CreateTool(currentTool);
            settedUp = true;
        }
    }
    
    void Awake()
    {
        m_SelectAction.action.performed += Next;
    }

    void OnDestroy()
    {
        m_SelectAction.action.performed -= Next;
    }

    private void Next(InputAction.CallbackContext context)
    {
        //It ain`t much, but it`s honest work.
        DestroyTool(currentTool);
        currentTool = NextEnumTool();
        CreateTool(currentTool);
    }

    //get next tool in enum (cyclic)
    private Tools NextEnumTool() {
        int currentIndex = (int)currentTool;
        currentIndex = (currentIndex + 1) % Enum.GetNames(typeof(Tools)).Length;
        return (Tools)currentIndex;
    }

    // destroy all tools here
    private void DestroyTool(Tools tool)
    {
        //casting sucked, sorry
        switch (tool)
        {
            case Tools.P_Tool_01 :
                GameObject.DestroyImmediate(rightHandController.transform.Find("P_Tool_01(Clone)").gameObject);
                break;
            case Tools.P_Tool_02 :
                GameObject.DestroyImmediate(rightHandController.transform.Find("P_Tool_02(Clone)").gameObject);
                break;
            default :
                GameObject.DestroyImmediate(rightHandController.transform.Find("P_Tool_03(Clone)").gameObject);
                break;
        }
        print("Removed tool " + tool);
    }

    // create tools here
    private void CreateTool(Tools tool)
    {
        switch (tool)
        {
            case Tools.P_Tool_01 :
                Instantiate(P_Tool_01,rightHandController.transform); break;
            case Tools.P_Tool_02 :
                Instantiate(P_Tool_02,rightHandController.transform); break;
            default :
                Instantiate(P_Tool_03,rightHandController.transform); break;
        }
        //Debug
        print("Created tool " + currentTool + "(Clone)"); 
    }
}
