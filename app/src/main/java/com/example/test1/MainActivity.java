package com.example.test1;

import androidx.appcompat.app.AppCompatActivity;

import android.os.Bundle;

import android.hardware.Sensor;
import android.hardware.SensorManager;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
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


public class MainActivity extends AppCompatActivity implements SensorEventListener{

    private TextView duration, curT, gyroX, gyroY, gyroZ;
    private Sensor myAccelerometer, myGyroscope;
    private SensorManager SM;
    private double timestamp;// in ms

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        // Create our Sensor Manager
        SM = (SensorManager)getSystemService(SENSOR_SERVICE);

        // Accelerometer Sensor
        myAccelerometer = SM.getDefaultSensor(Sensor.TYPE_ACCELEROMETER);
        myGyroscope = SM.getDefaultSensor(Sensor.TYPE_GYROSCOPE);


        // Register sensor Listener
        //SM.registerListener(this, myAccelerometer, SensorManager.SENSOR_DELAY_NORMAL);
        SM.registerListener(this, myGyroscope, SensorManager.SENSOR_DELAY_FASTEST);

        // Assign TextView
        duration = (TextView)findViewById(R.id.duration);
        curT = (TextView)findViewById(R.id.curT);
        gyroX = (TextView)findViewById(R.id.gyroX);
        gyroY = (TextView)findViewById(R.id.gyroY);
        gyroZ = (TextView)findViewById(R.id.gyroZ);


    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        gyroX.setText("X: " + event.values[0]);
        gyroY.setText("Y: " + event.values[1]);
        gyroZ.setText("Z: " + event.values[2]);
        duration.setText("duration: " + ((double)event.timestamp / 1000000000-timestamp) + "ms");
        timestamp = (double)event.timestamp / 1000000000;

       showCurrentDateTime(event.timestamp);

        try {
            sendUDP2(event.values);
        } catch (IOException e) {
            e.printStackTrace();
        }
    }

    private void showCurrentDateTime(long ts){
        // visualize the current date, not really necessary
        String target = "1904/01/01 12:00 AM";  // Your given date string
        long millis = TimeUnit.MILLISECONDS.convert(ts , TimeUnit.NANOSECONDS);
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
        if(client_socket != null)
            client_socket.close();
        super.onStop();
        getDelegate().onStop();
    }

    private void sendUDP(){
        try {
            if(client_socket == null){
                client_socket = new DatagramSocket(12345);
                IPAddress =  InetAddress.getByName("216.165.71.242");
            }
        } catch (SocketException e) {
            e.printStackTrace();
        } catch (UnknownHostException e) {
            e.printStackTrace();
        }

        String messageStr="Hello Android!";
        int server_port = 12345;
        try {
            DatagramSocket s = new DatagramSocket();
            InetAddress local = InetAddress.getByName("216.165.71.242");
            int msg_length=messageStr.length();
            byte[] message = messageStr.getBytes();
            DatagramPacket p = new DatagramPacket(message, msg_length,local,server_port);
            s.send(p);
        } catch (SocketException e) {
            e.printStackTrace();
        } catch (UnknownHostException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }

    }
    DatagramSocket client_socket;
    InetAddress IPAddress;
    private void sendUDP2(float[] gyros) throws IOException {

        if(client_socket == null){
            client_socket = new DatagramSocket(12345);
            IPAddress =  InetAddress.getByName("216.165.71.242");
        }
        //String messageStr="Hello Android!";
        //while (true)
        // {
        //byte[] send_data = messageStr.getBytes();
        //System.out.println("send UPD 2");

        DatagramPacket send_packet = new DatagramPacket(encode(gyros),gyros.length*4, IPAddress, 12345);
        client_socket.send(send_packet);
    }

    public static byte[] encode (float floatArray[]) {
        byte byteArray[] = new byte[floatArray.length*4];

// wrap the byte array to the byte buffer
        ByteBuffer byteBuf = ByteBuffer.wrap(byteArray);

// create a view of the byte buffer as a float buffer
        FloatBuffer floatBuf = byteBuf.asFloatBuffer();

// now put the float array to the float buffer,
// it is actually stored to the byte array
        floatBuf.put (floatArray);

        return byteArray;
    }
}
