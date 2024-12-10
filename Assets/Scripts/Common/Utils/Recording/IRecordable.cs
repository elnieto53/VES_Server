using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRecordable
{
    public void StartRecording(Func<int> GetTimeStamp);
    public void StopRecording();

    /*Returns the serialized recording in JSON format*/
    //public string GetRecording();
    public void SaveRecording(string folderPath, bool overwrite);
    public void SaveRecording(string filename, string folderPath, bool overwrite);
}
