package com.example.test1;

import android.os.AsyncTask;
import android.util.Log;

import java.io.IOException;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.SocketException;
import java.net.UnknownHostException;
import java.nio.ByteBuffer;
import java.nio.FloatBuffer;

public class SendStringTask extends AsyncTask<String, Void, Boolean> {
    public DatagramSocket getClient_socket() {
        return client_socket;
    }

    public void setClient_socket(DatagramSocket client_socket) {
        this.client_socket = client_socket;
    }

    DatagramSocket client_socket;
    InetAddress IPAddress;

    public static byte[] encode(String str) {
        byte[] b = str.getBytes();
        return b;
    }

    @Override
    protected Boolean doInBackground(String... str) {

        try {
            if (client_socket == null) {
                Log.d("execute", "client_socket=null");
                client_socket = BGService.client_socket;
            }
            IPAddress = MainActivity.IPAddress;
            DatagramPacket send_packet = new DatagramPacket(encode(str[0]), str[0].length(), IPAddress, 12345);
            client_socket.send(send_packet);
        } catch (UnknownHostException e) {
            e.printStackTrace();
        } catch (SocketException e) {
            e.printStackTrace();
        } catch (IOException e) {
            e.printStackTrace();
        }
        return true;

    }
}
