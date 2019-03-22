using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using static OpenCvSharp.Unity;
using Wikitude;

public struct GridBoard
{
    int markersX;
    int markersY;
    float markersLength;
    float markerSeparation;
    Dictionary dictionary;
    int firstMarker;
    public int[] ids;
    public Point3f[][] objPoints;

    public GridBoard(int x, int y, float length, float separation, Dictionary dict)
    {
        Debug.Assert(x > 0 && y > 0 && length > 0 && separation > 0);

        markersX = x;
        markersY = y;
        markersLength = length;
        markerSeparation = separation;
        dictionary = dict;
        firstMarker = 0;

        int totalMarkers = markersX * markersY;
        ids = new int[totalMarkers];
        objPoints = new Point3f[totalMarkers][];

        for (int i = 0; i < totalMarkers; i++){
            ids[i] = i + firstMarker;
        }

        float maxY = (float)markersY * markersLength + (markersY - 1) * markerSeparation;
        int count = 0;
        for (int b = 0; b < markersY; b++){
            for (int a = 0; a < markersX; a++){
                Point3f[] corners = new Point3f[4];
                corners[0] = new Point3f(a * (markersLength + markerSeparation),
                                     maxY - b * (markersLength + markerSeparation), 0);
                corners[1] = corners[0] + new Point3f(markersLength, 0, 0);
                corners[2] = corners[0] + new Point3f(markersLength, -markersLength, 0);
                corners[3] = corners[0] + new Point3f(0, -markersLength, 0);

                objPoints[count] = corners;

                count++;
            }
        }
    }

}

public class ArucoTest : MonoBehaviour {

    //When Using Mac Cam:
    static WebCamTexture backCam;
    public GameObject cam_viewer;
    public GameObject cv_viewer;
    public GameObject test;

    public GameObject camera;
    DetectorParameters parameters = DetectorParameters.Create();
    double[,] cameraMatrix;
    List<double> distCoeffs;
    // These are read from Matlab
    static Dictionary dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.DictArucoOriginal);
    //Make Gridboard Ob =ject
    GridBoard arucoBoard = new GridBoard(6, 6, 0.03f, 0.002f, dictionary);

    public void loadCalibration(){

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

    }


    int estimatePoseBoard(Point2f[][] corners, int[] ids, GridBoard board, double[,] cameraMat, IEnumerable<double> dist, out double[] rvec, out double[] tvec, bool useExtrinsicGuess){

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
     
        Cv2.SolvePnP(objPoints, imgPoints, cameraMat, dist, out rvec, out tvec);
        return (int)objPoints.Count / 4;
    }

    void getBoardObjectAndImagePoints(GridBoard board, Point2f[][] detectedCorners, int[] detectedIds, out List<Point3f> objPoints, out List<Point2f> imgPoints){
        Debug.Assert(board.ids.Length == board.objPoints.Length);
        Debug.Assert(detectedIds.Length == detectedCorners.Length);

        int nDetectedMarkers = detectedIds.Length;

        Point3f[] objPnts = new Point3f[nDetectedMarkers];
        Point2f[] imgPnts = new Point2f[nDetectedMarkers];

        int count = 0;

        for (int i = 0; i < nDetectedMarkers; i ++){
            //Make sure this line is correct...
            int currentId = detectedIds[i];
            for (int j = 0; j < board.ids.Length; j++){
                if(currentId == board.ids[j] && objPnts.Length >= 4){
                    for (int p = 0; p < 4; p++){
                        Debug.Log("HERE IS THE LENGTH:" + objPnts.Length);
                        Debug.Log("HERE IS THE current index:" + count);
                        objPnts[count] = board.objPoints[j][p];
                        imgPnts[count] = detectedCorners[i][p];
                        count++;
                    }
                }
            }
        }
        objPoints = new List<Point3f>(objPnts);
        imgPoints = new List<Point2f>(imgPnts);
    }

    public Quaternion RightHandToLeftHand(Quaternion quat)
    {
        return new Quaternion(quat.x, quat.y, -1 * quat.z, -1 * quat.w);
    }

    // Use this for initialization
    void Start () {
        loadCalibration();
#if UNITY_EDITOR
        if (backCam == null)
            backCam = new WebCamTexture();
        cam_viewer.GetComponent<Renderer>().material.mainTexture = backCam;
        if (!backCam.isPlaying)
            backCam.Play();
#endif
    }

    // Update is called once per frame
    void Update () {
        Texture2D image;
#if UNITY_EDITOR
        image = new Texture2D(backCam.width, backCam.height);
        image.SetPixels(backCam.GetPixels());
#elif UNITY_IOS
        image = camera.GetComponent<WikitudeCamera>().CameraTexture;
#endif       
        Mat img = TextureToMat(image, null);
        Mat imgCopy = new Mat();
        img.CopyTo(imgCopy);
        int[] ids;
        Point2f[][] corners;
        Point2f[][] rejected;
        CvAruco.DetectMarkers(img, dictionary, out corners, out ids, parameters, out rejected);

        if (ids.Length > 0){
#if UNITY_EDITOR
            CvAruco.DrawDetectedMarkers(imgCopy, corners, ids);

            //cv_viewer.GetComponent<Renderer>().material.mainTexture = MatToTexture(imgCopy,null);
#endif     

            double[] rvec = new double[0];
            double[] tvec = new double[0];
            //THE BOOL MIGHT NOT BE TRUE...change to False if necessary
            int valid = estimatePoseBoard(corners, ids, arucoBoard, cameraMatrix, distCoeffs, out rvec, out tvec, false);
            if (valid > 0)
            {
                if (!double.IsNaN(tvec[0]))
                {
                    double[,] current_rotation;
                    Cv2.Rodrigues(rvec, out current_rotation);
                    Matrix4x4 rotation_transform = Matrix4x4.identity;
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            rotation_transform[i, j] = (float)current_rotation[i, j];
                        }
                    }
                    test.transform.rotation = RightHandToLeftHand(rotation_transform.rotation);
                    test.transform.position = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);

                    CvAruco.DrawAxis(imgCopy, cameraMatrix, distCoeffs, rvec, tvec, 1);
                }
            }

        }
        //
        cv_viewer.GetComponent<Renderer>().material.mainTexture = MatToTexture(imgCopy, null);

    }
}
