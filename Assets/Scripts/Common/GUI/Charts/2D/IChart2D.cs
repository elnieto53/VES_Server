namespace VESCharts
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;


    public interface IChart2D
    {
        bool isEmpty();
        Chart2DRange GetRange();
        void Draw();
        void SetMaterial(Material mat);
        void SetZOffset(float offset);
    }

    public class Chart2DRange
    {
        public float yMax;
        public float yMin;
        public float xMax;
        public float xMin;

        public Chart2DRange(float xMin, float xMax, float yMin, float yMax)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.yMin = yMin;
            this.yMax = yMax;
        }

        public Chart2DRange(Rect rect)
        {
            this.xMin = rect.xMin;
            this.xMax = rect.xMax;
            this.yMin = rect.yMin;
            this.yMax = rect.yMax;
        }

        public Chart2DRange() : this(0, 1, 0, 1) { }

        public static Chart2DRange GetMaxRange(List<Chart2DRange> ranges)
        {
            if (ranges.Count == 0)
                return null;
            return new Chart2DRange(ranges.Min(p => p.xMin), ranges.Max(p => p.xMax), ranges.Min(p => p.yMin), ranges.Max(p => p.yMax));
        }
    }



}


