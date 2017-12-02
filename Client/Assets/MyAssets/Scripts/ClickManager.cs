using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickManager : MonoBehaviour {

    float lastClickTimestamp;

    // Use this for initialization
    void OnEnable () {
        HoloToolkit.Unity.InputModule.InputManager.holoClickDelegate += OnInputClicked;
    }

    private void OnDisable()
    {
        HoloToolkit.Unity.InputModule.InputManager.holoClickDelegate -= OnInputClicked;
    }

    /// <summary>
    /// Called whenever a click is registered on Hololens. Blocked by nothing! Pure, simple, holo click...
    /// </summary>
    public void OnInputClicked()
    {
        //single click
        if (lastClickTimestamp - Time.timeSinceLevelLoad > 0.5f)
        {
            ImageCapture.Instance.StartStopCapturing();
        }
        //for now considering everything as double click
        else
        {
            ObjectLocator.Instance.ClearMarkers();
        }

        lastClickTimestamp = Time.timeSinceLevelLoad;
    }
}
