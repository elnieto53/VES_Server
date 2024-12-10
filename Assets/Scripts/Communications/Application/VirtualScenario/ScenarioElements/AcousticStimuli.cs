using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class AcousticStimuli : ScenarioElement
{
    private bool propertiesModified;
    private AudioSource audioSource;

    public Pose pose
    {
        set { go.transform.position = value.position; go.transform.rotation = value.rotation; propertiesModified = true; }
        get { return new Pose(go.transform.position, go.transform.rotation); }
    }

    public bool isPlaying
    {
        set { if (value && !audioSource.isPlaying) { audioSource.Play(); } if (!value && audioSource.isPlaying) { audioSource.Stop(); } propertiesModified = true; }
        get { return audioSource.isPlaying; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PkgData
    {
        public Pose pose;
        public bool isPlaying;
        public PkgData(Pose pose, bool isPlaying)
        {
            this.pose = pose;
            this.isPlaying = isPlaying;
        }

        public PkgData(byte[] data) : this() => this = PkgSerializer.GetStruct<PkgData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public enum SpawnableAudio
    {
        MetalHit = 1,
    }

    public AcousticStimuli() : base() { }


    private static readonly Dictionary<int, AudioClip> spawnableAudio = new Dictionary<int, AudioClip> {
        { (int)SpawnableAudio.MetalHit, (AudioClip)Resources.Load(ResourcesPath.AudioClip.longMetalHit) },
    };

    private protected override void LoadPrefab(byte prefabID)
    {
        if (spawnableAudio.TryGetValue(prefabID, out AudioClip clip))
        {
            go = new GameObject(elementID.ToString());
            audioSource = go.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = true;
        }
    }

    public override void Config(byte[] updateData)
    {
        PkgData remote = new PkgData(updateData);
        go.transform.position = remote.pose.position;
        go.transform.rotation = remote.pose.rotation;
        if (remote.isPlaying && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        if (!remote.isPlaying && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        if (propertiesModified)
        {
            propertiesModified = false;
            data = BuildPkg(new PkgData(new Pose(go.transform.position, go.transform.rotation), isPlaying).GetBytes());
            return true;
        }
        data = null;
        return false;
    }
}

