using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;


public class TeleportElement : MonoBehaviour, IRecordable
{
    private int counter = 0;
    public List<Vector3> teleportPoints = new List<Vector3>();
    private List<int> teleportTimestamps = new List<int>();
    private bool isRecording = false;
    private Func<int> GetTimeStamp;


    [Serializable]
    public class Record
    {
        public List<Vector3> teleportPoints = new List<Vector3>();
        public List<int> teleportTimestamps = new List<int>();

        public Record()
        {
            teleportPoints = new List<Vector3>();
            teleportTimestamps = new List<int>();
        }

        public Record(TeleportElement element)
        {
            teleportPoints = new List<Vector3>(element.teleportPoints);
            teleportTimestamps = new List<int>(element.teleportTimestamps);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        if(teleportPoints.Count != 0)
            transform.localPosition = teleportPoints[0];
        RecordingManager.Add(this);
    }

    private void OnDestroy() => RecordingManager.Remove(this);

    public void Teleport()
    {
        if(teleportPoints.Count > 0)
        {
            if (counter >= teleportPoints.Count - 1)
                gameObject.GetComponent<MeshRenderer>().material = (Material)Resources.Load(ResourcesPath.Material.red);
            transform.localPosition = teleportPoints[++counter % teleportPoints.Count];
            if (isRecording)
                teleportTimestamps.Add(GetTimeStamp());
        }
    }

    public void StartRecording(Func<int> GetTimeStamp)
    {
        this.GetTimeStamp = GetTimeStamp;
        isRecording = true;
    }

    public void StopRecording() => isRecording = false;

    public void SaveRecording(string folderPath, bool overwrite) => SaveRecording("Focused elements", folderPath, overwrite);

    public void SaveRecording(string filename, string folderPath, bool overwrite)
        => new JsonSerializer<Record>(new Record(this)).SaveDataToFile(filename, folderPath, overwrite);
}
