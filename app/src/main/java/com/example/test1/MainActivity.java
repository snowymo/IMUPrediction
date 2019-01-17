package com.example.test1;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;

import android.hardware.Sensor;
import android.hardware.SensorManager;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.text.Editable;
import android.text.TextWatcher;
import android.util.Log;
import android.widget.CompoundButton;
import android.widget.Switch;
import android.widget.TextView;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketException;
import java.net.UnknownHostException;
import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.TimeZone;
import java.util.concurrent.TimeUnit;

import java.nio.*;

import static com.example.test1.R.*;


public class MainActivity extends AppCompatActivity implements SensorEventListener, TextWatcher, CompoundButton.OnCheckedChangeListener {

    private TextView duration, curT, gyroX, gyroY, gyroZ;
    private Sensor myGyroscope;
    private SensorManager SM;
    private double timestamp;// in ms

    private TextView ipaddress;
    private Switch udpSwitch;

    public static float[] gyros;

    public static InetAddress IPAddress;
    Intent bgService;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(layout.activity_main);

        // Create our Sensor Manager
        SM = (SensorManager) getSystemService(SENSOR_SERVICE);

        // Accelerometer Sensor
        //myAccelerometer = SM.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
        myGyroscope = SM.getDefaultSensor(Sensor.TYPE_GYROSCOPE);

        // Register sensor Listener
        //SM.registerListener(this, myAccelerometer, SensorManager.SENSOR_DELAY_NORMAL);
        SM.registerListener(this, myGyroscope, SensorManager.SENSOR_DELAY_FASTEST);

        // Assign TextView
        duration = (TextView) findViewById(id.duration);
        curT = (TextView) findViewById(id.curT);
        gyroX = (TextView) findViewById(id.gyroX);
        gyroY = (TextView) findViewById(id.gyroY);
        gyroZ = (TextView) findViewById(id.gyroZ);

        ipaddress = (TextView) findViewById(id.ipaddress);
        ipaddress.setText("216.165.71.242");
        ipaddress.addTextChangedListener(this);

        udpSwitch = findViewById(id.udpSwitch);
        udpSwitch.setOnCheckedChangeListener(this);

        gyros = new float[3];
        bgService = new Intent(this, BGService.class);

        try {
            IPAddress = InetAddress.getByName(ipaddress.getText().toString());
        } catch (UnknownHostException e) {
            e.printStackTrace();
        }

    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        gyroX.setText("X: " + event.values[0]);
        gyroY.setText("Y: " + event.values[1]);
        gyroZ.setText("Z: " + event.values[2]);
        gyros[0] = event.values[0];
        gyros[1] = event.values[1];
        gyros[2] = event.values[2];

        duration.setText("duration: " + ((double) event.timestamp / 1000000000 - timestamp) + "ms");
        timestamp = (double) event.timestamp / 1000000000;

        showCurrentDateTime(event.timestamp);

        //if(udpSwitch.isChecked())
        //new SendTask().execute(event.values);
    }

    private void showCurrentDateTime(long ts) {
        // visualize the current date, not really necessary
        String target = "1904/01/01 12:00 AM";  // Your given date string
        long millis = TimeUnit.MILLISECONDS.convert(ts, TimeUnit.NANOSECONDS);
        DateFormat formatter = new SimpleDateFormat("yyyy/MM/dd hh:mm aaa");
        formatter.setTimeZone(TimeZone.getTimeZone("UTC"));
        Date date = null;
        try {
            date = formatter.parse(target);
        } catch (ParseException e) {
            e.printStackTrace();
        }
        long newTimeInmillis = date.getTime() + millis;
        Date date2 = new Date(newTimeInmillis);
        curT.setText("now: " + date2);
    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {

    }

    @Override
    protected void onStop() {
//        if (client_socket != null)
//            client_socket.close();

        super.onStop();
        getDelegate().onStop();
    }

    @Override
    public void beforeTextChanged(CharSequence s, int start, int count, int after) {

    }

    @Override
    public void onTextChanged(CharSequence s, int start, int before, int count) {

    }

    @Override
    public void afterTextChanged(Editable s) {
        try {
            Log.d("ip",s.toString());
            IPAddress = InetAddress.getByName(s.toString());
        } catch (UnknownHostException e) {
            e.printStackTrace();
        }
    }

    @Override
    public void onCheckedChanged(CompoundButton buttonView, boolean isChecked) {
        if(isChecked){
            // sending
            Log.d("switch","startService");
            startService(bgService );
            //startService(new Intent(String.valueOf(BGService.class)));
        }
        else{
            // close connection
            Log.d("switch","stopService");
            stopService(bgService );
        }
    }
}
