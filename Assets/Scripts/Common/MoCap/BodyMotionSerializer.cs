using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[Serializable]
public class BodyMotionSerializer : IRecordable
{
    public Body body { get; private set; }
    private List<BodyPartPose> _bodyMotion;
    private NetClock netClock;

    /*Serializable elements*/
    public MoCapElement.Spawnable rootID;
    public List<Body.Articulation> articulations;
    public List<BodyPartPose> bodyMotion;
    private Func<int> GetTimeStamp;

    [Serializable]
    public class BodyPartPose
    {
        public byte id;
        public Pose pose;
        public int timestamp;

        public BodyPartPose(byte id, Pose pose, int timestamp)
        {
            this.id = id;
            this.pose = pose;
            this.timestamp = timestamp;
        }
    }

    public delegate void DataLoadedCallback(BodyMotionSerializer data);


    public BodyMotionSerializer()
    {
        rootID = 0;
        articulations = new List<Body.Articulation>();
        _bodyMotion = new List<BodyPartPose>();
    }

    public BodyMotionSerializer(Body body, NetClock netClock)
    {
        this.body = body;
        this.netClock = netClock;
        rootID = body.rootID;
        articulations = body.articulations;
        _bodyMotion = new List<BodyPartPose>();
    }


    public void SaveMotionToFile(string name, string folderPath, bool overwrite, Action callback)
    {
        bodyMotion = _bodyMotion;
        _bodyMotion = new List<BodyPartPose>();
        //bodyMotion.Sort((p, q) => p.timestamp.CompareTo(q.timestamp));
        JsonSerializer<BodyMotionSerializer> serializer = new JsonSerializer<BodyMotionSerializer>(this);
        Thread newIOThread = new Thread(() => { serializer.SaveDataToFile(name, folderPath, overwrite); callback?.Invoke(); });
        newIOThread.Start();
        //serializer.SaveDataToFile(name, folderPath, overwrite); //"Body_ehe"
    }

    public void StartRecording(Func<int> GetTimeStamp)
    {
        this.GetTimeStamp = GetTimeStamp;
        body.updateCallback = AddPoseRecord;
    }

    public void StopRecording() => body.updateCallback = null;

    public void SaveRecording(string folderPath, bool overwrite) => SaveMotionToFile("Body Motion", folderPath, overwrite, null);
    public void SaveRecording(string filename, string folderPath, bool overwrite) => SaveMotionToFile(filename, folderPath, overwrite, null);

    public void AddPoseRecord()
    {
        List<BodyPart> bodyParts = body.GetBodyParts();
        int timestamp = GetTimeStamp();
        foreach (BodyPart bodyPart in bodyParts)
        {
            _bodyMotion.Add(new BodyPartPose((byte)bodyPart.prefabID, bodyPart.pose, timestamp));
        }
    }

    public static void LoadData(string name, string folderPath, DataLoadedCallback callback)
    {
        Thread newIOThread = new Thread(() => callback?.Invoke(JsonSerializer<BodyMotionSerializer>.LoadDataFromFile(folderPath, name)));
        newIOThread.Start();
    }


    //public static void LoadMotionReplay(string name, string folderPath, Action<Body> callback)
    //{
    //    BodyMotionSerializer bodyMotion = JsonSerializer<BodyMotionSerializer>.LoadDataFromFile(folderPath, name);
    //    Thread ioThread = new Thread(() => callback?.Invoke(JsonSerializer<BodyMotionSerializer>.LoadDataFromFile(folderPath, name).body));
    //    ioThread.Start();
    //}

    public static BodyMotionSerializer LoadMotionReplay(string name, string folderPath)
        => JsonSerializer<BodyMotionSerializer>.LoadDataFromFile(folderPath, name);

    public static void LoadMotionReplay(string name, string folderPath, Action<BodyMotionSerializer> callback)
    {
        BodyMotionSerializer bodyMotion = JsonSerializer<BodyMotionSerializer>.LoadDataFromFile(folderPath, name);
        callback?.Invoke(bodyMotion);
        return;
    }
}

