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


public class MainActivity extends AppCompatActivity implements SensorEventListener, CompoundButton.OnCheckedChangeListener {

    private TextView duration, curT, fps, gyroX, gyroY, gyroZ, rotX, rotY, rotZ, rotW;
    private Sensor myGyroscope, myRotation;
    private SensorManager SM;
    private double timestamp;// in ms

    private TextView ipaddress, tvPort;
    private Switch udpSwitch;

    public static float[] gyros, rot;

    public static InetAddress IPAddress;
    public static int sendPort;
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
        myRotation = SM.getDefaultSensor(Sensor.TYPE_ROTATION_VECTOR);

        // Register sensor Listener
        //SM.registerListener(this, myAccelerometer, SensorManager.SENSOR_DELAY_NORMAL);
        SM.registerListener(this, myGyroscope, SensorManager.SENSOR_DELAY_FASTEST);
        SM.registerListener(this, myRotation, SensorManager.SENSOR_DELAY_FASTEST);


        // Assign TextView
        duration = (TextView) findViewById(id.duration);
        curT = (TextView) findViewById(id.curT);
        fps = (TextView) findViewById(id.fps);
        gyroX = (TextView) findViewById(id.gyroX);
        gyroY = (TextView) findViewById(id.gyroY);
        gyroZ = (TextView) findViewById(id.gyroZ);
        rotX = (TextView) findViewById(id.rotX);
        rotY = (TextView) findViewById(id.rotY);
        rotZ = (TextView) findViewById(id.rotZ);
        rotW = (TextView) findViewById(id.rotW);

        ipaddress = (TextView) findViewById(id.ipaddress);
        ipaddress.setText("172.24.71.214");
        ipaddress.addTextChangedListener(
                new TextWatcher() {
                    @Override
                    public void beforeTextChanged(CharSequence s, int start, int count, int after) { }
                    @Override
                    public void onTextChanged(CharSequence s, int start, int before, int count) { }
                    @Override
                    public void afterTextChanged(Editable editable) {
                        try {
                            Log.d("ip",editable.toString());
                            IPAddress = InetAddress.getByName(editable.toString());
                        } catch (UnknownHostException e) {
                            e.printStackTrace();
                        }
                    }
                }
        );

        tvPort = (TextView) findViewById(id.port);
        tvPort.setText("12345");
        tvPort.addTextChangedListener(new TextWatcher() {
            @Override
            public void beforeTextChanged(CharSequence s, int start, int count, int after) { }
            @Override
            public void onTextChanged(CharSequence s, int start, int before, int count) { }
            @Override
            public void afterTextChanged(Editable editable) {
                try {
                    Log.d("port",editable.toString());
                    sendPort = Integer.parseInt(editable.toString());
                }catch(NumberFormatException nfe){
                    System.out.println("NumberFormatException: " + nfe.getMessage());
                }
            }
        });

        udpSwitch = findViewById(id.udpSwitch);
        udpSwitch.setOnCheckedChangeListener(this);

        gyros = new float[3];
        rot = new float[4];
        bgService = new Intent(this, BGService.class);

        try {
            IPAddress = InetAddress.getByName(ipaddress.getText().toString());
            sendPort = Integer.parseInt(tvPort.getText().toString());
        } catch (UnknownHostException e) {
            e.printStackTrace();
        }
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        if (event.sensor.getType() == Sensor.TYPE_GYROSCOPE) {
            gyroX.setText("DX: " + event.values[0]);
            gyroY.setText("DY: " + event.values[1]);
            gyroZ.setText("DZ: " + event.values[2]);
            gyros[0] = event.values[0];
            gyros[1] = event.values[1];
            gyros[2] = event.values[2];
        } else if (event.sensor.getType() == Sensor.TYPE_ROTATION_VECTOR) {
            rotX.setText("QX: " + event.values[0]);
            rotY.setText("QY: " + event.values[1]);
            rotZ.setText("QZ: " + event.values[2]);
            rotW.setText("QW: " + event.values[3]);
            rot[0] = event.values[0];
            rot[1] = event.values[1];
            rot[2] = event.values[2];
            rot[3] = event.values[3];
        }

        duration.setText("duration: " + ((double) event.timestamp / 1000000000 - timestamp) + "ms");
        fps.setText("fps: " + 1/((double) event.timestamp / 1000000000 - timestamp));
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
