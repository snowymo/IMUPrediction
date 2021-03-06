﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using static OpenCvSharp.Unity;
using Wikitude;

public struct GridBoard {
    int markersX;
    int markersY;
    float markersLength;
    float markerSeparation;
    public Dictionary dictionary;
    int firstMarker;
    public int[] ids;
    public Point3f[][] objPoints;
    public List<List<Point3f>> objPtsList;
    public List<Point3f> singleCorners;

    public GridBoard(int x, int y, float length, float separation, Dictionary dict)
    {
        Debug.Assert(x > 0 && y > 0 && length > 0 && separation > 0);

        markersX = x;
        markersY = y;
        markersLength = length;
        markerSeparation = separation;
        dictionary = dict;
        firstMarker = 0;
        objPtsList = new List<List<Point3f>>(0);

        int totalMarkers = markersX * markersY;
        ids = new int[totalMarkers];
        objPoints = new Point3f[totalMarkers][];

        for (int i = 0; i < totalMarkers; i++) {
            ids[i] = i + firstMarker;
        }

        float maxY = (float)markersY * markersLength + (markersY - 1) * markerSeparation;
        int count = 0;
        for (int b = 0; b < markersY; b++) {
            for (int a = 0; a < markersX; a++) {
                Point3f[] corners = new Point3f[4];
                corners[0] = new Point3f(a * (markersLength + markerSeparation),
                                     maxY - b * (markersLength + markerSeparation), 0);
                corners[1] = corners[0] + new Point3f(markersLength, 0, 0);
                corners[2] = corners[0] + new Point3f(markersLength, -markersLength, 0);
                corners[3] = corners[0] + new Point3f(0, -markersLength, 0);

                objPoints[count] = corners;
                objPtsList.Add(new List<Point3f> { corners[0], corners[1], corners[2], corners[3] });

                count++;
            }
        }

        singleCorners = new List<Point3f>();
        singleCorners.Add(new Point3f(0, 0, 0));
        singleCorners.Add(singleCorners[0] + new Point3f(markersLength, 0, 0));
        singleCorners.Add(singleCorners[0] + new Point3f(markersLength, -markersLength, 0));
        singleCorners.Add(singleCorners[0] + new Point3f(0, -markersLength, 0));
    }

}

public class ArucoTest : MonoBehaviour {

    //When Using Mac Cam:
    static WebCamTexture backCam;
    static Texture2D imageTexture, undistortTexture;
    public GameObject cam_viewer;
    //public GameObject cv_viewer;
    public GameObject test;

    //public GameObject camera;
    DetectorParameters parameters = DetectorParameters.Create();
    double[,] cameraMatrix;
    List<double> distCoeffs;
    // These are read from Matlab
    static Dictionary dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.DictArucoOriginal);
    //Make Gridboard Ob =ject
    GridBoard arucoBoard = new GridBoard(6, 6, 0.03f, 0.002f, dictionary);
    int[] random_markers = { 307, 555, 904, 346, 491, 717, 49, 577, 659, 449,
        396, 263, 786, 438, 182, 699,498, 443, 848, 183, 969, 275, 741, 664, 612, 205,
    499, 630, 341, 248, 166, 429, 23, 546, 164, 635};

    // zhenyi
    public Renderer background;
    public Camera bgCamera;
    float image_height = 480, image_width = 640;
    Vector2 cameraF = new Vector2(408.7f, 428.3f);
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
    public GameObject originalImage;

    public void loadCalibration()
    {

#if UNITY_EDITOR
        double[] radialTanDist = { 0.2088f, -0.5676f, 0f, 0f };
        double[,] intrinsicMat = {
            {1075.65317917815f,  0f,   0f},
            {0,  1074.54135558759f,   0f},
            {526.116586278550f,  344.344306651758f,   1f}
        };
#elif UNITY_IOS
        double[] radialTanDist = { 0.1259f, -0.1828f, 0f, 0f };
        double[,] intrinsicMat = {
            {3503.62632009662f,  0f,   0f},
            {0,  3507.23444098677f,   0f},
            {2036.86732960421f,  1503.93779989350f,   1f}
        };
#endif    
        //objPoints = new Mat(1, objPnts.Length, MatType.CV_32F, objPnts);
        cameraMatrix = intrinsicMat;
        distCoeffs = new List<double>(radialTanDist);
        // zhenyi
        cameraMatrix[0, 0] = 540.01955364602134;
        cameraMatrix[0, 1] = 0;
        cameraMatrix[0, 2] = 294.76282342359644;
        cameraMatrix[1, 0] = 0;
        cameraMatrix[1, 1] = 565.86980782912451;
        cameraMatrix[1, 2] = 299.38921380782074;
        cameraMatrix[2, 0] = 0;
        cameraMatrix[2, 1] = 0;
        cameraMatrix[2, 2] = 1;
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
        distCoeffsArray[0] = distCoeffs[0] = -1.8902355308452077;
        distCoeffsArray[1] = distCoeffs[1] = 26.940687515100784;
        distCoeffsArray[2] = distCoeffs[2] = 0.0121538382238777;
        distCoeffsArray[3] = distCoeffs[3] = 0.050890060910626686;
        distCoeffs.Add(-111.11737159034945);
        distCoeffsArray[4] = -111.11737159034945;
        imageSize = new Size(image_width, image_height);
        RectifiedCameraMatrices = new double[9];
    }


    int estimatePoseBoard(Point2f[][] corners, int[] ids, GridBoard board, double[,] cameraMat, IEnumerable<double> dist, out double[] rvec, out double[] tvec, bool useExtrinsicGuess)
    {

        Debug.Assert(corners.Length == ids.Length);
        List<Point3f> objPoints = new List<Point3f>();
        List<Point2f> imgPoints = new List<Point2f>();

        getBoardObjectAndImagePoints(board, corners, ids, out objPoints, out imgPoints);
        Debug.Assert(imgPoints.Count == objPoints.Count);

        if (objPoints.Count == 0) {
            rvec = new double[0];
            tvec = new double[0];
            return 0;
        }

        //Debug Items
        /*
        Debug.Log("Corners:");
        Debug.Log("IDS:");
        Debug.Log(string.Join(", ", ids));
        Debug.Log("GridBoard:");*/
        /*
        Debug.Log("objPoints:");
        Debug.Log(string.Join(", ", objPoints));
        Debug.Log("imgPoints:");
        Debug.Log(string.Join(", ", imgPoints));*/
        /*
        Debug.Log("Camera Matrix");
        for (int i = 0; i < 3; i++)
        {   string current_line = "";

            for (int j = 0; j < 3; j++)
            {
                current_line += string.Format("{0} ", cameraMat[i, j]);
            }
            Debug.Log(current_line);
        }
        Debug.Log("Distortion Coeffs:");
        Debug.Log(string.Join(", ", dist));*/


        Cv2.SolvePnP(objPoints, imgPoints, cameraMat, dist, out rvec, out tvec);
        //Cv2.SolvePnPRansac(objPoints, imgPoints, cameraMat, dist, out rvec, out tvec);
        return (int)objPoints.Count / 4;
    }

    void getBoardObjectAndImagePoints(GridBoard board, Point2f[][] detectedCorners, int[] detectedIds, out List<Point3f> objPoints, out List<Point2f> imgPoints)
    {
        Debug.Assert(board.ids.Length == board.objPoints.Length);
        Debug.Assert(detectedIds.Length == detectedCorners.Length);

        int nDetectedMarkers = detectedIds.Length;

        Point3f[] objPnts = new Point3f[nDetectedMarkers];
        Point2f[] imgPnts = new Point2f[nDetectedMarkers];

        int count = 0;
        //Debug.Log("Detected Markers###:" + nDetectedMarkers);
        //Debug.Log("Board Ids Length###:" + board.ids.Length);
        for (int i = 0; i < nDetectedMarkers; i++) {
            //Make sure this line is correct...
            int currentId = detectedIds[i];
            for (int j = 0; j < board.ids.Length; j++) {
                //Debug.Log("Current ID:" + currentId + " vs. board id[j]:" + board.ids[j]);
                if (currentId == board.ids[j] && objPnts.Length >= 4) {
                    for (int p = 0; p < 4; p++) {
                        if (count < objPnts.Length) {
                            //Debug.Log("COUNT IN  LOOP:" + count);
                            //Debug.Log("Length of objPnts:" + objPnts.Length);
                            objPnts[count] = board.objPoints[j][p];
                            imgPnts[count] = detectedCorners[i][p];
                            count++;
                        }
                    }
                }
            }
        }
        //Debug.Log("COUNT###:" + count);
        objPoints = new List<Point3f>(objPnts);
        imgPoints = new List<Point2f>(imgPnts);
    }

    void refineDetectedMarkers(Mat image, GridBoard board, Point2f[][] detectedCorners, int[] detectedIds,
                               Point2f[][] rejected, double[,] cameraMatrix, List<double> distCoeffs,
                               float minRepDistance, float errorCorrectionRate, bool checkAllOrders,
                               out List<int> recoveredIdxs, DetectorParameters parameters)
    {

        Debug.Assert(minRepDistance > 0);
        recoveredIdxs = new List<int>();
        if (detectedIds.Length == 0 || rejected.Length == 0) return;


        List<List<Point2f>> undetectedMarkersCorners;
        List<int> undetectedMarkersIds;

        //Camera Matrix will always be defined I think
        projectUndetectedMarkers(board, detectedCorners, detectedIds, cameraMatrix, distCoeffs, out undetectedMarkersCorners, out undetectedMarkersIds);

        bool[] alreadyIdentified = new bool[rejected.Length * rejected[0].Length];

        Dictionary dictionary = board.dictionary;
        int maxCorrectionRecalculated = (int)((double)(dictionary.MaxCorrectionBits) * errorCorrectionRate);

        Mat grey = new Mat();
        Cv2.CvtColor(image, grey, ColorConversionCodes.BGR2GRAY);

        //image.ConvertTo(grey, MatType.)
        List<Point2f[]> finalAcceptedCorners = new List<Point2f[]>();
        List<int> finalAcceptedIds = new List<int>();

        for (int i = 0; i < detectedIds.Length; i++) {
            finalAcceptedCorners.Add(detectedCorners[i]);
            finalAcceptedIds.Add(detectedIds[i]);
        }

        for (int i = 0; i < undetectedMarkersIds.Count; i++) {
            int closestCandidateIdx = -1;
            double closestCandidateDistance = minRepDistance * minRepDistance + 1;
            Point2f[] closestRotatedMarker = new Point2f[0];

            for (int j = 0; j < (rejected.Length - 1 * rejected[0].Length - 1); j++) {
                if (alreadyIdentified[j]) continue;

                double minDistance = closestCandidateDistance + 1;
                bool valid = false;
                int validRot = 0;
                for (int c = 0; c < 4; c++) {
                    double currentMaxDistance = 0;
                    for (int k = 0; k < 4; k++) {
                        Point2f rejCorner = rejected[j][(c + k) % 4];
                        Point2f distVector = distVector = undetectedMarkersCorners[i][k] - rejCorner;
                        double cornerDist = distVector.X * distVector.X + distVector.Y * distVector.Y;
                        currentMaxDistance = Mathf.Max((float)currentMaxDistance, (float)cornerDist);
                    }

                    if (currentMaxDistance < closestCandidateDistance) {
                        valid = true;
                        validRot = c;
                        minDistance = currentMaxDistance;
                    }
                    if (!checkAllOrders) break;
                }
                if (!valid) continue;

                Point2f[] rotatedMarker;
                if (checkAllOrders) {
                    rotatedMarker = new Point2f[4];
                    for (int c = 0; c < 4; c++)
                        rotatedMarker[c] = rejected[j][(c + 4 + validRot) % 4];

                }
                else rotatedMarker = rejected[j];

                int codeDistance = 0;

                if (errorCorrectionRate >= 0) {
                    //TODO HERE
                }

                if (errorCorrectionRate < 0 || codeDistance < maxCorrectionRecalculated) {
                    closestCandidateIdx = j;
                    closestCandidateDistance = minDistance;
                    closestRotatedMarker = rotatedMarker;
                }
            }

            if (closestCandidateIdx >= 0) {

                //remove from rejected
                alreadyIdentified[closestCandidateIdx] = true;

                //add to detected

                finalAcceptedCorners.Add(closestRotatedMarker);
                finalAcceptedIds.Add(undetectedMarkersIds[i]);
                recoveredIdxs.Add(closestCandidateIdx);

            }
        }

        grey.Dispose();
        //parse output :(
        if (finalAcceptedIds.Count != detectedIds.Length) {
            //MAYBE I WILL NEED TO CLEAR DETECTED CORNERS AND IDS....
            finalAcceptedIds.CopyTo(detectedIds);
            for (int i = 0; i < finalAcceptedCorners.Count; i++) {
                for (int j = 0; j < 4; j++) {
                    detectedCorners[i][j] = finalAcceptedCorners[i][j];
                }
            }

            List<Point2f[]> finalRejected = new List<Point2f[]>();
            for (int i = 0; i < alreadyIdentified.Length; i++) {
                if (!alreadyIdentified[i]) {
                    finalRejected.Add(rejected[i]);
                }
            }

            for (int i = 0; i < finalRejected.Count; i++) {
                for (int j = 0; j < 4; j++) {
                    rejected[i][j] = finalRejected[i][j];
                }
            }
            //RECOVERED IDS?
        }
    }

    void projectUndetectedMarkers(GridBoard board, Point2f[][] detectedCorners, int[] detectedIds,
                                  double[,] cameraMatrix, List<double> distCoeffs,
                                  out List<List<Point2f>> undetectedMarkersProjectedCorners, out List<int> undetectedMarkersIds)
    {
        double[] rvec = new double[0];
        double[] tvec = new double[0];
        int boardDetectedMarkers;
        boardDetectedMarkers = estimatePoseBoard(detectedCorners, detectedIds, board, cameraMatrix, distCoeffs, out rvec, out tvec, false);

        if (boardDetectedMarkers == 0) {
            undetectedMarkersIds = new List<int>();
            undetectedMarkersProjectedCorners = new List<List<Point2f>>();
            return;
        }

        List<List<Point2f>> undetectedCorners = new List<List<Point2f>>();
        List<int> undetectedIds = new List<int>();

        for (int i = 0; i < board.ids.Length; i++) {
            int foundIdx = -1;
            for (int j = 0; j < detectedIds.Length; j++) {
                if (board.ids[i] == detectedIds[j]) {
                    foundIdx = j;
                    break;
                }
            }
            if (foundIdx == -1) {
                undetectedCorners.Add(new List<Point2f>());
                undetectedIds.Add(board.ids[i]);
                double[,] jacobian = new double[0, 0];
                Point2f[] back;
                Cv2.ProjectPoints(board.objPtsList[i], rvec, tvec, cameraMatrix, distCoeffs.ToArray(), out back, out jacobian);

                undetectedCorners[undetectedCorners.Count - 1].Add(back[0]);
                undetectedCorners[undetectedCorners.Count - 1].Add(back[1]);
                undetectedCorners[undetectedCorners.Count - 1].Add(back[2]);
                undetectedCorners[undetectedCorners.Count - 1].Add(back[3]);
            }

        }


        undetectedMarkersIds = undetectedIds;
        undetectedMarkersProjectedCorners = undetectedCorners;
    }

    public Quaternion RightHandToLeftHand(Quaternion quat)
    {
        return new Quaternion(quat.x, quat.y, -1 * quat.z, -1 * quat.w);
    }


    public Quaternion RvecToQuat(double[] rvec)
    {
        float rnorm = new Vector3((float)rvec[0], (float)rvec[1], (float)rvec[2]).magnitude;
        Quaternion relRot = Quaternion.AngleAxis(rnorm * 180f / Mathf.PI,
            new Vector3((float)rvec[0] / rnorm, (float)rvec[1] / rnorm, (float)rvec[2] / rnorm));

        return relRot;
    }

    // Use this for initialization
    void Start()
    {
        loadCalibration();
        //#if UNITY_EDITOR
        if (backCam == null)
            backCam = new WebCamTexture();
        imageTexture = new Texture2D(640, 480);
        undistortTexture = new Texture2D((int)image_width, (int)image_height);
        cam_viewer.GetComponent<Renderer>().material.mainTexture = undistortTexture;// backCam;
        //originalImage.GetComponent<Renderer>().material.mainTexture = imageTexture;
        originalImage.GetComponent<Renderer>().material.mainTexture = backCam;

        if (!backCam.isPlaying)
            backCam.Play();
        // zhenyi
        InitializeRectificationAndUndistortionMaps();
        // config background
        configBackground();
        //background.material.mainTexture = backCam;
        //#endif
    }

    // hehe 
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

    //ConfigureRectifiedCamera
    void configBackground()
    {
        //background.material.mainTexture = backCam;
        background.material.mainTexture = undistortTexture;
        float fovY = 2f * Mathf.Atan(0.5f * image_height / cameraF.y) * Mathf.Rad2Deg;
        //mainCamera.fieldOfView = fovY;
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

    // Update is called once per frame
    void Update()
    {

        //Texture2D image;
        //image = new Texture2D(backCam.width, backCam.height);
        //image.SetPixels(backCam.GetPixels());

        Mat img = TextureToMat(backCam, null);

        // zhenyi remap
        UndistortRectifyImages(img);
        undistortTexture = MatToTexture(img);
        background.material.mainTexture = undistortTexture;

        Mat imgCopy = new Mat();
        img.CopyTo(imgCopy);
        
        int[] ids;
        Point2f[][] corners;
        Point2f[][] rejected;
        CvAruco.DetectMarkers(img, dictionary, out corners, out ids, parameters, out rejected);
        List<int> recovered;

        refineDetectedMarkers(img, arucoBoard, corners, ids, rejected, cameraMatrix, distCoeffs, 10.0f, 3.0f, true, out recovered, parameters);


        if (ids.Length > 0) {

            CvAruco.DrawDetectedMarkers(imgCopy, corners, ids);
            double[] rvec = new double[0];
            double[] tvec = new double[0];
            //THE BOOL MIGHT NOT BE TRUE...change to False if necessary
            int valid = estimatePoseBoard(corners, ids, arucoBoard, cameraMatrix, distCoeffs, out rvec, out tvec, false);
            if (valid > 0) {
                if (!double.IsNaN(tvec[0])) {

                    //test.transform.rotation = RvecToQuat(rvec);
                    test.transform.rotation = Quaternion.Inverse(RvecToQuat(rvec));


#if UNITY_EDITOR
                    //test.transform.position = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
                    test.transform.position = -1 * new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
#elif UNITY_IOS
     //test.transform.position = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
                    test.transform.position = -1 * new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
#endif


                }
            }

        }


        //cv_viewer.GetComponent<Renderer>().material.mainTexture = image;//MatToTexture(imgCopy, null);

        img.Dispose();
        imgCopy.Dispose();

    }
}
