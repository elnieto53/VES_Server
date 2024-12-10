using UnityEngine;
using UnityEngine.UI;

public class CameraViewGUI : MonoBehaviour
{
    public Camera cam;
    private RenderTexture renderTexture;
    private RawImage rawImage;

    // Start is called before the first frame update
    void Start()
    {
        rawImage = gameObject.GetComponent<RawImage>();
        renderTexture = new RenderTexture(10, 10, 24);
        cam.targetTexture = renderTexture;
        rawImage.texture = renderTexture;
    }
}
