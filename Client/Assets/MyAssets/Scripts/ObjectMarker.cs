using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object marker class
/// </summary>
/// <seealso cref="UnityEngine.MonoBehaviour" />
public class ObjectMarker : MonoBehaviour
{

    /// <summary>
    /// Label to show details on
    /// </summary>
    [SerializeField] TextMesh labelMesh;

    /// <summary>
    /// Type of the detected object
    /// </summary>
    public string type;

    /// <summary>
    /// Name used for the label and storage
    /// </summary>
    public string markerName;

    /// <summary>
    /// Holds the value of confidence of the prediction in percent (eg 98.2 out of 100)
    /// </summary>
    public float confScore;

    /// <summary>
    /// Sets the properties... type etc. upon init
    /// </summary>
    /// <param name="pos">position</param>
    /// <param name="type">type</param>
    /// <param name="confScore">The conf score.</param>
    /// <param name="markerName">Name of the marker.</param>
    public void SetProperties(Vector3 pos, string type, float confScore)
    {
        transform.position = pos;
        this.type = type;
        this.confScore = confScore;
        this.markerName = type + ":" + confScore.ToString("00.000") + ":"
            + Random.Range(0, 100000).ToString();
        SetLabel();
    }

    /// <summary>
    /// Sets the label for the marker
    /// </summary>
    void SetLabel()
    {
        this.labelMesh.text = type + ":" + (confScore * 100).ToString("00.0");
    }
    
    /// <summary>
    /// Renames the type of this object
    /// </summary>
    /// <param name="name">The name.</param>
    public void Rename(string name)
    {

        PersistenceManager.Instance.DeleteAnchor(markerName);
        this.type = name;
        this.markerName = type + ":" + confScore.ToString("00.000") + ":"
             + Random.Range(0, 100000).ToString();
        SetLabel();
        PersistenceManager.Instance.AddAnchor(this);
    }

    /// <summary>
    /// Rotate label to face camera when being rendered
    /// </summary>
    void OnWillRenderObject()
    {
        labelMesh.transform.rotation = Camera.current.transform.rotation;
    }
}
