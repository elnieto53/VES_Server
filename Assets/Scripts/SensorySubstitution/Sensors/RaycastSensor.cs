using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Debug = UnityEngine.Debug;

public abstract class RaycastSensor : MonoBehaviour
{
    public bool enable { get; set; } = true;
    public Action<RaycastHit> onDetectionCallback { get; set; } = null;
    public Action onNullDetection { get; set; } = null;

    /*Private variables*/
    protected List<Vector3> sensors;
    private int layerMask;
    public float scanPeriod { get => maxFrameCounter * 0.02f; set { maxFrameCounter = Mathf.FloorToInt(value / 0.02f); frameCounter = 0; } }
    private int maxFrameCounter = 1;
    private int frameCounter = 0;
    private Record hitsRecording;
    

    [Serializable]
    public class Record : IRecordable
    {
        public List<Vector3> sensorHits;
        public List<int> hitTimestamps;
        public List<SensibleElement.Data> hitElement;
        //private Stopwatch clock;
        private Func<int> GetTimeStamp;
        public bool isRecording { get; private set; } = false;

        public Record()
        {
            sensorHits = new List<Vector3>();
            hitTimestamps = new List<int>();
            hitElement = new List<SensibleElement.Data>();
        }

        public void AddHit(RaycastHit hit)
        {
            if (isRecording)
            {
                sensorHits.Add(hit.point);
                hitTimestamps.Add(GetTimeStamp());
                hitElement.Add(hit.transform.GetComponent<SensibleElement>().data);
            }
        }

        public void SaveRecording(string folderPath, bool overwrite) => SaveRecording("Sensor Hits", folderPath, overwrite);

        public void SaveRecording(string filename, string folderPath, bool overwrite)
        {
            JsonSerializer<Record> serializer = new JsonSerializer<Record>(this);
            serializer.SaveDataToFile(filename, folderPath, overwrite);
        }

        public void StartRecording(Func<int> GetTimeStamp)
        {
            this.GetTimeStamp = GetTimeStamp;
            sensorHits.Clear();
            hitTimestamps.Clear();
            hitElement.Clear();
            isRecording = true;
        }

        public void StopRecording()
        {
            isRecording = false;
        }
    }

    private void Awake()
    {
        sensors = new List<Vector3>();
        hitsRecording = new Record();
        layerMask = int.MaxValue;
        enable = false;
    }


    public void ClearSensors()
    {
        sensors.Clear();
        Config();
    }

    /*NOTA: cambiar esto. Todo será "StimuliTrigger"; hay que utilizar otro campo para el filtro, p.e. tipo de material*/
    public void SetLayerMask(int layerMask) => this.layerMask = layerMask;

    public void AddSensor(Vector3 direction, float detectionDistance)
    {
        if (direction.magnitude == 0)
            return;
        sensors.Add(direction.normalized * detectionDistance);
    }

    public void AddSensorsInFoV(float fieldOfViewX, float fieldOfViewY, int M, int N, float detectionDistance)
    {
        if (M < 1 || N < 1)
            return;
        int size = M * N;
        for (int i = 0; i < size; i++)
            sensors.Add(GetRay(fieldOfViewX, fieldOfViewY, M, N, i) * detectionDistance);
        Config();
    }

    private Vector3 GetRay(float fieldOfViewX, float fieldOfViewY, int M, int N, int index)
    {
        if (M < 1 || N < 1)
            return Vector3.zero;
        float angleY = N > 1 ? (((float)(Mathf.Floor((float)index / M)) / (N - 1)) - 0.5f) * fieldOfViewY : 0;
        float angleX = M > 1 ? (((float)(index % M) / (M - 1)) - 0.5f) * fieldOfViewX : 0;
        return Quaternion.Euler(angleY, angleX, 0) * Vector3.forward;
    }

    public IRecordable GetRecordable() => hitsRecording;

    public abstract List<Vector3> GetAllDetectionsFromMovement(List<Pose> movement);

    public abstract void Config();

    protected abstract bool Detect(Vector3 position, Quaternion rotation, out RaycastHit hit);

    // Update is called once per frame
    void FixedUpdate()
    {
        if (enable && (++frameCounter >= maxFrameCounter))
        {
            if(Detect(transform.position, transform.rotation, out RaycastHit hit))
            {
                onDetectionCallback?.Invoke(hit);
                hitsRecording.AddHit(hit);
            }
            else
            {
                onNullDetection?.Invoke();
            }
            frameCounter = 0;
        }
    }

    /*Subclasses - implemented Raycast sensors*/
    public class GetClosest : RaycastSensor
    {
        public bool focusableOnly { get; set; } = false;
        public override void Config() { }

        public override List<Vector3> GetAllDetectionsFromMovement(List<Pose> movement)
        {
            List<Vector3> retVal = new List<Vector3>();
            int count = 0;
            foreach (Pose pose in movement)
            {
                if (Detect(pose.position, pose.rotation, out RaycastHit hit))
                {
                    count++;
                    retVal.Add(hit.point);
                }

            }
            return retVal;
        }

        protected override bool Detect(Vector3 position, Quaternion rotation, out RaycastHit hit)
        {
            hit = new RaycastHit();
            float minDistance = float.MaxValue;
            //Debug.Log("Name of prefab: " + gameObject.name);
            //Debug.DrawRay(position, rotation * sensors[0], Color.red);
            foreach (Vector3 sensor in sensors)
            {
                if (Physics.Raycast(position, rotation * sensor, out RaycastHit lastHit, sensor.magnitude, layerMask))
                {
                    if (minDistance > hit.distance)
                    {
                        minDistance = hit.distance;
                        hit = lastHit;
                    }
                }
            }
            return minDistance != float.MaxValue;
        }
    }

    public class SweepFoV : RaycastSensor
    {
        private int sweepIndex = 0;
        private int sweepIndexIncrement = 1;
        public bool focusableOnly { get; set; } = false;

        /*Only takes Sweep Increments which verify the Hull-Dobell theorem - which are seeds of this linear congruential generator*/
        public bool SetSeed(int seed)
        {
            if (BigInteger.GreatestCommonDivisor(seed, sensors.Count) != 1)
                return false;
            sweepIndexIncrement = seed;
            return true;
        }

        public List<int> GetValidSeeds()
        {
            List<int> result = new List<int>();

            for (int i = 0; i < sensors.Count; i++)
                if (BigInteger.GreatestCommonDivisor(i, sensors.Count) == 1)
                    result.Add(i);

            return result;
        }

        public override void Config()
        {
            sweepIndex = 0;
            sweepIndexIncrement = 1;
        }

        public override List<Vector3> GetAllDetectionsFromMovement(List<Pose> movement)
        {
            List<Vector3> retVal = new List<Vector3>();
            foreach (Pose pose in movement)
            {
                if (Detect(pose.position, pose.rotation, out RaycastHit hit))
                    retVal.Add(hit.point);
            }
            return retVal;
        }


        protected override bool Detect(Vector3 position, Quaternion rotation, out RaycastHit hit)
        {
            hit = new RaycastHit();
            if (sensors.Count == 0)
                return false;

            sweepIndex = (sweepIndex + sweepIndexIncrement) % sensors.Count;
            //Debug.Log("Sweep index: " + sweepIndex);
            //Debug.DrawRay(position, rotation * sensors[sweepIndex]);
            if (sensors.Count == 1)
                Debug.DrawRay(position, rotation * sensors[0], Color.red);
            if (Physics.Raycast(position, rotation * sensors[sweepIndex], out hit, sensors[sweepIndex].magnitude, layerMask))
                return focusableOnly ? hit.transform.GetComponent<SensibleElement>().focusable : true;

            return false;
        }
    }
}
