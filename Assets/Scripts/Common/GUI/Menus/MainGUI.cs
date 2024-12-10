using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Linq;
using static VirtualScenarioManager;

public class MainGUI : MonoBehaviour
{
    private Session session;
    private Camera userCamera;

    private Button subscribeButton;
    private Button unsubscribeButton;
    private Button calibratePoseButton;
    private Button calibrateIMUOffsetButton;
    private Button resetPositionAndScaleButton;
    private Button switchCameraButton;
    private Toggle masterEnableToggle;
    private Button loadConfigButton;
    private Button saveConfigButton;
    private Button pvasConfigButton;
    private Button evasConfigButton;
    private Button thevOICeConfigButton;
    private Button jitterConfigButton;
    private Button heatMapButton;
    private Dropdown scenariosDropdown;
    private Button startRecordingButton;
    private Button highlightRecordingButton;
    private Button stopRecordingButton;
    private InputField fileNameInputField;
    private TimerGUI timerGUI;

    /*Submenus*/
    private GameObject devicesPanel;
    private GameObject loadTestConfigPanel;
    private GameObject pvasPanel;
    private GameObject evasPanel;
    private GameObject thevOICePanel;
    private GameObject qosPanel;
    private GameObject heatmapPanel;

    /*User camera selection*/
    private static List<Transform> userCameraLocations;
    private static int iUserCameraLocation;

    /*Data recording*/
    private BodyMotionSerializer moCapSerializer;
    private JsonSerializer<DataListContainer<int>> timestampReferences;
    private string testConfigFilePath;

    public void Init(Session session)
    {
        this.session = session;

        subscribeButton = transform.Find("MoCap/SubscribeButton").GetComponent<Button>();
        unsubscribeButton = transform.Find("MoCap/UnsubscribeButton").GetComponent<Button>();
        calibratePoseButton = transform.Find("MoCap/CalibrateMoCapButton").GetComponent<Button>();
        calibrateIMUOffsetButton = transform.Find("MoCap/CalibrateIMUOffsetButton").GetComponent<Button>();
        resetPositionAndScaleButton = transform.Find("MoCap/ResetOriginButton").GetComponent<Button>();
        switchCameraButton = transform.Find("MoCap/SwitchCameraButton").GetComponent<Button>();
        masterEnableToggle = transform.Find("HMI/MasterEnableToggle").GetComponent<Toggle>();
        pvasConfigButton = transform.Find("HMI/PvasConfigurationButton").GetComponent<Button>();
        evasConfigButton = transform.Find("HMI/EvasConfigurationButton").GetComponent<Button>();
        thevOICeConfigButton = transform.Find("HMI/ThevOICeConfigurationButton").GetComponent<Button>();
        jitterConfigButton = transform.Find("HMI/JitterConfigurationButton").GetComponent<Button>();
        heatMapButton = transform.Find("HMI/HeatMapButton").GetComponent<Button>();
        loadConfigButton = transform.Find("Test/LoadButton").GetComponent<Button>();
        saveConfigButton = transform.Find("Test/SaveButton").GetComponent<Button>();
        scenariosDropdown = transform.Find("Test/EnvironmentDropdown").GetComponent<Dropdown>();
        startRecordingButton = transform.Find("Test/StartRecordingButton").GetComponent<Button>();
        highlightRecordingButton = transform.Find("Test/HighlightNextRecordingButton").GetComponent<Button>();
        stopRecordingButton = transform.Find("Test/StopRecordingButton").GetComponent<Button>();
        fileNameInputField = transform.Find("Test/FileNameInputField").GetComponent<InputField>();
        userCamera = transform.Find("UserCamera").GetComponent<Camera>();
        timerGUI = transform.Find("TimerPanel").GetComponent<TimerGUI>();

        subscribeButton.onClick.AddListener(OnClickSubscribeMoCap);
        unsubscribeButton.onClick.AddListener(OnClickUnsubscribeMoCap);
        calibratePoseButton.onClick.AddListener(OnClickCalibrateMoCapPose);
        calibrateIMUOffsetButton.onClick.AddListener(delegate { LoadDoubleCheckMenu(OnClickCalibrateIMUOffset); });
        loadConfigButton.onClick.AddListener(OnClickLoadTestConfig);
        saveConfigButton.onClick.AddListener(OnClickSaveTestConfig);
        pvasConfigButton.onClick.AddListener(OnClickLoadPvasConfig);
        evasConfigButton.onClick.AddListener(OnClickLoadEvasConfig);
        thevOICeConfigButton.onClick.AddListener(OnClickLoadThevOICeConfig);
        resetPositionAndScaleButton.onClick.AddListener(OnClickResetPositionAndScale);
        jitterConfigButton.onClick.AddListener(OnClickLoadJitterConfig);
        startRecordingButton.onClick.AddListener(OnClickStartRecording);
        highlightRecordingButton.onClick.AddListener(OnClickHighlightNextRecording);
        stopRecordingButton.onClick.AddListener(OnClickStopRecording);
        switchCameraButton.onClick.AddListener(OnClickSwitchUserCamera);
        heatMapButton.onClick.AddListener(OnClickLoadHeatMapMenu);
        masterEnableToggle.onValueChanged.AddListener(OnClickSetEnableAllSSD);

        userCamera.enabled = false;

        /*Load the scenarios' dropdown*/
        List<string> scenarioNames = new List<string>();
        scenarioNames.AddRange(session.environments.Select(o => o.name));
        scenariosDropdown.ClearOptions();
        scenariosDropdown.AddOptions(scenarioNames);
        scenariosDropdown.onValueChanged.AddListener(delegate { UpdateScenarioDropdown(scenariosDropdown.value); });

        /*Load the default scenario 'Construct'*/
        int defaultScenarioIndex = scenarioNames.FindIndex(p => p.Equals("Construct"));
        if (defaultScenarioIndex >= 0)
            UpdateScenarioDropdown(defaultScenarioIndex);
        scenariosDropdown.value = defaultScenarioIndex;

        highlightRecordingButton.interactable = false;
        stopRecordingButton.interactable = false;

        /*Instantiate and init the 'devices' GUI panel*/
        devicesPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.devices), transform.parent);
        devicesPanel.GetComponent<DevicesGUI>().Init(session.networkManager);

        /*Get the list of user camera locations*/
        userCameraLocations = new List<Transform>();
        List<BodyPart> bodyPartsWithCamera = session.body.GetBodyParts(bodyPart => bodyPart.hasCameraPivot);
        if (bodyPartsWithCamera.Count > 0)
        {
            userCameraLocations.AddRange(bodyPartsWithCamera.Select(bodyPart => bodyPart.prefab.cameraAnchor));
            userCamera.transform.SetParent(bodyPartsWithCamera[iUserCameraLocation].prefab.GetPivot(MoCapPrefab.SubPrefabIndex.cameraAnchor), false);
        }

        moCapSerializer = new BodyMotionSerializer(session.body, session.networkManager.netClock);
        RecordingManager.Add(moCapSerializer);
        timestampReferences = new JsonSerializer<DataListContainer<int>>();

        testConfigFilePath = ResourcesPath.Files.testConfig + "/defaultConfig.json";
        //bodyPoseSerializer = new List<(BodyPart, JsonStreamSerializer<Pose>)>();
    }

    private void UpdateScenarioDropdown(int value) => session.SetEnvironment(session.environments[value].name);

    public void OnClickSubscribeMoCap()
    {
        List<NetworkManager.NetDevice> list = session.GetAvailableNetDevices();
        Debug.Log("(Client) Subscribing...");
        foreach (NetworkManager.NetDevice device in list)
        {
            session.poseMoCapChannel.SubscribeTo(device.address);
            session.orientationMoCapChannel.SubscribeTo(device.address);
            //session.proximityChannel.SubscribeTo(device.address);
        }
    }

    public void OnClickUnsubscribeMoCap()
    {
        List<NetworkManager.NetDevice> list = session.GetAvailableNetDevices();

        Debug.Log("(Client) Unsubscribing...");
        foreach (NetworkManager.NetDevice device in list)
        {
            session.poseMoCapChannel.UnsubscribeFrom(device.address);
            session.orientationMoCapChannel.UnsubscribeFrom(device.address);
            //session.proximityChannel.UnsubscribeFrom(device.address);
        }
    }

    public void OnClickCalibrateMoCapPose() => session.moCap.CalibrateBody(session.body.ID);

    public void OnClickCalibrateIMUOffset()
    {
        List<NetworkManager.NetDevice> list = session.GetAvailableNetDevices();

        foreach (NetworkManager.NetDevice device in list)
        {
            session.moCap.CalibrateIMUOffset(device.address);
        }
    }

    public void OnClickResetPositionAndScale()
    {
        session.moCap.ResetRootTracking(session.body.ID, new Vector3(0, Body.averageHeight, 0), Vector2.up);
        /*In future versions the user height will be used to scale the Body object*/
    }

    public void OnClickSwitchUserCamera()
    {
        if (userCameraLocations.Count == 0)
            return;

        if (!userCamera.enabled)
        {
            userCamera.enabled = true;
            iUserCameraLocation = 0;
        }
        else
        {
            if (++iUserCameraLocation >= userCameraLocations.Count)
            {
                userCamera.enabled = false;
                return;
            }
        }

        userCamera.transform.SetParent(userCameraLocations[iUserCameraLocation], false);
    }

    public void OnClickSetEnableAllSSD(bool enable) => session.SetEnableSSD(enable);

    public void OnClickLoadPvasConfig()
    {
        if (pvasPanel != null)
            return;
        pvasPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.pvas), transform.parent);
        pvasPanel.GetComponent<PvasGUI>().Init(session);
    }

    public void OnClickLoadEvasConfig()
    {
        if (evasPanel != null)
            return;
        evasPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.evas), transform.parent);
        //Debug.Log("Session available haptics: " + session.availableHaptics.Count);
        evasPanel.GetComponent<EvasGUI>().Init(session);
    }

    public void OnClickLoadThevOICeConfig()
    {
        if (thevOICePanel != null)
            return;
        thevOICePanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.thevOICe), transform.parent);
        thevOICePanel.GetComponent<ThevOICeGUI>().Init(session);
    }

    public void OnClickLoadJitterConfig()
    {
        if (qosPanel != null)
            return;
        qosPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.qos), transform.parent);
        qosPanel.GetComponent<QoSGUI>().Init(session);
    }

    public void OnClickLoadHeatMapMenu()
    {
        if (heatmapPanel != null)
            return;
        heatmapPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.stimuliHeatmap), transform);
        StimuliHeatmapGUI gui = heatmapPanel.GetComponent<StimuliHeatmapGUI>();
        gui.Init(session);
    }

    private void LoadTestConfig(string path)
    {
        testConfigFilePath = path;
        session.SetConfiguration(Session.Configuration.LoadDataFromFile(path));
        scenariosDropdown.SetValueWithoutNotify(session.environments.FindIndex(p => p.name.Equals(session.environmentLoaded.name)));
    }

    public void OnClickLoadTestConfig()
    {
        if (loadTestConfigPanel != null)
            return;
        loadTestConfigPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.fileExplorer), transform.parent);
        loadTestConfigPanel.GetComponent<FileExplorerGUI>().Init(ResourcesPath.Files.testConfig, LoadTestConfig, "*.json");
    }

    public void OnClickSaveTestConfig() => session.GetConfiguration().SaveDataToFile(testConfigFilePath, true);

    public void OnClickStartRecording()
    {
        /*Start recording raw update data packages with the "generated" and "updated" timestamps*/
        //session.poseMoCapChannel.GetRecordManager().StartRecording();
        /*Start recording the position and rotation of each bodypart (main mocap channel, no jitter)*/
        //moCapSerializer.StartRecording();
        RecordingManager.StartRecording();

        /*Reset the QoSManager*/
        session.moCapQoSManager.ResetRecording();

        startRecordingButton.interactable = false;
        highlightRecordingButton.interactable = true;
        stopRecordingButton.interactable = true;

        timerGUI.PlayTimer();
    }

    public void OnClickHighlightNextRecording()
    {
        /*Save the NetClock timestamp*/
        timestampReferences.data.list.Add(session.networkManager.netClock.GetTimeStamp());
        Debug.Log("Data added: " + timestampReferences.data.list[timestampReferences.data.list.Count - 1] + ". Total data: " + timestampReferences.data.list.Count);
    }

    public void OnClickStopRecording()
    {
        /*Stop recording and save all gathered data*/
        if (fileNameInputField.text.Equals(""))
            fileNameInputField.text = "No Name";
        string folderPath = ResourcesPath.Files.motionData + "/" + fileNameInputField.text;

        /*Save MoCap data*/
        //moCapSerializer.StopRecording();
        RecordingManager.StopRecording();
        RecordingManager.SaveRecording(folderPath, false);
        //moCapSerializer.SaveMotionToFile("Body Motion", folderPath, false, delegate{ Debug.Log("Data saved"); } );
        /*Save highlighted timestamps*/
        timestampReferences.SaveDataToFile("Highlighted timestamps", folderPath, false);
        /*Save session data*/
        JsonSerializer<Session.Configuration> sessionSerializer = new JsonSerializer<Session.Configuration>(new Session.Configuration(session));
        sessionSerializer.SaveDataToFile("Session", folderPath, false);

        startRecordingButton.interactable = true;
        highlightRecordingButton.interactable = false;
        stopRecordingButton.interactable = false;

        timerGUI.StopTimer();
        timerGUI.ResetTimer();
    }

    /*If the user confirms the action through the GUI, 'call' will be executed.*/
    public void LoadDoubleCheckMenu(UnityAction call)
    {
        GameObject doubleCheckMenu = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.doubleCheck), transform.parent);
        doubleCheckMenu.GetComponent<DoubleCheckGUI>().Init(call);
    }
}
