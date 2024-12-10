using UnityEngine;


[System.Serializable]
public struct Pose
{
    public Vector3 position;
    public Quaternion rotation;

    public Pose(Vector3 position, Quaternion rotation)
    {
        this.position = position;
        this.rotation = rotation;
    }

    public Pose(byte[] data) : this() => this = PkgSerializer.GetStruct<Pose>(data);
    public byte[] GetBytes() => PkgSerializer.GetBytes(this);
};


//public class CommonTypes
//{

//}