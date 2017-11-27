using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMarker : MonoBehaviour {

    [SerializeField] TextMesh labelMesh;
    public string type;


    /// <summary>
    /// Sets the label type etc. upon init
    /// </summary>
    /// <param name="pos">position</param>
    /// <param name="type">type</param>
    /// <param name="label">text for label</param>
    public void SetLabel(Vector3 pos, string type, string label)
    {
        transform.position = pos;
        this.labelMesh.text = label;
        this.type = type;
    }

    /// <summary>
    /// Rotate label to face camera when being rendered
    /// </summary>
    void OnWillRenderObject()
    {
        labelMesh.transform.rotation = Camera.current.transform.rotation;
    }
}
