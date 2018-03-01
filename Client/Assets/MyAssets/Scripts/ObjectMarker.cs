using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMarker : MonoBehaviour {

    /// <summary>
    /// Label to show details on
    /// </summary>
    [SerializeField] TextMesh labelMesh;

    /// <summary>
    /// Type of the detected object
    /// </summary>
    public string type;

    /// <summary>
    /// Holds the value of confidence of the prediction in percent (eg 98.2 out of 100)
    /// </summary>
    public float confScore;

    /// <summary>
    /// Sets the properties... type etc. upon init
    /// </summary>
    /// <param name="pos">position</param>
    /// <param name="type">type</param>
    /// <param name="label">text for label</param>
    public void SetProperties(Vector3 pos, string type, float confScore)
    {
        transform.position = pos;
        this.type = type;
        this.confScore = confScore;
        SetLabel();
    }

    /// <summary>
    /// Sets the label for the marker
    /// </summary>
    void SetLabel()
    {
        this.labelMesh.text = type + ":" + confScore.ToString("00.0");
    }

    /// <summary>
    /// Rotate label to face camera when being rendered
    /// </summary>
    void OnWillRenderObject()
    {
        labelMesh.transform.rotation = Camera.current.transform.rotation;
    }
}
