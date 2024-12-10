using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DoubleCheckGUI : MonoBehaviour
{
    private Button acceptButton;
    private Button cancelButton;

    // Start is called before the first frame update
    public void Init(UnityAction call)
    {
        acceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("CancelButton").GetComponent<Button>();

        acceptButton.onClick.AddListener(delegate { call(); Destroy(gameObject); });
        cancelButton.onClick.AddListener(delegate { Destroy(gameObject); });
    }
}
