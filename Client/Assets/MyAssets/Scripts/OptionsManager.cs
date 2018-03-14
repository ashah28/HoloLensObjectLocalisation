using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Examples.Prototyping;

public class OptionsManager : Singleton<OptionsManager> {

    ObjectMarker lastMarker;

    UnityEngine.TouchScreenKeyboard keyboard;
    public static string keyboardText = "";

    // Use this for initialization
    void Start () {
        gameObject.SetActive(false);
	}

    private void Update()
    {
        if (TouchScreenKeyboard.visible == false && keyboard != null)
        {
            if (keyboard.done == true)
            {
                keyboardText = keyboard.text;
                keyboard = null;

                DebugManager.Instance.PrintToInfoLog("Renaming " + lastMarker + " to " + keyboardText);

                lastMarker.Rename(keyboardText);
                ToggleMenuVisibility(lastMarker.gameObject);
            }
        }
    }

    /// <summary>
    /// Toggles the menu visibility.
    /// </summary>
    /// <param name="go">The gameobject of the marker.</param>
    public void ToggleMenuVisibility(GameObject go)
    {
        if(go != null && lastMarker != go.GetComponent<ObjectMarker>())
        {
            gameObject.SetActive(true);
            gameObject.transform.position = go.transform.position 
                + (Camera.main.transform.position - go.transform.position).normalized * 0.1f;
            lastMarker = go.GetComponent<ObjectMarker>();
        }
        //if last click was registered on me
        else
        {
            //if moving
            if(go.GetComponent<MoveWithObject>())
            {
                go.GetComponent<MoveWithObject>().StopRunning();
                Destroy(go.GetComponent<MoveWithObject>());
            }
            Pin();
            gameObject.SetActive(false);
            lastMarker = null;
        }
    }

    /// <summary>
    /// Deletes the marker.
    /// </summary>
    public void DeleteMarker()
    {
        DebugManager.Instance.PrintToInfoLog("DeleteMarker");
        if (lastMarker)
        {
            GameObject buffer = lastMarker.gameObject;
            PersistenceManager.Instance.DeleteAnchor(lastMarker);
            ToggleMenuVisibility(buffer);
            ObjectLocator.Instance.DeleteObject(buffer);            
        }
    }

    /// <summary>
    /// Drag drop this object.
    /// </summary>
    public void Placement()
    {
        DebugManager.Instance.PrintToInfoLog("Placement"+lastMarker);

        if (lastMarker)
        {
            PersistenceManager.Instance.DeleteAnchor(lastMarker);
            MoveWithObject mwo = lastMarker.gameObject.AddComponent<MoveWithObject>() as MoveWithObject;
            mwo.StartRunning();
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Pins this object.
    /// </summary>
    public void Pin()
    {
        if (lastMarker)
        {
            DebugManager.Instance.PrintToInfoLog("Pinning: " + lastMarker);
            PersistenceManager.Instance.AddAnchor(lastMarker);
            ToggleMenuVisibility(lastMarker.gameObject);
        }
    }

    /// <summary>
    /// Labels the object.
    /// </summary>
    public void LabelObject()
    {
        DebugManager.Instance.PrintToInfoLog("Label");
        if (lastMarker)
        {
            // Single-line textbox with title
            keyboard = TouchScreenKeyboard.Open(lastMarker.markerName.Split(':')[0], 
                TouchScreenKeyboardType.Default, false, false, false, false, "Marker name:");
        }
    }
}