namespace VESCharts
{
    using System.Collections.Generic;
    using UnityEngine;

    public class ChartAxes2D : MonoBehaviour
    {
        private RectTransform rectTransform;

        public ChartAxis xAxis;
        public ChartAxis yAxis;
        public bool AutoScale { get; set; }
        public bool Isometric { get; set; } = false;
        private Vector2 DrawingAreaSize { get; set; }

        private List<IChart2D> charts;

        //public float xMaxValue { get => rectTransform.rect.size.y ; }

        public void Init(string xLabel, string yLabel)
        {
            if (!gameObject.TryGetComponent(out RectTransform rectTransform))
                throw new System.Exception("The ChartAxes needs a RectTransform reference");

            this.rectTransform = rectTransform;
            AutoScale = true;
            xAxis = new ChartAxis(xLabel, 0, 1);
            yAxis = new ChartAxis(yLabel, 0, 1);
            charts = new List<IChart2D>();
        }

        public void AddChart(IChart2D chart, int depth)
        {
            charts.Add(chart);
            chart.SetZOffset(-depth);
        }

        public void Draw()
        {
            if (charts.Count == 0 || !charts.Exists(p => !p.isEmpty()))
                return;

            if (AutoScale)
            {
                Chart2DRange range = GetRange();
                xAxis.SetRange(range.xMin, range.xMax);
                yAxis.SetRange(range.yMin, range.yMax);
            }

            foreach (IChart2D chart in charts)
            {
                chart.Draw();
            }
        }

        public void SetRange(float xMin, float xMax, float yMin, float yMax)
        {
            xAxis.SetRange(xMin, xMax);
            yAxis.SetRange(yMin, yMax);
        }

        private Chart2DRange GetRange()
        {
            List<Chart2DRange> chartRanges = new List<Chart2DRange>();
            foreach (IChart2D chart in charts)
            {
                if (!chart.isEmpty()) 
                    chartRanges.Add(chart.GetRange());
            }
            return Chart2DRange.GetMaxRange(chartRanges);
        }

        public Vector2 GetCoordinates(Vector2 point)
        {
            return Vector2.Scale(new Vector2(xAxis.NormalizeCoordinates(point.x), yAxis.NormalizeCoordinates(point.y)), DrawingAreaSize);
        }

        public void Update()
        {
            if (Isometric)
            {
                /*Set a drawing area contained within the screen which mantains the aspect ratio*/
                DrawingAreaSize = rectTransform.rect.size.y / rectTransform.rect.size.x < yAxis.range / xAxis.range ?
                    new Vector2(rectTransform.rect.size.y * xAxis.range / yAxis.range, rectTransform.rect.size.y) :
                    new Vector2(rectTransform.rect.size.x, rectTransform.rect.size.x * yAxis.range / xAxis.range);
            }
            else
            {
                /*Set the screen's rect as the drawing area*/
                DrawingAreaSize = rectTransform.rect.size;
            }
        }
    }
}

