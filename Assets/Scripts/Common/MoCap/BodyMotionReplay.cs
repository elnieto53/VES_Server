using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BodyMotionReplay : MonoBehaviour
{
    public Body body { get; set; } = null;
    public List<BodyMotionSerializer.BodyPartPose> bodyMotion;

    private TimerGUI timerGUI;
    private int frameIndex = 0;
    private NetClock netClock;

    private bool initialized = false;

    public Action bodyMoved { get; set; }

    public void Init(BodyMotionSerializer serializer, Body body)
    {
        GameObject panel = GameObject.Find("MainPanel");
        timerGUI = ((GameObject)Instantiate(Resources.Load(ResourcesPath.Prefabs.GUI.timer), panel.transform, true)).GetComponent<TimerGUI>();
        timerGUI.playCallback = Play;
        timerGUI.stopCallback = Stop;
        timerGUI.resetCallback = Reload;

        
        this.body = body;
        ImportBodyMotion(serializer);
        

        
        //body = Body.LoadBody(serializer.articulations, serializer.rootID, 10);
        //bodyMotion = serializer.bodyMotion;
        netClock = new NetClock();
        //if (bodyMotion.Count > 0)
        //    netClock.Restart(bodyMotion[0].timestamp);
        
        netClock.Start();
        initialized = true;
    }

    public void Init(string name, string folderPath) => Init(BodyMotionSerializer.LoadMotionReplay(name, folderPath));

    public void Init(string name, string folderPath, Body body) => Init(BodyMotionSerializer.LoadMotionReplay(name, folderPath), body);

    public void Init(BodyMotionSerializer serializer) => Init(serializer, Body.GetDefault(200));


    private void ImportBodyMotion(BodyMotionSerializer data)
    {
        int replayTimeOffest = data.bodyMotion[1].timestamp; //Set the timestamp of the first motion event as the time origin

        List<BodyMotionSerializer.BodyPartPose> recordedBodyMotion = new List<BodyMotionSerializer.BodyPartPose>();

        foreach (BodyMotionSerializer.BodyPartPose bodyPartPose in data.bodyMotion)
            recordedBodyMotion.Add(new BodyMotionSerializer.BodyPartPose(bodyPartPose.id, bodyPartPose.pose, bodyPartPose.timestamp - replayTimeOffest));

        bodyMotion = recordedBodyMotion.OrderBy(x => x.timestamp).ToList();

        Debug.Log("OKKKeeyyyy body! " + data.bodyMotion.Count);
    }

    private bool UpdatePose()
    {
        bool motionUpdated = false;

        if (!initialized)
            return motionUpdated;

        if (bodyMotion == null)
            return motionUpdated;

        while (bodyMotion.Count > 0)
        {
            BodyMotionSerializer.BodyPartPose firstData = bodyMotion.First();
            if (netClock.GetTimeStamp() < firstData.timestamp)
                return motionUpdated;

            body.GetBodyPart((MoCapElement.Spawnable)firstData.id).prefab.transform.rotation = firstData.pose.rotation;
            bodyMotion.Remove(firstData);
            motionUpdated = true;
        }

        return motionUpdated;
        //replayBody.GetBodyPart(3).prefab.transform.rotation;
    }



    private void Reload()
    {
        Debug.Log("'Reload' pulsed");
        netClock.Restart(bodyMotion[0].timestamp);
        netClock.Stop();
        frameIndex = 0;
    }

    private void Play()
    {
        Debug.Log("'Play' pulsed");
        netClock.Start();
        //timerGUI.PlayTimer();
    }

    private void Stop()
    {
        Debug.Log("'Stop' pulsed");
        netClock.Stop();
    }

    public void FixedUpdate()
    {
        if (UpdatePose())
            bodyMoved?.Invoke();
    }
}
