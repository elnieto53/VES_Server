using System;
using System.Collections.Generic;
using System.Net;

public partial class VirtualScenarioManager : ApplicationLayer
{
    private Dictionary<byte, ChannelManager> activeChannels;
    public NetworkManager networkManager { get; set; }

    public enum ChannelCommands
    {
        Subscribe = 0,
        Unsubscribe = 1,
        ElementUpdate = 2,
    }

    public VirtualScenarioManager(int hostPort, int hostNetPort, byte deviceID, bool MoCapAvailable) : base(hostPort)
    {
        activeChannels = new Dictionary<byte, ChannelManager>();
        networkManager = new NetworkManager(hostNetPort, deviceID, MoCapAvailable);
    }

    public override void Init()
    {
        networkManager.Init();
        base.Init();
    }

    public override void DeInit()
    {
        networkManager.DeInit();
        base.DeInit();
    }

    public NetworkManager GetNetworkManager() => networkManager;

    public bool AddChannel<T>(byte channel, ChannelManager<T>.newRemoteCallback callback, out ChannelManager<T> channelManager) where T : ScenarioElement, new()
    {
        if (activeChannels.ContainsKey(channel))
        {
            channelManager = null;
            return false;
        }
        channelManager = new ChannelManager<T>(this, channel, callback);
        activeChannels.Add(channel, channelManager);
        return true;
    }

    public bool AddChannel<T>(byte channel, out ChannelManager<T> channelManager) where T : ScenarioElement, new()
    {
        return AddChannel(channel, null, out channelManager);
    }

    public bool TryGetChannelManager(byte channel, out ChannelManager channelManager) => activeChannels.TryGetValue(channel, out channelManager);
    public bool TryGetChannelManager<T>(byte channel, out ChannelManager<T> channelManager) where T : ScenarioElement, new()
    {
        channelManager = null;
        if (!activeChannels.TryGetValue(channel, out ChannelManager generalChannelManager))
            return false;
        bool retval;
        switch (generalChannelManager)
        {
            case ChannelManager<T> c:
                channelManager = c;
                retval = true;
                break;
            default:
                retval = false;
                channelManager = null;
                break;
        }
        return retval;
    }

    public void RemoveChannel(byte channel) => activeChannels.Remove(channel);

    public void SendCommand(ChannelCommands command, byte channel, IPAddress address)
    {
        byte[] data = new byte[] { channel };
        defaultIO.SendPkgTo(address, BuildCommandPkg((byte)command, data));
    }

    public void SendCommand(ChannelCommands command, byte channel, IPAddress address, byte[] data)
    {
        byte[] txData = new byte[1 + data.Length];

        txData[0] = channel;
        System.Buffer.BlockCopy(data, 0, txData, 1, data.Length);
        defaultIO.SendPkgTo(address, BuildCommandPkg((byte)command, txData));
    }


    private void UpdateRemoteElements()
    {
        foreach (ChannelManager channelManager in activeChannels.Values)
        {
            channelManager.UpdateRemoteScenario();
        }
    }

    private void UpdateHostElements()
    {
        foreach (ChannelManager channelManager in activeChannels.Values)
        {
            channelManager.UpdateHostScenario();
        }
    }

    public void Update()
    {
        UpdateHostElements();
        UpdateRemoteElements();
    }

    private static byte GetPkgChannel(byte[] data) => data[0];

    private static byte[] GetChannelData(byte[] data)
    {
        if (data.Length <= 1)
            return null;
        byte[] retVal = new byte[data.Length - 1];
        Array.Copy(data, 1, retVal, 0, retVal.Length);
        return retVal;
    }

    public override void ExecuteNetCommand(IPAddress origin, byte[] data)
    {
        ChannelCommands command = (ChannelCommands)GetPkgCommand(data);
        byte[] channelPkg = GetPkgData(data);
        byte channel = GetPkgChannel(channelPkg);
        //Debug.Log("(Virtual Scenario Manager) " + this.defaultIO.GetHostEP().Port + " Command " + data[0] + ". Channel: " + channel + " received from: " + origin.ToString());
        if (activeChannels.TryGetValue(channel, out ChannelManager channelManager))
        {
            switch (command)
            {
                case ChannelCommands.ElementUpdate:
                    channelManager.AddUpdate(GetChannelData(channelPkg), origin);
                    break;
                case ChannelCommands.Subscribe:
                    channelManager.SubscribeNewDevice(origin);
                    break;
                case ChannelCommands.Unsubscribe:
                    channelManager.UnsubscribeDevice(origin);
                    break;
            }
        }
    }
}
