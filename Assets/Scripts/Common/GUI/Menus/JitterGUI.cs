using UnityEngine;
using UnityEngine.UI;

public class JitterGUI : MonoBehaviour
{
    QoSManager moCapQoSManager;

    private Toggle moCapQoSManagerToggle;
    private InputField moCapLambdaInputField;

    private Button acceptButton;
    private Button cancelButton;

    private const int MAX_PKG_OUTPUT_PER_CALL = 20;

    public void Init(QoSManager moCapQoSManager)
    {
        this.moCapQoSManager = moCapQoSManager;

        moCapQoSManagerToggle = transform.Find("MoCapQoSConfiguration/IsActiveToggle").GetComponent<Toggle>();
        moCapLambdaInputField = transform.Find("MoCapQoSConfiguration/InputFields/LambdaMenu/InputField").GetComponent<InputField>();
        acceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("CancelButton").GetComponent<Button>();

        acceptButton.onClick.AddListener(delegate { if (OnClickUpdateConfig()) Destroy(gameObject); });
        cancelButton.onClick.AddListener(delegate { Destroy(gameObject); });

        RefreshConfig();
    }

    public void RefreshConfig()
    {
        JitterModeling pvasJitterModeling = moCapQoSManager.jitterModeling;

        if (pvasJitterModeling.GetType().Equals(typeof(JitterModeling.Poisson)))
        {
            moCapLambdaInputField.text = ((JitterModeling.Poisson)pvasJitterModeling).lambda.ToString("0.00");
            moCapQoSManagerToggle.isOn = true;
        }
        else
        {
            moCapLambdaInputField.text = "1,2";
            moCapQoSManagerToggle.isOn = false;
        }
    }

    public bool OnClickUpdateConfig()
    {
        try
        {
            float pvasLambda = float.Parse(moCapLambdaInputField.text);
            //moCapQoSManager.jitterModeling = moCapQoSManagerToggle.isOn ?
            //    (JitterModeling)new JitterModeling.PoissonDistribution(pvasLambda, MAX_PKG_OUTPUT_PER_CALL) :
            //    new JitterModeling.NoJitter();
            return true;
        }
        catch
        {
            RefreshConfig();
            return false;
        }
    }
}
