using System;
using UnityEngine;



public class EnvelopingVAS : MonoBehaviour, IRaycastSSD, ISSD
{
    private Configuration config;
    private bool _ssdEnabled;
    VirtualScenarioManager.ChannelManager<HapticStimuli> channelManager;
    private HapticStimuli mainHapticStimuli;
    RaycastSensor.GetClosest sensor;

    private GameObject renderedFieldOfView;

    public bool ssdEnabled {
        get => _ssdEnabled;
        set {
            _ssdEnabled = value;
            sensor.enable = value;
            renderedFieldOfView.SetActive(value);
            mainHapticStimuli.isOn = value;
        }
    }
    public Action<RaycastHit> onDetectionCallback { get; set; }

    [Serializable]
    public struct Configuration
    {
        public float fieldOfViewX;      // Horizontal field of view
        public float fieldOfViewY;      // Vertical field of view
        public float detectionDistance; /*(Meters)*/
        public float period;            /*(Seconds)*/
        public byte channel;             //Haptic channel
    }

    // Use this for initialization
    public void Init(VirtualScenarioManager.ChannelManager<HapticStimuli> channelManager)
    {
        this.channelManager = channelManager;
        mainHapticStimuli = channelManager.AddHostScenarioElement((byte)HapticStimuli.SpawnableHapticStimuli.Default);
        //mainHapticStimuli.vibrationAmplitude = HapticStimuli.HapticEffect.drv2605L_effect_buzz5_20P;

        sensor = gameObject.AddComponent<RaycastSensor.GetClosest>();
        sensor.SetLayerMask(LayerMask.GetMask("StimuliTrigger"));
        sensor.onDetectionCallback = EnvelopingVASProcessor;
        sensor.focusableOnly = true;

        renderedFieldOfView = new GameObject();
        InitFoVRenderer(renderedFieldOfView);

        Configuration defaultConfig = new Configuration();
        defaultConfig.detectionDistance = 6f;
        defaultConfig.period = 0.02f;
        defaultConfig.fieldOfViewX = 5;
        defaultConfig.fieldOfViewY = 5;
        defaultConfig.channel = channelManager.channel;

        SetConfig(defaultConfig);
        ssdEnabled = true;
    }

    private void InitFoVRenderer(GameObject FoVGameobject)
    {
        /*
         * The purpose of this code is to make visible the field of view of this test, but probably the easiest way to do that would require
         * including this script in the 'player' gameObject
         */
        renderedFieldOfView.layer = 13;
        renderedFieldOfView.transform.localPosition = Vector3.zero;
        renderedFieldOfView.transform.localRotation = Quaternion.Euler(Vector3.zero);
        renderedFieldOfView.transform.parent = this.transform;
        renderedFieldOfView.AddComponent<MeshFilter>();
        renderedFieldOfView.AddComponent<MeshRenderer>().material = (Material)Resources.Load(ResourcesPath.Material.transparentRed); // Assign a material for the renderer
        renderedFieldOfView.SetActive(false);
    }


    private void EnvelopingVASProcessor(RaycastHit hit)
    {
        //Debug.Log("Material detected!!");
        //if ((audioMat = hit.transform.gameObject.GetComponent<AudioMaterial>()) != null)
        if (hit.transform.GetComponent<SensibleElement>().focusable)
        {
            mainHapticStimuli.Impulse((byte)((1 - hit.distance / config.detectionDistance) * 95 + 160), (int)(config.period * 1000) + 10);
            onDetectionCallback?.Invoke(hit);
        }
    }


    public VirtualScenarioManager.ChannelManager<HapticStimuli> GetChannelManager()
    {
        return channelManager;
    }

    public void DeInit()
    {
        channelManager.DestroyHostScenarioElement(mainHapticStimuli);
        Destroy(renderedFieldOfView);
        Destroy(gameObject);
    }

    public void OnDestroy()
    {
        DeInit();
    }

    //public void SetEnable(bool enable)
    //{
    //    testEnabled = enable;
    //    sensor.enable = enable;
    //    renderedFieldOfView.SetActive(enable);
    //    mainHapticStimuli.isOn = enable;
    //}

    /* This function is called by the player through the GUI ('ProjectedVASGUI'), in order to make this aura "envelop and follow" him
     */
    public void AdoptMe(Transform parent)
    {
        transform.parent = parent.transform;
        transform.localEulerAngles = Vector3.zero;
        transform.localPosition = Vector3.zero;
        renderedFieldOfView.transform.localPosition = Vector3.zero;
        renderedFieldOfView.transform.localEulerAngles = new Vector3(-90, 0, 0);
    }

    public void SetConfig(Configuration config)
    {
        this.config = config;
        sensor.ClearSensors();
        sensor.AddSensorsInFoV(config.fieldOfViewX, config.fieldOfViewY, (int)Math.Ceiling(config.fieldOfViewX / 5), (int)Math.Ceiling(config.fieldOfViewY / 5), config.detectionDistance);

        /*Work around to avoid */
        if (config.fieldOfViewX <= 60 && config.fieldOfViewY <= 60)
        {
            renderedFieldOfView.GetComponent<MeshFilter>().sharedMesh = MeshGenerator.FieldOfViewMesh(config.detectionDistance, config.fieldOfViewX, config.fieldOfViewY);
        }
        else
        {
            renderedFieldOfView.GetComponent<MeshFilter>().sharedMesh = null;
        }
        sensor.scanPeriod = config.period;
    }

    public Configuration GetConfig() => config;
}
