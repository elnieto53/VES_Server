using UnityEngine;


public abstract class PhysicalObject : ScenarioElement
{
    private protected bool propertiesModified;

    public Pose Pose
    {
        set
        {
            if (go.transform.position != value.position || go.transform.rotation != value.rotation)
                propertiesModified = true;
            go.transform.position = value.position;
            go.transform.rotation = value.rotation;
        }
        get { return new Pose(go.transform.position, go.transform.rotation); }
    }

    public float Scale
    {
        set
        {
            if (!go.transform.localScale.Equals(new Vector3(Scale, Scale, Scale)))
                propertiesModified = true;
            go.transform.localScale = new Vector3(Scale, Scale, Scale);
        }
        get => go.transform.localScale.magnitude;
    }

    public PhysicalObject() : base()
    {
        propertiesModified = false;
    }


    public override void Config(byte[] updateData)
    {
        Pose aux = new Pose(updateData);
        go.transform.position = aux.position;
        go.transform.rotation = aux.rotation;
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        if (propertiesModified)
        {
            propertiesModified = false;
            data = BuildPkg(new Pose(go.transform.position, go.transform.rotation).GetBytes());
            return true;
        }
        data = null;
        return false;
    }
}