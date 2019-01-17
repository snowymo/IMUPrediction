package com.example.test1;

import android.app.Service;
import android.content.Intent;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.IBinder;
import android.util.Log;

import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketException;

import androidx.annotation.Nullable;

public class BGService extends Service implements SensorEventListener {

    private Sensor myGyroscope;
    private SensorManager SM;
    private double timestamp;// in ms

    public static InetAddress IPAddress;
    public static DatagramSocket client_socket;

    boolean isStop;

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
            new SendTask().execute(event.values);
            Log.d("service", "onSensorChanged");
        }
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {

    }

    @Override
    public void onCreate(){
        Log.d("service", "onCreate");
        isStop = false;
        // Create our Sensor Manager
        SM = (SensorManager) getSystemService(SENSOR_SERVICE);
        // Accelerometer Sensor
        myGyroscope = SM.getDefaultSensor(Sensor.TYPE_GYROSCOPE);

        // Register sensor Listener
        SM.registerListener(this, myGyroscope, SensorManager.SENSOR_DELAY_FASTEST);
        if (client_socket == null) {
            try {
                client_socket = new DatagramSocket(12345);
            } catch (SocketException e) {
                e.printStackTrace();
            }
        }
    }
}
