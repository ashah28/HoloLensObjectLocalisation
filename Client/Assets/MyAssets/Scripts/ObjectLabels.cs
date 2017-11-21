using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLabels : MonoBehaviour {

    [SerializeField] TextMesh label;

    public void SetLabel(Vector3 pos, string label)
    {
        transform.position = pos;
        this.label.text = label;
    }
}
