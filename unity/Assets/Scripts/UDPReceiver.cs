using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    public int PORT = 12345;
    public string HOST = "216.165.71.223";

    bool active;

    public int last_time= 0;

    UdpClient client;
    Thread pthread;

    IPEndPoint ep;

    public Predictor predictor;

    // Start is called before the first frame update
    void Start()
    {
        active = true;

        // create socket and initiate other parameters
        ep = new IPEndPoint(IPAddress.Parse(HOST), PORT);
        client = new UdpClient();
        client.ExclusiveAddressUse = false;
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Debug.Log("opening connection...");
        Debug.Log(ep.Address.ToString() + ", " + ep.Port);
        client.Client.Bind(ep);

        // start async receive
        pthread = new Thread(DataListen);
        pthread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    class UdpState : System.Object
    {
        public UdpState(IPEndPoint e, UdpClient c) { this.e = e; this.c = c; }
        public IPEndPoint e;
        public UdpClient c;
    }

    private void OnApplicationQuit()
    {
        active = false;
    }

    void DataListen()
    {
        UdpState state = new UdpState(ep, client);
        client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
        while (active)
        {
            Thread.Sleep(100);
        }
        // active is set to false when the application terminates, then this thread can cleanup
        Debug.Log("closing connection...");
        client.Close();
    }

    void HandleGyroData(Byte[] buf)
    {
        // we flip endianness here
        for (int i = 0; i < buf.Length; i+= 4)
        {
            byte[] cpy = new Byte[4];
            Buffer.BlockCopy(buf, i, cpy, 0, 4);
            Array.Reverse(cpy);
            Buffer.BlockCopy(cpy, 0, buf, i, 4);
        }
        float gyrox = BitConverter.ToSingle(buf, 0);
        float gyroy = BitConverter.ToSingle(buf, 4);
        float gyroz = BitConverter.ToSingle(buf, 8);
        float time = BitConverter.ToSingle(buf, 28);
        //Debug.Log(string.Format("gyro data: {0} - ({1}, {2}, {3})", time, gyrox, gyroy, gyroz));
        predictor.PredictByGyro(time, new Vector3(gyrox, gyroy, gyroz));
    }

    void ReceiveCallback(IAsyncResult ar)
    {
        if (!active)
        {
            return;
        }
        UdpClient c = (UdpClient)((UdpState)(ar.AsyncState)).c;
        IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
        // get data
        Byte[] recvbuf = c.EndReceive(ar, ref e);
        // parse data
        HandleGyroData(recvbuf);
        // loop the callback
        UdpState state = new UdpState(e, c);
        c.BeginReceive(new AsyncCallback(ReceiveCallback), state);
    }
    
}
