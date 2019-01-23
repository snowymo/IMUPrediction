package com.example.test1;

import android.app.Service;
import android.content.Intent;
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

import static java.lang.Math.sqrt;

public class BGService extends Service implements SensorEventListener {

    private Sensor myGyroscope, myRotation;
    private SensorManager SM;
    private double timestamp;// in ms

    private static final double SEND_RATE = 1.0 / 100.0;

    private float[] data;

    public static InetAddress IPAddress;
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

        if(!isStop) {
            if (event.sensor.getType() == Sensor.TYPE_GYROSCOPE) {
                data[0] = event.values[0];
                data[1] = event.values[1];
                data[2] = event.values[2];
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
        }
        data[7] = (float) event.timestamp / 1000000000;
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
        data = new float[8];
        isStop = false;
        // Create our Sensor Manager
        SM = (SensorManager) getSystemService(SENSOR_SERVICE);
        // Accelerometer Sensor
        myGyroscope = SM.getDefaultSensor(Sensor.TYPE_GYROSCOPE);
        myRotation = SM.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR);

        // Register sensor Listener
        SM.registerListener(this, myGyroscope, SensorManager.SENSOR_DELAY_FASTEST);
        SM.registerListener(this, myRotation, SensorManager.SENSOR_DELAY_FASTEST);
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
                    new SendTask().execute(data);
                }

            });
        }

    }
}
