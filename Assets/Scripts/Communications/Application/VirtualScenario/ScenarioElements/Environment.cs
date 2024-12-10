using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Environment : PhysicalObject
{
    private float keepAliveTimestamp;
    private const int keepAlivePeriod = 5000; /*miliseconds*/

    public enum SpawnableElement
    {
        Head = 1,
        Arm = 2,
        Sphere = 3
    }

    private static readonly Dictionary<SpawnableElement, GameObject> spawnableElements = new Dictionary<SpawnableElement, GameObject> {
        { SpawnableElement.Head, (GameObject)Resources.Load("Prefabs/Skeleton/Head") },
        { SpawnableElement.Arm, (GameObject)Resources.Load("Prefabs/Skeleton/Arm") },
        { SpawnableElement.Sphere, (GameObject)Resources.Load("Prefabs/Objects/Sphere") },
    };

    public Environment() : base() { }

    public override void Init(ulong elementID, byte prefabID, IPAddress origin, VirtualScenarioManager.ChannelManager channelManager)
    {
        base.Init(elementID, prefabID, origin, channelManager);
        keepAliveTimestamp = Time.realtimeSinceStartup;
    }

    private protected override void LoadPrefab(byte prefabID)
    {
        if (spawnableElements.TryGetValue((SpawnableElement)prefabID, out GameObject prefab))
        {
            go = MonoBehaviour.Instantiate(prefab);
            go.name = elementID.ToString();
            propertiesModified = true;
        }
        else { throw new Exception("The item with prefabID " + prefabID + " could not be loaded"); }
    }

    public override void Config(byte[] updateData)
    {
        Pose aux = new Pose(updateData);
        go.transform.position = aux.position;
        go.transform.rotation = aux.rotation;
        //Debug.Log("Rotation received: (" + aux.rotation.w + ", " + aux.rotation.x + ", " + aux.rotation.y + ", " + aux.rotation.z + ")");
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        if (propertiesModified || (Time.realtimeSinceStartup > keepAliveTimestamp))
        {
            propertiesModified = false;
            keepAliveTimestamp = Time.realtimeSinceStartup + keepAlivePeriod;
            data = BuildPkg(new Pose(go.transform.position, go.transform.rotation).GetBytes());
            return true;
        }
        data = null;
        return false;
    }

}
