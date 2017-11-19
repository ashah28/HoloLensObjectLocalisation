using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectLocator : MonoBehaviour {

    //[SerializeField] Camera secCam;
    [SerializeField] RawImage preview;
    [SerializeField] GameObject marker;

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

	public void LocateInScene(ResponseStruct resp, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
    {
        //Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

        //secCam.transform.position = position;
        //secCam.transform.rotation = rotation;
        //secCam.projectionMatrix = projectionMatrix;

        foreach (ObjectRecognition o in resp.recognizedObjects)
        {
            StartCoroutine(DefineBoundary(o.type, (int) (o.details[0] * camResolutionWidth), (int)(o.details[2] * camResolutionWidth),
                (int)(o.details[1] * camResolutionHeight), (int)(o.details[3] * camResolutionHeight), o.score));
            PixelToWorldPoint(new Vector2(o.details[0] + (o.details[2] - o.details[0]) / 2, 
                                        o.details[1] + (o.details[3] - o.details[1]) / 2),
                                cameraToWorldMatrix, projectionMatrix);
        }
    }

    void PixelToWorldPoint(Vector2 pixelPos, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
    {
        //Pixel positions : Unity starts from bottom left. CNN start from top left.
        pixelPos.y = 1 - pixelPos.y;

        Vector3 camPosition = cameraToWorldMatrix.MultiplyPoint(Vector3.zero);

        Vector3 imagePosProjected = ((pixelPos * 2) - Vector2.one); // -1 to 1 space
        imagePosProjected.z = 1;
        DebugManager.Instance.PrintToRunningLog("-1 to 1:" + imagePosProjected);
        Vector3 cameraSpacePos = UnProjectVector(projectionMatrix, imagePosProjected);

        //worldSpaceMultiplier
        //Vector3 WorldSpaceRayPoint1 = cameraToWorldMatrix * new Vector4(0, 0, 0, 1); // camera location in world space
        Vector3 worldSpaceRayPoint2 = cameraToWorldMatrix * cameraSpacePos; // ray point in world space

        DebugManager.Instance.PrintToRunningLog("point2:" + worldSpaceRayPoint2);

        DropMarker(camPosition, (worldSpaceRayPoint2));
        DrawLineRenderer(camPosition, (worldSpaceRayPoint2) * 10);
        DebugManager.Instance.PrintToRunningLog("world pos" + ((worldSpaceRayPoint2).normalized * 10));
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

    //public void DropMarker(float x, float y, string type, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
    //{
    //    //Pixel positions : Unity starts from bottom left. CNN start from top left.
    //    y = camResolutionWidth - y;

    //    //a camera ray cast can be done and scaled linearly to find the best scaling factor
        

    //    //Vector3 poiPoint = new Vector3(x, y, 10); // point2D is a 2D vector in the RGB camera space;
    //    //Matrix4x4 inverseMVP = (projectionMatrix * worldToCameraMatrix).inverse; // the projectionMatrix and worldToCameraMatrix are from the photoCapture information
    //    //Vector2 poiPointInWorld = inverseMVP.MultiplyPoint3x4(poiPoint);


    //    Vector3 objPosition = Camera.main.ScreenToWorldPoint(new Vector3(x, y ));
    //    print(objPosition);

    //    DrawLineRenderer(objPosition);

    //    DebugManager.Instance.PrintToRunningLog("Pos:" + x + ", " + y + "\n" + objPosition);
    //}

    void DropMarker(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit, 100))
        {
            DebugManager.Instance.PrintToRunningLog("Found at:" + hit.distance + ":" + hit.point);
            marker.transform.position = hit.point;
        }
        else
            DebugManager.Instance.PrintToRunningLog("No boundary");
    }

    void DrawLineRenderer(Vector3 from, Vector3 objPosition)
    {
        LineRenderer line = GetComponent<LineRenderer>();
        line.SetPosition(0, from + new Vector3(0, 0.001f));
        line.SetPosition(1, objPosition);
    }
}
