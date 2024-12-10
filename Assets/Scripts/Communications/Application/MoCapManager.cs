using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using UnityEngine;

public class MoCapManager : ApplicationLayer
{
    private VirtualScenarioManager vsManager;
    private VirtualScenarioManager.ChannelManager<MoCapPose> poseChannelManager;
    private VirtualScenarioManager.ChannelManager<MoCapOrientation> orientationChannelManager;
    private List<Body> bodies;

    public byte poseChannel { get => poseChannelManager.channel; }
    public byte orientationChannel { get => poseChannelManager.channel; }
    public Action<(Vector3 position, Vector2 xzRotation)> ResetPoseTracking { get; set; } = null;
    public Action StopPoseTracking { get; set; } = null;
    public Action ResumePoseTracking { get; set; } = null;
    public Action<AlignementData> CalibrateRotation { get; set; } = null;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CalibrationData
    {
        public Quaternion initialRotation;
        public Vector3 localRotationReference;
        public Vector3 globalRotationReference;
        public CalibrationData(Quaternion initialRotation, Vector3 localRotationReference, Vector3 globalRotationReference)
        {
            this.initialRotation = initialRotation;
            this.localRotationReference = localRotationReference;
            this.globalRotationReference = globalRotationReference;
        }

        public CalibrationData(AlignementData data)
        {
            initialRotation = data.initialRotation;
            localRotationReference = data.localRotationReference;
            globalRotationReference = data.globalRotationReference;
        }

        public CalibrationData(byte[] data) : this() => this = PkgSerializer.GetStruct<CalibrationData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ResetPoseData
    {
        public Vector3 initialPosition;
        public Vector2 xzOrientation;
        public ResetPoseData(Vector3 initialPosition, Vector2 xzOrientation)
        {
            this.initialPosition = initialPosition;
            this.xzOrientation = xzOrientation;
        }

        public ResetPoseData(byte[] data) : this() => this = PkgSerializer.GetStruct<ResetPoseData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public enum MoCapCommand
    {
        CalibrateBodyPart = 0,
        CalibrateIMUOffset = 1,
        ResetPoseTracking = 2,
        StopPoseTracking = 3,
        ResumePoseTracking = 4
    }

    public MoCapManager(int hostPort, VirtualScenarioManager vsManager, byte poseChannel, byte orientationChannel) : base(hostPort)
    {
        this.vsManager = vsManager;
        if (!vsManager.AddChannel(poseChannel, LinkToBodyPart, out poseChannelManager))
            throw new Exception("The channel " + poseChannel + " already exists");

        if (!vsManager.AddChannel(orientationChannel, LinkToBodyPart, out orientationChannelManager))
            throw new Exception("The channel " + orientationChannel + " already exists");

        bodies = new List<Body>();
    }

    public Body CreateBody(byte bodyID)
    {
        Body newBody = Body.GetDefault(bodyID);
        bodies.Add(newBody);
        return newBody;
    }

    private void LinkToBodyPart(MoCapPose moCapPose)
    {
        Body body = bodies.Find(body => body.ID == moCapPose.bodyID);
        if (body == null)
            body = CreateBody(moCapPose.bodyID);

        /*Search for a body part with the same prefabID (spawnable element)*/
        BodyPart bodyPart = body.GetBodyPart((MoCapElement.Spawnable)moCapPose.prefabID);
        if (bodyPart != null)
            bodyPart.poseElement = moCapPose;
    }

    private void LinkToBodyPart(MoCapOrientation moCapOrientation)
    {
        Body body = bodies.Find(body => body.ID == moCapOrientation.bodyID);
        if (body == null)
            body = CreateBody(moCapOrientation.bodyID);

        /*Search for a body part with the same prefabID (spawnable element)*/
        BodyPart bodyPart = body.GetBodyPart((MoCapElement.Spawnable)moCapOrientation.prefabID);
        if (bodyPart != null)
            bodyPart.orientationElement = moCapOrientation;
    }

    public bool TryGetBodyParts(int bodyID, out List<BodyPart> bodyParts)
    {
        Body body = bodies.Find(body => body.ID == bodyID);
        if (body == null)
        {
            bodyParts = null;
            return false;
        }

        //bodyParts = body.GetBodyParts(bodyPart => bodyPart.moCapElement != null);
        bodyParts = body.GetBodyParts();
        return bodyParts.Count > 0 ? true : false;
    }

    public bool TryGetBodyParts(int bodyID, out List<BodyPart> bodyParts, Predicate<BodyPart> predicate)
    {
        Body body = bodies.Find(body => body.ID == bodyID);
        if (body == null)
        {
            bodyParts = null;
            return false;
        }

        //bodyParts = body.GetBodyParts(bodyPart => bodyPart.moCapElement != null).FindAll(predicate);
        bodyParts = body.GetBodyParts().FindAll(predicate);
        return bodyParts.Count > 0 ? true : false;
    }

    public MoCapPose AddHostPoseElement(MoCapElement.Spawnable prefabID, byte bodyID)
    {
        /*Search for a body which matches the bodyID. If it does not exist, create it*/
        MoCapPose retval = poseChannelManager.AddHostScenarioElement((byte)prefabID);
        retval.bodyID = bodyID;
        LinkToBodyPart(retval);
        return retval;
    }

    public MoCapOrientation AddHostOrientationElement(MoCapElement.Spawnable prefabID, byte bodyID)
    {
        /*Search for a body which matches the bodyID. If it does not exist, create it*/
        MoCapOrientation retval = orientationChannelManager.AddHostScenarioElement((byte)prefabID);
        retval.bodyID = bodyID;
        LinkToBodyPart(retval);
        return retval;
    }

    public void UpdateBodyPose()
    {
        foreach (Body body in bodies)
            body.UpdatePose();
    }
    public VirtualScenarioManager.ChannelManager<MoCapPose> GetPoseChannelManager() => poseChannelManager;
    public VirtualScenarioManager.ChannelManager<MoCapOrientation> GetOrientationChannelManager() => orientationChannelManager;

    public void CalibrateBody(byte bodyID)
    {
        if (!TryGetBody(bodyID, out Body body))
            return;

        body.ResetPose();
        Debug.Log("Calibrating...");
        List<(MoCapElement, AlignementData)> calibrationData = body.GetAlignementData();

        foreach ((MoCapElement element, AlignementData alignement) data in calibrationData)
        {
            if (!data.element.IsHost)
            {
                /*Send "calibrate" commands to remote devices*/
                byte[] pkg = BuildCommandPkg((byte)MoCapCommand.CalibrateBodyPart, new CalibrationData(data.alignement).GetBytes());
                defaultIO.SendPkgTo(data.element.origin, pkg);
            }
            else
            {
                /*THIS IS RESERVED FOR HOST ELEMENTS*/
                CalibrateRotation?.Invoke(data.alignement);
            }
        }
    }

    public void CalibrateIMUOffset(IPAddress address)
    {
        byte[] pkg = BuildCommandPkg((byte)MoCapCommand.CalibrateIMUOffset);
        defaultIO.SendPkgTo(address, pkg);
    }

    public void StopRootTracking(int bodyID)
    {
        MoCapElement rootElement;
        if (!TryGetBody(bodyID, out Body body) || (rootElement = body.GetRootBodyPart().moCapElement) == null)
            return;

        if (rootElement.IsHost)
        {
            /*If the element is owned by this device, execute the command*/
            StopPoseTracking?.Invoke();
        }
        else
        {
            /*If it is a remote element, send a 'StopPoseTracking' command*/
            byte[] pkg = BuildCommandPkg((byte)MoCapCommand.StopPoseTracking);
            defaultIO.SendPkgTo(rootElement.origin, pkg);
        }
    }


    public void ResetRootTracking(int bodyID, Vector3 position, Vector2 xzOrientation)
    {
        MoCapElement rootElement;
        if (!TryGetBody(bodyID, out Body body) || (rootElement = body.GetRootBodyPart().moCapElement) == null)
            return;

        if (rootElement.IsHost)
        {
            /*If the element is owned by this device, execute the command*/
            StopPoseTracking?.Invoke();
        }
        else
        {
            /*If it is a remote element, send a 'ResetPoseTracking' command*/
            ResetPoseData data = new ResetPoseData(position, xzOrientation);
            byte[] pkg = BuildCommandPkg((byte)MoCapCommand.ResetPoseTracking, data.GetBytes());
            defaultIO.SendPkgTo(rootElement.origin, pkg);
        }
    }

    public void ResumeRootTracking(int bodyID)
    {
        MoCapElement rootElement;
        if (!TryGetBody(bodyID, out Body body) || (rootElement = body.GetRootBodyPart().moCapElement) == null)
            return;

        if (rootElement.IsHost)
        {
            /*If the element is owned by this device, execute the command*/
            ResumePoseTracking?.Invoke();
        }
        else
        {
            /*If it is a remote element, send a 'ResumePoseTracking' command*/
            byte[] pkg = BuildCommandPkg((byte)MoCapCommand.ResumePoseTracking);
            defaultIO.SendPkgTo(rootElement.origin, pkg);
        }
    }

    private bool TryGetBody(int bodyID, out Body body)
    {
        body = bodies.Find(body => body.ID == bodyID);
        return body != null;
    }

    public override void ExecuteNetCommand(IPAddress origin, byte[] data)
    {
        MoCapCommand command = (MoCapCommand)GetPkgCommand(data);
        //Debug.Log("(Virtual Scenario Manager) " + this.defaultIO.GetHostEP().Port + " Command " + data[0] + ". Channel: " + channel + " received from: " + origin.ToString());
        switch (command)
        {
            case MoCapCommand.CalibrateBodyPart:
                /*(for now it is not needed)*/
                break;
            case MoCapCommand.CalibrateIMUOffset:
                /*(for now it is not needed)*/
                break;
            case MoCapCommand.ResetPoseTracking:
                ResetPoseData rpData = new ResetPoseData(GetPkgData(data));
                ResetPoseTracking?.Invoke((rpData.initialPosition, rpData.xzOrientation));
                break;
            case MoCapCommand.StopPoseTracking:
                StopPoseTracking?.Invoke();
                break;
            case MoCapCommand.ResumePoseTracking:
                ResumePoseTracking?.Invoke();
                break;
        }
    }
}
