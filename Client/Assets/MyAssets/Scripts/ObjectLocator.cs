using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectLocator : MonoBehaviour {

    [SerializeField] RawImage preview;

    [SerializeField] int boundaryWidth;

    [SerializeField] Color[] colors;

    /// <summary>
    /// A hacky way to debug mark boundaries. Stretch to use setpixel32
    /// </summary>
    /// <param name="xMin"></param>
    /// <param name="xMax"></param>
    /// <param name="yMin"></param>
    /// <param name="yMax"></param>
    /// <param name="score"></param>
    /// <returns></returns>
	public IEnumerator DefineBoundary(int xMin, int xMax, int yMin, int yMax, int score)
    {
        print( xMin + " " +  xMax + " " +  yMin + " " +  yMax);
        Texture2D tex = preview.texture as Texture2D;
        tex = Object.Instantiate(tex);

        Color selectionColor = colors[Random.Range(0, colors.Length - 1)];

        for (int i = xMin - boundaryWidth; i < (xMax) + boundaryWidth; i++)
        {
            if (i == xMin + boundaryWidth)
                i = xMax - boundaryWidth;

            //Inverted pixel(0,0) positioning! Unity starts from bottom left. CNN start from top left.
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

    public void LocateInScene(ResponseStruct resp, Vector3 lastPosition, Vector3 lastRotation)
    {
        foreach (ObjectRecognition o in resp.recognizedObjects)
        {
            StartCoroutine(DefineBoundary(o.details[0], o.details[2], o.details[1], o.details[3], o.details[4]));
        }
    }
}
