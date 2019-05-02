using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorFusion : MonoBehaviour
{
    //public ArucoTracker tracker;
    public ArucoCamera tracker;
    public Predictor imu;
    public enum Mode { SensorFusion, IMU, Aruco};
    public Mode rotation_mode;

    Quaternion[] cameraQuatOptions = new Quaternion[17];
    public int buttonIdx;
    //public GameObject arucoCube;
    public UnityEngine.UI.Text button4cmr;

    public bool positionTracking;


    Quaternion unityToIphone(Quaternion q){
        return new Quaternion(-q.x, -q.y, q.z, q.w);
        //return new Quaternion(q.x, -q.y, -q.z, q.w);
        // -y -z  
        //zyx
        //xzy
        //return new Quaternion(-q.y, -q.x, q.z, q.w);
    }

    Quaternion yRot = Quaternion.identity;
    Quaternion prev_aruco = Quaternion.identity;
    Quaternion prev_imu = Quaternion.identity;
    Vector3 prev_pose;
    void UpdateTracking(){
        //TODO: There might need to be an offset 
        //because we want the center of the board.


        if(rotation_mode == Mode.Aruco){
            //THIS IS CORRECT ARUCO.
            //
            //cameraQuatOptions[0] = Quaternion.Euler(0, 0, 90) * Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0)));
            //cameraQuatOptions[0] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0)));
            cameraQuatOptions[0] = Quaternion.Inverse(Quaternion.Euler(45, 0, 0) * Quaternion.Euler(0, 180, 0) * unityToIphone(tracker.markerDetector.current_rotation));
            cameraQuatOptions[1] = Quaternion.Euler(0, 90, 0) * Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0)));

            cameraQuatOptions[2] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, 45, 0)));
            cameraQuatOptions[3] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(0, 45, 0)));

            cameraQuatOptions[4] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 45, 0)));
            cameraQuatOptions[5] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, -90, 0) * Quaternion.Euler(0, 45, 0)));

            cameraQuatOptions[6] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 45, 0)));
            cameraQuatOptions[7] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 0, -90) * Quaternion.Euler(0, 45, 0)));

            cameraQuatOptions[8] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation  * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(90, 0, 0)));
            cameraQuatOptions[9] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(-90, 0, 0)));

            cameraQuatOptions[10] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, 90, 0)));
            cameraQuatOptions[11] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation  * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, -90, 0)));

            cameraQuatOptions[12] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, 0, 90)));
            cameraQuatOptions[13] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation  * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, 0, -90)));


            //Debug.Log("###Aruco Rotation:" + (Quaternion.Euler(90,0,0) * Quaternion.Euler(0, 180, 0)* unityToIphone(tracker.markerDetector.current_rotation)).eulerAngles);
         
            //Debug.Log("####ARUCO rotation:" + Quaternion.Inverse(Quaternion.Euler(45, 0, 0) * Quaternion.Euler(0, 180, 0) * unityToIphone(tracker.markerDetector.current_rotation)).eulerAngles);
            //Debug.Log("#### Aruco with 45deg" + (tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0)).eulerAngles);
            //Debug.Log("#### Unity To Iphone" + unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0)).eulerAngles);
            //Debug.Log("###Final Rotation:" + cameraQuatOptions[buttonIdx].eulerAngles);
            //Alex
            //cameraQuatOptions[14] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, 0, 45)));
            //cameraQuatOptions[15] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(45, 0, 0)));
            //cameraQuatOptions[16] = Quaternion.Inverse(unityToIphone(tracker.markerDetector.current_rotation * Quaternion.Euler(0, 45, 0) * Quaternion.Euler(0, 45, 0)));
            this.transform.rotation = new Quaternion(cameraQuatOptions[buttonIdx].x, -cameraQuatOptions[buttonIdx].z, -cameraQuatOptions[buttonIdx].y, -cameraQuatOptions[buttonIdx].w);
            
        }
        else if(rotation_mode == Mode.IMU){
            //THIS CORRECT IMU (headset needs to be face down)
            this.transform.rotation = imu.iphone2unity(imu.calculated_pose * Quaternion.Euler(0, -45, 0));
            Debug.Log(" IMU: " + transform.rotation.eulerAngles);
        }
        else if(rotation_mode == Mode.SensorFusion){
            Quaternion imu_rot = imu.iphone2unity(imu.calculated_pose * Quaternion.Euler(0, -45, 0));
            Quaternion inv = Quaternion.Inverse(imu_rot);
            Quaternion aruco_rot = Quaternion.Inverse(Quaternion.Euler(45, 0, 0) * Quaternion.Euler(0, 180, 0) * unityToIphone(tracker.markerDetector.current_rotation));
            Quaternion aruco_sign = new Quaternion(aruco_rot.x, -aruco_rot.y, -aruco_rot.z, -aruco_rot.w);
            Quaternion optical = aruco_sign * inv;

            Quaternion old_orientation = this.transform.rotation;

            if (tracker.markerDetector.isTracked)
            {
                float yOpt = optical.eulerAngles.z;
                float yOld = old_orientation.eulerAngles.y;
                float yDiff = Mathf.Abs(yOpt - yOld);
                if (yDiff > 180f)
                {
                    if (yOpt < yOld)
                    {
                        yOpt += 360f;
                    }
                    else
                    {
                        yOld += 360f;
                    }
                    yDiff = Mathf.Abs(yOpt - yOld);
                }
                float t = Mathf.Abs(yOpt - yOld);
                t = t * t;
                float yNew = Mathf.LerpAngle(yOld, yOpt, t);

                yRot = Quaternion.AngleAxis(yNew, Vector3.up);

                Quaternion imuTransform = imu_rot * Quaternion.Inverse(prev_imu);
                Quaternion arucoTransform = aruco_sign * Quaternion.Inverse(prev_aruco);
                float angleDiff = Quaternion.Angle(imuTransform, arucoTransform);

                /*
                if(imuTransform.eulerAngles != new Vector3(0,0,0)){
                    Debug.Log("$TRACKED$ IMU Transform: " + imuTransform.eulerAngles + " ArUco Transform: " + arucoTransform.eulerAngles + " Angle Difference: " + angleDiff);
                }*/
            }
            else
            {
                Quaternion imuTransform = imu_rot * Quaternion.Inverse(prev_imu);
                Quaternion arucoTransform = aruco_sign * Quaternion.Inverse(prev_aruco);
                float angleDiff = Quaternion.Angle(imuTransform, arucoTransform);

                //if (imuTransform.eulerAngles != new Vector3(0, 0, 0))
                //{
                //    Debug.Log("#UNTRACKED# IMU Transform: " + imuTransform.eulerAngles + " ArUco Transform: " + arucoTransform.eulerAngles + " Angle Difference: " + angleDiff);
                //}
            }

            //Debug.Log("prev to current:" + Quaternion.Angle(transform.rotation, yRot * imu_rot));
            transform.rotation = yRot * imu_rot;


            prev_aruco = aruco_sign;
            prev_imu = imu_rot;
            //When ARUCO tracking stops it gives a random value, we should have a threshold for this.

            //Debug.Log("Sensor Fusion:" + transform.rotation.eulerAngles + " yrot: " + yRot.eulerAngles + " ARUCO:" + aruco_sign.eulerAngles + " IMU: " + imu_rot.eulerAngles);
        }


    }

    public void SwitchButtonIndex(){
        buttonIdx += 1;
        buttonIdx %= 17;
        button4cmr.text = "Cur:" + buttonIdx.ToString() + "\tNext";
    }

    // Start is called before the first frame update
    void Start()
    {
        buttonIdx = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //IMU ONLY
        if(rotation_mode == Mode.IMU){
            if (imu != null && imu.receiver != null)
            {
                if (imu.receiver.initiated)
                {
                    UpdateTracking();

                }
            }
        }
        else if(rotation_mode == Mode.Aruco){
            if (tracker != null)
            {
                if (tracker.isInitiated)
                {
                    if (tracker.markerDetector.isInitiated)
                    {
                        UpdateTracking();
                    }
                }
            }
        }
        else if(rotation_mode == Mode.SensorFusion){
            if (imu != null && imu.receiver != null && tracker != null)
            {
                if (imu.receiver.initiated && tracker.isInitiated)
                {
                    if (tracker.markerDetector.isInitiated)
                    {
                        UpdateTracking();
                    }
                }
            }
        }

        if (positionTracking && tracker.markerDetector.isTracked)
        {
            Vector3 base_rot = -1 * new Vector3(tracker.markerDetector.current_position.x, -tracker.markerDetector.current_position.y, -tracker.markerDetector.current_position.z);
            Debug.Log("Tracked position:" + (Quaternion.Euler(45, 0, 0) * Quaternion.Euler(0, 180, 0) * base_rot).ToString("F3"));
            prev_pose = Quaternion.Euler(45, 0, 0) * Quaternion.Euler(0, 180, 0) * base_rot;
            if(prev_pose != new Vector3(0,0,0)){
                transform.position = Quaternion.Euler(45, 0, 0) * Quaternion.Euler(0, 180, 0) * base_rot;
            }
        }

        else
            transform.position = prev_pose;
        //Debug.Log("position:" + transform.position.ToString("F3") + " Is tracked?:" + tracker.markerDetector.isTracked);
    }
}
