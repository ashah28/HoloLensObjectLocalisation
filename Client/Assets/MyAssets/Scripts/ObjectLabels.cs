using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLabels : MonoBehaviour {

    [SerializeField] TextMesh labelMesh;
    [SerializeField] string type;

    public void SetLabel(Vector3 pos, string type, string label)
    {
        transform.position = pos;
        this.labelMesh.text = label;
        this.type = type;
    }

    void OnWillRenderObject()
    {
        labelMesh.transform.rotation = Camera.current.transform.rotation;
    }
}
