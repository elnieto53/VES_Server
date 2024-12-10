using UnityEngine;
using UnityEngine.UI;
using VESCharts;

public class ChartGUI : MonoBehaviour
{
    public GameObject screen;
    public ChartAxes2D Axes { get; private set; }
    private bool initialized = false;
    public bool AutomaticRefresh { get; private set; }

    private Text yLabelText;
    private Text xLabelText;
    private Text yMaxText;
    private Text yMinText;
    private Text xMaxText;
    private Text xMinText;

    // Start is called before the first frame update
    public void Init(string xLabel, string yLabel)
    {
        yLabelText = transform.Find("Screen/OrdinateAxis/Title/RotatedTitle").GetComponent<Text>();
        xLabelText = transform.Find("Screen/AbscisseAxis/Title").GetComponent<Text>();
        yMaxText = transform.Find("Screen/OrdinateAxis/Values/Max").GetComponent<Text>();
        yMinText = transform.Find("Screen/OrdinateAxis/Values/Min").GetComponent<Text>();
        xMaxText = transform.Find("Screen/AbscisseAxis/Values/Max").GetComponent<Text>();
        xMinText = transform.Find("Screen/AbscisseAxis/Values/Min").GetComponent<Text>();

        Axes = screen.AddComponent<ChartAxes2D>();
        Axes.Init(xLabel, yLabel);

        xLabelText.text = xLabel;
        yLabelText.text = yLabel;

        AutomaticRefresh = true;
        initialized = true;
    }

    //public void AddChart(IChart2D chart, int depth) => Axes.AddChart(chart, depth);

    // Update is called once per frame
    void FixedUpdate()
    {
        if (AutomaticRefresh && initialized)
        {
            Axes.Draw();
            yMaxText.text = Axes.yAxis.maxValue.ToString("0.#");
            yMinText.text = Axes.yAxis.minValue.ToString("0.#");
            xMaxText.text = Axes.xAxis.maxValue.ToString("0.#");
            xMinText.text = Axes.xAxis.minValue.ToString("0.#");
        }
            
    }
}
