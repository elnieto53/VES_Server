using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public abstract class JitterModeling
{
    //public Action<int, int, ScenarioElement> recordMan { get; set; } = null;
    public abstract Distribution distribution { get; }
    //public event Action<int, int, ScenarioElement> qosNotify;

    public enum Distribution { None, Constant, Poisson }

    public JitterModeling() { }

    public static JitterModeling Deserialize(Distribution distribution, string json)
    {
        JitterModeling jitterModeling;
        switch (distribution)
        {
            case Distribution.Constant:
                jitterModeling = new ConstantDelay();
                break;
            case Distribution.Poisson:
                jitterModeling = new Poisson();
                break;
            default:
                return null;
        }
        jitterModeling.Deserialize(json);
        return jitterModeling;
    }

    public abstract string Serialize();
    public abstract void Deserialize(string json);
    public abstract void UpdateCriteria(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, int currentTime);

    /*SUBCLASSES*/

#if false
    public class NoJitter : JitterModeling
    {
        public NoJitter() : base() { }

        public override void UpdateCriteria(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, int currentTime)
        {
            while (updateBuffer.Count > 0)
            {
                var firstPkg = updateBuffer.First();
                if (currentTime < firstPkg.Key)
                    return;
                element.Config(firstPkg.Value);
                //recordManager.RecordRemoteUpdate(element.elementID, (byte[])firstPkg.Value.Clone(), firstPkg.Key, currentTime);
                element.updateCallback?.Invoke(firstPkg.Key, currentTime);
                updateBuffer.Remove(firstPkg.Key);
            }
        }
    }
#endif

    public class ConstantDelay : JitterModeling
    {
        public override Distribution distribution { get => Distribution.Constant; }
        public int delay { get; set; } //Delay in ms

        public static readonly int defaultDelay = 0;

        public ConstantDelay() : base() => delay = defaultDelay;

        public ConstantDelay(int delay) : base() => this.delay = delay;


        [Serializable]
        public class Configuration
        {
            public int delay;

            public Configuration() => delay = defaultDelay;

            public Configuration(int delay) => this.delay = delay;

            public Configuration(ConstantDelay distribution) => delay = distribution.delay;

            public string Serialize() => JsonUtility.ToJson(this, false);
            public static Configuration Deserialize(string json) => JsonUtility.FromJson<Configuration>(json);
        }

        public override void UpdateCriteria(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, int currentTime)
        {
            while (updateBuffer.Count > 0)
            {
                var firstPkg = updateBuffer.First();
                if (currentTime - delay < firstPkg.Key)
                    return;
                element.Config(firstPkg.Value);
                //recordManager.RecordRemoteUpdate(element.elementID, (byte[])firstPkg.Value.Clone(), firstPkg.Key, currentTime);
                element.updateCallback?.Invoke(firstPkg.Key, currentTime);
                updateBuffer.Remove(firstPkg.Key);
            }
        }

        public override string Serialize() => "";
        public override void Deserialize(string json) => new ConstantDelay(); /*FIX!!!!*/
    }

    public class Poisson : JitterModeling
    {
        public override Distribution distribution { get => Distribution.Poisson; }
        private System.Random random;
        private double iLambda;  /*Internal value of 'lambda'*/
        private int bufferSize;

        private double[] distributionFunc;

        public static readonly double defaultLambda = 1.2;
        public static readonly int defaultPkgOutputPerCall = 20;

        public double lambda
        {
            set { if (value < 0) throw new ArgumentOutOfRangeException("Landa out of bounds"); iLambda = value; LoadDistributionFunc(); }
            get { return iLambda; }
        }

        [Serializable]
        public class Configuration
        {
            public double lambda;
            public int bufferSize;
            private JsonSerializer<Configuration> serializer;

            public Configuration()
            {
                lambda = defaultLambda;
                bufferSize = defaultPkgOutputPerCall;
            }

            public Configuration(double lambda, int bufferSize)
            {
                this.lambda = lambda;
                this.bufferSize = bufferSize;
            }

            public Configuration(Poisson distribution)
            {
                lambda = distribution.lambda;
                bufferSize = distribution.bufferSize;
            }

            public string Serialize() => JsonUtility.ToJson(this, false);
            public static Configuration Deserialize(string json) => JsonUtility.FromJson<Configuration>(json);
        }

        public override string Serialize() => new Configuration(this).Serialize();
        public override void Deserialize(string json) => SetConfiguration(Configuration.Deserialize(json));


        public Poisson() : this(defaultLambda, defaultPkgOutputPerCall) { }

        public Poisson(double lambda, int bufferSize) : base()
        {
            if (lambda < 0)
                throw new ArgumentOutOfRangeException("Lambda out of bounds");
            iLambda = lambda;
            this.bufferSize = bufferSize;
            random = new System.Random();

            LoadDistributionFunc();
        }

        public Poisson(Configuration configuration)
        {
            bufferSize = configuration.bufferSize;
            lambda = configuration.lambda;
            Debug.Log("New Poisson (" + configuration.lambda + ", " + lambda + ")");
        }

        public void SetConfiguration(Configuration configuration)
        {
            bufferSize = configuration.bufferSize;
            lambda = configuration.lambda;
        }

        private void LoadDistributionFunc()
        {
            distributionFunc = new double[bufferSize];

            distributionFunc[0] = ProbDensityFunc(0);
            for (int i = 1; i < bufferSize - 1; i++)
            {
                distributionFunc[i] = distributionFunc[i - 1] + ProbDensityFunc(i);
            }
            distributionFunc[bufferSize - 1] = 1;
        }

        private double ProbDensityFunc(int k)
        {
            /*Calculate factorial*/
            double factorial = k == 0 ? 1 : k;
            int aux = k;
            while (aux > 2)
                factorial *= (--aux);
            /*Return fdp*/
            return Math.Exp(-lambda) * Math.Pow(lambda, k) / factorial;
        }

        private int PoissonDistribution_()
        {
            double aux = random.NextDouble();
            return Array.FindIndex(distributionFunc, val => val >= aux); /*Returns the first match*/
        }

        public override void UpdateCriteria(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, int currentTime)
        {
            if (updateBuffer.Count > 0)
            {
                int pkgsReady;
                if ((pkgsReady = updateBuffer.Keys.Count(timestamp => timestamp <= currentTime)) == 0)
                    return;

                /*Get the number of packages to be removed from the queue. It is the minimum of Poisson() and the available packages*/
                int pkgsToUpdate = Math.Min(PoissonDistribution_(), pkgsReady);

                KeyValuePair<int, byte[]> firstEntry;
                while (pkgsToUpdate-- > 0)
                {
                    firstEntry = updateBuffer.First();
                    //RecordUpdate(new RegisterEntry(ID, firstEntry.Key, NetClock.GetTimeStamp()));
                    //if (recordManager != null)
                    //{
                    //    recordManager.RecordRemoteUpdate(element.elementID, (byte[])firstEntry.Value.Clone(), firstEntry.Key, netClock.GetTimeStamp());
                    //    element.updateCallback?.Invoke(firstEntry.Key, currentTime);
                    //}
                        
                    element.Config(firstEntry.Value);
                    updateBuffer.Remove(firstEntry.Key);
                }
            }
        }
    }


}
