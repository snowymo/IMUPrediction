using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NativeCamera : MonoBehaviour
{
	static WebCamTexture backCam;
	static Texture2D imageTexture;
	public Renderer background;
	//public Camera bgCamera;
	//public GameObject originalImage;

	public float image_height, image_width;
	//public Vector2 cameraF;
	//Vector2 principalPoint;
	//float cameraBackgroundDistance = 3f;


	// Start is called before the first frame update
	void Start()
	{
#if UNITY_EDITOR
		if (backCam == null)
			backCam = new WebCamTexture(1080, 720);

		image_height = backCam.height;// 720;
		image_width = backCam.width;// 1280;
		Debug.Log("cam.name:" + backCam.name);
#elif UNITY_IOS
        foreach (WebCamDevice cam in WebCamTexture.devices)
        {
			Debug.Log("cam.name:" + cam.name);
            if (cam.isFrontFacing)
            {
                string frontCamName = cam.name;
               //backCam = new WebCamTexture(frontCamName, 1932, 2576);
        		backCam = new WebCamTexture(frontCamName);
        		Debug.Log("h:" + backCam.height + " w:" + backCam.width);

        		image_height = backCam.height;//1932;
        		image_width = backCam.width;//2576;
       
            }
       }
#endif

		//originalImage.GetComponent<Renderer>().material.mainTexture = backCam;
		background.material.mainTexture = backCam;


		if (!backCam.isPlaying)
			backCam.Play();


	}

	// Update is called once per frame
	void Update()
	{
		background.transform.localScale = new Vector3((float)backCam.width/(float)backCam.height, 1, 1);
		Debug.Log(Time.time + "\tw:" + backCam.width + "\th:" + backCam.height + "\ts:" + background.transform.localScale);
	}

	IEnumerator saveImg()
	{
		// We should only read the screen buffer after rendering is complete
		yield return new WaitForEndOfFrame();

		Texture2D temp = new Texture2D(backCam.width, backCam.height);
		temp.SetPixels(backCam.GetPixels());
		temp.Apply();
		// Encode texture into PNG
		byte[] bytes = temp.EncodeToPNG();


		// For testing purposes, also write to a file in the project folde
#if UNITY_EDITOR
		File.WriteAllBytes(Application.dataPath + "/Saved/SavedScreen_" + Time.frameCount + ".png", bytes);
#elif UNITY_IOS
		File.WriteAllBytes(Application.persistentDataPath + "/SavedScreen_" + Time.frameCount + ".png", bytes);
#endif

		Object.Destroy(temp);
	}

	public void Capture()
	{
		Texture2D temp = new Texture2D(backCam.width, backCam.height);
		temp.SetPixels(backCam.GetPixels());
		temp.Apply();
		// Encode texture into PNG
		byte[] bytes = temp.EncodeToPNG();


		// For testing purposes, also write to a file in the project folde
#if UNITY_EDITOR
		File.WriteAllBytes(Application.dataPath + "/Saved/SavedScreen_" + Time.frameCount + ".png", bytes);
#elif UNITY_IOS
		File.WriteAllBytes(Application.persistentDataPath + "/SavedScreen_" + Time.frameCount + ".png", bytes);
#endif

		Object.Destroy(temp);
	}
}
