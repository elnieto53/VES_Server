using UnityEngine;

public class CameraSensor : MonoBehaviour
{
    private Camera cam;
    private RenderTexture renderTexture;


    /*renderTexture config*/
    private uint renderedWidth;
    private uint renderedHeight;

    //bool sensorEnable;

    public float DetectionDistance { get => cam.farClipPlane; set { cam.farClipPlane = value; } }
    public float VerticalFoV { get => cam.fieldOfView; set { cam.fieldOfView = value; } }
    public float HorizontalFoV { get => Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView, cam.aspect); }
    public float CameraAspectRatio { get => cam.aspect; }

    void Awake()
    {
        cam = gameObject.AddComponent<Camera>();
        //sensorEnable = false;
        Debug.Log("Camera FoV: (" + cam.fieldOfView + ", " + Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView, cam.aspect) + ")");
    }

    public void Init(uint width, uint height)
    {
        /*Virtual camera initialization*/
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.cullingMask = LayerMask.GetMask("StimuliTrigger");

        /*Renderer initialization*/
        renderedWidth = width;
        renderedHeight = height;
        InitRenderTexture();
    }

    public void SetRenderSize(uint width, uint height)
    {
        renderedWidth = width;
        renderedHeight = height;
        InitRenderTexture();
    }

    private void InitRenderTexture()
    {
        renderTexture = new RenderTexture((int)renderedWidth, (int)renderedHeight, 24)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            filterMode = FilterMode.Point
        };
        cam.targetTexture = renderTexture;
    }


    //void Update()
    //{
    //    if (sensorEnable)
    //    {
    //        Texture2D texture2D = GetScreenshot();
    //        texture2D.EncodeToPNG();

    //        //Color color = texture2D.GetPixel(texture2D.width / 2, texture2D.height / 2);
    //        //Debug.Log("Pixel color at (" + (texture2D.width / 2) + ", " + (texture2D.height / 2) + "): " + color.grayscale);
    //        //Debug.Log("Grayscale: " + color.grayscale);
    //        //ImageAudioMapping(texture2D);

    //        Destroy(texture2D);

    //        sensorEnable = true;

    //        PrintGrayscale(ref texture2D);
    //    }
    //}

    public Texture2D GetScreenshot()
    {
        if (!renderTexture.IsCreated())
            InitRenderTexture();
        RenderTexture.active = renderTexture;
        Texture2D retval = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture
        retval.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
        retval.Apply();
        return retval;
    }

    private void PrintGrayscale(ref Texture2D texture)
    {
        for (int i = 0; i < texture.width; i++)
        {
            for (int j = 0; j < texture.height; j++)
            {
                Color color = texture.GetPixel(i, j);
                float k = (color.r + color.g + color.b) / 3.0f;
                texture.SetPixel(i, j, new Color(k, k, k));
            }
        }
    }

    public Texture GetRawTexture()
    {
        return renderTexture;
    }

}
