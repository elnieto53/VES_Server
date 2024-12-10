using System;
using System.Collections.Generic;
using UnityEngine;



public class ProjectedVAS : MonoBehaviour, IRaycastSSD, ISSD
{
    private GameObject stereopixelPrefab;
    private bool _ssdEnabled;
    private Configuration config;

    private RaycastSensor.SweepFoV sensor { get; set; }
    private RaycastSensor.SweepFoV foveaSensor { get; set; }
    private GameObject RenderedFieldOfView;
    private SensibleElement focusedElement { get; set; }

    /*Provides 'hit' data*/
    public Action<RaycastHit> onDetectionCallback { get; set; }


    /*Variables to detect 'focused element'*/
    private float focusTimeout;
    private float _focusInterval;
    public float focusInterval {
        get => _focusInterval;
        set {
            if (value > 0)
            {
                _focusInterval = value;
                focusTimeout = value + Time.time;
            }
        }
    }

    public bool ssdEnabled
    {
        get => _ssdEnabled;
        set
        {
            _ssdEnabled = value;
            sensor.enable = value;
            foveaSensor.enable = value;
            RenderedFieldOfView.SetActive(value);
        }
    }

    [Serializable]
    public struct Configuration
    {
        public int M;                   //Stereopixels array - number of columns - use prime numbers
        public int N;                   //Stereopixels array - number of rows - use prime numbers
        public float fieldOfViewX;      //Horizontal field of view for acoustic image (stereopixels)
        public float fieldOfViewY;      //Vertical field of view which contains the stereopixels
        public float detectionDistance;
        public int seed;                //This integer serves for randomizing the sequence of stereopixels (nextIndex = randomizer%(M*N))
                                        //For this to work, SCM(randomizer, M, N) = 1 , where SCM is the smallest common multiple.
                                        //Therefore, prime numbers are preferred for M and N
        public float period;            //Stereopixels' spawning frequency in seconds. In the original VAS project, it was of 1 KHz aprox. (probably too fast for this design)
        public float focusVolume;   
        public float peripheralVolume;
        public Configuration(float fieldOfViewX, float fieldOfViewY, int M, int N, float detectionDistance, int seed, float period, float focusVolume, float peripheralVolume)
        {
            this.fieldOfViewX = fieldOfViewX;
            this.fieldOfViewY = fieldOfViewY;
            this.M = M;
            this.N = N;
            this.detectionDistance = detectionDistance;
            this.seed = seed;
            this.period = period;
            this.focusVolume = focusVolume;
            this.peripheralVolume = peripheralVolume;
        }
    }

    //[Serializable]
    //public struct PVASElementData
    //{
    //    public StereopixelData backgroundData;
    //    public StereopixelData figureData;
    //}

    [Serializable]
    public struct StereopixelData
    {
        public string audioClipPath;
        public float volume;
        public float pitch;
        public StereopixelData(string audioClipPath, float volume, float pitch)
        {
            this.audioClipPath = audioClipPath;
            this.volume = volume;
            this.pitch = pitch;
        }
    }

    // Use this for initialization
    public void Init()
    {
        //Debug.Log("Trying to start the projectedVAS!!!");
        stereopixelPrefab = (GameObject)Resources.Load(ResourcesPath.Prefabs.audioSource);

        sensor = gameObject.AddComponent<RaycastSensor.SweepFoV>();
        sensor.SetLayerMask(LayerMask.GetMask("StimuliTrigger"));
        sensor.onDetectionCallback = ProjectedVASProcessor;
        foveaSensor = gameObject.AddComponent<RaycastSensor.SweepFoV>();
        foveaSensor.SetLayerMask(LayerMask.GetMask("StimuliTrigger"));
        foveaSensor.onDetectionCallback = OnFoveaDetection;
        foveaSensor.onNullDetection = OnFoveaIdle;

        //Default configuration
        Configuration initialConfig;
        initialConfig.M = 17;                              //VAS: 17
        initialConfig.N = 7;                               //VAS: 9
        initialConfig.fieldOfViewX = 50;                   //VAS: 80
        initialConfig.fieldOfViewY = 30;                   //VAS: 45
        initialConfig.detectionDistance = 20;              //VAS: infinite
        initialConfig.seed = 400;
        initialConfig.period = 0.02f;                      //VAS: tones of 1ms; 17x9 tones in 153 ms (1kHz)
        //initialConfig.discriminationIndex = 0;
        initialConfig.focusVolume = 1;
        initialConfig.peripheralVolume = 1;

        RenderedFieldOfView = new GameObject("FieldOfView");
        
        InitFoVRenderer(RenderedFieldOfView);

        SetConfig(initialConfig);
        RecordingManager.Add(foveaSensor.GetRecordable());

        /*Default focus interval*/
        focusInterval = 3;
        //focusInterval = float.MaxValue;

        ssdEnabled = false;
    }

    private void InitFoVRenderer(GameObject FoVGameobject)
    {
        RenderedFieldOfView.layer = 13;
        RenderedFieldOfView.transform.localPosition = Vector3.zero;
        RenderedFieldOfView.transform.localRotation = Quaternion.Euler(Vector3.zero);
        RenderedFieldOfView.transform.parent = transform;
        RenderedFieldOfView.AddComponent<MeshFilter>().sharedMesh = MeshGenerator.FieldOfViewMesh(config.detectionDistance, config.fieldOfViewX, config.fieldOfViewY);
        RenderedFieldOfView.AddComponent<MeshRenderer>().material = (Material)Resources.Load(ResourcesPath.Material.transparentGreen); // Assign a material for the renderer
        RenderedFieldOfView.SetActive(false);
    }

    private void OnFoveaIdle()
    {
        focusTimeout = Time.time + _focusInterval;
        focusedElement = null;
    }

    private void OnFoveaDetection(RaycastHit hit) 
    {
        SensibleElement newFocusedElement = hit.transform.gameObject.GetComponent<SensibleElement>();
        if (focusedElement == newFocusedElement)
        {
            if(Time.time > focusTimeout)
            {
                focusedElement.ExecuteOnDetectionCallbacks();
                focusTimeout = Time.time + _focusInterval;
                Debug.Log("Element focused: " + transform.name);
            }
        }
        else
        {
            focusedElement = hit.transform.gameObject.GetComponent<SensibleElement>();
            focusTimeout = Time.time + _focusInterval;
        }
    }

    public void ProjectedVASProcessor(RaycastHit hit)
    {
        SensibleElement element = hit.transform.gameObject.GetComponent<SensibleElement>();
        if (element != null && element.stereoPixelData.volume > 0)
        {
            StereopixelData data = element.stereoPixelData;
            data.volume *= (element.focusable && element == focusedElement) ?
                config.focusVolume :
                config.peripheralVolume;

            Vector3 relativePosition = sensor.transform.InverseTransformPoint(hit.point);
            data.pitch = 1 + (relativePosition.y / relativePosition.z) * 0.5f;


            GameObject go = LoadStereoPixel(data, hit.point);
            go.GetComponent<ShortLivedAudioSource>().PlayAndDie(0.1f);
            onDetectionCallback?.Invoke(hit);
        }
    }

    public GameObject LoadStereoPixel(StereopixelData data, Vector3 position)
    {
        GameObject retval = Instantiate(stereopixelPrefab, position, Quaternion.identity);
        AudioSource audioSource = retval.GetComponent<AudioSource>();

        audioSource.clip = (AudioClip)Resources.Load(data.audioClipPath);
        audioSource.pitch = data.pitch;
        audioSource.volume = data.volume;

        return retval;
    }

    public Configuration GetConfig() => config;

    public void SetConfig(Configuration config)
    {
        this.config = config;
        sensor.ClearSensors();
        sensor.AddSensorsInFoV(config.fieldOfViewX, config.fieldOfViewY, config.M, config.N, config.detectionDistance);
        if (!sensor.SetSeed(config.seed))
        {
            sensor.SetSeed(1);
            this.config.seed = 1;
        }
        sensor.scanPeriod = config.period;

        foveaSensor.ClearSensors();
        foveaSensor.AddSensor(Vector3.forward, config.detectionDistance);
        foveaSensor.SetSeed(1);
        foveaSensor.scanPeriod = config.period;

        RenderedFieldOfView.GetComponent<MeshFilter>().sharedMesh = MeshGenerator.FieldOfViewMesh(config.detectionDistance, config.fieldOfViewX, config.fieldOfViewY);
    }

    /* This function is called by the player through the GUI ('ProjectedVASGUI'), in order to make this aura "envelop and follow" him
     */
    public void AdoptMe(Transform parent)
    {
        transform.parent = parent.transform;
        transform.localEulerAngles = Vector3.zero;
        transform.localPosition = Vector3.zero;
        RenderedFieldOfView.transform.localPosition = Vector3.zero;
        RenderedFieldOfView.transform.localEulerAngles = new Vector3(-90, 0, 0);
    }
}
