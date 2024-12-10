using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkManager : ApplicationLayer
{
    public NetClock netClock { get; set; }
    private NetDevice.Package deviceDataPkg;

    /*Available remote devices. DeviceID-NetDevice entries*/
    private ConcurrentDictionary<int, NetDevice> netDevices;

    /*Device public data*/
    public byte deviceID { get => deviceDataPkg.deviceID; set => deviceDataPkg.deviceID = value; }

    /*List of extra devices to include in the network*/
    private List<IPAddress> extraDevices;
    
    protected enum NetworkCommand
    {
        GetNodeData = 0,    //Upstream
        SyncClock = 1,      //Upstream
        NodeData = 2,       //Downstream
        AqSyncClock = 3,    //Downstream
    }

    [Serializable]
    public class NetDevice
    {
        /*Device parameters*/
        public byte ID { get; private set; }
        public IPAddress address { get; set; }
        public bool hapticsAvailable { get; private set; }
        public bool moCapAvailable { get; private set; }
        public UInt32 batteryLevel { get; private set; }
        public int synchronizedTimestamp { get; private set; } //THIS SHOULD BE PRIVATE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public byte hapticsChannel { get; private set; }
        private Package sharedData
        {
            get => new Package(this);
            //It updates all 'sharedData' except 'synchronizedTimestamp', and isSynchronized bool
            set
            {
                ID = value.deviceID;
                hapticsAvailable = (value.HapticInterfaceChannel != 0);
                if (hapticsAvailable)
                    hapticsChannel = value.HapticInterfaceChannel;
                moCapAvailable = value.MoCapAvailable == 1;
                batteryLevel = value.batteryLevel;
                isSynchronized = (synchronizedTimestamp == value.synchronizedTimestamp);
                //synchronizedTimestamp = value.synchronizedTimestamp;
            }
        }

        public int lastUpdateTimestamp { get; private set; }
        public bool isSynchronized { get; private set; }

        private const int SyncTimeout = 80; //Timeout for synchronization packages (ms)
        private static readonly int MinClockAccuracy = 10; //2-5 ms recommended in WLAN
        private bool synchronizing;
        private int syncSentTimespan;
        private int syncReceivedTimespan;

        private NetworkManager networkManager;

        public NetDevice(IPAddress address, Package receivedInfo, NetworkManager networkManager)
        {
            ID = receivedInfo.deviceID;
            this.address = address;
            this.networkManager = networkManager;
            syncSentTimespan = 0;
            syncReceivedTimespan = int.MaxValue;
            synchronizing = false;

            isSynchronized = false;
            Package newNetData = receivedInfo;
            newNetData.synchronizedTimestamp = receivedInfo.synchronizedTimestamp + 1; //Set a different sync reference.
            lastUpdateTimestamp = networkManager.netClock.GetTimeStamp();
        }

        public void StartSynchronization()
        {
            if (synchronizing && networkManager.netClock.GetTimeStamp() < (syncSentTimespan + SyncTimeout))
                return;

            syncReceivedTimespan = int.MaxValue;
            syncSentTimespan = networkManager.netClock.GetTimeStamp();
            byte[] pkg = BuildCommandPkg((byte)NetworkCommand.SyncClock, BitConverter.GetBytes(syncSentTimespan));
            networkManager.defaultIO.SendPkgTo(address, pkg);
            //Debug.Log("Sync pkg sent to '" + address.ToString() + "' at: " + syncSentTimespan);
            synchronizing = true;
        }

        public void EndSynchronization(int receivedTimestamp)
        {
            syncReceivedTimespan = networkManager.netClock.GetTimeStamp();
            if (GetOffsetAccuracy() <= MinClockAccuracy)
            {
                //Debug.Log("END!!! Timestamp received: " + receivedTimestamp + ". Diff: " + GetOffsetAccuracy());
                isSynchronized = true;
                synchronizedTimestamp = receivedTimestamp;
            }
            else
            {
                isSynchronized = false;
            }
            synchronizing = false;
        }

        public int GetOffsetAccuracy() => Mathf.Abs(syncReceivedTimespan - syncSentTimespan); //Offset measure accuracy in ms

        //public bool ConnectedWithinTimeSpan(int timeSpan) => (networkManager.GetNetClock().GetTimeStamp() - lastUpdateTimestamp) < timeSpan;

        public void UpdateData(Package pkg)
        {
            sharedData = pkg; //It updates all 'sharedData' except 'synchronizedTimestamp'
            lastUpdateTimestamp = networkManager.netClock.GetTimeStamp();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Package
        {
            public byte deviceID;
            public byte HapticInterfaceChannel;
            public byte MoCapAvailable;
            public UInt32 batteryLevel;
            public int synchronizedTimestamp;

            public Package(NetDevice device)
            {
                deviceID = device.ID;
                HapticInterfaceChannel = (byte)(device.hapticsAvailable ? 1 : 0);
                MoCapAvailable = (byte)(device.moCapAvailable ? 1 : 0);
                batteryLevel = device.batteryLevel;
                synchronizedTimestamp = device.synchronizedTimestamp;
            }

            public Package(byte[] data) : this() => this = PkgSerializer.GetStruct<Package>(data);
            public byte[] GetBytes() => PkgSerializer.GetBytes(this);
        };
    }

    public NetworkManager(int hostPort, byte deviceID, bool MoCapAvailable) : base(hostPort)
    {
        netDevices = new ConcurrentDictionary<int, NetDevice>();

        deviceDataPkg = new NetDevice.Package();
        deviceDataPkg.deviceID = deviceID;
        deviceDataPkg.MoCapAvailable = (byte)(MoCapAvailable ? 1 : 0);
        deviceDataPkg.synchronizedTimestamp = int.MaxValue;

        extraDevices = new List<IPAddress>();

        netClock = new NetClock();
        netClock.Start();
    }


    public void SynchronizeDevices()
    {
        List<NetDevice> notSyncDevices = new List<NetworkManager.NetDevice>(GetAvailableDevices().FindAll(p => !p.isSynchronized));
        foreach (NetDevice device in notSyncDevices)
            device.StartSynchronization();
    }

    public void ScanNetDevices()
    {
        //Debug.Log("(Client) Scanning...");
        byte[] pkg = BuildCommandPkg((byte)NetworkCommand.GetNodeData);

        /*Broadcast scan package*/
        defaultIO.SendPkgTo(IPAddress.Broadcast, pkg);
        defaultIO.SendPkgTo(IPAddress.Parse("10.7.255.255"), pkg);
        defaultIO.SendPkgTo(IPAddress.Parse("192.168.255.255"), pkg);
        defaultIO.SendPkgTo(IPAddress.Parse("10.42.255.255"), pkg);

        defaultIO.SendPkgTo(IPAddress.Parse("192.168.232.140"), pkg);
        //defaultIO.SendPkgTo(IPAddress.Parse("192.168.189.187"), pkg);
        //Debug.Log("Broadcast msg sent to " + IPAddress.Broadcast.ToString() + ". IP local: " + TransportLayer.GetHostLocalIP().ToString());

        /*Connect to the registered IP list*/
        foreach (IPAddress address in extraDevices)
            defaultIO.SendPkgTo(address, pkg);
    }

    /*Returns the info of the available devices*/
    public List<NetDevice> GetAvailableDevices()
    {
        List<NetDevice> retVal = new List<NetDevice>();

        foreach (KeyValuePair<int, NetDevice> entry in netDevices)
            retVal.Add(entry.Value);

        return retVal;
    }


    /* Returns the ID of the available devices which answered with a 'devInfo' package within
     * the last 'timeSpan' miliseconds*/
    public List<NetDevice> GetAvailableDevices(int timeSpan)
    {
        List<NetDevice> retVal = new List<NetDevice>();
        int currentTime = netClock.GetTimeStamp();
        foreach (KeyValuePair<int, NetDevice> entry in netDevices)
        {
            if ((currentTime - entry.Value.lastUpdateTimestamp) < timeSpan)
                retVal.Add(entry.Value);
        }
        return retVal;
    }

    public bool TryAddDevice(NetDevice newDevice)
    {
        bool retval = true;
        if (FindDevice(device => device.address.Equals(newDevice.address), out NetDevice oldDevice))
        {
            retval = netDevices.TryRemove(oldDevice.ID, out NetDevice match);
        }
        retval = netDevices.TryAdd(newDevice.ID, newDevice);
        return retval;
    }

    private bool FindDevice(Predicate<NetDevice> predicate, out NetDevice device)
    {
        foreach (KeyValuePair<int, NetDevice> entry in netDevices)
        {
            if (predicate(entry.Value))
            {
                device = entry.Value;
                return true;
            }
        }
        device = null;
        return false;
    }

    public void AddIPToScan(IPAddress address) => extraDevices.Add(address);
    public void RemoveIPFromScan(IPAddress address) => extraDevices.Remove(address);

    public override void ExecuteNetCommand(IPAddress origin, byte[] data)
    {
        //Debug.Log("OK2");
        if (origin.Equals(defaultIO.hostIP))
            return;
        //Debug.Log("(Client) Command " + data[0] +  " received from: " + origin.ToString());
        NetworkCommand command = (NetworkCommand)GetPkgCommand(data);
        int receivedTimestamp;
        switch (command)
        {
            case NetworkCommand.GetNodeData:
                defaultIO.SendPkgTo(origin, BuildCommandPkg((byte)NetworkCommand.NodeData, deviceDataPkg.GetBytes()));
                //Debug.Log("(Server) Node data requested. Info: " + devInfo.ARCamPoseAvalailable + ", " + devInfo.SkeletonPoseAvailable);
                break;
            case NetworkCommand.SyncClock:
                /*Synchronize the NetClock with the received timestamp*/
                receivedTimestamp = BitConverter.ToInt32(GetPkgData(data), 0);
                netClock.Restart(receivedTimestamp);
                deviceDataPkg.synchronizedTimestamp = receivedTimestamp;
                defaultIO.SendPkgTo(origin, BuildCommandPkg((byte)NetworkCommand.AqSyncClock, BitConverter.GetBytes(receivedTimestamp)));
                //Debug.Log("(Server) Sync Pkg Received at " + netClock.GetTimeStamp());
                break;
            case NetworkCommand.AqSyncClock:
                if (FindDevice(p => p.address.Equals(origin), out NetDevice device))
                {
                    receivedTimestamp = BitConverter.ToInt32(GetPkgData(data), 0);
                    device.EndSynchronization(receivedTimestamp);
                    if (!device.isSynchronized)
                    {
                        //Debug.Log("Not synchronized. Offset: " + device.GetOffsetAccuracy());
                        device.StartSynchronization();
                        break;
                    }
                    //Debug.Log("(Client) AT LAST!! Sync Pkg from " + device.address.ToString() + ". Offset accuracy: " + device.GetOffsetAccuracy());
                }
                break;
            case NetworkCommand.NodeData:
                NetDevice.Package newDeviceInfo = new NetDevice.Package(GetPkgData(data));
                /*Check if the device is already registered*/
                if (netDevices.TryGetValue(newDeviceInfo.deviceID, out NetDevice netDevice))
                {
                    /*Update the device's data*/
                    netDevice.UpdateData(newDeviceInfo);
                    if (!netDevice.address.Equals(origin))
                        netDevice.address = origin;
                }
                else
                {
                    /*Register a new device*/
                    NetDevice newDevice = new NetDevice(origin, newDeviceInfo, this);
                    if (!TryAddDevice(newDevice))
                        Debug.Log("(Client) Could not add new device");
                    //Debug.Log("New device " + newDeviceInfo.synchronizedTimestamp + ", " + newDevice.isSynchronized);
                }
                break;
        }
    }


}
