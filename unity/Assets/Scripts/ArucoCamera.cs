using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using static OpenCvSharp.Unity;


public class ArucoCamera : MonoBehaviour {

    //When Using Mac Cam:
    static WebCamTexture backCam;
    static Texture2D imageTexture, undistortTexture;    
    public Renderer background;
    public Camera bgCamera;
    public GameObject originalImage;

    double[,] cameraMatrix;
    float image_height = 480, image_width = 640;
    Vector2 cameraF;
    Vector2 principalPoint = new Vector2(320.0f, 240.0f);
    float cameraBackgroundDistance = 1f;
    double[] RectifiedCameraMatrices;
    double[] cameraMatrixArray;
    double[] distCoeffsArray;
    Mat cameraMatrixMat, distCoeffsMat, RectifiedCameraMat;
    Size imageSize;
    OpenCvSharp.Rect noROI = new OpenCvSharp.Rect();
    Mat RectificationMat;
    double[] noRectificationMatrix = new double[0];
    Mat UndistortionRectificationMaps1 = new Mat(480, 640, MatType.CV_16SC2),
        UndistortionRectificationMaps2 = new Mat(480, 640, 2);

    // Use this for initialization
    void Start () {
        if (backCam == null)
            backCam = new WebCamTexture();
        imageTexture = new Texture2D((int)image_width, (int)image_height);
        undistortTexture = new Texture2D((int)image_width, (int)image_height);
        originalImage.GetComponent<Renderer>().material.mainTexture = backCam;

        if (!backCam.isPlaying)
            backCam.Play();

        loadCalibration();
        InitializeRectificationAndUndistortionMaps();
        configBackground();
    }
	
	// Update is called once per frame
	void Update () {
        //Mat img = TextureToMat(backCam, null);

        // zhenyi remap
        //UndistortRectifyImages(img);
        //undistortTexture = MatToTexture(img);
        //background.material.mainTexture = undistortTexture;

        //Mat imgCopy = new Mat();
        //img.CopyTo(imgCopy);

        //img.Dispose();
    }

    public void loadCalibration()
    {
        // zhenyi
        cameraMatrix = new double[3, 3];
        cameraMatrix[0, 0] = 540.01955364602134;        cameraMatrix[0, 1] = 0;        cameraMatrix[0, 2] = 294.76282342359644;
        cameraMatrix[1, 0] = 0;        cameraMatrix[1, 1] = 565.86980782912451;        cameraMatrix[1, 2] = 299.38921380782074;
        cameraMatrix[2, 0] = 0;        cameraMatrix[2, 1] = 0;        cameraMatrix[2, 2] = 1;
        distCoeffsArray = new double[5];
        cameraMatrixArray = new double[9];
        for (int i = 0; i < 9; i++) {
            cameraMatrixArray[i] = cameraMatrix[i / 3, i % 3];
        }
        cameraMatrixMat = new Mat(3, 3, MatType.CV_64FC1, cameraMatrixArray);
        distCoeffsMat = new Mat(1, 5, MatType.CV_64FC1, distCoeffsArray);
        //    Debug.Log("cameraMatrix\t"
        //+ cameraMatrixArray[0, 0] + "\t" + cameraMatrixArray[0, 1] + "\t" + cameraMatrixArray[0, 2] + "\t"
        //+ cameraMatrixArray[1, 0] + "\t" + cameraMatrixArray[1, 1] + "\t" + cameraMatrixArray[1, 2] + "\t"
        //+ cameraMatrixArray[2, 0] + "\t" + cameraMatrixArray[2, 1] + "\t" + cameraMatrixArray[2, 2]);
        distCoeffsArray[0] = -1.8902355308452077;
        distCoeffsArray[1] = 26.940687515100784;
        distCoeffsArray[2] = 0.0121538382238777;
        distCoeffsArray[3] = 0.050890060910626686;
        distCoeffsArray[4] = -111.11737159034945;
        imageSize = new Size(image_width, image_height);
        RectifiedCameraMatrices = new double[9];
    }

    void InitializeRectificationAndUndistortionMaps()
    {
        RectifiedCameraMat = Cv2.GetOptimalNewCameraMatrix(cameraMatrixMat, distCoeffsMat, imageSize, 1.0, imageSize, out noROI, true);
        RectificationMat = new Mat();
        cameraF = new Vector2((float)RectifiedCameraMat.At<double>(0, 0), (float)RectifiedCameraMat.At<double>(1, 1));
        print("camera F:" + cameraF);

        Cv2.InitUndistortRectifyMap(cameraMatrixMat, distCoeffsMat,
          RectificationMat, RectifiedCameraMat, imageSize, MatType.CV_16SC2,
          UndistortionRectificationMaps1, UndistortionRectificationMaps2);
    }

    void configBackground()
    {
        //background.material.mainTexture = backCam;
        background.material.mainTexture = undistortTexture;
        float fovY = 2f * Mathf.Atan(0.5f * image_height / cameraF.y) * Mathf.Rad2Deg;
        bgCamera.fieldOfView = fovY;

        float localPositionX = (0.5f * image_width - principalPoint.x) / cameraF.x * cameraBackgroundDistance;
        float localPositionY = -(0.5f * image_height - principalPoint.y) / cameraF.y * cameraBackgroundDistance; // a minus because OpenCV camera coordinates origin is top - left, but bottom-left in Unity

        // Considering https://stackoverflow.com/a/41137160
        // scale.x = 2 * cameraBackgroundDistance * tan(fovx / 2), cameraF.x = imageWidth / (2 * tan(fovx / 2))
        float localScaleX = image_width / cameraF.x * cameraBackgroundDistance;
        float localScaleY = image_height / cameraF.y * cameraBackgroundDistance;

        // Place and scale the background
        background.transform.localPosition = new Vector3(localPositionX, localPositionY, cameraBackgroundDistance);
        background.transform.localScale = new Vector3(localScaleX, localScaleY, 1);
    }

    void UndistortRectifyImages(Mat image)
    {
        //print("UndistortionRectificationMaps1:" + UndistortionRectificationMaps1);
        //print("UndistortionRectificationMaps2:" + UndistortionRectificationMaps2);
        //Mat remapImage = new Mat();
        //image.CopyTo(remapImage);
        //testRemap.GetComponent<Renderer>().material.mainTexture = MatToTexture(image);
        Cv2.Remap(image, image, UndistortionRectificationMaps1,
          UndistortionRectificationMaps2);
    }
}
