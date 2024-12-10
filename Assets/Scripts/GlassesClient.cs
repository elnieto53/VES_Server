using UnityEngine;
#if XR
using UnityEngine.XR.ARFoundation;
#endif

public class GlassesClient : MonoBehaviour
{
#if XR
  private readonly int netPort = 11000;
    private readonly int vsPort = 11001;
    private readonly int moCapPort = 11002;
    private readonly byte deviceID = 6;
    private readonly byte moCapBodyID = 1;
    private readonly byte moCapPoseChannel = 4;
    private readonly byte moCapOrientationChannel = 3;

    public GameObject arCam;
    public ARSession arSession;
    //public GameObject headPrefab;

    private VirtualScenarioManager virtualScenario;
    private MoCapManager moCapManager;
    private MoCapPose head;

    private Vector3 positionOffset = Vector3.zero;
    private Quaternion rotationOffset = Quaternion.identity;

    private Vector3 startingPosition = Vector3.one;
    private Vector2 startingXZOrientation = Vector2.up;
    private bool resetPoseTracking = false;
    private bool stopPoseTracking = false;
    private bool resumePoseTracking = false;

    void Start()
    {
        if (arCam == null)
            throw new System.Exception("No AR Camera was selected");
        if (arSession == null)
            throw new System.Exception("No AR Session was selected");

        /*Create and init a new VirtualScenario manager. This suffices to share host/remote ScenarioElements*/
        virtualScenario = new VirtualScenarioManager(vsPort, netPort, deviceID, true);
        virtualScenario.Init();

        /*Create a new "MoCapPose" channel shared in the VirtualScenario*/
        moCapManager = new MoCapManager(moCapPort, virtualScenario, moCapPoseChannel, moCapOrientationChannel, new JitterModeling.NoJitter());
        moCapManager.Init();

        /*Create a new pose element, which coordinates and fuses "BodyPart" ScenarioElements*/
        head = moCapManager.AddHostPoseElement((byte)MoCapElement.Spawnable.Head, moCapBodyID);

        /*Add pose tracking commands callback for remote control*/
        moCapManager.ResetPoseTracking = NotifyResetTracking;
        moCapManager.StopPoseTracking = NotifyStopTracking;
        moCapManager.ResumePoseTracking = NotifyResumeTracking;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        /*This line limits the fps. Without it, the power consumption increases several fold*/
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
    }

    void NotifyResetTracking((Vector3 position, Vector2 xzRotation) pose)
    {
        startingPosition = pose.position;
        startingXZOrientation = pose.xzRotation;
        resetPoseTracking = true;
    }
    void NotifyStopTracking() => stopPoseTracking = true;
    void NotifyResumeTracking() => resumePoseTracking = true;


    void CheckResetPoseTracking()
    {
        if (!resetPoseTracking)
            return;
        resetPoseTracking = false;

        arSession.Reset();

        //positionOffset = startingPosition - arCam.transform.position;
        //Vector3 userForward = arCam.transform.rotation * Vector3.forward;
        //userForward.y = 0;
        positionOffset = startingPosition;
        Vector3 userForward = Vector3.forward;
        rotationOffset = Quaternion.FromToRotation(userForward, new Vector3(startingXZOrientation.x, 0, startingXZOrientation.y));
    }

    void CheckStopPoseTracking()
    {
        if (!stopPoseTracking)
            return;
        stopPoseTracking = false;

        arSession.enabled = false;
    }
        

    void CheckResumePoseTracking()
    {
        if (!resumePoseTracking)
            return;
        resumePoseTracking = false;

        arSession.enabled = true;
    }

    void FixedUpdate()
    {
        head.pose = new Pose(arCam.transform.position + positionOffset, rotationOffset * arCam.transform.rotation);
        //headPrefab.transform.position = head.pose.position;
        //headPrefab.transform.rotation = head.pose.rotation;
        virtualScenario.Update();

        CheckResetPoseTracking();
        CheckStopPoseTracking();
        CheckResumePoseTracking();

#if UNITY_ANDROID
        virtualScenario.networkManager.deviceDataPkg.batteryLevel = (uint)(SystemInfo.batteryLevel * 100);
#endif
    }


    void OnDestroy()
    {
        virtualScenario.DeInit();
        moCapManager.DeInit();
    }
#endif
}

