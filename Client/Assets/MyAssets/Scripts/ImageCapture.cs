using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.VR.WSA.WebCam;
using System;
using UnityEngine.UI;
using System.IO;

using HoloToolkit.Unity;

/// <summary>
/// This class handles image capturing and sending the captured image to the web server as a post form call
/// </summary>
public class ImageCapture :  Singleton<ImageCapture>
{
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;

    [SerializeField] RawImage previewImage;
    
    Boolean capturingImages;
    Boolean lastResponseRecieved = true;

    /// <summary>
    /// Activate camera on app activation
    /// </summary>
    void OnEnable()
    {
        if (!Application.isEditor)
        {
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).Last();
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

                ObjectLocator.Instance.camResolutionWidth = cameraResolution.width;
                ObjectLocator.Instance.camResolutionHeight = cameraResolution.height;
            });
        }
        else
        {
            ObjectLocator.Instance.camResolutionWidth = previewImage.texture.width;
            ObjectLocator.Instance.camResolutionHeight = previewImage.texture.height;
            StartCoroutine(ParseSampleResponse());
        }
    }

    /// <summary>
    /// Release camera on sleep
    /// </summary>
    private void OnDisable()
    {
        if(photoCaptureObject != null)
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);        

        if (DebugManager.Instance)
            DebugManager.Instance.PrintToRunningLog("Cam disabled");
    }
    
    public void StartStopCapturing()
    {
        capturingImages = !capturingImages;

        if (capturingImages)
            InvokeRepeating("CaptureImage", 0, AppManager.Instance.scanPeriod);
        else
            CancelInvoke("CaptureImage");
        DebugManager.Instance.PrintToRunningLog("Capturing images:" + capturingImages 
            + "@" + AppManager.Instance.scanPeriod);
    }
    
    /// <summary>
    /// This function triggers the camera
    /// </summary>
    public void CaptureImage()
    {
        if (!Application.isEditor)
            if (lastResponseRecieved)
            {
                DebugManager.Instance.PrintToInfoLog("Proc started.");
                lastResponseRecieved = false;
                try
                {
                    photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                }
                catch (Exception e)
                {
                    DebugManager.Instance.PrintToInfoLog("Error during TakePhotoAsync:" + e.ToString());
                }
            }
            else
                DebugManager.Instance.PrintToRunningLog("Skipping update...");
        else
            print("Fake capture in editor");
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

        previewImage.texture = targetTexture;

        try
        {
            byte[] imageData = targetTexture.EncodeToJPG(90);
            //WriteImageToDisk(imageData);

            Matrix4x4 cameraToWorldMatrix;
            Matrix4x4 projectionMatrix;

            photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
            photoCaptureFrame.TryGetProjectionMatrix(0, 5, out projectionMatrix);

            StartCoroutine(SendImageToServer(imageData, cameraToWorldMatrix, projectionMatrix));
        }
        catch (Exception e)
        {
            DebugManager.Instance.PrintToInfoLog("Error in OnCapturedPhotoToMemory:" + e.ToString());
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
        WWW www = new WWW(AppManager.Instance.serverAddress + AppManager.Instance.queryAPI, form);
        //while(www.uploadProgress != 1)
        //{            
        //    DebugManager.Instance.PrintToRunningLog("Upload %:" + (www.uploadProgress * 100).ToString("00.00"));
        //    yield return new WaitForSeconds(0.5f);
        //}
        //DebugManager.Instance.PrintToRunningLog("Upload complete");
        yield return www;

        if (www.error != null)
        {
            DebugManager.Instance.PrintToInfoLog("Server-> " + www.error);
            yield break;
        }

        imageData = null;
        ResponseStruct resp = JsonUtility.FromJson<ResponseStruct>(www.text);

        ParseResponse(resp, cameraToWorldMatrix, projectionMatrix);

        lastResponseRecieved = true;
        DebugManager.Instance.PrintToInfoLog("Last response analysed.");
    }

    void ParseResponse(ResponseStruct resp, Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix)
    {
        foreach (RecognisedObject obj in resp.recognizedObjects)
        {
            Vector3? hitPoint = ObjectLocator.Instance.PixelToWorldPoint(new Vector2(obj.details[0] + (obj.details[2] - obj.details[0]) / 2,
                                            obj.details[1] + (obj.details[3] - obj.details[1]) / 2),
                                    cameraToWorldMatrix, projectionMatrix);

            if (hitPoint.HasValue)
            {
                ObjectMarker marker = ObjectLocator.Instance.AttemptToDropMarker(hitPoint.Value, obj);               
            }
            else
                DebugManager.Instance.PrintToRunningLog("No boundary found");
        }
    }

    /// <summary>
    /// Parse a static sample response for the sake of faster testing
    /// </summary>
    /// <returns></returns>
    IEnumerator ParseSampleResponse()
    {
        WWW www = new WWW(AppManager.Instance.serverAddress + "/sample_resp");
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

        RecognisedObject obj = resp.recognizedObjects[0];
        Vector3? hitPoint = ObjectLocator.Instance.PixelToWorldPoint(new Vector2(obj.details[0] + (obj.details[2] - obj.details[0]) / 2,
                                            obj.details[1] + (obj.details[3] - obj.details[1]) / 2), lastCameraToWorldMatrix, lastProjectionMatrix);

        if (hitPoint.HasValue)
            ObjectLocator.Instance.AttemptToDropMarker(new Vector3(0, 0.5f, 5), resp.recognizedObjects[0]);
        else
            DebugManager.Instance.PrintToRunningLog("No boundary found");
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