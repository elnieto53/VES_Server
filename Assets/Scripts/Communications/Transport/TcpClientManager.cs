using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class TcpClientManager : TransportLayer
{
    Socket tcpSocket;
    private Semaphore tryToConnect;

    //RX thread constants
    private const int TCP_RX_BUF_LENGTH = 256;

    public TcpClientManager(int remotePort, int hostPort) : base(remotePort, hostPort)
    {

    }

    public override void Init()
    {
        base.Init();

        tryToConnect = new Semaphore(0, 1);
        tcpSocket = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    }

    public void Connect()
    {
        if (!tcpSocket.Connected)
            tryToConnect.Release();
    }

    public void Disconnect()
    {
        if (tcpSocket.Connected)
        {
            tcpSocket.Shutdown(SocketShutdown.Both);
            tcpSocket.Close();
        }
    }

    public override void ReceiverTask()
    {
        byte[] pkg;
        int length;
        byte[] rxBuf = new Byte[TCP_RX_BUF_LENGTH];

        while (true)
        {
            while (tcpSocket.Connected)
            {
                length = tcpSocket.Receive(rxBuf);
                pkg = new byte[length];
                Array.Copy(rxBuf, pkg, length);
                rxQueue.Enqueue(pkg);
            }
        }
    }

    public bool IsConnected()
    {
        return tcpSocket.Connected;
    }

    public override void SenderTask()
    {
        byte[] txBuf;
        while (true)
        {
            while (!tcpSocket.Connected)
            {
                tryToConnect.WaitOne();
                tcpSocket.Connect(remoteEP);
            }

            while (tcpSocket.Connected)
            {
                txBuf = txQueue.Take().Item1;
                tcpSocket.Send(txBuf);
            }
        }
    }

    public override void DeInit()
    {
        base.DeInit();
        tcpSocket.Close();
    }
}
