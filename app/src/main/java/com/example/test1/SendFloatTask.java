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

public class SendFloatTask extends AsyncTask<float[], Void, Boolean> {
    public DatagramSocket getClient_socket() {
        return client_socket;
    }

    public void setClient_socket(DatagramSocket client_socket) {
        this.client_socket = client_socket;
    }

    DatagramSocket client_socket;
    InetAddress IPAddress;

    public static byte[] encode(float floatArray[]) {
        byte byteArray[] = new byte[floatArray.length * 4];
// wrap the byte array to the byte buffer
        ByteBuffer byteBuf = ByteBuffer.wrap(byteArray);
// create a view of the byte buffer as a float buffer
        FloatBuffer floatBuf = byteBuf.asFloatBuffer();
// now put the float array to the float buffer,
// it is actually stored to the byte array
        floatBuf.put(floatArray);
        return byteArray;
    }

    @Override
    protected Boolean doInBackground(float[]... floats) {

        try {
            if (client_socket == null) {
                Log.d("execute", "client_socket=null");
                client_socket = BGService.client_socket;
            }
            IPAddress = MainActivity.IPAddress;
            DatagramPacket send_packet = new DatagramPacket(encode(floats[0]), floats[0].length * 4, IPAddress, 12345);
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
