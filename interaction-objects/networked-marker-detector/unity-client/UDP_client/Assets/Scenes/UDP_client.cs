using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

public class UDP_client : MonoBehaviour
{
    public GameObject Sphere1;
    public GameObject Cube1;
    public GameObject Cylinder1;
    public String host = "localhost";
    public Int32 port = 52275;

    internal Boolean socket_ready = false;
    TcpClient tcp_socket;
    //NetworkStream net_stream;

    //StreamReader socket_reader;
    void Start()
    {
        Sphere1.SetActive(false);
        Cube1.SetActive(false);
        Cylinder1.SetActive(false);
    }

    void Update()
    {
        List<int> received_data = new List<int>(readSocket());


        if (received_data != null)
        {
            // Do something with the received data,
            // print it in the log for now
            Debug.Log(received_data);

            //for (int i = 0; i < 8; i++)
            //{
            //    if (!received_data.Contains(i)) {
            //        i.SetActive(false);
            //    }
            //    else {
            //        i.SetActive(true);
            //    }
            //}

            // Decode String into Array / Int Values
            var condition1 = new List<int>() { 0, 1, 2 };
            if (received_data.Equals(condition1)){
                Sphere1.SetActive(true);
                Cube1.SetActive(true);
                Cylinder1.SetActive(true);

                //Set Colors
            }
            if (received_data.Contains(0))
            {
                Sphere1.SetActive(true);
            }
            if (received_data.Contains(1))
            {
                Cube1.SetActive(true);
            }
            if (received_data.Contains(2))
            {
                Cylinder1.SetActive(true);
            }
        }

        if (doCloseDebug)
        {
            closeSocket();
            this.gameObject.SetActive(false);
            Sphere1.SetActive(false);
            Cube1.SetActive(false);
            Cylinder1.SetActive(false);
        }
    }

    void Awake()
    {
        setupSocket();
    }

    void OnApplicationQuit()
    {
        closeSocket();
    }

    public void setupSocket()
    {
        //try
        //{
            tcp_socket = new TcpClient(host, port);
            tcp_socket.Client.Blocking = false;
            //socket_reader = new StreamReader(net_stream);

            socket_ready = true;
        //}
        //catch (Exception e)
        //{
        //    // Something went wrong
        //    Debug.Log("Socket error: " + e);
        //    Debug.Log(e.StackTrace.ToString());
        //}
    }

    byte[] received_bytes = new byte[1024];
    String decoded;

    private static void Print(int s)
    {
        Debug.Log(s);
    }

    public List<int> readSocket()
    {
        if (!socket_ready)
            return new List<int>(null);

        //Debug.Log("Data available: " + net_stream.DataAvailable);

        if (tcp_socket.Available > 0)
        {
            int size = tcp_socket.Client.Receive(received_bytes);
            int cursor = 0;


            int payloadSize = received_bytes[0] | (received_bytes[1] << 8);
            Debug.Log("SIZE: " + size);

            while (size > cursor + 2 + payloadSize)
            {
                cursor += payloadSize;
                cursor += 2;
                payloadSize = received_bytes[cursor] | (received_bytes[cursor + 1] << 8);
            }

            Debug.Log("PAYLOAD SIZE = " + payloadSize);
            Debug.Log("Cursor : " + cursor);


            Debug.Log("<color=green>{</color>");

            //StringBuilder sb = new StringBuilder();
            List<int> detected = new List<int>();
            for (int i = cursor+2; i < cursor + payloadSize+2; i += 1)
            {
                detected.Add(received_bytes[i]);
            }
            detected.ForEach(Print);
            Debug.Log("<color=green>}</color>");

            //decoded = Encoding.ASCII.GetString(received_bytes, 0, size);
            return detected;
        }
        return new List<int>(null);
    }

    public bool doCloseDebug = false;
    public void closeSocket()
    {
        if (!socket_ready)
            return;

        tcp_socket.Close();
        socket_ready = false;
    }
}