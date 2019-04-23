using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using OpenCvSharp.Aruco;

public class Marker
{
	public int id;
	public Point2f[] corners;//4 corners
	public double[] rvec;
	public double[] tvec;
	public Marker(int i, Point2f[] c, double[] r, double[] t)
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
		// TODO
		//for (int idx = 0; idx < c.Length; idx++) {
		//corners[idx].Y = 1932 - c[idx].Y;
		//}

	}
}

public class MarkerDetection : MonoBehaviour
{

	public Transform cube;
	public GameObject singleCube;
	GameObject[] singleCubes;

	ArucoCamera webCamera;

	static Dictionary dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.DictArucoOriginal);
	Dictionary<int, Marker> markers = new Dictionary<int, Marker>();

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

    public bool isInitiated;
    public Quaternion current_rotation;

	// Use this for initialization
	void Start()
	{
        isInitiated = false;
		webCamera = GameObject.Find("Camera").GetComponent<ArucoCamera>();
		singleCubes = new GameObject[arucoBoard.ids.Length];
		for (int i = 0; i < singleCubes.Length; i++)
		{
			singleCubes[i] = GameObject.Instantiate(singleCube);
		}

	}

	// Update is called once per frame
	void Update()
	{

	}

	void hideCubes()
	{
		for (int i = 0; i < singleCubes.Length; i++)
		{
			singleCubes[i].SetActive(false);
		}
	}

    public void ProcessFrame(Mat image, bool wantToDraw = false)
    {
        detect(image);

       
        foreach (int key in markers.Keys)
            estimateSingleMarker(markers[key]);

        estimateBoard();
        if (wantToDraw)
            drawAxes(image);//optional

        fixY();

        if(wantToDraw)
            drawCubes(image);//optional


        if (markers.Count > 0)
        {
            current_rotation = RvecToQuat(rvec);
            isInitiated = true;
        }


    }
    List<int> lowConfIds = new List<int>();
   
	void detect(Mat image)
	{
		CvAruco.DetectMarkers(image, dictionary, out corners, out ids, parameters, out rejected);

		// flip the corners
		for (int i = 0; i < corners.Length; i++)
		{
			for (int j = 0; j < corners[i].Length; j++)
			{
				//corners[i][j].Y = webCamera.image_height - corners[i][j].Y;
			}
		}

		markers.Clear();
		lowConfIds.Clear();
		for (int i = 0; i < ids.Length; i++)
		{
			if (ids[i] < 12)
			{
				if (!markers.ContainsKey(ids[i]))
				{
					markers.Add(ids[i], new Marker(ids[i], corners[i]));
				}
				else
				{
					lowConfIds.Add(ids[i]);
				}
			}
		}
		for (int i = 0; i < lowConfIds.Count; i++)
		{
			print("removed: " + lowConfIds[i]);
			markers.Remove(lowConfIds[i]);
		}
		if (lowConfIds.Count > 0)
			foreach (int key in markers.Keys)
			{
				Marker val = markers[key];
				print("marker:" + key + " = " + val);
			}
		// reconstruct the corners and ids
		corners = new Point2f[markers.Count][];
		ids = new int[markers.Count];
		int count = 0;
		foreach (int key in markers.Keys)
		{
			Marker val = markers[key];
			corners[count] = markers[key].corners;
			ids[count] = key;
			++count;
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

	void drawAxes(Mat image)
	{
		// draw each marker
		CvAruco.DrawDetectedMarkers(image, corners, ids);
		foreach (int key in markers.Keys)
		{
			CvAruco.DrawAxis(image, webCamera.cameraMatrix, webCamera.distCoeffsArray, markers[key].rvec, markers[key].tvec, 0.03f);
		}
		if (markers.Count > 0)
		{
			CvAruco.DrawAxis(image, webCamera.cameraMatrix, webCamera.distCoeffsArray, rvec, tvec, 0.2f);
		}
	}

	void drawCubes(Mat image)
	{
		hideCubes();

		foreach (int key in markers.Keys)
		{
			singleCubes[key].transform.position = new Vector3((float)markers[key].tvec[0], (float)markers[key].tvec[1], (float)markers[key].tvec[2]);
			singleCubes[key].transform.rotation = RvecToQuat(markers[key].rvec);
			singleCubes[key].SetActive(true);
		}
		if (markers.Count > 0)
		{

			cube.position = new Vector3((float)tvec[0], (float)tvec[1], (float)tvec[2]);
			cube.rotation = RvecToQuat(rvec);
            current_rotation = RvecToQuat(rvec);
            isInitiated = true;
            cube.gameObject.SetActive(true);
		}
	}

	void fixY()
	{
		foreach (int key in markers.Keys)
		{
			markers[key].rvec[0] = -markers[key].rvec[0];
			markers[key].rvec[2] = -markers[key].rvec[2];
			markers[key].tvec[1] = -markers[key].tvec[1];
		}
		if (markers.Count > 0)
		{
			rvec[0] = -rvec[0];
			rvec[2] = -rvec[2];
			tvec[1] = -tvec[1];
		}
	}

	void estimateBoard()
	{
		if (markers.Count > 0)
		{
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
		Cv2.SolvePnP(arucoBoard.singleCorners, marker.corners, webCamera.cameraMatrix, webCamera.distCoeffsArray, out marker.rvec, out marker.tvec);
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
		for (int i = 0; i < nDetectedMarkers; i++)
		{
			//Make sure this line is correct...
			int currentId = detectedIds[i];
			for (int j = 0; j < board.ids.Length; j++)
			{
				//Debug.Log("Current ID:" + currentId + " vs. board id[j]:" + board.ids[j]);
				if (currentId == board.ids[j])
				{
					for (int p = 0; p < 4; p++)
					{
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
