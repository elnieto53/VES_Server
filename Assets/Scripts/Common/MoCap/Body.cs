using System;
using System.Collections.Generic;
using UnityEngine;


public class Body : MonoBehaviour
{
    /*The body is configured in Unity Editor through a list of articulations.
     Each articulation connects two body parts through two "pivot" points*/
    [Serializable]
    public class Articulation
    {
        public MoCapElement.Spawnable bodyPart1;
        public MoCapPrefab.SubPrefabIndex pivot1;
        public MoCapElement.Spawnable bodyPart2;
        public MoCapPrefab.SubPrefabIndex pivot2;
    }

    public MoCapElement.Spawnable rootID;
    [Header("Add the list of articulations...")]
    public List<Articulation> articulations;

    private readonly Vector3 defaultStartingPoint = new Vector3(0, averageHeight, 0);
    private BodyPart rootBodyPart;
    private List<BodyPart> bodyParts;
    private List<Transform> sensorAnchors;
    public byte ID { get; private set; }
    public static readonly float averageHeight = 1.7f;

    public Action updateCallback { get; set; }

    public static Body GetDefault(byte bodyID)
    {
        Body body = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.Body.manager)).GetComponent<Body>();
        body.Init();
        body.ID = bodyID;
        return body;
    }

    public static Body LoadBody(List<Articulation> articulations, MoCapElement.Spawnable root, byte bodyID)
    {
        GameObject go = new GameObject("Body_" + bodyID);
        Body body = go.AddComponent<Body>();
        body.rootID = root;
        body.articulations = articulations;
        body.Init();
        return body;
    }

    private void Init()
    {
        /*Create all the linked body parts from the 'articulations' info*/
        bodyParts = DefaultBody(articulations);

        /*Starting from the specified root body body part, update the dependencies.
         E.g., if the head is the root, the torso is set as a 'child' with the neck
         set as the rotation pivot*/
        rootBodyPart = bodyParts.Find(p => p.prefab.prefabID == rootID);
        rootBodyPart.UpdateDependencies();
        rootBodyPart.prefab.transform.position = defaultStartingPoint;
        ResetPose();

        /*Add sensor anchors*/
        sensorAnchors = new List<Transform>();
        foreach (BodyPart bodyPart in bodyParts)
            sensorAnchors.AddRange(bodyPart.prefab.detectionAreaAnchors);
    }

    public void ResetPose() => rootBodyPart.ResetPose();
    public void UpdatePose()
    {
        rootBodyPart.UpdatePose();
        updateCallback?.Invoke();
    }

    private List<BodyPart> DefaultBody(List<Articulation> articulationList)
    {
        List<BodyPart> retval = new List<BodyPart>();
        BodyPart bodyPart1, bodyPart2;
        foreach (Articulation articulation in articulationList)
        {
            if ((bodyPart1 = retval.Find(p => p.prefabID == articulation.bodyPart1)) == null)
            {
                bodyPart1 = new BodyPart(articulation.bodyPart1);
                retval.Add(bodyPart1);
            }

            if ((bodyPart2 = retval.Find(p => p.prefabID == articulation.bodyPart2)) == null)
            {
                bodyPart2 = new BodyPart(articulation.bodyPart2);
                retval.Add(bodyPart2);
            }

            bodyPart1.AddLinkedElement((bodyPart2, bodyPart1.prefab.GetPivot(articulation.pivot1)));
            bodyPart2.AddLinkedElement((bodyPart1, bodyPart2.prefab.GetPivot(articulation.pivot2)));
        }
        return retval;
    }

    public void SetLocalScale(float scale)
    {
        foreach (BodyPart bodyPart in bodyParts)
        {
            bodyPart.prefab.transform.localScale = scale * Vector3.one;
        }
    }

    public BodyPart GetRootBodyPart() => rootBodyPart;

    public BodyPart GetBodyPart(MoCapElement.Spawnable prefabID) => bodyParts.Find(p => p.prefabID == prefabID);

    public List<BodyPart> GetBodyParts() => bodyParts;

    public List<BodyPart> GetBodyParts(Predicate<BodyPart> predicate) => bodyParts.FindAll(predicate);


    //public List<Transform> GetSensorAnchors()
    //{
    //    List<Transform> retval = new List<Transform>();
    //    foreach(BodyPart bodyPart in bodyParts)
    //    {
    //        if (bodyPart.prefab.sensorAttachable)
    //            retval.AddRange(bodyPart.prefab.detectionAreaAnchors);
    //    }
    //    return retval;
    //}

    public List<(MoCapElement, AlignementData)> GetAlignementData()
    {
        List<(MoCapElement, AlignementData)> bodyAlignmentData = new List<(MoCapElement, AlignementData)>();
        foreach (BodyPart bodyPart in bodyParts)
        {
            if (bodyPart.moCapElement != null && !bodyPart.isRoot)
            {
                /*Adjust the alignement data according to the 'root' current orientation in the XZ plane*/
                bodyAlignmentData.Add((bodyPart.moCapElement, bodyPart.GetRelativeAlignementData(rootBodyPart.prefab.transform)));
            }
        }
        return bodyAlignmentData;
    }

    public IList<Transform> GetSensorAnchors() => sensorAnchors.AsReadOnly();

    /// <summary>
    /// Returns the ID of the corresponding 'sensorAnchor' in the current Body. The sensorAnchors are specified in each BodyPart.prefab
    /// </summary>
    /// <param name="transform"></param>
    /// <returns> Sensor anchor ID </returns> 
    public int GetSensorAnchorID(Transform transform) => sensorAnchors.IndexOf(transform);

    public Transform GetSensorAnchor(int ID) => sensorAnchors[ID];
}