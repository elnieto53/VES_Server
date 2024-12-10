using System;
using System.Net;


public abstract class ApplicationLayer
{
    protected UdpManager defaultIO;
    public int hostPort { get => defaultIO.port; }

    public ApplicationLayer(int hostPort)
    {
        defaultIO = new UdpManager(hostPort, hostPort, ExecuteNetCommand);
    }

    public virtual void Init()
    {
        defaultIO.Init();
    }

    public virtual void DeInit()
    {
        defaultIO.DeInit();
    }

    protected static byte[] BuildCommandPkg(byte command, byte[] data)
    {
        byte[] retVal = new byte[1 + data.Length];

        retVal[0] = command;
        System.Buffer.BlockCopy(data, 0, retVal, 1, data.Length);

        return retVal;
    }

    protected static byte[] BuildCommandPkg(byte command)
    {
        return new byte[1] { command };
    }

    protected static int GetPkgCommand(byte[] pkg)
    {
        return pkg[0];
    }

    protected static byte[] GetPkgData(byte[] pkg)
    {
        if (pkg.Length <= 1)
            return null;
        byte[] retVal = new byte[pkg.Length - 1];
        Array.Copy(pkg, 1, retVal, 0, retVal.Length);
        return retVal;
    }

    /// <summary> Udp callback to be implemented by child classes. It has no access to Unity API. </summary>
    public abstract void ExecuteNetCommand(IPAddress origin, byte[] data);
}
