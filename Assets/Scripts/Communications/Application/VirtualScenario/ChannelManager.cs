using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

public partial class VirtualScenarioManager
{
    public abstract class ChannelManager
    {
        public byte channel { get; private set; }
        public NetClock netClock { get => vsManager.networkManager.netClock; }

        protected private ConcurrentQueue<(byte[], IPAddress)> remoteScenarioUpdates;
        protected private List<IPAddress> subscribedDevices;
        protected private List<ChannelManager> multicastChannels;
        
        protected VirtualScenarioManager vsManager;
        protected private QoSManager qosManager;
        

        private void Init(VirtualScenarioManager manager, byte channel) {
            remoteScenarioUpdates = new ConcurrentQueue<(byte[], IPAddress)>();
            subscribedDevices = new List<IPAddress>();
            vsManager = manager;
            this.channel = channel;
            multicastChannels = new List<ChannelManager>();
        }

        public ChannelManager(VirtualScenarioManager manager, byte channel) {
            Init(manager, channel);
            qosManager = new QoSManager(netClock);
        }

        public void SubscribeNewDevice(IPAddress deviceAddress)
        {
            Monitor.Enter(subscribedDevices);
            if (!subscribedDevices.Any(p => p.Equals(deviceAddress)))
            {
                Debug.Log("Device added to channel " + channel + ": " + deviceAddress.ToString());
                subscribedDevices.Add(deviceAddress);
            }
            Monitor.Exit(subscribedDevices);
        }

        public void UnsubscribeDevice(IPAddress deviceAddress)
        {
            Monitor.Enter(subscribedDevices);
            if (subscribedDevices.Any(p => p.Equals(deviceAddress)))
            {
                subscribedDevices.Remove(deviceAddress);
            }
            Monitor.Exit(subscribedDevices);
        }

        public void UnsubscribeAllDevices()
        {
            Monitor.Enter(subscribedDevices);
            subscribedDevices.Clear();
            Monitor.Exit(subscribedDevices);
        }

        public void AddUpdate(byte[] data, IPAddress origin)
        {
            if (multicastChannels.Count > 0)
            {
                foreach (ChannelManager channelManager in multicastChannels) { channelManager.AddUpdate(data, origin); }
            }
            remoteScenarioUpdates.Enqueue((data, origin));
        }

        public void SubscribeTo(IPAddress deviceAddress) => vsManager.SendCommand(ChannelCommands.Subscribe, channel, deviceAddress);

        public void UnsubscribeFrom(IPAddress deviceAddress) => vsManager.SendCommand(ChannelCommands.Unsubscribe, channel, deviceAddress);

        public QoSManager GetQoSManager() => qosManager;

        public void SetQoSManager(QoSManager qosManager) => this.qosManager = qosManager;

        public VirtualScenarioManager GetVirtualScenarioManager() => vsManager;

        /* If this channelManager includes 'multicast' channelManagers, then all host elements are
         * also updated in the 'multicast' channelManagers*/
        public void AddMulticast(List<ChannelManager> channels) => multicastChannels.AddRange(channels);

        public void AddMulticast(ChannelManager channel) => multicastChannels.Add(channel);

        public abstract void UpdateRemoteScenario();
        public abstract void UpdateHostScenario();
    }


    public class ChannelManager<T> : ChannelManager where T : ScenarioElement, new()
    {
        private List<T> remoteScenarioElements;
        private List<T> hostScenarioElements;
        public int HostElementsCount { get { return hostScenarioElements.Count; } }

        public delegate void newRemoteCallback(T obj);
        private newRemoteCallback remoteCallback = null;

        public ChannelManager(VirtualScenarioManager manager, byte channel) : base(manager, channel)
        {
            remoteScenarioElements = new List<T>();
            hostScenarioElements = new List<T>();
        }

        public ChannelManager(VirtualScenarioManager manager, byte channel, newRemoteCallback remoteCallback) : this(manager, channel)
        {
            this.remoteCallback = remoteCallback;
        }

        public T AddHostScenarioElement(byte prefabID)
        {
            Monitor.Enter(hostScenarioElements);
            /*Creates the new element and adds it to the list*/
            T newElement = new T();
            newElement.Init(GenerateID(), prefabID, vsManager.defaultIO.hostIP, this);
            hostScenarioElements.Add(newElement);
            //remoteCallback?.Invoke(newElement);
            Monitor.Exit(hostScenarioElements);

            return newElement;
        }

        public bool DestroyHostScenarioElement(T element)
        {
            element.DeInit();
            return hostScenarioElements.Remove(element);
        }

        public void DestroyRemoteScenarioElements(IPAddress address)
        {
            T currentElement;
            Monitor.Enter(remoteScenarioElements);
            while ((currentElement = remoteScenarioElements.Find(p => p.origin.Equals(address))) != null)
            {
                currentElement.DeInit();
                remoteScenarioElements.Remove(currentElement);
                //GC.SuppressFinalize(currentElement);    /*BEWARE!!!!!!!*/
            }
            Monitor.Exit(remoteScenarioElements);
        }

        public bool GetScenarioElements(out List<T> elements, Predicate<T> predicate)
        {
            elements = remoteScenarioElements.FindAll(predicate);
            elements.AddRange(hostScenarioElements.FindAll(predicate));
            return elements.Count > 0;
        }

        public ulong GenerateID() => (ulong)vsManager.defaultIO.hostIP.GetHashCode() + (ulong)hostScenarioElements.Count << 32;
        public IList<T> GetRemoteElements() => remoteScenarioElements.AsReadOnly();
        public IList<T> GetHostElements() => hostScenarioElements.AsReadOnly();

        public override void UpdateRemoteScenario()
        {
            int availableData = remoteScenarioUpdates.Count;

            /*Dequeue all received data*/
            while (availableData-- > 0)
            {
                /*Dequeue the received data and the IP origin*/
                if (remoteScenarioUpdates.TryDequeue(out (byte[] data, IPAddress origin) update))
                {
                    /*Check if the corresponding ID already exists*/
                    ulong ID = new ScenarioElement.Header(update.data).elementID;
                    T element = remoteScenarioElements.Find(p => p.elementID.Equals(ID));
                    if (element == null)
                    {
                        /*If it does not exist, create it*/
                        element = new T();
                        element.Init(update.data, update.origin, this);
                        remoteCallback?.Invoke(element);  /*It is used for further initialization of the new remote element outside ChannelManager*/
                        Monitor.Enter(remoteScenarioElements);
                        remoteScenarioElements.Add(element);
                        Monitor.Exit(remoteScenarioElements);
                    }
                    else
                    {
                        /*If it exists, add this data update to the corresponding buffer*/
                        element.AddUpdateToBuffer(update.data);
                    }
                }
            }

            /*Update ScenarioElement data*/
            foreach (T activeElement in remoteScenarioElements)
            {
                activeElement.Update();
            }
        }

        public override void UpdateHostScenario()
        {
            /*If there are no subscribers, no info is sent*/
            if (subscribedDevices.Count == 0 && multicastChannels.Count == 0)
                return;

            Monitor.Enter(hostScenarioElements);
            foreach (T activeElement in hostScenarioElements)
            {
                if (activeElement.UpdateAvailable(out byte[] data))
                {
                    //Debug.Log("Element Id: " + header.elementID + ", prefab ID: " + header.prefabID + ", timestamp " + header.timeStamp);
                    Monitor.Enter(subscribedDevices);
                    foreach (IPAddress address in subscribedDevices)
                    {
                        //Debug.Log("Updating subscriptor " + address.ToString() + " data.");
                        vsManager.SendCommand(ChannelCommands.ElementUpdate, channel, address, data);
                        //recordManager.RecordHostUpdate(activeElement.elementID, data, vsManager.netClock.GetTimeStamp());
                    }
                    Monitor.Exit(subscribedDevices);

                    if (multicastChannels.Count > 0)
                    {
                        foreach (ChannelManager channelManager in multicastChannels)
                        {
                            channelManager.AddUpdate(data, vsManager.defaultIO.hostIP);
                            //recordManager.RecordHostUpdate(activeElement.elementID, data, vsManager.netClock.GetTimeStamp());
                        }
                    }
                }
            }
            Monitor.Exit(hostScenarioElements);
        }
    }
}

