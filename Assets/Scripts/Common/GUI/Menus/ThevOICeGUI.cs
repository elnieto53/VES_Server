using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThevOICeGUI : MonoBehaviour
{
    //ProjectedVAS projectedVAS;
    List<(Transform anchor, ThevOICe thevOICe)> detectionAreas;
    //MoCapManager moCapManager;
    IList<Transform> sensorAnchors;

    private int lastDetectionAreaIndex { get => detectionAreas.Count > 0 ? detectionAreas.Count - 1 : 0; }

    //GUI elements
    private Dropdown sensorAnchorsDropdown;
    private Button createButton;
    private Button destroyButton;
    private RawImage preview;

    private Dropdown frameWidthDropdown;
    private Dropdown frameHeightDropdown;
    private InputField horizontalFoV;
    private InputField verticalFoV;
    private InputField grayLevels;
    private InputField lowerFrequency;
    private InputField higherFrequency;
    private Dropdown frequencyDistributionDropdown;
    private InputField frameDuration;
    private InputField detectionDistance;
    private Slider volumeSlider;
    private Toggle isActiveToggle;
    private Button acceptButton;
    private Button cancelButton;

    private Button exitButton;

    public void Init(Session session)
    {
        detectionAreas = session.thevOICeDetectionAreas;

        sensorAnchorsDropdown = transform.Find("Manager/BodyPartDropdown").GetComponent<Dropdown>();
        createButton = transform.Find("Manager/CreateButton").GetComponent<Button>();
        destroyButton = transform.Find("Manager/DestroyButton").GetComponent<Button>();
        preview = transform.Find("Manager/RawImage").GetComponent<RawImage>();

        frameWidthDropdown = transform.Find("Configuration/Data/MatrixMenu/FrameWidthDropdown").GetComponent<Dropdown>();
        frameHeightDropdown = transform.Find("Configuration/Data/MatrixMenu/FrameHeightDropdown").GetComponent<Dropdown>();
        horizontalFoV = transform.Find("Configuration/Data/FieldOfViewMenu/HorizontalFoVInputField").GetComponent<InputField>();
        verticalFoV = transform.Find("Configuration/Data/FieldOfViewMenu/VerticalFoVInputField").GetComponent<InputField>();
        grayLevels = transform.Find("Configuration/Data/GrayLevelsMenu/GrayLevelsInputField").GetComponent<InputField>();
        lowerFrequency = transform.Find("Configuration/Data/FrequencyRangeMenu/LowerFrequencyInputField").GetComponent<InputField>();
        higherFrequency = transform.Find("Configuration/Data/FrequencyRangeMenu/HigherFrequencyInputField").GetComponent<InputField>();
        frequencyDistributionDropdown = transform.Find("Configuration/Data/FrequencyDistributionMenu/ModesDropdown").GetComponent<Dropdown>();
        frameDuration = transform.Find("Configuration/Data/FrameDurationMenu/InputField").GetComponent<InputField>();
        detectionDistance = transform.Find("Configuration/Data/DetectionDistanceMenu/InputField").GetComponent<InputField>();
        volumeSlider = transform.Find("Configuration/Data/VolumeMenu/Slider").GetComponent<Slider>();
        isActiveToggle = transform.Find("Configuration/Data/IsActiveMenu/Toggle").GetComponent<Toggle>();
        acceptButton = transform.Find("Configuration/AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("Configuration/CancelButton").GetComponent<Button>();

        exitButton = transform.Find("ExitButton").GetComponent<Button>();

        createButton.onClick.AddListener(OnClickCreateThevOICe);
        destroyButton.onClick.AddListener(OnClickDestroyThevOICe);
        exitButton.onClick.AddListener(delegate { Destroy(gameObject); });
        acceptButton.onClick.AddListener(OnClickAcceptChanges);
        cancelButton.onClick.AddListener(delegate { RefreshConfig(this.detectionAreas[detectionAreas.Count - 1].thevOICe); });

        /*Get all bodyParts which supports virtual sensors. If there are none, destroy this menu*/
        sensorAnchors = session.body.GetSensorAnchors();
        if (sensorAnchors.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        /*Update the 'parentDropdown' with compatible bodyParts*/
        List<string> bodyNames = new List<string>();
        foreach (Transform anchor in sensorAnchors)
        {
            bodyNames.Add(anchor.name);
        }
        sensorAnchorsDropdown.ClearOptions();
        sensorAnchorsDropdown.AddOptions(bodyNames);

        /*Update the 'frequencyDistributionDropdown'*/
        List<string> freqDistributionModes = new List<string>();
        foreach (int mode in Enum.GetValues(typeof(ThevOICe.Configuration.FrequencyDistribution)))
        {
            freqDistributionModes.Add(Enum.GetName(typeof(ThevOICe.Configuration.FrequencyDistribution), mode));
        }
        frequencyDistributionDropdown.ClearOptions();
        frequencyDistributionDropdown.AddOptions(freqDistributionModes);

        preview.texture = null;
        if (detectionAreas.Count > 0)
        {
            RefreshPreview(detectionAreas[lastDetectionAreaIndex].thevOICe);
            RefreshConfig(detectionAreas[lastDetectionAreaIndex].thevOICe);
        }
        SetPVASConfigInteractable(detectionAreas.Count != 0);
    }

    private void OnClickAcceptChanges()
    {
        OnClickUpdateConfig();
        if (detectionAreas.Count > 0)
        {
            RefreshConfig(detectionAreas[lastDetectionAreaIndex].thevOICe);
            RefreshPreview(detectionAreas[lastDetectionAreaIndex].thevOICe);
        }
    }

    private void OnClickCreateThevOICe()
    {
        if (detectionAreas.Count != 0)
            return;

        /*Get the placement of the virtual sensor within the 'bodyPart' prefab*/
        Transform newParent = sensorAnchors[sensorAnchorsDropdown.value];

        /*Init the sensory substitution module, which includes its detection area*/
        ThevOICe thevOICe = new GameObject("ThevOICe").AddComponent<ThevOICe>();
        thevOICe.Init();
        thevOICe.AdoptMe(newParent);
        thevOICe.ssdEnabled = true;
        /*Register the new sensory substitution module*/
        detectionAreas.Add((sensorAnchors[sensorAnchorsDropdown.value], thevOICe));

        SetPVASConfigInteractable(true);
        RefreshConfig(thevOICe);
        RefreshPreview(thevOICe);
    }

    private void OnClickDestroyThevOICe()
    {
        if (detectionAreas.Count == 0)
            return;
        preview.texture = null;
        Destroy(detectionAreas[lastDetectionAreaIndex].thevOICe.gameObject);
        detectionAreas.RemoveAt(lastDetectionAreaIndex);
        SetPVASConfigInteractable(false);
    }

    private void SetPVASConfigInteractable(bool enable)
    {
        frameWidthDropdown.interactable = enable;
        frameHeightDropdown.interactable = enable;
        verticalFoV.interactable = enable;
        grayLevels.interactable = enable;
        lowerFrequency.interactable = enable;
        higherFrequency.interactable = enable;
        frequencyDistributionDropdown.interactable = enable;
        frameDuration.interactable = enable;
        detectionDistance.interactable = enable;
        volumeSlider.interactable = enable;
        isActiveToggle.interactable = enable;

        acceptButton.interactable = enable;
        cancelButton.interactable = enable;

        createButton.interactable = !enable;
        destroyButton.interactable = enable;
    }


    private void RefreshConfig(ThevOICe thevOICe)
    {
        ThevOICe.Configuration config = thevOICe.GetConfig();

        frameWidthDropdown.value = frameWidthDropdown.options.FindIndex(p => int.Parse(p.text) == config.Width);
        frameHeightDropdown.value = frameHeightDropdown.options.FindIndex(p => int.Parse(p.text) == config.Height);
        horizontalFoV.text = config.HorizontalFoV.ToString();
        verticalFoV.text = config.VerticalFoV.ToString();
        grayLevels.text = config.GrayLevels.ToString();
        lowerFrequency.text = config.LowerFrequency.ToString();
        higherFrequency.text = config.HigherFrequency.ToString();
        frequencyDistributionDropdown.value = frequencyDistributionDropdown.options
            .FindIndex(p => p.text.Equals(Enum.GetName(typeof(ThevOICe.Configuration.FrequencyDistribution), config.mode)));
        frameDuration.text = config.FrameDuration.ToString();
        detectionDistance.text = config.DetectionDistance.ToString();
        volumeSlider.value = config.Volume;
        isActiveToggle.isOn = thevOICe.ssdEnabled;
    }

    private void RefreshPreview(ThevOICe thevOICe)
    {
        preview.texture = thevOICe.GetRawTexture();
    }

    private void OnClickUpdateConfig()
    {
        if (detectionAreas.Count == 0)
            return;
        ThevOICe thevOICe = detectionAreas[lastDetectionAreaIndex].thevOICe;
        ThevOICe.Configuration config = thevOICe.GetConfig();
        config.Width = uint.Parse(frameWidthDropdown.options[frameWidthDropdown.value].text);
        config.Height = uint.Parse(frameHeightDropdown.options[frameHeightDropdown.value].text);
        config.VerticalFoV = float.Parse(verticalFoV.text);
        config.DetectionDistance = float.Parse(detectionDistance.text);
        config.GrayLevels = uint.Parse(grayLevels.text);
        config.HigherFrequency = float.Parse(higherFrequency.text);
        config.LowerFrequency = float.Parse(lowerFrequency.text);
        config.mode = (ThevOICe.Configuration.FrequencyDistribution)Enum.Parse(
            typeof(ThevOICe.Configuration.FrequencyDistribution),
            frequencyDistributionDropdown.options[frequencyDistributionDropdown.value].text
            );
        config.FrameDuration = float.Parse(frameDuration.text);
        config.DetectionDistance = float.Parse(detectionDistance.text);
        config.Volume = volumeSlider.value;
        thevOICe.SetConfig(config);
        thevOICe.ssdEnabled = isActiveToggle.isOn;
    }

}
