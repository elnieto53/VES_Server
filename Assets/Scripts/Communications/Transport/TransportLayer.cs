using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

public abstract class TransportLayer
{
    public delegate void PkgReceivedCallback(IPAddress origin, byte[] data);

    public readonly IPAddress hostIP;
    public int port { get => hostEP.Port; }

    protected IPEndPoint remoteEP { get; set; }
    protected IPEndPoint hostEP { get; private set; }

    protected private Thread rxThread;
    protected Thread txThread;
    protected ConcurrentQueue<byte[]> rxQueue;
    protected BlockingCollection<Tuple<byte[], IPEndPoint>> txQueue;

    protected PkgReceivedCallback interruptCallback = null;

    public TransportLayer(int remotePort, int hostPort)
    {
        hostIP = GetHostLocalIP();
        remoteEP = new IPEndPoint(IPAddress.Broadcast, remotePort);
        hostEP = new IPEndPoint(hostIP, hostPort);
    }

    public TransportLayer(int remotePort, int hostPort, PkgReceivedCallback interruptCallback) : this(remotePort, hostPort)
    {
        this.interruptCallback = interruptCallback;
    }

    public static IPAddress GetHostLocalIP()
    {
        IPAddress[] hostAddresses = GetHostIPv4Addresses();
        if (hostAddresses.Length < 1)
            throw new Exception("There is no valid host IPv4 address");
        return hostAddresses[0];
    }

    public static IPAddress[] GetHostIPv4Addresses()
    {
        //Note: a VPN host address can be differentiated as it does not have an associated PCI connection
        IPAddress[] hostAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
        //return hostAddresses.First(p => p.GetAddressBytes()[0] == 10) ?? hostAddresses[0];
        return Array.FindAll(hostAddresses, p => p.AddressFamily == AddressFamily.InterNetwork && p.GetAddressBytes()[0] == 10); // Escoge la primera ip cuyo primer valor sea 10: 10.X.X.X
        //return Array.FindAll(hostAddresses, p => p.AddressFamily == AddressFamily.InterNetwork && p.GetAddressBytes()[2] == 16); // Escoge la primera ip cuyo tercer valor sea 189: X.X.189.X
        //return Array.FindAll(hostAddresses, p => p.AddressFamily == AddressFamily.InterNetwork);
    }

    public virtual void Init()
    {
        rxQueue = new ConcurrentQueue<byte[]>();
        txQueue = new BlockingCollection<Tuple<byte[], IPEndPoint>>();

        rxThread = new Thread(ReceiverTask);
        txThread = new Thread(SenderTask);
        rxThread.Priority = ThreadPriority.AboveNormal;
        txThread.Priority = ThreadPriority.AboveNormal;

        rxThread.Start();
        txThread.Start();
    }

    public virtual void DeInit()
    {
        rxThread.Abort();
        txThread.Abort();
    }

    public void SendPkgTo(IPEndPoint address, byte[] pkg) => txQueue.Add(new Tuple<byte[], IPEndPoint>(pkg, address));

    public void SendPkgTo(IPAddress address, byte[] pkg) => txQueue.Add(new Tuple<byte[], IPEndPoint>(pkg, new IPEndPoint(address, remoteEP.Port)));


    /*Only when no callback has been declared*/
    public bool ReceivePkg(ref byte[] pkg) => rxQueue.Count > 0 && rxQueue.TryDequeue(out pkg);

    public abstract void SenderTask();

    public abstract void ReceiverTask();

}