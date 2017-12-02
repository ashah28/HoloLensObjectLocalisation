using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickManager : MonoBehaviour {

    float lastClickTimestamp;

    [SerializeField] float doubleClickDuration = 0.3f;

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
        //double click
        if (IsInvoking("Click"))
        {
            CancelInvoke("Click");
            ObjectLocator.Instance.ClearMarkers();

            print("Double click");
        }
        //count for single
        else
        {
            Invoke("Click", doubleClickDuration);
        }
    }

    /// <summary>
    /// Trigger a single click
    /// </summary>
    void Click()
    {
        ImageCapture.Instance.StartStopCapturing();
    }
}
