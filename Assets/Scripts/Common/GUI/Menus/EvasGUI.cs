using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using static VirtualScenarioManager;

public class EvasGUI : MonoBehaviour
{
    private Session session;
    private List<(Transform anchors, EnvelopingVAS envelopingVAS)> detectionAreas => session.evasDetectionAreas;
    IList<Transform> sensorAnchors => session.body.GetSensorAnchors();
    private List<NetworkManager.NetDevice> availableHapticDevices;

    private InputField detectionDistance;
    private InputField spawnSpeed;
    private Toggle isActiveToggle;
    private Button acceptButton;
    private Button cancelButton;

    private Dropdown sensorAnchorsDropdown;
    private Dropdown hapticDevicesDropdown;
    private Dropdown detectionAreasDropdown;
    private InputField fieldOfView_X;
    private InputField fieldOfView_Y;
    private Button addEvastButton;
    private Button removeDetectionAreaButton;
    private Button exitButton;

    public void Init(Session session)
    {
        this.session = session;
        availableHapticDevices = GetAvailableHapticDevices();

        fieldOfView_X = transform.Find("Configuration/InputFields/FieldOfViewMenu/AngleXInputField").GetComponent<InputField>();
        fieldOfView_Y = transform.Find("Configuration/InputFields/FieldOfViewMenu/AngleYInputField").GetComponent<InputField>();
        detectionDistance = transform.Find("Configuration/InputFields/DetectionDistanceMenu/InputField").GetComponent<InputField>();
        spawnSpeed = transform.Find("Configuration/InputFields/SpawnSpeedMenu/InputField").GetComponent<InputField>();
        isActiveToggle = transform.Find("Configuration/IsActiveToggle").GetComponent<Toggle>();
        acceptButton = transform.Find("Configuration/AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("Configuration/CancelButton").GetComponent<Button>();

        sensorAnchorsDropdown = transform.Find("Manager/BodyPartDropdown").GetComponent<Dropdown>();
        hapticDevicesDropdown = transform.Find("Manager/HapticDevicesDropdown").GetComponent<Dropdown>();
        addEvastButton = transform.Find("Manager/AddPvasButton").GetComponent<Button>();
        detectionAreasDropdown = transform.Find("Configuration/DetectionAreaDropdown").GetComponent<Dropdown>();
        removeDetectionAreaButton = transform.Find("Configuration/RemovePvasButton").GetComponent<Button>();
        exitButton = transform.Find("ExitButton").GetComponent<Button>();

        addEvastButton.onClick.AddListener(OnClickAddDetectionArea);
        removeDetectionAreaButton.onClick.AddListener(OnClickRemoveDetectionArea);
        exitButton.onClick.AddListener(delegate { Destroy(gameObject); });
        acceptButton.onClick.AddListener(OnClickUpdateConfig);
        cancelButton.onClick.AddListener(delegate { RefreshConfig(detectionAreas[detectionAreasDropdown.value].envelopingVAS); });

        /*Get all bodyParts which supports virtual sensors. If there are none, destroy this menu*/
        if (sensorAnchors.Count == 0)
        {
            Destroy(gameObject);
            return;
        }
        
        /*Update the 'parentDropdown' with compatible bodyParts*/
        List<string> bodyNames = sensorAnchors.Select(o => o.name).ToList();
        sensorAnchorsDropdown.ClearOptions();
        sensorAnchorsDropdown.AddOptions(bodyNames);

        List<string> hapticDevicesNames = new List<string>(availableHapticDevices.Select(o => o.ID.ToString()).ToList());
        hapticDevicesDropdown.ClearOptions();
        hapticDevicesDropdown.AddOptions(hapticDevicesNames);

        detectionAreasDropdown.onValueChanged.AddListener(delegate { RefreshConfig(detectionAreas[detectionAreasDropdown.value].envelopingVAS); });

        RefreshDetectionAreaDropdown();
        RefreshHapticDevicesDropdown();
    }

    private List<NetworkManager.NetDevice> GetAvailableHapticDevices()
    {
        List<NetworkManager.NetDevice> available = new List<NetworkManager.NetDevice>();
        available = session.GetAvailableNetDevices().FindAll(p => p.hapticsAvailable);

        /* WARNING! For now, the HUZZAH32 devices with a haptic actuator only supports one remote haptic stimuli element in the channel.
        * If there are more than one haptic stimuli, the haptic interface will overwrite continuously the corresponding ERM driver signals.
        * Therefore, the only truly available haptic channels are those with no haptic stimuli.*/
        available.RemoveAll(p =>
            session.virtualScenario.TryGetChannelManager(p.hapticsChannel, out ChannelManager<HapticStimuli> channelManager)
            && channelManager.HostElementsCount > 0);

        return available;
    }


    private void OnClickAddDetectionArea()
    {
        /*Get the placement of the virtual sensor within the 'bodyPart' prefab*/
        Transform newParent = sensorAnchors[sensorAnchorsDropdown.value];
        /*Init the sensory substitution module, which includes its detection area*/

        NetworkManager.NetDevice selectedHapticDevice = availableHapticDevices[hapticDevicesDropdown.value];
        ChannelManager<HapticStimuli> currentChannelManager;
        if (!session.virtualScenario.TryGetChannelManager(selectedHapticDevice.hapticsChannel, out currentChannelManager))
        {
            session.virtualScenario.AddChannel(selectedHapticDevice.hapticsChannel, out currentChannelManager);
        }

        EnvelopingVAS newEnvelopingVAS = new GameObject("EnvelopingVAS").AddComponent<EnvelopingVAS>();
        /*Subscribe the device to the correspoding haptic stimuli channel*/
        currentChannelManager.SubscribeNewDevice(selectedHapticDevice.address);
        newEnvelopingVAS.Init(currentChannelManager);
        newEnvelopingVAS.AdoptMe(newParent);
        newEnvelopingVAS.ssdEnabled = true;
        /*Register the new sensory substitution module*/
        detectionAreas.Add((sensorAnchors[sensorAnchorsDropdown.value], newEnvelopingVAS));

        RefreshHapticDevicesDropdown();
        RefreshDetectionAreaDropdown();
    }

    private void OnClickRemoveDetectionArea()
    {
        if (detectionAreasDropdown.options.Count == 0)
            return;

        EnvelopingVAS currentEnvelopingVAS = detectionAreas[detectionAreasDropdown.value].envelopingVAS;

        currentEnvelopingVAS.GetChannelManager().UnsubscribeAllDevices();
        currentEnvelopingVAS.ssdEnabled = false;
        currentEnvelopingVAS.DeInit();


        detectionAreas.RemoveAt(detectionAreasDropdown.value);

        RefreshHapticDevicesDropdown();
        RefreshDetectionAreaDropdown();
    }

    private void SetEVASConfigInteractable(bool enable)
    {
        detectionAreasDropdown.interactable = enable;
        removeDetectionAreaButton.interactable = enable;
        detectionDistance.interactable = enable;
        spawnSpeed.interactable = enable;
        isActiveToggle.interactable = enable;
        fieldOfView_X.interactable = enable;
        fieldOfView_Y.interactable = enable;

        acceptButton.interactable = enable;
        cancelButton.interactable = enable;
    }

    private void RefreshDetectionAreaDropdown()
    {
        if (detectionAreas.Count == 0)
        {
            SetEVASConfigInteractable(false);
            return;
        }

        List<string> options = new List<string>();
        foreach ((Transform anchor, EnvelopingVAS envelopingVAS) entry in detectionAreas)
        {
            options.Add(detectionAreas.IndexOf(entry) + "-" + "-" + entry.anchor.name);
        }
        detectionAreasDropdown.ClearOptions();
        detectionAreasDropdown.AddOptions(options);
        RefreshConfig(detectionAreas[detectionAreasDropdown.value].envelopingVAS);
        SetEVASConfigInteractable(true);
    }

    private void RefreshHapticDevicesDropdown()
    {
        availableHapticDevices = GetAvailableHapticDevices();

        List<string> hapticDevicesNames = new List<string>(availableHapticDevices.Select(o => o.ID.ToString()).ToList());

        hapticDevicesDropdown.ClearOptions();
        hapticDevicesDropdown.AddOptions(hapticDevicesNames);
    }

    private void RefreshConfig(EnvelopingVAS envelopingVAS)
    {
        EnvelopingVAS.Configuration config = envelopingVAS.GetConfig();

        detectionDistance.text = config.detectionDistance.ToString();
        spawnSpeed.text = config.period.ToString();
        fieldOfView_X.text = config.fieldOfViewX.ToString();
        fieldOfView_Y.text = config.fieldOfViewY.ToString();

        isActiveToggle.isOn = envelopingVAS.ssdEnabled;
    }

    private void OnClickUpdateConfig()
    {
        EnvelopingVAS envelopingVAS = detectionAreas[detectionAreasDropdown.value].envelopingVAS;
        EnvelopingVAS.Configuration config = envelopingVAS.GetConfig(); /*Keep unchanged config fields*/

        config.detectionDistance = float.Parse(detectionDistance.text);
        config.period = float.Parse(spawnSpeed.text);
        config.fieldOfViewX = float.Parse(fieldOfView_X.text);
        config.fieldOfViewY = float.Parse(fieldOfView_Y.text);

        envelopingVAS.SetConfig(config);
        envelopingVAS.ssdEnabled = isActiveToggle.isOn;
    }
}
