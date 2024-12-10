using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PvasGUI : MonoBehaviour
{
    //ProjectedVAS projectedVAS;
    private Session session;
    private List<(Transform anchor, ProjectedVAS projectedVAS)> detectionAreas => session.pvasDetectionAreas;
    private IList<Transform> sensorAnchors => session.body.GetSensorAnchors();

    //GUI elements
    private Dropdown pixelMatrix_M;
    private Dropdown pixelMatrix_N;
    private InputField fieldOfView_X;
    private InputField fieldOfView_Y;
    private InputField detectionDistance;
    private InputField spawnSpeed;
    private InputField randomizerInt;
    private Slider focusVolume;
    private Slider peripheralVolume;
    private Toggle isActiveToggle;
    private Button acceptButton;
    private Button cancelButton;

    private Dropdown sensorAnchorsDropdown;
    private Dropdown detectionAreasDropdown;
    private Button addPvastButton;
    private Button removeDetectionAreaButton;
    private Button exitButton;

    public void Init(Session session)
    {
        this.session = session;

        pixelMatrix_N = transform.Find("Configuration/InputFields/MatrixMenu/NDropdown").GetComponent<Dropdown>();
        pixelMatrix_M = transform.Find("Configuration/InputFields/MatrixMenu/MDropdown").GetComponent<Dropdown>();
        fieldOfView_X = transform.Find("Configuration/InputFields/FieldOfViewMenu/AngleXInputField").GetComponent<InputField>();
        fieldOfView_Y = transform.Find("Configuration/InputFields/FieldOfViewMenu/AngleYInputField").GetComponent<InputField>();
        detectionDistance = transform.Find("Configuration/InputFields/DetectionDistanceMenu/InputField").GetComponent<InputField>();
        spawnSpeed = transform.Find("Configuration/InputFields/SpawnSpeedMenu/InputField").GetComponent<InputField>();
        randomizerInt = transform.Find("Configuration/InputFields/RandomizerMenu/InputField").GetComponent<InputField>();
        focusVolume = transform.Find("Configuration/InputFields/FocusVolumeMenu/Slider").GetComponent<Slider>();
        peripheralVolume = transform.Find("Configuration/InputFields/PeripheralVolumeMenu/Slider").GetComponent<Slider>();
        isActiveToggle = transform.Find("Configuration/IsActiveToggle").GetComponent<Toggle>();
        acceptButton = transform.Find("Configuration/AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("Configuration/CancelButton").GetComponent<Button>();

        sensorAnchorsDropdown = transform.Find("Manager/BodyPartDropdown").GetComponent<Dropdown>();
        addPvastButton = transform.Find("Manager/AddPvasButton").GetComponent<Button>();
        detectionAreasDropdown = transform.Find("Configuration/DetectionAreaDropdown").GetComponent<Dropdown>();
        removeDetectionAreaButton = transform.Find("Configuration/RemovePvasButton").GetComponent<Button>();
        exitButton = transform.Find("ExitButton").GetComponent<Button>();

        addPvastButton.onClick.AddListener(OnClickAddDetectionArea);
        removeDetectionAreaButton.onClick.AddListener(OnClickRemoveDetectionArea);
        exitButton.onClick.AddListener(delegate { Destroy(gameObject); });
        acceptButton.onClick.AddListener(OnClickUpdateConfig);
        cancelButton.onClick.AddListener(delegate { RefreshConfig(detectionAreas[detectionAreasDropdown.value].projectedVAS); });


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

        /*Each time a detection area is chosen in the dropdown, refresh the corresponding PVAS config fields*/
        detectionAreasDropdown.onValueChanged.AddListener(delegate { RefreshConfig(detectionAreas[detectionAreasDropdown.value].projectedVAS); });

        /*Refresh the DetectionAreasDropdown*/
        RefreshDetectionAreasDropdown();
    }

    private void OnClickAddDetectionArea()
    {
        /*Get the placement of the virtual sensor within the 'bodyPart' prefab*/
        Transform newParent = sensorAnchors[sensorAnchorsDropdown.value];
        /*Init the sensory substitution module, which includes its detection area*/
        ProjectedVAS projectedVAS = new GameObject("ProjectedVAS").AddComponent<ProjectedVAS>();
        projectedVAS.Init();
        projectedVAS.AdoptMe(newParent);
        projectedVAS.ssdEnabled = true;
        /*Register the new sensory substitution module*/
        detectionAreas.Add((sensorAnchors[sensorAnchorsDropdown.value], projectedVAS));

        /*Refresh the DetectionAreasDropdown*/
        RefreshDetectionAreasDropdown();
        /*Allow to modify the configuration of the new sensory substitution module in the GUI*/
        SetPVASConfigInteractable(true);
    }

    private void OnClickRemoveDetectionArea()
    {
        if (detectionAreasDropdown.options.Count == 0)
            return;

        Destroy(detectionAreas[detectionAreasDropdown.value].projectedVAS.gameObject);

        detectionAreas.Remove(detectionAreas[detectionAreasDropdown.value]);
        RefreshDetectionAreasDropdown();
        if (detectionAreas.Count == 0)
        {
            SetPVASConfigInteractable(false);
        }
    }

    private void SetPVASConfigInteractable(bool enable)
    {
        detectionAreasDropdown.interactable = enable;
        removeDetectionAreaButton.interactable = enable;
        pixelMatrix_M.interactable = enable;
        pixelMatrix_N.interactable = enable;
        fieldOfView_X.interactable = enable;
        fieldOfView_Y.interactable = enable;
        detectionDistance.interactable = enable;
        spawnSpeed.interactable = enable;
        randomizerInt.interactable = enable;
        isActiveToggle.interactable = enable;
        focusVolume.interactable = enable;
        peripheralVolume.interactable = enable;

        acceptButton.interactable = enable;
        cancelButton.interactable = enable;
    }

    private void RefreshDetectionAreasDropdown()
    {
        detectionAreasDropdown.ClearOptions();
        if (detectionAreas.Count == 0)
        {
            SetPVASConfigInteractable(false);
            return;
        }

        List<string> options = new List<string>();
        foreach ((Transform anchor, ProjectedVAS projectedVAS) entry in detectionAreas)
        {
            options.Add(detectionAreas.IndexOf(entry) + "-" + entry.anchor.name);
        }
        detectionAreasDropdown.AddOptions(options);
        RefreshConfig(detectionAreas[detectionAreasDropdown.value].projectedVAS);
    }

    private void RefreshConfig(ProjectedVAS projectedVAS)
    {
        //ProjectedVAS projectedVAS = detectionAreas[detectionAreasDropdown.value].projectedVAS;
        ProjectedVAS.Configuration config = projectedVAS.GetConfig();

        pixelMatrix_M.value = pixelMatrix_M.options.FindIndex(p => int.Parse(p.text) == config.M);
        pixelMatrix_N.value = pixelMatrix_N.options.FindIndex(p => int.Parse(p.text) == config.N);
        fieldOfView_X.text = config.fieldOfViewX.ToString();
        fieldOfView_Y.text = config.fieldOfViewY.ToString();
        detectionDistance.text = config.detectionDistance.ToString();
        spawnSpeed.text = config.period.ToString();
        randomizerInt.text = config.seed.ToString();
        focusVolume.value = config.focusVolume;
        peripheralVolume.value = config.peripheralVolume;

        isActiveToggle.isOn = projectedVAS.ssdEnabled;
    }

    private void OnClickUpdateConfig()
    {
        ProjectedVAS projectedVAS = detectionAreas[detectionAreasDropdown.value].projectedVAS;
        ProjectedVAS.Configuration config = new ProjectedVAS.Configuration();

        config.M = int.Parse(pixelMatrix_M.options[pixelMatrix_M.value].text);
        config.N = int.Parse(pixelMatrix_N.options[pixelMatrix_N.value].text);
        config.fieldOfViewX = float.Parse(fieldOfView_X.text);
        config.fieldOfViewY = float.Parse(fieldOfView_Y.text);
        config.detectionDistance = float.Parse(detectionDistance.text);
        config.period = float.Parse(spawnSpeed.text);
        config.seed = int.Parse(randomizerInt.text);
        config.focusVolume = focusVolume.value;
        config.peripheralVolume = peripheralVolume.value;

        projectedVAS.SetConfig(config);
        projectedVAS.ssdEnabled = isActiveToggle.isOn;
    }
}
