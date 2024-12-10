namespace VESCharts
{
    using System;


    [Serializable]
    public class ChartAxis
    {
        public string name { get; private set; }
        public float maxValue { get; private set; }
        public float minValue { get; private set; }
        public float range { get => maxValue - minValue; }

        public ChartAxis(string name, float minValue, float maxValue)
        {
            this.name = name;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        /*It normalizes the coordinates*/
        public float NormalizeCoordinates(float x) => (x - minValue) / range;

        public void SetRange(float minValue, float maxValue)
        {
            if (minValue >= maxValue) return;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public void SetRange(float minValue, float maxValue, float guard)
        {
            if (minValue >= maxValue) return;
            float range = maxValue - minValue;
            this.minValue = minValue - range * guard;
            this.maxValue = maxValue + range * guard;
        }

        public bool Contains(float value) => value >= minValue && value <= maxValue;

        public bool CheckRange(float minValue, float maxValue)
        {
            bool retval = false;
            if (minValue < this.minValue)
            {
                this.minValue = minValue;
                retval = true;
            }
            if (maxValue > this.maxValue)
            {
                this.maxValue = maxValue;
                retval = true;
            }

            return retval;
        }
    }
}