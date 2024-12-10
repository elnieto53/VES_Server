using System;
using System.Net;
using System.Net.Sockets;

public class UdpManager : TransportLayer
{
    private Socket udpSocket_rx;
    private Socket udpSocket_tx;

    //RX thread constants
    private const int UDP_RX_BUF_LENGTH = 1024;

    public UdpManager(int remotePort, int hostPort) : base(remotePort, hostPort) { }

    public UdpManager(int remotePort, int hostPort, PkgReceivedCallback interruptCallback) : base(remotePort, hostPort, interruptCallback) { }

    public override void Init()
    {
        udpSocket_rx = new Socket(remoteEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        udpSocket_tx = new Socket(remoteEP.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        udpSocket_rx.EnableBroadcast = true;
        udpSocket_rx.MulticastLoopback = true;
        udpSocket_tx.EnableBroadcast = true;
        udpSocket_tx.MulticastLoopback = true;

        base.Init();
    }

    public override void ReceiverTask()
    {
        byte[] pkg;
        // Creates an IpEndPoint to capture the identity of the sending host.
        EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);

        // Binding is required with ReceiveFrom calls.
        udpSocket_rx.Bind(hostEP);

        int length;
        byte[] rxBuf = new byte[UDP_RX_BUF_LENGTH];
        while (true)
        {
            // This call blocks. 
            length = udpSocket_rx.ReceiveFrom(rxBuf, SocketFlags.None, ref senderRemote);

            pkg = new byte[length];
            Array.Copy(rxBuf, pkg, length);
            if (interruptCallback != null)
            {
                interruptCallback(((IPEndPoint)senderRemote).Address, pkg);
            }
            else
            {
                rxQueue.Enqueue(pkg);
            }
        }
    }

    public override void SenderTask()
    {
        while (true)
        {
            Tuple<byte[], IPEndPoint> pkg = txQueue.Take();
            udpSocket_tx.SendTo(pkg.Item1, 0, pkg.Item1.Length, SocketFlags.None, pkg.Item2);
        }
    }

    public override void DeInit()
    {
        base.DeInit();
        udpSocket_rx.Close();
        udpSocket_tx.Close();
    }

}
