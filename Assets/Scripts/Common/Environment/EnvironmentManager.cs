using System;
using System.Collections.Generic;
using UnityEngine;


public class EnvironmentManager : MonoBehaviour
{
    public Bounds bounds { get; private set; }
    public Rect perimeter { get; private set; }
    public int elementsCount { get => elements.Count; }

    private List<SensibleElement> elements;

    private DataMapping<SensibleElement, ProjectedVAS.StereopixelData> pvasMapping = new DataMapping<SensibleElement, ProjectedVAS.StereopixelData>(defaultPvasFilters);
    private DataMapping<SensibleElement, PathContainer> materialRenderMapping = new DataMapping<SensibleElement, PathContainer>(defaultRenderFilters);

    //private void Awake() => Init();

    public void Init()
    {
        bounds = GetBounds();
        perimeter = new Rect(new Vector2(bounds.min.x, bounds.min.z), new Vector2(bounds.size.x, bounds.size.z));
        elements = new List<SensibleElement>(GetComponentsInChildren<SensibleElement>());

        foreach (SensibleElement element in elements)
            InitElement(element);
        //ResetRenderMaterials();
    }

    private Bounds GetBounds()
    {
        Bounds scenarioBounds = new Bounds(transform.position, Vector3.one);
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            scenarioBounds.Encapsulate(renderer.bounds);
        }
        return scenarioBounds;
    }

    private void InitElement(SensibleElement element)
    {
        element.Init();
        /*Load PVAS data*/
        pvasMapping.TryGetData(element, out ProjectedVAS.StereopixelData stereoPixelData);
        element.stereoPixelData = stereoPixelData;
        /*Load default render material*/
        materialRenderMapping.TryGetData(element, out PathContainer materialRenderPath);
        element.renderMaterial = (Material)Resources.Load(materialRenderPath.path);
    }

    public void AddElement(SensibleElement element)
    {
        elements.Add(element);
        InitElement(element);
        bounds = GetBounds();
    }

    private void ResetRenderMaterials()
    {
        foreach (SensibleElement element in elements)
            element.ResetRenderMaterial();
    }


    /*Default mapping filters*/

    private static List<(Predicate<SensibleElement> filter, ProjectedVAS.StereopixelData stimuli)> defaultPvasFilters = new List<(Predicate<SensibleElement>, ProjectedVAS.StereopixelData)>
    {
        { ( element => element.material == SensibleElement.Material.Brick , new ProjectedVAS.StereopixelData(ResourcesPath.AudioClip.shortWoodHit1, 0.1f, 2.3f))  },
        { ( element => element.material == SensibleElement.Material.Wood , new ProjectedVAS.StereopixelData(ResourcesPath.AudioClip.shortWoodHit2, 0.6f, 1f))  },
        { ( element => element.material == SensibleElement.Material.Metal , new ProjectedVAS.StereopixelData(ResourcesPath.AudioClip.shortMetalHit, 1f, 1f))  },
        { ( element => element.material == SensibleElement.Material.Marble , new ProjectedVAS.StereopixelData(ResourcesPath.AudioClip.shortWoodHit3, 1f, 1f))  },
        { ( element => true , new ProjectedVAS.StereopixelData(ResourcesPath.AudioClip.shortClick, 0f, 1f))  }
    };

    private static List<(Predicate<SensibleElement> filter, PathContainer path)> defaultRenderFilters = new List<(Predicate<SensibleElement>, PathContainer)>
    {
        { ( element => element.type == SensibleElement.Type.Floor , new PathContainer(ResourcesPath.Material.blue)) },
        { ( element => element.material == SensibleElement.Material.Brick , new PathContainer(ResourcesPath.Material.gray) ) },
        { ( element => element.material == SensibleElement.Material.Metal , new PathContainer(ResourcesPath.Material.black) ) },
        { ( element => element.material == SensibleElement.Material.Wood , new PathContainer(ResourcesPath.Material.brown) ) },
        { ( element => true , new PathContainer(ResourcesPath.Material.gray) )  }
    };
}
