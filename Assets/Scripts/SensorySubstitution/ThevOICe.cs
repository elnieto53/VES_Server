using System;
using System.Linq;
using UnityEngine;


public class ThevOICe : MonoBehaviour, ISSD
{
    private Configuration config;
    public bool ssdEnabled { get; set; }

    private CameraSensor sensor;
    private AudioSource audioSource;

    private double[,] tones;
    private int sampleCounter = 0;

    private float[] soundToPlay;
    /*Used to manage concurrent access to 'soundToPlay' and limit the screenshots' rate*/
    private bool soundOutputEnabled = false;

    public void Init()
    {
        /*Initialize default configuration*/
        config = new Configuration();

        /*Audio source initialization*/
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.14f;

        /*Camera initialization*/
        sensor = gameObject.AddComponent<CameraSensor>();
        sensor.Init(config.Width, config.Height);
        sensor.DetectionDistance = config.DetectionDistance;

        /*Sensory substitution algorithm inicialization*/
        InitTonesArray();

        ssdEnabled = true;
    }

    [Serializable]
    public class Configuration
    {
        public const uint MaxWidth = 128;
        public const uint MaxHeight = 128;
        public const uint MaxGrayLevels = 10;
        public const float MinVerticalFoV = 50;
        public const float MaxVerticalFoV = 80;
        public const float MaxFrequency = 20000;
        public const float MinFrequency = 0;
        public const float MaxFrameDuration = 5;
        public const float MinFrameDuration = 0.5f;
        public const float MinRange = 0;
        public const float MaxRange = 200;
        public const float MinVolume = 0;
        public const float MaxVolume = 1;
        public enum FrequencyDistribution { Linear, Exponential }

        [SerializeField]
        private uint _width = 64;               //Width (pixels) of the rendered image
        [SerializeField]
        private uint _height = 64;              //Height (pixels) of the rendered image
        [SerializeField]
        private uint _grayLevels = 8;           //Number of possible pixel grayscale values
        [SerializeField]
        private float _verticalFoV = 50;        //Vertical field of view
        [SerializeField]
        private float _lowerFrequency = 500;    //Minimum output frequency
        [SerializeField]
        private float _higherFrequency = 5000;  //Maximum output frequency
        [SerializeField]
        private float _frameDuration = 1.05f;   //Time duration of the audio generated from a fotogram (seconds)
        [SerializeField]
        private float _detectionDistance = 10;  //Camera depth range
        [SerializeField]
        private float _volume = 0.14f;          //Output volume. BEWARE with audio saturation
        public FrequencyDistribution mode = FrequencyDistribution.Exponential;      //Frequency distribution of the base sinusoidal signals

        public uint Width { get => _width; set { if (WithinBounds(value, 0, MaxWidth)) _width = value; } }
        public uint Height { get => _height; set { if (WithinBounds(value, 0, MaxHeight)) _height = value; } }
        public uint GrayLevels { get => _grayLevels; set { if (WithinBounds(value, 0, MaxGrayLevels)) _grayLevels = value; } }
        public float VerticalFoV { get => _verticalFoV; set { if (WithinBounds(value, MinVerticalFoV, MaxVerticalFoV)) _verticalFoV = value; } }
        public float HorizontalFoV { get => Camera.VerticalToHorizontalFieldOfView(_verticalFoV, _width / _height); }
        public float LowerFrequency { get => _lowerFrequency; set { if (WithinBounds(value, MinFrequency, MaxFrequency)) _lowerFrequency = value; } }
        public float HigherFrequency { get => _higherFrequency; set { if (WithinBounds(value, MinFrequency, MaxFrequency)) _higherFrequency = value; } }
        public float FrameDuration { get => _frameDuration; set { if (WithinBounds(value, MinFrameDuration, MaxFrameDuration)) _frameDuration = value; } }
        public float DetectionDistance { get => _detectionDistance; set { if (WithinBounds(value, MinRange, MaxRange)) _detectionDistance = value; } }
        public float Volume { get => _volume; set { if (WithinBounds(value, 0, 1)) _volume = value; } }

        private bool WithinBounds(uint x, uint min, uint max) => (x >= min) && (x <= max);
        private bool WithinBounds(float x, float min, float max) => (x >= min) && (x <= max);

        public Configuration() { }
    }

    private class SoundtrackManager
    {
        private readonly int channels = 2;
        public int counter { get; private set; }
        public float[] soundToPlay { get; private set; } = { 0, 0 };

        public SoundtrackManager(float[] soundToPlay)
        {
            this.soundToPlay = soundToPlay;
            counter = 0;
        }

        public void PlayNext(ref float[] data, ref bool soundOutputEnabled)
        {
            float volume;
            for (int i = 0; i < data.Length; i += channels)
            {
                volume = counter * 1.0f / soundToPlay.Length;
                data[i + 1] = volume * soundToPlay[counter % soundToPlay.Length];
                data[i] = (1 - volume) * soundToPlay[counter % soundToPlay.Length];
                if (++counter >= soundToPlay.Length)
                {
                    counter = 0;
                    soundOutputEnabled = false;
                    return;
                }
            }
        }
    }


    void OnAudioFilterRead(float[] data, int channels)
    {
        if (soundOutputEnabled)
        {
            float volume;
            for (int i = 0; i < data.Length; i += channels)
            {
                volume = sampleCounter * 1.0f / soundToPlay.Length;
                data[i + 1] = volume * soundToPlay[sampleCounter];
                data[i] = (1 - volume) * soundToPlay[sampleCounter];

                if (++sampleCounter >= soundToPlay.Length)
                {
                    sampleCounter = 0;
                    soundOutputEnabled = false;
                    return;
                }
            }
        }
    }

    private void InitTonesArray()
    {
        double[] frequencies = new double[config.Height];
        double freqStep;
        switch (config.mode)
        {
            case Configuration.FrequencyDistribution.Linear:
                freqStep = (config.HigherFrequency - config.LowerFrequency) / (config.Height - 1);
                for (int i = 0; i < frequencies.Length; i++)
                {
                    frequencies[i] = config.LowerFrequency + i * freqStep;
                }
                break;
            case Configuration.FrequencyDistribution.Exponential:
                freqStep = Mathf.Log(config.HigherFrequency / config.LowerFrequency) / (config.Height - 1);
                for (int i = 0; i < frequencies.Length; i++)
                {
                    frequencies[i] = config.LowerFrequency * Mathf.Exp(i * (float)freqStep);
                }
                break;
        }

        /*Create tones with the calculated frequencies and random phases*/
        tones = new double[config.Height, Mathf.FloorToInt(config.FrameDuration * AudioSettings.outputSampleRate)];

        for (int toneIndex = 0; toneIndex < config.Height; toneIndex++)
        {
            double phase = (double)UnityEngine.Random.Range(0.0f, 1.0f) * Mathf.PI;
            double phaseStep = frequencies[toneIndex] * 2.0 * Mathf.PI / AudioSettings.outputSampleRate;
            for (int i = 0; i < tones.GetLength(1); i += 1)
            {
                phase += phaseStep;
                tones[toneIndex, i] = Mathf.Sin((float)phase);
            }
        }
    }

    void Update()
    {
        if (!soundOutputEnabled && ssdEnabled)
        {
            Texture2D texture2D = sensor.GetScreenshot();

            ImageAudioMapping(texture2D);
            Destroy(texture2D);

            soundOutputEnabled = true;
        }

    }


    private void ImageAudioMapping(Texture2D texture)
    {
        soundToPlay = new float[tones.GetLength(1)];

        int columnLength = Mathf.FloorToInt(soundToPlay.Length / config.Width);

        //Debug.Log("Maximum 1: " + soundToPlay.Max());
        double weight = 1;
        int currentColumn = 0;
        for (int row = 0; row < config.Height; row++)
        {
            for (int i = 0; i < soundToPlay.Length; i++)
            {
                if (currentColumn != (i / columnLength))
                {
                    currentColumn = i / columnLength;
                    Color color = texture.GetPixel(i / columnLength, row);
                    weight = Mathf.Round(color.grayscale * config.GrayLevels) / config.GrayLevels;
                }
                soundToPlay[i] += (float)(weight * tones[row, i]);
            }
        }
        float max = soundToPlay.Max();
        int numberMax = soundToPlay.Count(p => p == max);
        //Debug.Log("Maximum 2: " + max);
        //Debug.Log("Number of maximums: " + numberMax);
        //float averageEnergy = 0;
        //foreach (float value in soundToPlay)
        //{
        //    averageEnergy += value * value;
        //}
        //soundOutput.Enqueue(soundToPlay);
        //averageEnergy = averageEnergy / soundToPlay.Length;
        //Debug.Log("Energy: " + averageEnergy);
    }

    public void AdoptMe(Transform parent)
    {
        transform.parent = parent.transform;
        transform.localEulerAngles = Vector3.zero;
        transform.localPosition = Vector3.zero;
        //RenderedFieldOfView.transform.localPosition = Vector3.zero;
        //RenderedFieldOfView.transform.localEulerAngles = new Vector3(-90, 0, 0);
    }

    public void SetConfig(Configuration config)
    {
        this.config = config;

        /*Update AudioSource config*/
        audioSource.volume = config.Volume;

        /*Update camera config*/
        sensor.DetectionDistance = config.DetectionDistance;
        sensor.SetRenderSize(config.Width, config.Height);
        sensor.VerticalFoV = config.VerticalFoV;

        /*Update sensory substitution algorithm*/
        InitTonesArray();
    }

    public Configuration GetConfig()
    {
        return config;
    }

    public Texture GetRawTexture() => sensor.GetRawTexture();
}
