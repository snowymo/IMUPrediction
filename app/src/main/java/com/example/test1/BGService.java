package com.example.test1;

import android.app.Service;
import android.content.Intent;
import android.graphics.Matrix;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Handler;
import android.os.IBinder;
import android.renderscript.Matrix4f;
import android.util.Log;

import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketException;
import java.util.Timer;
import java.util.TimerTask;

import androidx.annotation.Nullable;

import static java.lang.Math.cos;
import static java.lang.Math.sin;
import static java.lang.Math.sqrt;

public class BGService extends Service implements SensorEventListener {

    private Sensor myGyroscope, myRotation, myAcc;
    private SensorManager SM;
    private double timestamp;// in ms

    private static final double SEND_RATE = 1.0 / 72;

    private float[] data, acc;
    private String jsondata;

//    public static InetAddress IPAddress;
    public static DatagramSocket client_socket;

    boolean isStop;

    // run on another Thread to avoid crash
    private Handler mHandler = new Handler();
    // timer handling
    private Timer mTimer = null;

    @Nullable
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        //return super.onStartCommand(intent, flags, startId);
        //new SendTask().execute(MainActivity.gyros);
        Log.d("service", "onStartCommand");
        return START_STICKY;
    }

    @Override
    public void onDestroy() {
        isStop = true;
        super.onDestroy();
    }


    @Override
    public void onSensorChanged(SensorEvent event) {
        // calculate everything I need and the nsend
        if(!isStop) {
            if (event.sensor.getType() == Sensor.TYPE_GYROSCOPE) {
                data[0] = event.values[0];
                data[1] = event.values[1];
                data[2] = event.values[2];
                calculateRotationByGyro(event);
//                float[] f= new float[9];
//                rotationCurrent.getValues(f);
//                for(int i = 0; i < 9; i++)
//                    data[i+8] = f[i];
                //Log.d("service", "onSensorChanged");
            } else if (event.sensor.getType() == Sensor.TYPE_ROTATION_VECTOR) {
                data[3] = event.values[0];
                data[4] = event.values[1];
                data[5] = event.values[2];
                data[6] = event.values[3];
                // debug
//                float[] mRotationMatrix = new float[16];
//                mRotationMatrix[ 0] = 1;
//                mRotationMatrix[ 4] = 1;
//                mRotationMatrix[ 8] = 1;
//                mRotationMatrix[12] = 1;
//                Log.d("event.value", String.valueOf(event.values[0]));
//                SensorManager.getRotationMatrixFromVector(
//                        mRotationMatrix , event.values);
//                Matrix2Quaternion(mRotationMatrix);
            }
            else if (event.sensor.getType() == Sensor.TYPE_ACCELEROMETER) {
                acc[0] = event.values[0];
                acc[1] = event.values[1];
                acc[2] = event.values[2];
                CalculatePosByAcc(event);
                long l1 = System.currentTimeMillis();
                data[7] = (float) System.currentTimeMillis()/(float)1000;// in ms
                jsondata = "{\"gyro\":[" +String.valueOf(data[0]) + "," + String.valueOf(data[1]) + "," + String.valueOf(data[2]) + "],"
                        +"\"acc\":[" +String.valueOf(acc[0]) + "," + String.valueOf(acc[1]) + "," + String.valueOf(acc[2]) + "],"
//                        + "\"rvec\":[" + String.valueOf(data[3]) + "," + String.valueOf(data[4]) + "," + String.valueOf(data[5])+ "," + String.valueOf(data[6]) + "],"
                        + "\"rotation\":[" + String.valueOf(rotationCurrentFA[0]) + ","+ String.valueOf(rotationCurrentFA[1]) + ","+ String.valueOf(rotationCurrentFA[2]) + ","
                        + String.valueOf(rotationCurrentFA[3]) + ","+ String.valueOf(rotationCurrentFA[4]) + ","+ String.valueOf(rotationCurrentFA[5]) + ","
                        + String.valueOf(rotationCurrentFA[6]) + ","+ String.valueOf(rotationCurrentFA[7]) + ","+ String.valueOf(rotationCurrentFA[8]) + "],"
                        + "\"pos\":[" + String.valueOf(position[0])+ "," + String.valueOf(position[1])+ ","+ String.valueOf(position[2])+ "],"
                        + "\"timestamp\":" + String.valueOf(l1)
                        + "}";
            }
        }
    }

    private static final float NS2S = 1.0f / 1000000000.0f;
    private final float[] deltaRotationVector = new float[4];
    Matrix rotationCurrent = new Matrix();
    float[] rotationCurrentFA = new float[9];
    void calculateRotationByGyro(SensorEvent event){
        if (timestamp != 0) {
            final float dT = (float) ((event.timestamp - timestamp) * NS2S);
            // Axis of the rotation sample, not normalized yet.
            float axisX = event.values[0];
            float axisY = event.values[1];
            float axisZ = event.values[2];

            // Calculate the angular speed of the sample
            double omegaMagnitude = sqrt(axisX*axisX + axisY*axisY + axisZ*axisZ);

            // Normalize the rotation vector if it's big enough to get the axis
            // (that is, EPSILON should represent your maximum allowable margin of error)
            //if (omegaMagnitude > EPSILON) {
//                axisX /= omegaMagnitude;
//                axisY /= omegaMagnitude;
//                axisZ /= omegaMagnitude;
//            }

            // Integrate around this axis with the angular speed by the timestep
            // in order to get a delta rotation from this sample over the timestep
            // We will convert this axis-angle representation of the delta rotation
            // into a quaternion before turning it into the rotation matrix.
            float thetaOverTwo = (float) (omegaMagnitude * dT / 2.0f);
            float sinThetaOverTwo = (float) sin(thetaOverTwo);
            float cosThetaOverTwo = (float) cos(thetaOverTwo);
            deltaRotationVector[0] = sinThetaOverTwo * axisX;
            deltaRotationVector[1] = sinThetaOverTwo * axisY;
            deltaRotationVector[2] = sinThetaOverTwo * axisZ;
            deltaRotationVector[3] = cosThetaOverTwo;
        }
        timestamp = event.timestamp;
        float[] deltaRotationMatrix = new float[9];
        SensorManager.getRotationMatrixFromVector(deltaRotationMatrix, deltaRotationVector);
        // User code should concatenate the delta rotation we computed with the current rotation
        // in order to get the updated rotation.
        Matrix deltaMatrix = new Matrix();
        deltaMatrix.setValues(deltaRotationMatrix);
        rotateMatrix(rotationCurrent, deltaMatrix);
        rotationCurrent.getValues(rotationCurrentFA);
        Log.d("rotationCurrent", String.valueOf(rotationCurrent));
    }

    float[] last_values = null;
    float[] velocity = null;
    float[] position = {0,0,0};
    float[] gravity = new float[3];
    float[] linear_acceleration ={0,0,0};
    long last_timestamp = 0;
    void CalculatePosByAcc(SensorEvent event) {
        if(last_values != null){
            float dt = (event.timestamp - last_timestamp) * NS2S;

            float alpha = 0.8f;

            // Isolate the force of gravity with the low-pass filter.
            gravity[0] = alpha * gravity[0] + (1 - alpha) * event.values[0];
            gravity[1] = alpha * gravity[1] + (1 - alpha) * event.values[1];
            gravity[2] = alpha * gravity[2] + (1 - alpha) * event.values[2];

            // Remove the gravity contribution with the high-pass filter.
            linear_acceleration[0] = event.values[0] - gravity[0];
            linear_acceleration[1] = event.values[1] - gravity[1];
            linear_acceleration[2] = event.values[2] - gravity[2];

            for(int index = 0; index < 3;++index){
                velocity[index] += (linear_acceleration[index] + last_values[index])/2 * dt;
                position[index] += velocity[index] * dt;
            }
        }
        else{
            last_values = new float[3];
            velocity = new float[3];
            position = new float[3];
            velocity[0] = velocity[1] = velocity[2] = 0f;
            position[0] = position[1] = position[2] = 0f;
            System.arraycopy(event.values, 0, gravity, 0, 3);
        }
        System.arraycopy(linear_acceleration, 0, last_values, 0, 3);
        last_timestamp = event.timestamp;
    }

    void rotateMatrix(Matrix m1, Matrix m2){
        float[] f1 = new float[9], f2 = new float[9], f3 = new float[9];
        m1.getValues(f1);
        m2.getValues(f2);
        for(int i = 0; i < 3; i++){
            for (int j = 0; j < 3; j++){
                f3[i*3+j] = f1[i*3]*f2[j] + f1[i*3+1]*f2[1*3+j] + f1[i*3+2]*f2[2*3+j];
            }
        }
        m1.setValues(f3);
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {

    }

     void Matrix2Quaternion(float[] m){
         double tr = m[0] + m[1*4+1] + m[2*4+2];
        double qw,qx,qy,qz;
        double m00 = m[0], m01 = m[1], m10 = m[1*4], m11 = m[1*4+1],
                m02 = m[2], m20 = m[2*4], m22 = m[2*4+2], m21 = m[2*4+1], m12 = m[1*4+2];
         //float[] marray = m.getArray();

         if (tr > 0) {
            double S = sqrt(tr+1.0) * 2; // S=4*qw
            qw = 0.25 * S;
            qx = (m21 - m12) / S;
            qy = (m02 - m20) / S;
            qz = (m10 - m01) / S;
        } else if ((m00 > m11)&(m00 > m22)) {
            double S = sqrt(1.0 + m00 - m11 - m22) * 2; // S=4*qx
            qw = (m21 - m12) / S;
            qx = 0.25 * S;
            qy = (m01 + m10) / S;
            qz = (m02 + m20) / S;
        } else if (m11 > m22) {
             double S = sqrt(1.0 + m11 - m00 - m22) * 2; // S=4*qy
            qw = (m02 - m20) / S;
            qx = (m01 + m10) / S;
            qy = 0.25 * S;
            qz = (m12 + m21) / S;
        } else {
             double S = sqrt(1.0 + m22 - m00 - m11) * 2; // S=4*qz
            qw = (m10 - m01) / S;
            qx = (m02 + m20) / S;
            qy = (m12 + m21) / S;
            qz = 0.25 * S;
        }
        Log.d("quaternion", "qw:" + qw + "\tqx" + qx);
    }

    @Override
    public void onCreate(){
        Log.d("service", "onCreate");
        data = new float[8+9];
        acc = new float[3];
        isStop = false;
        // Create our Sensor Manager
        SM = (SensorManager) getSystemService(SENSOR_SERVICE);
        // Accelerometer Sensor
        myGyroscope = SM.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
        myRotation = SM.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR);
        myAcc = SM.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);

        // Register sensor Listener
        SM.registerListener(this, myGyroscope, SensorManager.SENSOR_DELAY_FASTEST);
        SM.registerListener(this, myRotation, SensorManager.SENSOR_DELAY_FASTEST);
        SM.registerListener(this, myAcc, SensorManager.SENSOR_DELAY_FASTEST);

        // do it in background
        if (client_socket == null) {
            try {
                client_socket = new DatagramSocket(12345);
            } catch (SocketException e) {
                e.printStackTrace();
            }
        }

        // cancel if already existed
        if(mTimer != null) {
            mTimer.cancel();
        } else {
            // recreate new
            mTimer = new Timer();
        }
        // schedule task
        mTimer.scheduleAtFixedRate(new SendTimerTask(), 0, (long)(1000 * SEND_RATE));
    }

    class SendTimerTask extends TimerTask {

        @Override
        public void run() {
            if(!isStop)
                // run on another thread
                mHandler.post(new Runnable() {

                @Override
                public void run() {
                    //new SendFloatTask().execute(data);
                    new SendStringTask().execute(jsondata);
                }

            });
        }

    }
}
