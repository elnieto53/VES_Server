using System;

public abstract class MoCapElement : ScenarioElement
{
    public byte bodyID;

    public MoCapElement() : base() { }

    [Serializable]
    public enum Spawnable : byte
    {
        Head = 0,
        Torso = 1,
        RightArm = 2,
        LeftArm = 3,
        RightForearm = 4,
        LeftForearm = 5,
        //RightThigh = 6,
        //LeftThigh = 7,
        //RightCalf = 8,
        //LeftCalf = 9,
        //Hip = 10
    }

}
