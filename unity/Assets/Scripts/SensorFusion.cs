using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorFusion : MonoBehaviour
{
    public ArucoTracker tracker;
    public Predictor imu;



    void UpdateTracking(){
        //TODO: There might need to be an offset 
        //because we want the center of the board.
        Quaternion imu_rot = Quaternion.Inverse(imu.iphone2unity(imu.calculated_pose * Quaternion.Euler(0, -45, 0)));
        Quaternion inv = Quaternion.Inverse(imu_rot);
        Quaternion optical = tracker.rotation_vec * inv;

        Quaternion old_orientation = this.transform.rotation;

        float yOpt = optical.eulerAngles.y;
        float yOld = old_orientation.eulerAngles.y;
        float yDiff = Mathf.Abs(yOpt - yOld);
        if(yDiff >  180f){
            if(yOpt < yOld){
                yOpt += 360f;
            }
            else{
                yOld += 360f;
            }
            yDiff = Mathf.Abs(yOpt - yOld);
        }
        float t = Mathf.Abs(yOpt - yOld);
        t = t * t;
        float yNew = Mathf.LerpAngle(yOld, yOpt, t);

        Quaternion yRot = Quaternion.AngleAxis(yNew, Vector3.up);
        Vector3 eulerY = yRot.eulerAngles;
        Vector3 eulerImu = imu_rot.eulerAngles;
        Vector3 final_rotation = new Vector3(eulerImu.x, eulerY.y, eulerImu.z);
        this.transform.rotation = Quaternion.Euler(final_rotation);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (imu.receiver.initiated && tracker.initiated)
        {
            UpdateTracking();
        }
    }
}
