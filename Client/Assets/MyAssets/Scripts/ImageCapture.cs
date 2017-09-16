using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.VR.WSA.WebCam;
using System;
using System.IO;

public class ImageCapture : MonoBehaviour
{
    PhotoCapture photoCaptureObject = null;
    Texture2D targetTexture = null;

    [SerializeField] Renderer quadRendererCustom;

    // Use this for initialization
    void OnEnable()
    {
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

        // Create a PhotoCapture object
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject) {
            photoCaptureObject = captureObject;
            CameraParameters cameraParameters = new CameraParameters();
            cameraParameters.hologramOpacity = 0.0f;
            cameraParameters.cameraResolutionWidth = cameraResolution.width;
            cameraParameters.cameraResolutionHeight = cameraResolution.height;
            cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

            // Activate the camera
            photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result) {
                DebugManager.Instance.PrintToRunningLog("Cam enabled");
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            });
        });
    }

    private void OnDisable()
    {
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        DebugManager.Instance.PrintToRunningLog("Cam disabled");
    }

    public void CaptureImage()
    {
        DebugManager.Instance.PrintToRunningLog("Capture");
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into the target texture
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);

        //Set texture to renderer. Probably need to do just once... WIP
        quadRendererCustom.material.mainTexture = targetTexture;

        try
        {
            byte[] imageData = targetTexture.EncodeToJPG(100);
            WriteImageToDisk(imageData);
        }
        catch (Exception e)
        {
            DebugManager.Instance.PrintToInfoLog("Writing to disk failed:" + e.ToString());
        }
    }

    void WriteImageToDisk(byte[] imageData)
    {
        String currentFileName = "Capture_" + " " + String.Format("{0:MM-dd,hh-mm-ss}", System.DateTime.Now);
        String filePath = Application.persistentDataPath + "/" + currentFileName;
        File.WriteAllBytes(filePath + ".jpg", imageData);
        DebugManager.Instance.PrintToInfoLog("Saved:" + filePath);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }
}