using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class AppManager : Singleton<AppManager> {

    /// <summary>
    /// The server address with port number
    /// </summary>
    public string serverAddress = " http://10.208.19.44:5000";

    /// <summary>
    /// The url address/page to query image contents
    /// </summary>
    public string queryAPI = "/queryimg";

    /// <summary>
    /// The scan period after which next image would be taken in auto mode.
    /// In manual mode this value hold no relevance as capturing is triggered by click
    /// </summary>
    public float scanPeriod = 1.5f;

    /// <summary>
    /// True: auto mode is active and image will be taken automatically, after every scan period
    /// False: Image would be taken only when pointer is clicked in an empty area and when camera is not preoccupied
    /// </summary>
    public bool autoMode = true;

    /// <summary>
    /// Cam resolution at which iamge/video is to be captured
    /// </summary>
    public int camResolution = 0;

    /// <summary>
    /// Holds the Canvas to hide/show debug log
    /// </summary>
    [SerializeField] GameObject debugCanvas;

    /// <summary>
    /// Setup on Start
    /// </summary>
    void OnEnable()
    {
        StartCoroutine(CheckServerStatus());
        StartCoroutine(FetchSettings());
    }
    
    /// <summary>
    /// Confirms if server is accessibke
    /// </summary>
    /// <returns></returns>
    IEnumerator CheckServerStatus()
    {
        WWW www = new WWW(serverAddress);
        yield return www;
        DebugManager.Instance.PrintToInfoLog("Server Status-> " + (www.error == null ? www.text : " ERR :" + www.error));

        DebugManager.Instance.PrintToRunningLog("Screen W:" + Screen.width + " H:" + Screen.height);
    }

    /// <summary>
    /// Fetches settings dynamically on each time
    /// </summary>
    /// <returns></returns>
    IEnumerator FetchSettings()
    {
        WWW www = new WWW(serverAddress + "/settings");
        yield return www;

        SettingsJSON settings = JsonUtility.FromJson<SettingsJSON>(www.text);
        DebugManager.Instance.PrintToRunningLog("Debug:" + settings.debugActive 
            + " Period:" + settings.refreshPeriod + " Auto: " + settings.autoMode);

        debugCanvas.SetActive(settings.debugActive);
        scanPeriod = settings.refreshPeriod;
        autoMode = settings.autoMode;

        camResolution = settings.camResolution;
    }    
}