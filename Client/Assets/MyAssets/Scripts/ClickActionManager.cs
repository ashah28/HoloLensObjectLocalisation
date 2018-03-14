using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ClickActionManager : MonoBehaviour, IInputClickHandler {

    float lastClickTimestamp;

    [SerializeField] float doubleClickDuration = 0.3f;

    // Use this for initialization
    void Start ()
    {
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }
    
    /// <summary>
    /// Called whenever a click is registered on Hololens.
    /// </summary>
    public virtual void OnInputClicked(InputClickedEventData eventData)
    {
        print("on click");
        //double click
        if (IsInvoking("Click"))
        {
            CancelInvoke("Click");
            ObjectLocator.Instance.ClearMarkers();
            PersistenceManager.Instance.ClearAllAnchors();
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
        if (GazeManager.Instance.IsGazingAtObject && GazeManager.Instance.HitObject.GetComponent<ObjectMarker>())
        {
            OptionsManager.Instance.ToggleMenuVisibility(GazeManager.Instance.HitObject);
        }
        else
        {
            if (AppManager.Instance.autoMode)
                ImageCapture.Instance.StartStopCapturing();
            else
                ImageCapture.Instance.CaptureImage();
        }
    }
}