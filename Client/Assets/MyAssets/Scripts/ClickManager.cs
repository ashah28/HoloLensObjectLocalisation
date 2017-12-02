using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ClickActionManager : MonoBehaviour {

    float lastClickTimestamp;

    [SerializeField] float doubleClickDuration = 0.3f;

    // Use this for initialization
    void OnEnable () {
        InputManager.holoClickDelegate += OnInputClicked;
    }

    private void OnDisable()
    {
        InputManager.holoClickDelegate -= OnInputClicked;
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
        DebugManager.Instance.PrintToRunningLog(GazeManager.Instance.IsGazingAtObject.ToString());
        if (GazeManager.Instance.IsGazingAtObject)
            DeleteObject(GazeManager.Instance.HitObject);
        else
            ImageCapture.Instance.StartStopCapturing();
    }

    void DeleteObject(GameObject obj)
    {
        DebugManager.Instance.PrintToRunningLog("Clicked on" + obj.name);
    }
}
