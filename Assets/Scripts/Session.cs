using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using static VirtualScenarioManager;

public partial class Session
{
    public static readonly int SCAN_PERIOD = 1000;
    public static readonly int STAY_ALIVE_TIMEOUT = SCAN_PERIOD * 2;
    public static readonly byte DEVICE_ID = 10;

    /*Shared Virtual Scenario*/
    public VirtualScenarioManager virtualScenario;
    public MoCapManager moCap;
    public Body body;
    public ChannelManager<MoCapPose> poseMoCapChannel;
    public ChannelManager<MoCapOrientation> orientationMoCapChannel;

    /*MoCap QoS Configuration*/
    public QoSManager moCapQoSManager;
    public string qosRecordingFilePath;

    /*Detection areas added in runtime. These are linked to a specific BodyPart*/
    public List<(Transform anchor, ProjectedVAS projectedVAS)> pvasDetectionAreas;
    public List<(Transform anchor, EnvelopingVAS envelopingVAS)> evasDetectionAreas;
    public List<(Transform anchor, ThevOICe thevOICe)> thevOICeDetectionAreas;

    /*Local environment*/
    public GameObject environmentLoaded = null;
    public List<UnityEngine.Object> environments;

    public NetworkManager networkManager { get => virtualScenario.GetNetworkManager(); }

    [Serializable]
    public class InitData
    {
        public int netPort;                 /*Host Port of "Network Manager"*/
        public int virtualScenarioPort;     /*Host Port of "Virtual Scenario Manager"*/
        public int moCapPort;               /*Host Port of "Motion Capture Manager"*/
        public byte poseMoCapChannel;       /*MoCap Channel for sharing data among all MoCap devices*/
        public byte orientationMoCapChannel;
        public byte moCapBodyID;            /*ID of the body used in this session.*/
        public List<string> scenarioPaths;

        public InitData() => scenarioPaths = new List<string>();
        public void AddScenarioPaths(params string[] filePaths) => scenarioPaths.AddRange(filePaths);
    }

    public Session(InitData initData)
    {
        virtualScenario = new VirtualScenarioManager(initData.virtualScenarioPort, initData.netPort, DEVICE_ID, true);
        virtualScenario.Init();

        /*Initialize the motion capture system, and create a new Body*/
        moCap = new MoCapManager(initData.moCapPort, virtualScenario, initData.poseMoCapChannel, initData.orientationMoCapChannel);
        moCap.Init();
        body = moCap.CreateBody(initData.moCapBodyID);
        poseMoCapChannel = moCap.GetPoseChannelManager();
        orientationMoCapChannel = moCap.GetOrientationChannelManager();

        /*These list will contain the detection areas generated in runtime (which are linked to BodyParts correspoding to 'pvas' and 'evas' bodies) */
        pvasDetectionAreas = new List<(Transform anchor, ProjectedVAS projectedVAS)>();
        evasDetectionAreas = new List<(Transform anchor, EnvelopingVAS envelopingVAS)>();
        thevOICeDetectionAreas = new List<(Transform anchor, ThevOICe thevOICe)>();

        /*Share the QoSManager among all MoCap channels*/
        moCapQoSManager = orientationMoCapChannel.GetQoSManager();
        poseMoCapChannel.SetQoSManager(moCapQoSManager);

        /*Initialize the listener and attach it to the user's head*/
        GameObject listener = new GameObject("Listener");
        listener.AddComponent<AudioListener>();
        //listener.AddComponent<ResonanceAudioListener>(); //Can be removed if ResonanceAudio is not available
        BodyPart head = body.GetBodyPart(MoCapElement.Spawnable.Head);
        if (head != null)
        {
            listener.transform.SetParent(head.prefab.transform, false);
        }

        /*Load selected environments*/
        environments = LoadObjects(initData.scenarioPaths);

        qosRecordingFilePath = "";

        //Devices to include which are not automatically detected (e.g. not in LAN)
        //In this case, devices which discard broadcast packages are added manually.
        byte[] devAddress = TransportLayer.GetHostLocalIP().GetAddressBytes();
        devAddress[3] = 1;
        virtualScenario.networkManager.AddIPToScan(new IPAddress(devAddress));
        virtualScenario.networkManager.AddIPToScan(new IPAddress(new byte[] { 192, 168, 1, 23 }));
        virtualScenario.networkManager.AddIPToScan(new IPAddress(new byte[] { 192, 168, 2, 81 }));
        virtualScenario.networkManager.AddIPToScan(new IPAddress(new byte[] { 192, 168, 30, 180 }));
    }

    public void Update()
    {
        virtualScenario.Update();
        moCap.UpdateBodyPose();
    }

    public void DeInit()
    {
        virtualScenario.GetNetworkManager().DeInit();
        virtualScenario.DeInit();
        moCap.DeInit();
    }

    public void ScanNetwork() => virtualScenario.GetNetworkManager().ScanNetDevices();

    public List<NetworkManager.NetDevice> GetAvailableNetDevices()
        => virtualScenario.GetNetworkManager().GetAvailableDevices(STAY_ALIVE_TIMEOUT);

    /*Configuration*/

    public void ClearSSD()
    {
        /*Delete all SSD*/
        while (pvasDetectionAreas.Count > 0)
        {
            GameObject.Destroy(pvasDetectionAreas[0].projectedVAS.gameObject);
            pvasDetectionAreas.RemoveAt(0);
        }

        while (evasDetectionAreas.Count > 0)
        {
            GameObject.Destroy(evasDetectionAreas[0].envelopingVAS.gameObject);
            evasDetectionAreas.RemoveAt(0);
        }

        while (thevOICeDetectionAreas.Count > 0)
        {
            GameObject.Destroy(thevOICeDetectionAreas[0].thevOICe.gameObject);
            thevOICeDetectionAreas.RemoveAt(0);
        }
    }

    public void SetEnvironment(string newEnvironmentName)
    {
        UnityEngine.Object newEnvironment = environments.Find(p => p.name.Equals(newEnvironmentName));
        if (newEnvironment != null)
        {
            if (environmentLoaded != null)
                GameObject.Destroy(environmentLoaded);
            environmentLoaded = GameObject.Instantiate((GameObject)newEnvironment);
            environmentLoaded.name = newEnvironment.name;
            environmentLoaded.GetComponent<EnvironmentManager>().Init();
            environmentLoaded.transform.Find("Camera").gameObject.SetActive(true);
        }
    }

    public void SetEnableSSD(bool enable)
    {
        List<ISSD> allSSD = new List<ISSD>();
        allSSD.AddRange(pvasDetectionAreas.Select(o => o.projectedVAS));
        allSSD.AddRange(evasDetectionAreas.Select(o => o.envelopingVAS));
        allSSD.AddRange(thevOICeDetectionAreas.Select(o => o.thevOICe)); 

        foreach(ISSD ssd in allSSD)
            ssd.ssdEnabled = enable;
    }

    public Configuration GetConfiguration() => new Configuration(this);

    public void SetConfiguration(Configuration configuration)
    {
        if (configuration == null)
            return;
        /*For now the initData is not modified*/

        /*Environment configuration*/
        SetEnvironment(configuration.environment);

        /*Sensory substitution configuration*/
        ClearSSD();
        foreach (Configuration.DetectionArea<ProjectedVAS.Configuration> entry in configuration.pvasConfig)
        {
            Transform newParent = body.GetSensorAnchor(entry.sensorAnchorID);
            /*Init the sensory substitution module, which includes its detection area*/
            ProjectedVAS newProjectedVAS = new GameObject("ProjectedVAS").AddComponent<ProjectedVAS>();
            newProjectedVAS.Init();
            newProjectedVAS.AdoptMe(newParent);
            newProjectedVAS.SetConfig(entry.configuration);
            newProjectedVAS.ssdEnabled = true;
            /*Register the new sensory substitution module*/
            pvasDetectionAreas.Add((newParent, newProjectedVAS));
            //pvasDetectionAreas.Add()
        }

        List<NetworkManager.NetDevice> availableHapticDevices = GetAvailableNetDevices().FindAll(p => p.hapticsAvailable);

        foreach (Configuration.DetectionArea<EnvelopingVAS.Configuration> entry in configuration.evasConfig)
        {
            Transform newParent = body.GetSensorAnchor(entry.sensorAnchorID);

            /*Init the sensory substitution module, which includes its detection area*/
            ChannelManager<HapticStimuli> currentChannelManager;

            NetworkManager.NetDevice hapticDevice = availableHapticDevices.Find(p => p.hapticsChannel == entry.configuration.channel);
            if (hapticDevice == null)
                continue;

            if (!virtualScenario.TryGetChannelManager(entry.configuration.channel, out currentChannelManager))
                virtualScenario.AddChannel(entry.configuration.channel, out currentChannelManager);

            EnvelopingVAS newEnvelopingVAS = new GameObject("EnvelopingVAS").AddComponent<EnvelopingVAS>();
            /*Subscribe the device to the correspoding haptic stimuli channel*/
            currentChannelManager.SubscribeNewDevice(hapticDevice.address);
            newEnvelopingVAS.Init(currentChannelManager);
            newEnvelopingVAS.AdoptMe(newParent);
            newEnvelopingVAS.SetConfig(entry.configuration);
            newEnvelopingVAS.ssdEnabled = true;
            /*Register the new sensory substitution module*/
            evasDetectionAreas.Add((newParent, newEnvelopingVAS));
        }

        /*For now ThevOICe is not included*/

        /*Set the MoCap QoS Configuration*/
        moCapQoSManager.SetConfiguration(configuration.moCapQoSConfiguration);

        //moCapQoSManager
    }

    private static List<UnityEngine.Object> LoadObjects(List<string> filePaths)
    {
        List<UnityEngine.Object> objects = new List<UnityEngine.Object>();

        foreach (string filePath in filePaths)
        {
            UnityEngine.Object[] folderObjects = Resources.LoadAll(filePath);
            objects.AddRange(folderObjects);
        }
        return objects;
    }
}
