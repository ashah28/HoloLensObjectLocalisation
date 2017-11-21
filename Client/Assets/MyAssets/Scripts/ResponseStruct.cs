using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ResponseStruct {
    public RecognisedObject[] recognizedObjects;
}

[Serializable]
public class RecognisedObject
{
    public string type;
    public float[] details;
    public float score;
}