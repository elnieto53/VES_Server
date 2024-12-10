using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SensibleElement : MonoBehaviour
{
    [SerializeField] private Type _element;
    [SerializeField] private Material _material;
    [SerializeField] private Placement _placement;
    [SerializeField] private bool _focusable;
    private UnityEngine.Material originalRenderMaterial;
    public Type type { get => _element; }
    public Material material { get => _material; }
    public Placement placement { get => _placement; }
    public Data data { get => new Data(this); }
    public bool focusable { get => _focusable; }
    public UnityEngine.Material renderMaterial { get => GetComponent<Renderer>().material; set => GetComponent<Renderer>().material = value; }

    /*Stimuli to be triggered on detection*/
    public ProjectedVAS.StereopixelData stereoPixelData { get; set; }

    /*On Detection callback*/
    public UnityEvent onDetectionEvent;
    //public Action<Session> onDetectionEvent;

    public void Init()
    {
        originalRenderMaterial = renderMaterial;
        int layer = LayerMask.NameToLayer("StimuliTrigger");
        if (layer == -1)
        {
            Debug.LogError("Please, add a 'StimuliTrigger' Layer");
            return;
        }
        gameObject.layer = layer;
    }

    public void ResetRenderMaterial() => renderMaterial = originalRenderMaterial;

    public enum Material { Metal, Wood, Brick, Concrete, PaintedPaper, Ceramic, Marble }
    public enum Placement { Indoor, Outdoor }
    public enum Type { Wall, Floor, Ceiling, Door, Furniture, Pedestrian, Vehicle }


    [Serializable]
    public class Data
    {
        public Type type;
        public Material material;
        public Placement placement;

        public Data(SensibleElement element)
        {
            type = element.type;
            material = element.material;
            placement = element.placement;
        }
    }

    static float previousDetectionTimestamp;

    /*(Workaround) Avoid triggering 'onDetection' more than once per second*/
    public void ExecuteOnDetectionCallbacks()
    {
        float detectionTimestamp = Time.time;
        bool isValid = previousDetectionTimestamp + 1 >= detectionTimestamp ? false : true;
        previousDetectionTimestamp = detectionTimestamp;
        if (isValid)
            onDetectionEvent?.Invoke();
    }
}
