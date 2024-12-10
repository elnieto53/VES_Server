using System.Runtime.InteropServices;
using UnityEngine;

public class MoCapOrientation : MoCapElement
{
    public Quaternion orientation { get; set; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PkgData
    {
        public byte bodyID;
        public Quaternion rotation;

        public PkgData(byte bodyID, Quaternion rotation)
        {
            this.bodyID = bodyID;
            this.rotation = rotation;
        }

        public PkgData(byte[] data) : this() => this = PkgSerializer.GetStruct<PkgData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }


    public MoCapOrientation() : base()
    {

    }

    private protected override void LoadPrefab(byte prefabID)
    {
        /*This element only contains rotation data; it does no require a prefab*/
    }

    public override void Config(byte[] updateData)
    {
        PkgData data = new PkgData(updateData);
        orientation = data.rotation;
        bodyID = data.bodyID;
        //Debug.Log("ID: " + elementID + ". Orientation: " + orientation);
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        data = BuildPkg(new PkgData(bodyID, orientation).GetBytes());
        return true;
    }
}
