using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.VR.WSA.WebCam;
using System;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// This class handles image capturing and sending the captured image to the web server as a post form call
/// </summary>
public class ImageCapture : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;
    ObjectLocator objLocatorScript;

    [SerializeField] RawImage previewImage;
    [SerializeField] Renderer quadRendererCustom;
    [SerializeField] string serverAddress;
    [SerializeField] string queryAPI;
    
    /// <summary>
    /// Activate camera on app activation
    /// </summary>
    void OnEnable()
    {
        objLocatorScript = GetComponent<ObjectLocator>();
        StartCoroutine(CheckServerStatus());

        if (!Application.isEditor)
        {
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

            // Create a PhotoCapture object
            PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
            {
                photoCaptureObject = captureObject;
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                // Activate the camera
                photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
                {
                    DebugManager.Instance.PrintToRunningLog("Cam enabled @ " + cameraResolution.width + " X " + cameraResolution.height);
                    photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                });

                objLocatorScript.camResolutionWidth = cameraResolution.width;
                objLocatorScript.camResolutionHeight = cameraResolution.height;
            });
        }
        else
        {
            objLocatorScript.camResolutionWidth = previewImage.texture.width;
            objLocatorScript.camResolutionHeight = previewImage.texture.height;
            StartCoroutine(ParseSampleResponse());
        }
        HoloToolkit.Unity.InputModule.InputManager.holoClickDelegate += OnInputClicked;
    }

    /// <summary>
    /// Release camera on sleep
    /// </summary>
    private void OnDisable()
    {
        if(photoCaptureObject != null)
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

        HoloToolkit.Unity.InputModule.InputManager.holoClickDelegate -= OnInputClicked;

        if (DebugManager.Instance)
            DebugManager.Instance.PrintToRunningLog("Cam disabled");
    }

    /// <summary>
    /// Called whenever a click is registered on Hololens. Blocked by nothing! Pure, simple, holo click...
    /// </summary>
    public void OnInputClicked()
    {
        CaptureImage();
    }
    
    /// <summary>
    /// This function triggers the camera
    /// </summary>
    public void CaptureImage()
    {
        if(!Application.isEditor)
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    /// <summary>
    /// On image capture
    /// </summary>
    /// <param name="result"></param>
    /// <param name="photoCaptureFrame"></param>
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into the target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        //Set texture to renderer. Probably need to do just once... WIP
        quadRendererCustom.material.mainTexture = targetTexture;
        previewImage.texture = targetTexture;

        try
        {
            byte[] imageData = targetTexture.EncodeToJPG(90);
            WriteImageToDisk(imageData);

            Matrix4x4 cameraToWorldMatrix;
            Matrix4x4 projectionMatrix;

            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            photoCaptureFrame.TryGetProjectionMatrix(0, 5, out projectionMatrix);

            StartCoroutine(SendImageToServer(imageData, cameraToWorldMatrix, projectionMatrix));
        }
        catch (Exception e)
        {
            DebugManager.Instance.PrintToInfoLog("Writing to disk failed:" + e.ToString());
        }
    }

    /// <summary>
    /// Writes Image To Disk
    /// </summary>
    /// <param name="imageData"></param>
    void WriteImageToDisk(byte[] imageData)
    {
        String currentFileName = GenerateFileName();
        String filePath = Application.persistentDataPath + "/" + currentFileName;
        File.WriteAllBytes(filePath + ".jpg", imageData);
    }

    /// <summary>
    /// Sends image to server as data bytes with jpeg mime
    /// </summary>
    /// <param name="imageData"></param>
    /// <returns></returns>
    IEnumerator SendImageToServer(byte[] imageData, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, GenerateFileName() + ".jpg", "image/jpeg");
        DebugManager.Instance.PrintToInfoLog("Sending request of " + (imageData.Length / 1024) + "kBs");
        WWW www = new WWW(serverAddress + queryAPI, form);
        while(www.uploadProgress != 1)
        {            
            DebugManager.Instance.PrintToRunningLog("Upload %:" + (www.uploadProgress * 100).ToString("00.00"));
            yield return new WaitForSeconds(1.5f);
        }
        DebugManager.Instance.PrintToRunningLog("Upload complete");
        yield return www;

        print(www.text);

        ResponseStruct resp = JsonUtility.FromJson<ResponseStruct>(www.text);

		objLocatorScript.LocateInScene(resp, cameraToWorldMatrix, projectionMatrix);

        if (www.error != null)
        {
            DebugManager.Instance.PrintToInfoLog("Server-> " + www.error);
        }
    }

    /// <summary>
    /// Confirms if server is accessibke
    /// </summary>
    /// <returns></returns>
    IEnumerator CheckServerStatus()
    {
        WWW www = new WWW(serverAddress);
        yield return www;
        DebugManager.Instance.PrintToInfoLog("Server Status-> " + (www.error == null ? www.text : " ERR :" + www.error));

        DebugManager.Instance.PrintToRunningLog("Screen W:" + Screen.width + " H:" + Screen.height);
    }

    /// <summary>
    /// Parse a static sample response for the sake of faster testing
    /// </summary>
    /// <returns></returns>
    IEnumerator ParseSampleResponse()
    {
        WWW www = new WWW(serverAddress + "/resp");
        yield return www;

        if(www.error != null)
        {
            print(www.error);
            yield break;
        }

        Matrix4x4 lastCameraToWorldMatrix = Matrix4x4.identity;
        Matrix4x4 lastProjectionMatrix = Matrix4x4.identity;
        ResponseStruct resp = JsonUtility.FromJson<ResponseStruct>(www.text);
        print("Response Length: " + resp.recognizedObjects.Length);
		objLocatorScript.LocateInScene(resp, lastCameraToWorldMatrix, lastProjectionMatrix);
        objLocatorScript.DropMarker(new Vector3(0, 0.5f, 5), "sample");
    }

    /// <summary>
    /// Generates file name from current data and time
    /// </summary>
    /// <returns></returns>
    string GenerateFileName()
    {
        return "Capture_" + " " + String.Format("{0:MM-dd,hh-mm-ss}", System.DateTime.Now);
    }


    /// <summary>
    /// Releases camera
    /// </summary>
    /// <param name="result"></param>
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}