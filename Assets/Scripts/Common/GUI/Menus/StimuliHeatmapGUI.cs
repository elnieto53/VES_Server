using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VESCharts;

public class StimuliHeatmapGUI : MonoBehaviour
{
    private Dropdown inputSelectDropdown;
    private Button addInputButton;

    private Slider resolutionSlider;
    private Slider contrastSlider;
    private Slider timeWindowSlider;
    private Text resolutionValueText;
    private Text contrastValueText;
    private Text timeWindowValueText;
    private Button exitButton;

    private Session session;

    public ChartGUI chartGUI;
    public HeatMapChart2D heatMap;

    public readonly int maxDetectionWeight = 20;
    public readonly int minDetectionWeight = 5;
    private int detectionWeight;

    private Rect perimeter;
    private int[,] heights;
    List<(float timestamp, int column, int row)> heatMapBuffer;
    List<(float, RaycastHit)> inputBuffer;

    public readonly float maxTimeWindow = 5;
    public readonly float minTimeWindow = 1;
    private float _timeWindow;
    float timeWindow { get => _timeWindow; set { if (value >= minTimeWindow && value <= maxTimeWindow) _timeWindow = value; } }


    private List<IRaycastSSD> availableInputs;

    public void Init(Session session)
    {
        heatMapBuffer = new List<(float timestamp, int column, int row)>();
        inputBuffer = new List<(float, RaycastHit)>();

        inputSelectDropdown = transform.Find("InputSelect/Dropdown").GetComponent<Dropdown>();
        addInputButton = transform.Find("InputSelect/AddButton").GetComponent<Button>();
        resolutionSlider = transform.Find("Resolution/Slider").GetComponent<Slider>();
        contrastSlider = transform.Find("Contrast/Slider").GetComponent<Slider>();
        timeWindowSlider = transform.Find("TimeWindow/Slider").GetComponent<Slider>();
        resolutionValueText = transform.Find("Resolution/CurrentValue").GetComponent<Text>();
        contrastValueText = transform.Find("Contrast/CurrentValue").GetComponent<Text>();
        timeWindowValueText = transform.Find("TimeWindow/CurrentValue").GetComponent<Text>();
        exitButton = transform.Find("ExitButton").GetComponent<Button>();

        this.session = session;
        perimeter = session.environmentLoaded.GetComponent<EnvironmentManager>().perimeter;

        /*Initialize the chart GUI*/
        chartGUI.Init("X[m]", "Y[m]");
        chartGUI.Axes.Isometric = true;

        /*Initialize a heat map and add it to the chart GUI*/
        heatMap = new HeatMapChart2D(chartGUI.Axes, perimeter, 10);
        //chartGUI.AddChart(heatMap, 10);

        /*Initialize the heatmap 'heights' array*/
        heights = new int[heatMap.columns, heatMap.rows];

        timeWindow = minTimeWindow;
        detectionWeight = maxDetectionWeight;

        /*GUI CONFIGURATION*/
        /*Update the 'parentDropdown' with compatible bodyParts*/
        List<string> inputNames = new List<string>();
        availableInputs = new List<IRaycastSSD>();
        foreach ((Transform anchor, ProjectedVAS pvas) entry in session.pvasDetectionAreas)
        {
            inputNames.Add("PVAS - " + entry.anchor.name);
            availableInputs.Add(entry.pvas);
        }

        foreach ((Transform anchor, EnvelopingVAS evas) entry in session.evasDetectionAreas)
        {
            inputNames.Add("EVAS - " + entry.anchor.name);
            availableInputs.Add(entry.evas);
        }

        if (availableInputs.Count == 0)
            Destroy(gameObject);

        inputSelectDropdown.ClearOptions();
        inputSelectDropdown.AddOptions(inputNames);

        /*Set default GUI values*/
        resolutionSlider.minValue = heatMap.minResolution;
        resolutionSlider.maxValue = heatMap.maxResolution;
        resolutionSlider.value = heatMap.resolution;

        timeWindowSlider.minValue = minTimeWindow;
        timeWindowSlider.maxValue = maxTimeWindow;
        timeWindowSlider.value = timeWindow;

        contrastSlider.minValue = minDetectionWeight;
        contrastSlider.maxValue = maxDetectionWeight;
        contrastSlider.value = detectionWeight;

        resolutionValueText.text = heatMap.resolution.ToString("0.#");
        contrastValueText.text = detectionWeight.ToString("0.#");
        timeWindowValueText.text = timeWindow.ToString("0.#");

        /*Add listeners*/
        resolutionSlider.onValueChanged.AddListener(RefreshResolution);
        contrastSlider.onValueChanged.AddListener(RefreshContrast);
        timeWindowSlider.onValueChanged.AddListener(RefreshTimeWindow);
        exitButton.onClick.AddListener(delegate { Destroy(gameObject); });
        addInputButton.onClick.AddListener(onClickAddInput);

        transform.SetAsFirstSibling();
    }

    public void AddInput(ref Action<RaycastHit> onDetectionCallback) => onDetectionCallback = RegisterDetectionEvent;


    /*I do not use a sorted list, as in this method all datum is sorted by the time of arrival*/
    public void RegisterDetectionEvent(RaycastHit hit) => inputBuffer.Add((Time.time, hit));

    private void UpdateHeatMapData()
    {
        float currentTime = Time.time;
        while (inputBuffer.Count > 0)
        {
            (float timestamp, RaycastHit hit) = inputBuffer[0];
            if (currentTime - timestamp < timeWindow)
            {
                if (heatMap.TryGetCoordinates(hit.point, out int column, out int row))
                    AddHeatMapData(column, row, timestamp);
            }
            inputBuffer.RemoveAt(0);
        }

        while (heatMapBuffer.Count > 0)
        {
            (float timestamp, int column, int row) = heatMapBuffer[0];
            if (currentTime - timestamp < timeWindow)
                return;
            RemoveHeatMapData(0);
        }
    }

    private void AddHeatMapData(int column, int row, float timestamp)
    {
        heatMapBuffer.Add((timestamp, column, row));
        heights[column, row] += detectionWeight;
        heatMap.SetHeight(column, row, heights[column, row]);
    }

    private void RemoveHeatMapData(int index)
    {
        int column = heatMapBuffer[index].column;
        int row = heatMapBuffer[index].row;
        heights[column, row] -= detectionWeight;
        heatMap.SetHeight(column, row, heights[column, row]);
        heatMapBuffer.RemoveAt(index);
    }


    public void Update() => UpdateHeatMapData();

    public int MaxValue(int[,] array)
    {
        int retval = 0;
        foreach (int i in array)
        {
            if (retval < i)
                retval = i;
        }
        return retval;
    }

    private void ClearHeatmap()
    {
        heatMapBuffer.Clear();
        heights = new int[heatMap.columns, heatMap.rows];
        heatMap.SetHeight(0);
    }


    public void RefreshResolution(float resolution)
    {
        ClearHeatmap();
        heatMap.SetResolution(resolution);
        heights = new int[heatMap.columns, heatMap.rows];
        resolutionValueText.text = resolution.ToString("0.#");
    }

    public void RefreshContrast(float contrast)
    {
        ClearHeatmap();
        detectionWeight = (int)contrast;
        contrastValueText.text = contrast.ToString("0.#");
    }

    public void RefreshTimeWindow(float timeWindow)
    {
        this.timeWindow = timeWindow;
        timeWindowValueText.text = timeWindow.ToString("0.#");
    }

    public void onClickAddInput()
    {
        availableInputs[inputSelectDropdown.value].onDetectionCallback = RegisterDetectionEvent;
    }
}
