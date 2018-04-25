using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;

public class DebugManager : Singleton<DebugManager> {

    [SerializeField]
    Text runningLog;

    [SerializeField]
    Text infoLog;
    
	// Use this for initialization
	void Start ()
    {
        if (!runningLog)
            Debug.LogError("Visual log not found");
	}

    /// <summary>
    /// Prints to running log.
    /// </summary>
    /// <param name="message">The message.</param>
    public void PrintToRunningLog(string message)
    {
        string data = GetCurrentTimestamp() + " : " + message + "\n";
        print(data);
        runningLog.text = data + runningLog.text.Substring(0, Mathf.Min(runningLog.text.Length, 1000));
    }

    /// <summary>
    /// Prints to information log.
    /// </summary>
    /// <param name="message">The message.</param>
    public void PrintToInfoLog(string message)
    {
        string data = GetCurrentTimestamp() + " : " + message + "\n";
        print(data);
        infoLog.text = data + infoLog.text.Substring(0, Mathf.Min(infoLog.text.Length, 1000));
    }

    /// <summary>
    /// Gets the current timestamp in secs, since start.
    /// </summary>
    /// <returns>current time in string</returns>
    string GetCurrentTimestamp()
    {
        return Time.time.ToString("000.000");
    }
}
