using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using HoloToolkit.Unity;

public class ObjectLocator : Singleton<ObjectLocator> {

    [SerializeField] RawImage preview;
    [SerializeField] GameObject lastMarkerPlacement;

    [SerializeField] GameObject labelPrefab;
    [SerializeField] Transform markersParent;

    [SerializeField] List<ObjectMarker> markers;

    [SerializeField] int boundaryWidth;

    [SerializeField] Color[] colors;

    public int camResolutionWidth;
    public int camResolutionHeight;

    /// <summary>
    /// A hacky way to debug mark boundaries. Stretch to use setpixel32
    /// </summary>
    /// <param name="xMin"></param>
    /// <param name="xMax"></param>
    /// <param name="yMin"></param>
    /// <param name="yMax"></param>
    /// <param name="score"></param>
    /// <returns></returns>
	public IEnumerator DefineBoundary(string type, int xMin, int xMax, int yMin, int yMax, float score)
    {
        DebugManager.Instance.PrintToInfoLog(type + " " + xMin + " " +  xMax + " " +  yMin + " " +  yMax);
        Texture2D tex = preview.texture as Texture2D;
        tex = Object.Instantiate(tex);

        Color selectionColor = colors[Random.Range(0, colors.Length - 1)];

        for (int i = xMin - boundaryWidth; i < (xMax) + boundaryWidth; i++)
        {
            if (i == xMin + boundaryWidth)
                i = xMax - boundaryWidth;

            //Inconsistent pixel(0,0) positioning! Unity starts from bottom left. CNN start from top left.
            for (int j = tex.height - (yMax + boundaryWidth); j < tex.height - (yMin - boundaryWidth); j++)
            {
                tex.SetPixel(i, j, selectionColor);
                if (j == tex.height -( yMax - boundaryWidth))
                    j = tex.height - (yMin + boundaryWidth);
            }
        }
        
        tex.Apply();
        preview.texture = tex;
        yield return new WaitForEndOfFrame();
    }

    /// <summary>
    /// This is where the actual magic happens. Calculates the 3D direction where the object sits
    /// </summary>
    /// <param name="pixelPos">pixelPosition as given by CNN. Will be converted to Unity compatible</param>
    /// <param name="cameraToWorldMatrix">cameraToWorldMatrix</param>
    /// <param name="projectionMatrix">projectionMatrix</param>
    /// <returns>Returns a nullable vector3 with position of the hit point if a collider found. Returns null if miss</returns>
    public Vector3? PixelToWorldPoint(Vector2 pixelPos, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
    {
        //Pixel positions : Unity starts from bottom left. CNN start from top left.
        pixelPos.y = 1 - pixelPos.y;

        Vector3 camPosition = cameraToWorldMatrix.MultiplyPoint(Vector3.zero);

        Vector3 imagePosProjected = ((pixelPos * 2) - Vector2.one); // -1 to 1 space
        imagePosProjected.z = 1;

        Vector3 cameraSpacePos = UnProjectVector(projectionMatrix, imagePosProjected);
        Vector3 worldSpaceRayPoint2 = cameraToWorldMatrix * cameraSpacePos; // ray point in world space

        //DebugManager.Instance.PrintToRunningLog("point2:" + worldSpaceRayPoint2);

        return RayCastHitPoint(camPosition, worldSpaceRayPoint2);
        //DrawLineRenderer(camPosition, worldSpaceRayPoint2);
    }

    public static Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
    {
        Vector3 from = new Vector3(0, 0, 0);
        var axsX = proj.GetRow(0);
        var axsY = proj.GetRow(1);
        var axsZ = proj.GetRow(2);
        from.z = to.z / axsZ.z;
        from.y = (to.y - (from.z * axsY.z)) / axsY.y;
        from.x = (to.x - (from.z * axsX.z)) / axsX.x;
        return from;
    }

    /// <summary>
    /// Raycast towards the object to find a collider.
    /// </summary>
    /// <param name="origin">from</param>
    /// <param name="direction">direction</param>
    /// <returns>Returns a nullable vector3 with position of the hit point if a collider found. Returns null if miss</returns>
    Vector3? RayCastHitPoint(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit, 100))
        {
            //DebugManager.Instance.PrintToRunningLog("Found at:" + hit.distance + ":" + hit.point);
            lastMarkerPlacement.transform.position = hit.point;
            return hit.point;
        }
        else
        {            
            return null;
        }
    }


    /// <summary>
    /// Attempts to Drops the label and the marker at the given position if no overlaps found
    /// </summary>
    /// <param name="pos">Position</param>
    /// <param name="obj">RecognisedObject</param> 
    /// <returns>The Object Marker object</returns>
    public ObjectMarker AttemptToDropMarker(Vector3 pos, RecognisedObject obj)
    {
        if (IsOverlappingSimilarMarker(pos, obj.type))
        {
            DebugManager.Instance.PrintToRunningLog("Similar marker: " + obj.type);
            return null;
        }
        else
        {
            ObjectMarker marker = CreateMarker();
            marker.SetProperties(pos, obj.type, obj.score);

            markers.Add(marker);
            return marker;
        }
    }

    /// <summary>
    /// Creates a marker game object
    /// </summary>
    /// <returns>The Object Marker object</returns>
    ObjectMarker CreateMarker()
    {
        GameObject go = GameObject.Instantiate(labelPrefab as Object, markersParent) as GameObject;
        ObjectMarker label = go.GetComponent<ObjectMarker>();
        return label;
    }

    /// <summary>
    /// Determines whether [is overlapping similar marker] [the specified position].
    /// </summary>
    /// <param name="pos">The position.</param>
    /// <param name="type">The type.</param>
    /// <returns>
    ///   <c>true</c> if [is overlapping similar marker] [the specified position]; otherwise, <c>false</c>.
    /// </returns>
    bool IsOverlappingSimilarMarker(Vector3 pos, string type)
    {
        Vector3 offset;
        DebugManager.Instance.PrintToRunningLog("Markers in world:" + markers.Count);
        foreach (ObjectMarker t in markers)
        {
            if (t.type == type)
            {
                offset = t.transform.position - pos;
                DebugManager.Instance.PrintToRunningLog(offset.sqrMagnitude.ToString() + type);

                if (offset.sqrMagnitude < 0.1f)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Clears all the markers.
    /// </summary>
    public void ClearMarkers()
    {
        foreach(ObjectMarker om in markers)
        {
            GameObject.Destroy(om.gameObject);
        }
        markers.Clear();
    }

    /// <summary>
    /// Deletes the object.
    /// </summary>
    /// <param name="obj">The object.</param>
    public void DeleteObject(GameObject obj)
    {
        print(obj);
        if (obj == lastMarkerPlacement)
            return;

        DebugManager.Instance.PrintToRunningLog("Clicked on: " + obj.name);
        ObjectMarker marker = obj.GetComponent<ObjectMarker>();
        markers.Remove(marker);

        Destroy(marker.gameObject);
    }

    /// <summary>
    /// Draws the line renderer to help debug.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="objPosition">The object position.</param>
    void DrawLineRenderer(Vector3 from, Vector3 objPosition)
    {
        LineRenderer line = GetComponent<LineRenderer>();
        line.enabled = true;
        line.SetPosition(0, from);
        line.SetPosition(1, objPosition);
    }
}
