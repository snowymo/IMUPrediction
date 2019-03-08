using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldMaker : MonoBehaviour {

    public GameObject chair;
    public Transform world;

    public int sideNumber = 6;

    public float dis = 0.3f;

	// Use this for initialization
	void Start () {
        for (int i = 0; i < sideNumber; i++){
            for (int j = 0; j < sideNumber; j++){
                for (int k = 0; k < sideNumber; k++){
                    Vector3 pos = new Vector3((i - sideNumber / 2+1) * dis, (j - sideNumber / 2+1) * dis, 
                                              (k - sideNumber / 2+1) * dis);
                    GameObject go = Instantiate(chair, world);
                    go.transform.localPosition = pos;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                }
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
