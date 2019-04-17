using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine.UI;

public class Predictor : MonoBehaviour
{
    List<KeyValuePair<float, Vector3>> gyro_history;
    List<KeyValuePair<float, Vector3>> gyro_predictions;
    private static float NS2S = 1.0f / 1000000000.0f;
    private float timestamp;
    private float timestamp2;
    public GameObject world;
    public UDPReceiver receiver;
    int iters = 0;
    float sumx = 0;
    float sumy = 0;
    float sumz = 0;

    //Drift factor
    //float x_const = -0.0007957f;
    float x_const = -0.0008073f;
    //float y_const = -0.0004028f;
    float y_const = -0.0003908f;
    float z_const = -0.0000354f;

    public int history_length = 15;
    public float lag = 0.03f;

    float last_time = 0f;

    public Text output_rot;

    public Quaternion calculated_pose, headset_pose;
    // Start is called before the first frame update
    void Start()
    {
        gyro_history = new List<KeyValuePair<float, Vector3>>();
        gyro_predictions = new List<KeyValuePair<float, Vector3>>();
        calculated_pose = Quaternion.identity;
        headset_pose = new Quaternion(0.066f, -0.757f, 0.045f, -0.649f);
        headset_pose = Quaternion.Euler(0, -90, 0);
        //world.transform.rotation *= new Quaternion(-0.50116f, -0.72521f, -0.22508f, 0.415f);
    }

    float[] PolynomialRegression(List<KeyValuePair<float, float>> data, int order)
    {
        Matrix<float> X = Matrix<float>.Build.Dense(data.Count, order + 1);
        Vector<float> y = Vector<float>.Build.Dense(data.Count);
        Vector<float> c = Vector<float>.Build.Dense(order + 1);
        for (int i = 0; i < data.Count; i++)
        {
            float time = data[i].Key;
            float val = data[i].Value;
            for (int k = 0; k <= order; k++)
            {
                X[i, k] = Mathf.Pow(time, k);
            }
            y[i] = val;
        }
        c = (X.Transpose() * X).Inverse() * X.Transpose() * y;

        return c.AsArray();
    }

    float EvaluatePolynomial(float[] coeffs, float x)
    {
        float val = 0f;
        for (int i = 0; i < coeffs.Length; i++)
        {
            val += coeffs[i] * Mathf.Pow(x, i);
        }
        return val;
    }

    public void PredictByGyro(float time, Vector3 gyro)
    {
        if (time == last_time) return;
        last_time = time;
        // L1 norm to determine alpha: if there isn't much gyro motion, we don't need to use prediction.
        float alpha = Mathf.Clamp01(Mathf.Abs(gyro.x) + Mathf.Abs(gyro.y) + Mathf.Abs(gyro.z));
        gyro_history.Add(new KeyValuePair<float, Vector3>(time, gyro));
        if (gyro_history.Count > history_length)
        {
            gyro_history.RemoveAt(0);
            List<KeyValuePair<float, float>> xvals = new List<KeyValuePair<float, float>>();
            List<KeyValuePair<float, float>> yvals = new List<KeyValuePair<float, float>>();
            List<KeyValuePair<float, float>> zvals = new List<KeyValuePair<float, float>>();
            for (int i = 0; i < gyro_history.Count; i++)
            {
                float adjusted_time = gyro_history[i].Key - gyro_history[0].Key;
                xvals.Add(new KeyValuePair<float, float>(adjusted_time, gyro_history[i].Value.x));
                yvals.Add(new KeyValuePair<float, float>(adjusted_time, gyro_history[i].Value.y));
                zvals.Add(new KeyValuePair<float, float>(adjusted_time, gyro_history[i].Value.z));
            }
            float[] xcoeffs = PolynomialRegression(xvals, 2);
            float[] ycoeffs = PolynomialRegression(yvals, 2);
            float[] zcoeffs = PolynomialRegression(zvals, 2);
            /*
            string s = "";
            foreach (KeyValuePair<float, float> val in xvals)
            {
                s += string.Format("({0}: {1}) ", val.Key, val.Value);
            }
            Debug.Log(s);
            */
            float x = EvaluatePolynomial(xcoeffs, time - gyro_history[0].Key + lag);
            float y = EvaluatePolynomial(ycoeffs, time - gyro_history[0].Key + lag);
            float z = EvaluatePolynomial(zcoeffs, time - gyro_history[0].Key + lag);

            x = Mathf.Lerp(gyro.x, x, alpha);
            y = Mathf.Lerp(gyro.y, y, alpha);
            z = Mathf.Lerp(gyro.z, z, alpha);

            gyro_predictions.Add(new KeyValuePair<float, Vector3>(time + lag, new Vector3(x, y, z)));
            //Debug.Log(string.Format("{0} / ({1}, {2}, {3})", alpha, x, y, z));
        }
    }

    public Vector3 GetLatestPrediction() => gyro_predictions[gyro_predictions.Count - 1].Value;
    public KeyValuePair<float, Vector3> GetLatestPredictionPair() => gyro_predictions[gyro_predictions.Count - 1];
    public Vector3 GetLatestGyroData() => gyro_history[gyro_history.Count - 1].Value;
    public KeyValuePair<float, Vector3> GetLatestGyroDataPair() => gyro_history[gyro_history.Count - 1];


    public Quaternion GyroToQuat(KeyValuePair<float,Vector3> gyroData, bool drift){
        Vector3 gyro = gyroData.Value;
        //Vector3 gyro = new Vector3(gyroData.Value.y, gyroData.Value.z, gyroData.Value.x);//correct
        //Vector3 gyro = new Vector3(gyroData.Value.y, gyroData.Value.x, gyroData.Value.z);
        //Vector3 gyro = new Vector3(gyroData.Value.x, gyroData.Value.z, gyroData.Value.y);
        //Vector3 gyro = new Vector3(gyroData.Value.z, gyroData.Value.x, gyroData.Value.y);
        //Vector3 gyro = new Vector3(gyroData.Value.z, gyroData.Value.y, gyroData.Value.x);
        Debug.Log("test1:" + gyro);
        float event_time = gyroData.Key;
        Quaternion rotation;
        if(timestamp != 0){
            float dT = (event_time - timestamp);
            float omegaMagnitude = Mathf.Sqrt(gyro.x * gyro.x + gyro.y * gyro.y + gyro.z * gyro.z);
            if (omegaMagnitude > Mathf.Epsilon)
            {
                gyro.x /= omegaMagnitude;
                gyro.y /= omegaMagnitude;
                gyro.z /= omegaMagnitude;
            }
            float thetaOverTwo = omegaMagnitude * dT / 2.0f;
            float sinThetaOverTwo = Mathf.Sin(thetaOverTwo);
            float cosThetaOverTwo = Mathf.Cos(thetaOverTwo);
            //Debug.Log("Quat");
            //Debug.Log(sinThetaOverTwo * gyro.x + " " + sinThetaOverTwo * gyro.y + " " + sinThetaOverTwo * gyro.z + " " + cosThetaOverTwo);
            Vector3 v = new Vector3(sinThetaOverTwo * gyro.x - x_const, sinThetaOverTwo * gyro.y - y_const, sinThetaOverTwo * gyro.z - z_const);
            //Vector3 v = new Vector3(sinThetaOverTwo * gyro.x, sinThetaOverTwo * gyro.y, sinThetaOverTwo * gyro.z);
            //v.x *= -1;
            //v.y *= -1;
            //v.z *= -1;
            rotation = new Quaternion(v.x, v.y, v.z, cosThetaOverTwo);
            //Debug.Log("if case:" + rotation.ToString("F3"));
            //Unity Config
            //rotation = new Quaternion(v.y, -1 * v.x, v.z, cosThetaOverTwo);
            //Mira Prism Config
            //rotation = new Quaternion(v.y, v.x, -1 * v.z, cosThetaOverTwo);

        }
        else{
            rotation = Quaternion.identity;
            Debug.Log("else case:" + rotation.ToString("F3"));
        }
        timestamp = event_time;
        //Debug.Log("test2:" + gyro);
        return rotation;
    }

    public Quaternion RightHandToLeftHand(Quaternion quat){
        return new Quaternion(quat.x, quat.y, -1 * quat.z, -1 * quat.w);
    }

    public Quaternion UnityToPrism(Quaternion quat){
        return new Quaternion(-1 * quat.x, -1 * quat.y, -1 * quat.z, quat.w);
    }

    public Quaternion TestGyroToQuat(KeyValuePair<float, Vector3> gyroData){
        Vector3 gyro = gyroData.Value;
        float event_time = gyroData.Key;
        Quaternion rotation;

        if (timestamp2 != 0)
        {
            float dT = (event_time - timestamp2);
            float omegaMagnitude = Mathf.Sqrt(gyro.x * gyro.x + gyro.y * gyro.y + gyro.z * gyro.z);
            if (omegaMagnitude > Mathf.Epsilon)
            {
                gyro.x /= omegaMagnitude;
                gyro.y /= omegaMagnitude;
                gyro.z /= omegaMagnitude;
            }
            float thetaOverTwo = omegaMagnitude * dT / 2.0f;
            float sinThetaOverTwo = Mathf.Sin(thetaOverTwo);
            float cosThetaOverTwo = Mathf.Cos(thetaOverTwo);
            rotation = new Quaternion(sinThetaOverTwo * gyro.x, sinThetaOverTwo * gyro.y, sinThetaOverTwo * gyro.z, cosThetaOverTwo);
        }
        else{
            rotation = Quaternion.identity;
        }
        timestamp2 = event_time;
        return rotation;
    }
    Quaternion prev_baseline;
    // Update is called once per frame



    //Quaternion iphone2unityQuat = Quaternion.Euler(90,0,0) * Quaternion.Euler(0, 0, 90);

    public Quaternion iphone2unity(Quaternion q){
        //return new Quaternion(q.y, -q.x, -q.z, -q.w);
        return new Quaternion(q.y, -q.z, q.x, -q.w);
    }
    void Update()
    {
        if (receiver.initiated)
        {
           

            Quaternion imuquat = GyroToQuat(GetLatestGyroDataPair(), true);
            calculated_pose =  calculated_pose * (imuquat);
            world.transform.rotation = Quaternion.Inverse(iphone2unity(calculated_pose * Quaternion.Euler(0, -45, 0)));
            output_rot.text = "IMU: "+world.transform.rotation.ToString();

        }
    }
}
