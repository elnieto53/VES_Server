using System.Runtime.InteropServices;

public class MoCapPose : MoCapElement
{
    public Pose pose;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PkgData
    {
        public byte bodyID;
        public Pose pose;

        public PkgData(byte bodyID, Pose pose)
        {
            this.bodyID = bodyID;
            this.pose = pose;
        }

        public PkgData(byte[] data) : this() => this = PkgSerializer.GetStruct<PkgData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public MoCapPose() : base()
    {

    }

    private protected override void LoadPrefab(byte prefabID)
    {
        /*This element only contains pose data; it does no require a prefab*/
    }

    public override void Config(byte[] updateData)
    {
        PkgData data = new PkgData(updateData);
        pose = data.pose;
        bodyID = data.bodyID;
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        data = BuildPkg(new PkgData(bodyID, pose).GetBytes());
        return true;
    }
}
