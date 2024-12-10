using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QoSManager
{
    public Mode mode { get; private set; }
    public JitterModeling jitterModeling { get; private set; }
    public Recording qosRecording { get; set; } = null;
    public Action<ScenarioElement, SortedDictionary<int, byte[]>, SortedDictionary<int, byte[]>> UpdateScenarioElement { get; private set; }

    private NetClock netClock;
    
    public enum Mode { NotDegraded, RandomDegradation, RecordedDegradation }


    [Serializable]
    public class Configuration
    {
        public Mode mode;
        public JitterModeling.Distribution distribution;
        public string jitterModelingJson;
        public Recording recording;

        public Configuration() => mode = Mode.NotDegraded;

        //public static Configuration Load(string name, string folderPath) => JsonSerializer<Configuration>.LoadDataFromFile(folderPath, name);

        //public void Save(string name, string filePath, bool overwrite)
        //{
        //    JsonSerializer<Configuration> serializer = new JsonSerializer<Configuration>(this);
        //    serializer.SaveDataToFile(name, filePath, overwrite);
        //} 
    }

    [Serializable]
    public class Recording
    {
        public int period;
        public List<int> delay;
        private int timeOrigin;

        public Recording()
        {
            timeOrigin = 0;
            period = 20;
            delay = new List<int>();
            delay.Add(0);   //A periodic vector which results in no packet loss nor delay.
        }

        public int GetDelay(int timestamp)
        {
            try {
                return delay[(timestamp - timeOrigin) / period % delay.Count];
            } catch(Exception e)
            {
                Debug.LogError(e.Message);
                Debug.LogError("QoS Manager - recording error: (" + timestamp + ", " + timeOrigin + ", " + period + ", " + delay.Count);
                return 0;
            }
        }

        public static Recording Load(string name, string folderPath) => JsonSerializer<Recording>.LoadDataFromFile(folderPath, name);
        public static Recording Load(string filePath) => JsonSerializer<Recording>.LoadDataFromFile(filePath);

        public void SetTimeOrigin(int timestamp) => timeOrigin = timestamp;

        public List<int> GetRecordingWindow(int ms)
        {
            List<int> retval = new List<int>();
            int length = ms / period;

            if (delay.Count == 0)
                return null;

            for(int i = 0; i < length; i++)
                retval.Add(delay[i % delay.Count]);

            return retval;
        }

        public void SaveDataToFile(string name, string filePath, bool overwrite)
        {
            JsonSerializer<Recording> serializer = new JsonSerializer<Recording>(this);
            serializer.SaveDataToFile(name, filePath, overwrite);
        }
        
    }

    
    public QoSManager(NetClock netClock)
    {
        this.netClock = netClock;
        SetNoDegradation();
    }


    public QoSManager(NetClock netClock, Recording qosRecording)
    {
        this.netClock = netClock;
        SetRecordedDegradation(qosRecording);
    }

    public void SetConfiguration(Configuration configuration)
    {
        mode = configuration.mode;

        switch (mode)
        {
            case Mode.NotDegraded:
                SetNoDegradation();
                break;
            case Mode.RandomDegradation:
                /* Set specific distribution, i.e., JitterModeling */
                JitterModeling newJitterModeling = JitterModeling.Deserialize(configuration.distribution, configuration.jitterModelingJson);
                SetRandomDegradation(newJitterModeling);
                break;
            case Mode.RecordedDegradation:
                /* Load qos recorded degradation */
                SetRecordedDegradation(configuration.recording);
                break;
        }
    }

    public Configuration GetConfiguration()
    {
        Configuration configuration = new Configuration();
        configuration.mode = mode;
        switch (mode)
        {
            case Mode.NotDegraded:
                /* No more data to save */
                break;
            case Mode.RandomDegradation:
                /* Save specific distribution, i.e., JitterModeling */
                configuration.distribution = jitterModeling.distribution;
                configuration.jitterModelingJson = jitterModeling.Serialize();
                break;
            case Mode.RecordedDegradation:
                /* Save qos recorded degradation */
                configuration.recording = qosRecording;
                break;
        }
        
        return configuration;
    }


    /*Set QoS Modes*/
    public void SetNoDegradation()
    {
        UpdateScenarioElement = NotDegradedUpdateBehavior;
        mode = Mode.NotDegraded;
    }

    public void SetRandomDegradation(JitterModeling jitterModeling)
    {
        this.jitterModeling = jitterModeling;
        UpdateScenarioElement = RandomUpdateBehavior;
        mode = Mode.RandomDegradation;
    }

    public void SetRecordedDegradation(Recording qosRecording)
    {
        this.qosRecording = qosRecording;
        UpdateScenarioElement = RecordedUpdateBehavior;
        mode = Mode.RecordedDegradation;
    }


    /*Behavior of QoS modes*/
    private void NotDegradedUpdateBehavior(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, SortedDictionary<int, byte[]> qosOutputBuffer)
    {
        int currentTime = netClock.GetTimeStamp();
        while (updateBuffer.Count > 0)
        {
            var firstPkg = updateBuffer.First();
            if (currentTime < firstPkg.Key)
                return;
            element.Config(firstPkg.Value);
            element.updateCallback?.Invoke(firstPkg.Key, currentTime);
            updateBuffer.Remove(firstPkg.Key);
        }
    }

    private void RandomUpdateBehavior(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, SortedDictionary<int, byte[]> qosOutputBuffer)
    {
        jitterModeling.UpdateCriteria(element, updateBuffer, netClock.GetTimeStamp());
    }

    private void RecordedUpdateBehavior(ScenarioElement element, SortedDictionary<int, byte[]> updateBuffer, SortedDictionary<int, byte[]> qosOutputBuffer)
    {
        int currentTime = netClock.GetTimeStamp();
        /*Add delay & package loss*/
        while (updateBuffer.Count > 0)
        {
            KeyValuePair<int, byte[]> updatePkg = updateBuffer.First();

            int addedDelay = qosRecording.GetDelay(updatePkg.Key);
            if (addedDelay >= 0 && !qosOutputBuffer.ContainsKey(updatePkg.Key + addedDelay))
                qosOutputBuffer.Add(updatePkg.Key + addedDelay, updatePkg.Value);
            updateBuffer.Remove(updatePkg.Key);
        }

        /*'Receive' the qos-degraded stream*/
        while (qosOutputBuffer.Count > 0)
        {
            KeyValuePair<int, byte[]> firstPkg = qosOutputBuffer.First();
            if (currentTime < firstPkg.Key)
                return;
            element.Config(firstPkg.Value);
            element.updateCallback?.Invoke(firstPkg.Key, currentTime);
            qosOutputBuffer.Remove(firstPkg.Key);
        }
    }

    /*Auxiliar - */
    public void ResetRecording()
    {
        if (qosRecording != null)
            qosRecording.SetTimeOrigin(netClock.GetTimeStamp());
    }
}