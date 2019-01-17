package com.example.test1;

import android.app.Service;
import android.content.Intent;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Handler;
import android.os.IBinder;
import android.util.Log;

import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketException;
import java.util.Timer;
import java.util.TimerTask;

import androidx.annotation.Nullable;

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
            }
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {

    }

    @Override
    public void onCreate(){
        Log.d("service", "onCreate");
        data = new float[7];
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
