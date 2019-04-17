using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;

public class Marker {
    public int id;
    public Point2f[] corners;//4 corners
    public double[] rvec;
    public double[] tvec;
    public Marker(int i, Point2f[] c, double[] r, double [] t)
    {
        id = i;
        corners = c;
        rvec = r;
        tvec = t;
    }
    public Marker(int i, Point2f[] c)
    {
        id = i;
        corners = c;
        for (int idx = 0; idx < c.Length; idx++) {
           // corners[idx].Y = 480- c[idx].Y;
        }
        
    }
}

public class MarkerDetection : MonoBehaviour {

    public Transform cube;
    public GameObject singleCube;
    GameObject[] singleCubes;

    ArucoCamera webCamera;

    static Dictionary dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.DictArucoOriginal);
    List<Marker> markers = new List<Marker>();

    int[] ids;
    Point2f[][] corners;
    Point2f[][] rejected;
    DetectorParameters parameters = DetectorParameters.Create();

    Mat UndistortedDistCoeffs = new Mat();
    //Make Gridboard Object
    GridBoard arucoBoard = new GridBoard(3, 4, 0.048f, 0.0081f, dictionary);
    Mat grey;
    double[] rvec; double[] tvec;    
    List<int> recovered = new List<int>();

    
    

    Vector3 boardT;
    Quaternion boardR;

    // Use this for initialization
    void Start () {
        webCamera = GameObject.Find("Camera").GetComponent<ArucoCamera>();
        singleCubes = new GameObject[arucoBoard.ids.Length];
        for(int i = 0; i < singleCubes.Length; i++) {
            singleCubes[i] = GameObject.Instantiate(singleCube);
        }

    }
	
	// Update is called once per frame
	void Update () {
		
	}

    void hideCubes()
    {
        for (int i = 0; i < singleCubes.Length; i++) {
            singleCubes[i].SetActive(false);
        }
    }

    public void ProcessFrame(Mat image)
    {
        detect(image);

        for (int i = 0; i < markers.Count; i++)
            estimateSingleMarker(markers[i]);

        estimateBoard();
        //estimateTransforms(image);
        //estimatePoseBoard(corners, ids, arucoBoard, webCamera.cameraMatrixMat, webCamera.distCoeffsMat, out boardRVec, out boardTVec, false);
        //if (boardTVec.Cols > 0) {
        //    boardT = new Vector3((float)boardTVec.At<double>(0), (float)boardTVec.At<double>(1), (float)boardTVec.At<double>(2));
        //    boardR = RvecToQuat(boardRVec);
        //    cube.transform.position = boardT;
        //    cube.transform.rotation = boardR;
        //    cube.gameObject.SetActive(true);
        //}
        //else {
        //    cube.gameObject.SetActive(false);
        //}
        draw(image);
    }

    void detect(Mat image)
    {
        
        CvAruco.DetectMarkers(image, dictionary, out corners, out ids, parameters, out rejected);
        // flip the corners
        for(int i = 0; i < corners.Length; i++) {
            for(int j = 0; j < corners[i].Length; j++) {
                //corners[i][j].Y = webCamera.image_height - corners[i][j].Y;
            }
        }

        markers.Clear();
        for (int i = 0; i < ids.Length; i++) {
            if(ids[i] < 12)
                markers.Add(new Marker(ids[i], corners[i]));
        }

        
        //print("detected: " + ids.Length);
        //refineDetectedMarkers(image, arucoBoard, corners, ids, rejected, webCamera.cameraMatrixMat, webCamera.distCoeffsMat, 10.0f, 3.0f, true, out recovered, parameters);
    }

    public Quaternion RvecToQuat(double[] rvec)
    {
        float rnorm = new Vector3((float)rvec[0], (float)rvec[1], (float)rvec[2]).magnitude;
        Quaternion relRot = Quaternion.AngleAxis(rnorm * 180f / Mathf.PI,
            new Vector3((float)rvec[0] / rnorm, (float)rvec[1] / rnorm, (float)rvec[2] / rnorm));

        return relRot;
    }

    public Quaternion RightHandToLeftHand(Quaternion quat)
    {
        return new Quaternion(quat.x, quat.y, -1 * quat.z, -1 * quat.w);
    }

    void draw(Mat image)
    {
        hideCubes();
        // draw each marker
        CvAruco.DrawDetectedMarkers(image, corners, ids);
        //CvAruco.DrawAxis(image, webCamera.RectifiedCameraMat, webCamera.distCoeffsArray, UndistortedDistCoeffs,
        //MarkerRvecs[cameraId][dictionary].At(i), MarkerTvecs[cameraId][dictionary].At(i), estimatePoseMarkerLength);
        //print("markers " + markers.Count);
        //print("singleCubes " + singleCubes.Length);
        for (int i = 0; i < markers.Count; i++) {
            //for(int j = 0; j < 4; j++) {
                //Cv2.Circle(image, (int)markers[i].corners[j].X, (int)markers[i].corners[j].Y, j + 1, Scalar.Blue);
            //}
            CvAruco.DrawAxis(image, webCamera.cameraMatrix, webCamera.distCoeffsArray, markers[i].rvec, markers[i].tvec, 0.03f);
            singleCubes[i].transform.position = new Vector3((float)markers[i].tvec[0], (float)markers[i].tvec[1], (float)markers[i].tvec[2]);
            singleCubes[i].transform.rotation = RvecToQuat(markers[i].rvec);
            singleCubes[i].SetActive(true);
        }
        if(markers.Count > 0) {
            CvAruco.DrawAxis(image, webCamera.cameraMatrix, webCamera.distCoeffsArray, rvec, tvec, 0.2f);
            cube.position = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
            cube.rotation = RvecToQuat(rvec);
            cube.gameObject.SetActive(true);
        }
            
    }

    void estimateBoard()
    {
        if(markers.Count > 0) {
            List<Point3f> objPoints = new List<Point3f>();
            List<Point2f> imgPoints = new List<Point2f>();
            getBoardObjectAndImagePoints(arucoBoard, corners, ids, out objPoints, out imgPoints);
            Cv2.SolvePnP(objPoints, imgPoints, webCamera.cameraMatrix, webCamera.distCoeffsArray, out rvec, out tvec);
        }        
    }

    void estimateSingleMarker(Marker marker)
    {
        List<Point3f> objPoints = new List<Point3f>();
        List<Point2f> imgPoints = new List<Point2f>();
        getBoardObjectAndImagePoints(arucoBoard, new Point2f[][] { marker.corners }, new int[] { marker.id }, out objPoints, out imgPoints);
        //print("id:" + marker.id);
        //print("objPoints:" + objPoints.Count);
//        if (objPoints.Count == 4)
//            print("objPoints:" + objPoints[0] + "\t" + objPoints[1] + "\t" + objPoints[2] + "\t" + objPoints[3]);
        Cv2.SolvePnP(arucoBoard.singleCorners , marker.corners, webCamera.cameraMatrix, webCamera.distCoeffsArray, out marker.rvec, out marker.tvec);
    }

    void getBoardObjectAndImagePoints(GridBoard board, Point2f[][] detectedCorners, int[] detectedIds, out List<Point3f> objPoints, out List<Point2f> imgPoints)
    {
        Debug.Assert(board.ids.Length == board.objPoints.Length);
        Debug.Assert(detectedIds.Length == detectedCorners.Length);

        int nDetectedMarkers = detectedIds.Length;

        objPoints = new List<Point3f>();
        imgPoints = new List<Point2f>();
        //Point3f[] objPnts = new Point3f[nDetectedMarkers];
        //Point2f[] imgPnts = new Point2f[nDetectedMarkers];

        //int count = 0;
        //Debug.Log("Detected Markers###:" + nDetectedMarkers);
        //Debug.Log("Board Ids Length###:" + board.ids.Length);
        for (int i = 0; i < nDetectedMarkers; i++) {
            //Make sure this line is correct...
            int currentId = detectedIds[i];
            for (int j = 0; j < board.ids.Length; j++) {
                //Debug.Log("Current ID:" + currentId + " vs. board id[j]:" + board.ids[j]);
                if (currentId == board.ids[j]) {
                    for (int p = 0; p < 4; p++) {
                        //Debug.Log("COUNT IN  LOOP:" + count);
                        //Debug.Log("Length of objPnts:" + objPnts.Length);
                        objPoints.Add(board.objPoints[j][p]);
                        imgPoints.Add(detectedCorners[i][p]);
                    }
                }
            }
        }
        //Debug.Log("COUNT###:" + count);
        //objPoints = new List<Point3f>(objPnts);
        //imgPoints = new List<Point2f>(imgPnts);
    }

}
