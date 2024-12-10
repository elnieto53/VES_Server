using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;
using static VirtualScenarioManager;

public abstract class ScenarioElement
{
    private SortedDictionary<int, byte[]> updateBuffer; /*Buffer of incoming update packages*/
    private SortedDictionary<int, byte[]> qosOutputBuffer; /*Buffer of qos-modeled update packages*/
    public ChannelManager channelManager { get; protected private set; } /*Propietary channel*/
    private QoSManager qosManager { get => channelManager.GetQoSManager(); } /*QoS manager reference*/
    private protected NetClock netClock { get => channelManager.netClock; } /*NetClock reference*/
    public byte Channel { get => channelManager.channel; } /*Propietary channel number*/
    public GameObject go { get; protected private set; } = null; /*element's gameobject*/
    public ulong elementID { get; protected private set; } /*Element's ID*/
    public byte prefabID { get; protected private set; } /*GameObject prefab ID*/
    public IPAddress origin { get; protected private set; } /*Address of the hosting device*/
    public bool IsHost { get; private set; }

    public Action<int, int> updateCallback { get; set; } = null;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        public ulong elementID;
        public byte prefabID;
        public int timeStamp;

        public Header(ulong elementID, byte prefabID, int timeStamp)
        {
            this.prefabID = prefabID;
            this.timeStamp = timeStamp;
            this.elementID = elementID;
        }

        public Header(byte[] data) : this() => this = PkgSerializer.GetStruct<Header>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public ScenarioElement()
    {
        updateBuffer = new SortedDictionary<int, byte[]>();
        qosOutputBuffer = new SortedDictionary<int, byte[]>();
    }

    private protected byte[] BuildPkg(byte[] data)
    {
        byte[] header = new Header(elementID, prefabID, netClock.GetTimeStamp()).GetBytes();
        byte[] pkg = new byte[header.Length + data.Length];

        Buffer.BlockCopy(header, 0, pkg, 0, header.Length);
        Buffer.BlockCopy(data, 0, pkg, header.Length, data.Length);

        return pkg;
    }

    private static byte[] GetUpdateData(byte[] pkg)
    {
        int headerSize = Marshal.SizeOf(typeof(Header));
        if (pkg.Length <= headerSize)
            return null;
        byte[] retVal = new byte[pkg.Length - headerSize];
        Array.Copy(pkg, headerSize, retVal, 0, retVal.Length);
        return retVal;
    }

    public virtual void Init(ulong elementID, byte prefabID, IPAddress origin, ChannelManager channelManager)
    {
        this.origin = origin;
        this.prefabID = prefabID;
        this.elementID = elementID;
        this.channelManager = channelManager;
        IsHost = origin.Equals(TransportLayer.GetHostLocalIP());
        LoadPrefab(prefabID);
    }

    public virtual void Init(byte[] data, IPAddress origin, ChannelManager channelManager)
    {
        Header header = new Header(data);
        Init(header.elementID, header.prefabID, origin, channelManager);
        Config(GetUpdateData(data)); /*Update the element data*/
    }

    public void DeInit()
    {
        if (go != null)
            UnityEngine.Object.Destroy(go);
    }

    public void Update()
    {
        qosManager.UpdateScenarioElement(this, updateBuffer, qosOutputBuffer);
    }

    public void AddUpdateToBuffer(byte[] data)
    {
        Header aux = new Header(data); /*BEWARE: this gets the header from the package*/
        if (updateBuffer.ContainsKey(aux.timeStamp))
        {
            Debug.Log("Consecutive updates with the same timestamp (" + Channel + ", " + aux.elementID + ", " + aux.timeStamp + ").");
            return;
        }
        
        updateBuffer.Add(aux.timeStamp, GetUpdateData(data));
    }

    private protected abstract void LoadPrefab(byte prefabID);

    public abstract bool UpdateAvailable(out byte[] data);

    public abstract void Config(byte[] data);
}