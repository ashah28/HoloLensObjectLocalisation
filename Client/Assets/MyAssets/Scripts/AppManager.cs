using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class AppManager : Singleton<AppManager> {

    public string serverAddress = " http://10.208.19.44:5000";
    public string queryAPI = "/queryimg";
    public float scanRate = 1.5f;

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
    /// Fetches settings dynamically on each load
    /// </summary>
    /// <returns></returns>
    IEnumerator FetchSettings()
    {
        WWW www = new WWW(serverAddress + "/settings");
        yield return www;

        SettingsJSON settings = JsonUtility.FromJson<SettingsJSON>(www.text);
        DebugManager.Instance.PrintToRunningLog("Settings fetched");

        debugCanvas.SetActive(settings.debugActive);
        scanRate = settings.refreshRate;
    }    
}