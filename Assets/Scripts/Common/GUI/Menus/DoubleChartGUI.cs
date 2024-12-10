using UnityEngine;
using UnityEngine.UI;
using VESCharts;

public class DoubleChartGUI : MonoBehaviour
{
    public GameObject screen1, screen2;
    public ChartAxes2D Axes1 { get; private set; }
    public ChartAxes2D Axes2 { get; private set; }
    private bool initialized = false;
    public bool AutomaticRefresh { get; private set; }

    private Text yLabel1Text;
    private Text yMax1Text;
    private Text yMin1Text;
    private Text yLabel2Text;
    private Text yMax2Text;
    private Text yMin2Text;
    private Text xLabelText;
    private Text xMaxText;
    private Text xMinText;

    // Start is called before the first frame update
    public void Init(string xLabel, string yLabel1, string yLabel2)
    {
        yLabel1Text = transform.Find("Screen1/OrdinateAxis/Title/RotatedTitle").GetComponent<Text>();
        yMax1Text = transform.Find("Screen1/OrdinateAxis/Values/Max").GetComponent<Text>();
        yMin1Text = transform.Find("Screen1/OrdinateAxis/Values/Min").GetComponent<Text>();
        yLabel2Text = transform.Find("Screen1/Screen2/OrdinateAxis/Title/RotatedTitle").GetComponent<Text>();
        yMax2Text = transform.Find("Screen1/Screen2/OrdinateAxis/Values/Max").GetComponent<Text>();
        yMin2Text = transform.Find("Screen1/Screen2/OrdinateAxis/Values/Min").GetComponent<Text>();
        xLabelText = transform.Find("Screen1/AbscisseAxis/Title").GetComponent<Text>();
        xMaxText = transform.Find("Screen1/AbscisseAxis/Values/Max").GetComponent<Text>();
        xMinText = transform.Find("Screen1/AbscisseAxis/Values/Min").GetComponent<Text>();

        Axes1 = screen1.AddComponent<ChartAxes2D>();
        Axes1.Init(xLabel, yLabel1);
        Axes2 = screen2.AddComponent<ChartAxes2D>();
        Axes2.Init(xLabel, yLabel2);

        xLabelText.text = xLabel;
        yLabel1Text.text = yLabel1;
        yLabel2Text.text = yLabel2;

        AutomaticRefresh = true;
        initialized = true;
    }

    public void AddChart1(IChart2D chart, int depth) => Axes1.AddChart(chart, depth);
    public void AddChart2(IChart2D chart, int depth) => Axes2.AddChart(chart, depth);

    // Update is called once per frame
    void FixedUpdate()
    {
        if (AutomaticRefresh && initialized)
        {
            Axes1.Draw();
            yMax1Text.text = Axes1.yAxis.maxValue.ToString("0.#");
            yMin1Text.text = Axes1.yAxis.minValue.ToString("0.#");
            
            Axes2.Draw();
            yMax2Text.text = Axes2.yAxis.maxValue.ToString("0.#");
            yMin2Text.text = Axes2.yAxis.minValue.ToString("0.#");

            xMaxText.text = Axes1.xAxis.maxValue.ToString("0.#");
            xMinText.text = Axes1.xAxis.minValue.ToString("0.#");
        }
            
    }
}
