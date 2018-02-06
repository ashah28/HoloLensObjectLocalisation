using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Response structure for queryimg response
/// </summary>
[Serializable]
public class ResponseStruct {
    public RecognisedObject[] recognizedObjects;
}

/// <summary>
/// Details of the recognised object
/// </summary>
[Serializable]
public class RecognisedObject
{
    public string type;
    public float[] details;
    public float score;
}

/// <summary>
/// Settings data to configure a run
/// </summary>
[Serializable]
public class SettingsJSON
{
    public Boolean debugActive;
    public float refreshPeriod;
    public int camResolution;
    public Boolean autoMode;
}