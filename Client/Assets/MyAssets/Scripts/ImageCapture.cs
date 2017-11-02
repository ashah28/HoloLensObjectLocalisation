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
    
    [SerializeField] RawImage previewImage;
    [SerializeField] Renderer quadRendererCustom;
    [SerializeField] string serverAddress;
    [SerializeField] string queryAPI;

    [SerializeField] Vector3 lastLocation = Vector3.zero;
    [SerializeField] Vector3 lastRotation = Vector3.zero;

    /// <summary>
    /// Activate camera on app activation
    /// </summary>
    void OnEnable()
    {
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
            });
        }

        StartCoroutine(CheckServerStatus());

        StartCoroutine(ParseSampleResponse());

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
        lastLocation = Camera.main.transform.position;
        lastRotation = Camera.main.transform.rotation.eulerAngles;
        DebugManager.Instance.PrintToRunningLog("Capture at: R:" + lastRotation + " P:" + lastLocation);
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
            StartCoroutine(SendImageToServer(imageData));

            Matrix4x4 cameraToWorldMatrix;
            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);

            Vector3 position = cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
            // Position the canvas object slightly in front
            // of the real world web camera.
            //Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
            Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

            DebugManager.Instance.PrintToRunningLog("R:" + rotation.eulerAngles + " P " + position );
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
        DebugManager.Instance.PrintToInfoLog("Saved: " + filePath);
    }

    /// <summary>
    /// Sends image to server as data bytes with jpeg mime
    /// </summary>
    /// <param name="imageData"></param>
    /// <returns></returns>
    IEnumerator SendImageToServer(byte[] imageData)
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

        GetComponent<ObjectLocator>().LocateInScene(resp, lastLocation, lastRotation);

        DebugManager.Instance.PrintToInfoLog("Server-> " + (www.error == null ? www.text : " ERR :" + www.error ));
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
    }

    /// <summary>
    /// Parse a static sample response for the sake of faster testing
    /// </summary>
    /// <returns></returns>
    IEnumerator ParseSampleResponse()
    {
        WWW www = new WWW(serverAddress + "/resp");
        yield return www;

        ResponseStruct resp = JsonUtility.FromJson<ResponseStruct>(www.text);
        print("************  " + resp.recognizedObjects.Length);
        GetComponent<ObjectLocator>().LocateInScene(resp, lastLocation, lastRotation);
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