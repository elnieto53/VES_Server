using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TcpServerManager
{
    Socket tcpListener;
    Socket tcpSocket;
    IPEndPoint hostEndPoint;

    private ConcurrentQueue<byte[]> rxQueue;
    private BlockingCollection<byte[]> txQueue;

    private Thread rxThread;
    private Thread txThread;

    //RX thread constants
    private const int TCP_RX_BUF_LENGTH = 256;

    public TcpServerManager(int hostPort)
    {
        hostEndPoint = new IPEndPoint(TransportLayer.GetHostLocalIP(), hostPort);
        rxQueue = new ConcurrentQueue<byte[]>();
        txQueue = new BlockingCollection<byte[]>();
    }

    public virtual void Init()
    {
        tcpListener = new Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        rxQueue = new ConcurrentQueue<byte[]>();
        txQueue = new BlockingCollection<byte[]>();

        rxThread = new Thread(ReceiverTask);
        txThread = new Thread(SenderTask);
        rxThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        txThread.Priority = System.Threading.ThreadPriority.AboveNormal;

        rxThread.Start();
        txThread.Start();
    }


    public bool SendPkg(byte[] pkg)
    {
        txQueue.Add(pkg);
        return true;
    }

    public bool ReceivePkg(ref byte[] pkg) => rxQueue.Count > 0 && rxQueue.TryDequeue(out pkg);

    public void ReceiverTask()
    {
        byte[] pkg;
        int length;
        byte[] rxBuf = new byte[TCP_RX_BUF_LENGTH];

        Debug.Log("Starting TCP server...");
        tcpListener.Bind(hostEndPoint);
        tcpListener.Listen(1);
        tcpSocket = tcpListener.Accept();
        Debug.Log("Connected");

        while (true)
        {
            // This call blocks. 
            length = tcpSocket.Receive(rxBuf);
            if(length == 0)
            {
                tcpSocket.Disconnect(false);
                Debug.Log("Disconnected");
                tcpSocket = tcpListener.Accept();
                Debug.Log("Connected");
            }
            pkg = new byte[length];
            Array.Copy(rxBuf, pkg, length);
            rxQueue.Enqueue(pkg);
            Debug.Log("Package received of length: " + length);
            SendPkg(pkg);
        }
    }

    public void SenderTask()
    {
        while (true)
        {
            byte[] pkg = txQueue.Take();
            if (tcpSocket != null && tcpSocket.Connected)
                tcpSocket.Send(pkg, 0, pkg.Length, SocketFlags.None);
            //Debug.Log("Packet sent to IP " + pkg.Item2.ToString());
        }
    }

    public void DeInit()
    {
        rxThread.Abort();
        txThread.Abort();
        if (tcpListener != null)
            tcpListener.Close();
        if (tcpSocket != null)
            tcpSocket.Close();
    }
}
