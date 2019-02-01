using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class Predictor : MonoBehaviour
{
    List<KeyValuePair<float, Vector3>> gyro_history;
    List<KeyValuePair<float, Vector3>> gyro_predictions;

    public int history_length = 15;
    public float lag = 0.03f;

    float last_time = 0f;

    // Start is called before the first frame update
    void Start()
    {
        gyro_history = new List<KeyValuePair<float, Vector3>>();
        gyro_predictions = new List<KeyValuePair<float, Vector3>>();
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
    public Vector3 GetLatestGyroData() => gyro_history[gyro_history.Count - 1].Value;

    // Update is called once per frame
    void Update()
    {
        
    }
}
