using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private RenderingMaster renderingMaster;
    private Toggle[] toggles;
    private Toggle backFaceCullingToggle, openZDepthToggle;
    private Scrollbar firldOfViewScrollbar, sampleSizeScrollbar;
    // Start is called before the first frame update
    void Start()
    {
        backFaceCullingToggle = GameObject.Find("BackFaceCullingToggle").GetComponent<Toggle>();
        backFaceCullingToggle.onValueChanged.AddListener((bool value) => OnValueChangedForBackFaceCulling(backFaceCullingToggle, value));
        openZDepthToggle = GameObject.Find("OpenZDepthToggle").GetComponent<Toggle>();
        openZDepthToggle.onValueChanged.AddListener((bool value) => OnValueChangedForOnOpenZDepth(openZDepthToggle, value));


        toggles = GameObject.Find("ToggleGroup").GetComponentsInChildren<Toggle>();
        for (int i = 0; i < toggles.Length; i++)
        {
            Toggle toggle = toggles[i];
            toggle.onValueChanged.AddListener((bool value) => OnToggleClick(toggle, value));
        }

        firldOfViewScrollbar = GameObject.Find("FirldOfViewScrollbar").GetComponent<Scrollbar>();
        firldOfViewScrollbar.onValueChanged.AddListener(OnValueChangedForFirldOfView);

        sampleSizeScrollbar = GameObject.Find("SampleSizeScrollbar").GetComponent<Scrollbar>();
        sampleSizeScrollbar.onValueChanged.AddListener(OnValueChangedForSample);
        
        renderingMaster = RenderingMaster._instance;
        Init();
    }
    private void Init()
    {
        toggles[0].isOn = true;
        firldOfViewScrollbar.value = Camera.main.fieldOfView / 179f;
        sampleSizeScrollbar.value = renderingMaster._SampleSize / 1280f;
        backFaceCullingToggle.isOn = true;
        openZDepthToggle.isOn = true;
    }
    private void OnValueChangedForFirldOfView(float value) {
        Camera.main.fieldOfView = value * 179f;
    }

    private void OnValueChangedForSample(float value)
    {
        renderingMaster._SampleSize = value * 1280;
    }

    private void OnToggleClick(Toggle toggle, bool isSwitch) {
        if (isSwitch == false) return;
        renderingMaster.SetDrawTrianglesType(int.Parse(toggle.name));
    }

    private void OnValueChangedForBackFaceCulling(Toggle toggle, bool isSwitch)
    {
        renderingMaster.SetBackFaceCulling(isSwitch);
    }

    private void OnValueChangedForOnOpenZDepth(Toggle toggle, bool isSwitch) {
        renderingMaster.SetOpenZDepth(isSwitch);
    }
}
