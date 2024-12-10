using System;
using System.Collections.Generic;
using UnityEngine;


public class BodyPart
{
    /*Prefab of the body part*/
    public MoCapPrefab prefab;
    public MoCapElement.Spawnable prefabID { get => prefab.prefabID; }

    /*ScenarioElement references, which provides motion data*/
    public MoCapElement moCapElement { get => isRoot ? (MoCapElement) poseElement : orientationElement; }
    public MoCapOrientation orientationElement { get; set; } = null;
    public MoCapPose poseElement { get; set; } = null;
    public bool isRoot { get; private set; } = false;
    public bool hasCameraPivot { get => prefab.cameraAnchor != null; }
    public Pose pose {
        get => new Pose(prefab.transform.position, prefab.transform.rotation);
        set { prefab.transform.position = value.position; prefab.transform.rotation = value.rotation; }
    }

    /*Body parts' Hierarchical dependencies*/
    private Transform parentPivot;
    private List<BodyPart> childElements;

    /*Body part articulations*/
    private List<(BodyPart linkedBodyPart, Transform pivot)> articulations;

    private static int dependenciesCounter;
    private static readonly int MAX_DEPENDENCIES = 20;


    public BodyPart(MoCapElement.Spawnable prefabID)
    {
        articulations = new List<(BodyPart, Transform)>();
        childElements = new List<BodyPart>();
        prefab = MoCapPrefab.Load(prefabID);
    }

    public void AddLinkedElement((BodyPart, Transform) element) => articulations.Add(element);

    public void ClearLinkedElements() => articulations.Clear();

    public AlignementData GetRelativeAlignementData(Transform reference) => prefab.alignementData.GetRelativeAlignement(reference);

    public void UpdateDependencies()
    {
        dependenciesCounter = 0;
        isRoot = true;
        childElements.Clear();
        for (int i = 0; i < articulations.Count; i++)
        {
            childElements.Add(articulations[i].linkedBodyPart);
            articulations[i].linkedBodyPart.UpdateDependencies((this, articulations[i].pivot));
        }
    }

    private void UpdateDependencies((BodyPart linkedBodyPart, Transform pivot) parentArticulation)
    {
        if (dependenciesCounter++ > MAX_DEPENDENCIES)
            throw new Exception("The bodyPart dependencies could not be resolved - max depth reached");

        if (!articulations.Exists(articulation => articulation.linkedBodyPart == parentArticulation.linkedBodyPart))
            throw new Exception("The bodyPart dependencies could not be resolved at: " + prefab.prefabID);

        parentPivot = parentArticulation.pivot;
        childElements.Clear();
        foreach ((BodyPart bodyPart3d, Transform pivot) articulation in articulations)
        {
            if (articulation.bodyPart3d == parentArticulation.linkedBodyPart)
            {
                prefab.SetRotationPoint(articulation.pivot);
            }
            else
            {
                childElements.Add(articulation.bodyPart3d);
                articulation.bodyPart3d.UpdateDependencies((this, articulation.pivot));
            }
        }
    }

    public void ResetPose()
    {
        if (isRoot)
        {
            if (poseElement != null)
            {
                prefab.transform.position = poseElement.pose.position;
                prefab.transform.rotation = poseElement.pose.rotation;
            }
        }
        else
        {
            if (orientationElement != null)
            {
                prefab.transform.rotation = orientationElement.orientation;
            }
            prefab.transform.position = parentPivot.position;
        }

        foreach (BodyPart child in childElements)
        {
            child.ResetPose();
        }
    }


    public void UpdatePose()
    {
        if (isRoot)
        {
            if (poseElement != null)
            {
                /*Get the pose data from a ScenarioElement*/
                prefab.transform.position = poseElement.pose.position;
                prefab.transform.rotation = poseElement.pose.rotation;
            }
        }
        else
        {
            if (orientationElement != null)
            {
                /*Get the rotation data from a ScenarioElement*/
                prefab.transform.rotation = orientationElement.orientation;
            }
            prefab.transform.position = parentPivot.position;
        }
        
        foreach (BodyPart child in childElements)
        {
            child.UpdatePose();
        }
    }
}

