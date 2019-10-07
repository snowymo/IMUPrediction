using UnityEngine;
using OpenCvSharp;
using static OpenCvSharp.Unity;


public class ArucoCamera : MonoBehaviour
{
	public enum DeviceName { AlexIphone, AlexMac, ZhenyiIphone, ZhenyiMac };
	public DeviceName devName;

	//When Using Mac Cam:
	public bool isShowingTexture;
	[SerializeField]
	public bool isInitiated;


	static WebCamTexture backCam;
	static Texture2D imageTexture, undistortTexture;
	public Renderer background;
	//public Camera bgCamera;
	public GameObject originalImage;
	public MarkerDetection markerDetector;

	public double[,] cameraMatrix;
	public float image_height, image_width;
	public Vector2 cameraF;
	Vector2 principalPoint;
	float cameraBackgroundDistance = 3f;
	double[] RectifiedCameraMatrices;
	double[] cameraMatrixArray;
	public double[] distCoeffsArray;
	public Mat cameraMatrixMat, distCoeffsMat, RectifiedCameraMat;
	public Size imageSize;
	OpenCvSharp.Rect noROI = new OpenCvSharp.Rect();
	public Mat RectificationMat;
	double[] noRectificationMatrix = new double[0];
	public Mat UndistortionRectificationMaps1,
		UndistortionRectificationMaps2;

	void initWithNumbers()
	{
		principalPoint = new Vector2(image_width / 2, image_height / 2);
		UndistortionRectificationMaps1 = new Mat((int)image_width, (int)image_height, MatType.CV_16SC2);
		UndistortionRectificationMaps2 = new Mat((int)image_width, (int)image_height, 2);

		imageTexture = new Texture2D((int)image_width, (int)image_height);
		undistortTexture = new Texture2D((int)image_width, (int)image_height);
	}

	// Use this for initialization
	void Start()
	{
		//isShowingTexture = true;

		isInitiated = false;
		distCoeffsArray = new double[5];
		cameraMatrixArray = new double[9];
		RectifiedCameraMatrices = new double[9];
		cameraMatrix = new double[3, 3];

        if(isShowingTexture)
		    background.enabled = true;

		markerDetector = GameObject.Find("ArucoTracker").GetComponent<MarkerDetection>();

#if UNITY_EDITOR
		if (backCam == null)
			backCam = new WebCamTexture(720, 1280);

		image_height = backCam.height;
		image_width = backCam.width;
#elif UNITY_IOS
        foreach (WebCamDevice cam in WebCamTexture.devices)
        {
            if (cam.isFrontFacing)
            {
                string frontCamName = cam.name;
               	//backCam = new WebCamTexture(frontCamName, 1932, 2576);
		        backCam = new WebCamTexture(frontCamName);
		        Debug.Log("h:" + backCam.height + " w:" + backCam.width);

		        image_height = backCam.height;//1932;
		        image_width = backCam.width;//2576;
				break;
            }
        }
#endif
		if (isShowingTexture)
		{
			originalImage.GetComponent<Renderer>().material.mainTexture = backCam;
		}


		if (!backCam.isPlaying)
			backCam.Play();
	}

	// Update is called once per frame
	void Update()
	{

   
            Mat img = TextureToMat(backCam, null);
            image_height = img.Height;
            image_width = img.Width;
            if (image_height > 100 && !isInitiated)
            {
                Debug.Log("h:" + img.Height + " w:" + img.Width);
                initWithNumbers();

                loadCalibration();

                InitializeRectificationAndUndistortionMaps();

                configBackground();

                isInitiated = true;
            }


            // zhenyi remap
            //UndistortRectifyImages(img);
            if (isInitiated)
            {
                // undistortion
                //UndistortRectifyImages(img);

                markerDetector.ProcessFrame(img, isShowingTexture);
                if (isShowingTexture)
                {
                    undistortTexture = MatToTexture(img);
                    background.material.mainTexture = undistortTexture;
                }
            }

            img.Dispose();


       
	}

	public void loadCalibration()
	{
		/*cameraMatrix[0, 0] = 646.6561;        cameraMatrix[0, 1] = 0;        cameraMatrix[0, 2] = 324.4815;
        cameraMatrix[1, 0] = 0;        cameraMatrix[1, 1] = 647.4403;        cameraMatrix[1, 2] = 240.9121;
        cameraMatrix[2, 0] = 0;        cameraMatrix[2, 1] = 0;        cameraMatrix[2, 2] = 1;*/

		//cameraMatrix[0, 0] = 1062.9f; cameraMatrix[0, 1] = 0; cameraMatrix[0, 2] = 614.7f;
		//cameraMatrix[1, 0] = 0; cameraMatrix[1, 1] = 1062.0f; cameraMatrix[1, 2] = 354.15f;
		//cameraMatrix[2, 0] = 0; cameraMatrix[2, 1] = 0; cameraMatrix[2, 2] = 1;
#if UNITY_EDITOR
		if (devName == DeviceName.ZhenyiMac)
		{
			cameraMatrix[0, 0] = 1080.80234344448f; cameraMatrix[0, 1] = 0; cameraMatrix[0, 2] = 626.906893123974f;
			cameraMatrix[1, 0] = 0; cameraMatrix[1, 1] = 1086.35344468073f; cameraMatrix[1, 2] = 372.690890460390f;
			cameraMatrix[2, 0] = 0; cameraMatrix[2, 1] = 0; cameraMatrix[2, 2] = 1;
		}
		else if (devName == DeviceName.AlexMac)
		{
			cameraMatrix[0, 0] = 646.6561; cameraMatrix[0, 1] = 0; cameraMatrix[0, 2] = 324.4815;
			cameraMatrix[1, 0] = 0; cameraMatrix[1, 1] = 647.4403; cameraMatrix[1, 2] = 240.9121;
			cameraMatrix[2, 0] = 0; cameraMatrix[2, 1] = 0; cameraMatrix[2, 2] = 1;
		}

#elif UNITY_IOS
        cameraMatrix[0, 0] = 2408.75f; cameraMatrix[0, 1] = 0; cameraMatrix[0, 2] = 961.99f;//1279.48f;
        cameraMatrix[1, 0] = 0; cameraMatrix[1, 1] = 2401.26f; cameraMatrix[1, 2] = 1279.48f;//961.99f;
        cameraMatrix[2, 0] = 0; cameraMatrix[2, 1] = 0; cameraMatrix[2, 2] = 1;
		// zhenyi iphone
		if(devName == DeviceName.ZhenyiIphone){
			cameraMatrix[0, 0] = 611.166972217161f; cameraMatrix[0, 1] = 0; cameraMatrix[0, 2] = 327.284140948185f;
	        cameraMatrix[1, 0] = 0; cameraMatrix[1, 1] = 612.474596474278f; cameraMatrix[1, 2] = 244.417925187648f;
	        cameraMatrix[2, 0] = 0; cameraMatrix[2, 1] = 0; cameraMatrix[2, 2] = 1;
		}
        if(devName == DeviceName.AlexIphone){
            cameraMatrix[0, 0] = 608.123781700575f; cameraMatrix[0, 1] = 0; cameraMatrix[0, 2] = 320.601494629783f;
            cameraMatrix[1, 0] = 0; cameraMatrix[1, 1] = 610.539524247411f; cameraMatrix[1, 2] = 238.664646266522f;
            cameraMatrix[2, 0] = 0; cameraMatrix[2, 1] = 0; cameraMatrix[2, 2] = 1;
        }
#endif
        principalPoint = new Vector2((float)cameraMatrix[0, 2], (float)cameraMatrix[1, 2]);
		for (int i = 0; i < 9; i++)
		{
			cameraMatrixArray[i] = cameraMatrix[i / 3, i % 3];
		}
		cameraMatrixMat = new Mat(3, 3, MatType.CV_64FC1, cameraMatrixArray);
		//distCoeffsMat = new Mat(1, 5, MatType.CV_64FC1, distCoeffsArray);
		Debug.Log("cameraMatrix\t"
	+ cameraMatrix[0, 0] + "\t" + cameraMatrix[0, 1] + "\t" + cameraMatrix[0, 2] + "\t"
	+ cameraMatrix[1, 0] + "\t" + cameraMatrix[1, 1] + "\t" + cameraMatrix[1, 2] + "\t"
	+ cameraMatrix[2, 0] + "\t" + cameraMatrix[2, 1] + "\t" + cameraMatrix[2, 2]);
		distCoeffsArray[0] = -1.8902355308452077;
		distCoeffsArray[1] = 26.940687515100784;
		distCoeffsArray[2] = 0.0121538382238777;
		distCoeffsArray[3] = 0.050890060910626686;
		distCoeffsArray[4] = -111.11737159034945;
		//
		distCoeffsArray = new double[5] { 0f, 0f, 0f, 0f, 0f };
		distCoeffsMat = new Mat(1, 5, MatType.CV_64FC1, distCoeffsArray);
		imageSize = new Size(image_width, image_height);
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
		RectificationMat.Dispose();
	}

	void configBackground()
	{
		//background.material.mainTexture = backCam;
		//background.material.mainTexture = undistortTexture;

		//float fovY = 2f * Mathf.Atan(0.5f * image_height / cameraF.y) * Mathf.Rad2Deg;
		//bgCamera.fieldOfView = fovY;

		float localPositionX = (0.5f * image_width - principalPoint.x) / cameraF.x * cameraBackgroundDistance;
		float localPositionY = -(0.5f * image_height - principalPoint.y) / cameraF.y * cameraBackgroundDistance; // a minus because OpenCV camera coordinates origin is top - left, but bottom-left in Unity

		// Considering https://stackoverflow.com/a/41137160
		// scale.x = 2 * cameraBackgroundDistance * tan(fovx / 2), cameraF.x = imageWidth / (2 * tan(fovx / 2))
		float localScaleX = image_width / cameraF.x * cameraBackgroundDistance;
		float localScaleY = image_height / cameraF.y * cameraBackgroundDistance;

        // Place and scale the background
        if (isShowingTexture){
            background.transform.localPosition = new Vector3(localPositionX, localPositionY, cameraBackgroundDistance);
            background.transform.localScale = new Vector3(localScaleX, localScaleY, 1);
        }
         
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
