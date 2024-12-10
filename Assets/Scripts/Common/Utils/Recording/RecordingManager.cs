using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class RecordingManager
{
    private static List<IRecordable> records = new List<IRecordable>();
    public static Stopwatch clock { get; private set; } = new Stopwatch();
    public static bool isRecording = false;

    private static int GetTimeStamp()
    {
        int retVal;
        Monitor.Enter(clock);
        retVal = unchecked((int)clock.ElapsedMilliseconds);
        Monitor.Exit(clock);
        return retVal;
    }

    public static void Add(IRecordable recordable)
    {
        if(!records.Contains(recordable))
            records.Add(recordable);
    }
    public static void Remove(IRecordable recordable)
    {
        if (records.Contains(recordable))
            records.Remove(recordable);
    }
    public static void StartRecording()
    {
        clock.Start();
        foreach (IRecordable recordable in records)
            recordable.StartRecording(GetTimeStamp);
        isRecording = true;
    }
    public static void StopRecording()
    {
        foreach (IRecordable recordable in records)
            recordable.StopRecording();
        clock.Stop();
        clock.Reset();
        isRecording = false;
    }
    public static void SaveRecording(string folderPath, bool overwrite)
    {
        foreach (IRecordable recordable in records)
            recordable.SaveRecording(folderPath, overwrite);
    }
}
