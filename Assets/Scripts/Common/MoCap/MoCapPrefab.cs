using System;
using System.Collections.Generic;
using UnityEngine;

public class MoCapPrefab : MonoBehaviour
{
    public enum SubPrefabIndex { Pivot1 = 0, Pivot2 = 1, Pivot3 = 2, Pivot4 = 3, Pivot5 = 4, Pivot6 = 5, cameraAnchor = 6 };

    public MoCapElement.Spawnable prefabID { get; private set; }
    //public bool sensorAttachable { get => detectionAreaAnchor != null; }
    public bool sensorAttachable { get => detectionAreaAnchors != null && detectionAreaAnchors.Count > 0; }

    [Header("Prefab components")]
    public List<Transform> pivots;
    public Transform cameraAnchor;
    public List<Transform> detectionAreaAnchors;

    [Header("Alignement data")]
    public AlignementData alignementData;

    public Transform GetPivot(SubPrefabIndex pivotIndex)
    {
        if (pivots == null || pivots.Count < (int)pivotIndex)
            return null;
        return pivots[(int)pivotIndex];
    }

    private static readonly List<(MoCapElement.Spawnable bodyPart, string path)> spawnableElements = new List<(MoCapElement.Spawnable, string)> {
        { (MoCapElement.Spawnable.Head, ResourcesPath.Prefabs.Body.head) },
        { (MoCapElement.Spawnable.Torso, ResourcesPath.Prefabs.Body.torso) },
        { (MoCapElement.Spawnable.RightArm, ResourcesPath.Prefabs.Body.rightArm) },
        { (MoCapElement.Spawnable.LeftArm, ResourcesPath.Prefabs.Body.leftArm) },
        { (MoCapElement.Spawnable.RightForearm, ResourcesPath.Prefabs.Body.rightForearm) },
        { (MoCapElement.Spawnable.LeftForearm, ResourcesPath.Prefabs.Body.leftForearm) },
        //{ (MoCapElement.Spawnable.RightThigh, "Prefabs/Skeleton/RightThigh") },
        //{ (MoCapElement.Spawnable.LeftThigh, "Prefabs/Skeleton/LeftThigh") },
        //{ (MoCapElement.Spawnable.RightCalf, "Prefabs/Skeleton/RightCalf") },
        //{ (MoCapElement.Spawnable.LeftCalf, "Prefabs/Skeleton/LeftCalf") },
        //{ (MoCapElement.Spawnable.Hip, "Prefabs/Skeleton/Hip") },
    };

    public static MoCapPrefab Load(MoCapElement.Spawnable prefabID)
    {
        MoCapPrefab retval;
        int index = spawnableElements.FindIndex(p => p.bodyPart == prefabID);
        if (index < 0)
            throw new Exception("The item '" + prefabID + "' could not be loaded");
        string path = spawnableElements[index].path;
        GameObject go = MonoBehaviour.Instantiate((GameObject)Resources.Load(path));
        retval = go.GetComponent<MoCapPrefab>();
        retval.prefabID = prefabID;
        return retval;
    }

    public void SetRotationPoint(Transform anchor)
    {
        //Debug.Log(prefabID + " new anchor: " + anchor.name + " of " + anchor.parent.parent.name);
        Transform physics = transform.Find("Physics");
        Transform anchors = transform.Find("Anchors");
        Vector3 newPivot = transform.position - anchor.position;
        physics.position += newPivot;
        anchors.position += newPivot;
    }
}

[Serializable]
public class AlignementData
{
    public Quaternion initialRotation = Quaternion.identity;
    public Vector3 localRotationReference;
    public Vector3 globalRotationReference;

    public AlignementData GetRelativeAlignement(Transform reference)
    {
        AlignementData retval = new AlignementData();
        Vector3 forward = new Vector3(reference.forward.x, 0, reference.forward.z);
        if (forward.magnitude == 0)
            throw new System.ArgumentException("The 'reference orientation' vector must not be perpendicular to the XZ plane");
        retval.initialRotation = Quaternion.FromToRotation(Vector3.forward, forward) * initialRotation;
        retval.localRotationReference = localRotationReference;
        retval.globalRotationReference = Quaternion.FromToRotation(Vector3.forward, forward) * globalRotationReference;
        return retval;
    }
}