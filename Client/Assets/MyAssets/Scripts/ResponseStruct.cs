using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class ResponseStruct {
    public ObjectRecognition[] recognizedObjects;
}

[Serializable]
public class ObjectRecognition
{
    public string type;
    public int[] details;
}