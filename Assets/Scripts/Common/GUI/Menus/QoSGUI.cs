using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QoSGUI : MonoBehaviour
{
    private Session session;
    private Button notDegradedQoSButton;
    private Button randomPoissonQoSButton;
    private Button recordedQoSButton;
    private InputField lambdaPoissonInputField;
    private Button loadJsonFileButton;
    private Text selectedFileText;

    private Button acceptButton;
    private Button cancelButton;

    private string qosRecordingFilePath;


    private QoSManager.Configuration qosConfigurationGUI;
    private GameObject fileExplorerPanel = null;

    // Start is called before the first frame update
    public void Init(Session session)
    {
        this.session = session;

        notDegradedQoSButton = transform.Find("NotDegradedQoSConfiguration/SelectButton").GetComponent<Button>();
        randomPoissonQoSButton = transform.Find("RandomDelayQoSConfiguration/SelectButton").GetComponent<Button>();
        recordedQoSButton = transform.Find("RecordedQoSConfiguration/SelectButton").GetComponent<Button>();
        lambdaPoissonInputField = transform.Find("RandomDelayQoSConfiguration/InputFields/LambdaMenu/InputField").GetComponent<InputField>();
        loadJsonFileButton = transform.Find("RecordedQoSConfiguration/LoadJsonFileButton").GetComponent<Button>();
        selectedFileText = transform.Find("RecordedQoSConfiguration/SelectedFileText").GetComponent<Text>();
        acceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("CancelButton").GetComponent<Button>();

        notDegradedQoSButton.onClick.AddListener(delegate { OnClickSelectQoSMode(QoSManager.Mode.NotDegraded); });
        randomPoissonQoSButton.onClick.AddListener(delegate { OnClickSelectQoSMode(QoSManager.Mode.RandomDegradation); });
        recordedQoSButton.onClick.AddListener(delegate { OnClickSelectQoSMode(QoSManager.Mode.RecordedDegradation); });
        cancelButton.onClick.AddListener(delegate { DeInit(); });
        acceptButton.onClick.AddListener(delegate { SetConfiguration(); DeInit(); });

        loadJsonFileButton.onClick.AddListener(OnClickLoadRecordedQoSDegradation);

        GetConfiguration();
    }

    private void FileExplorerCallback(string filePath)
    {
        qosRecordingFilePath = filePath;
        RefreshGUI();
    }

    private void OnClickLoadRecordedQoSDegradation()
    {
        if (fileExplorerPanel != null)
            return;
        fileExplorerPanel = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.fileExplorer), transform.parent);
        fileExplorerPanel.GetComponent<FileExplorerGUI>().Init(ResourcesPath.Files.qosRecording, FileExplorerCallback, "*.json");
    }


    private void OnClickSelectQoSMode(QoSManager.Mode mode)
    {
        qosConfigurationGUI.mode = mode;
        RefreshGUI();
    }

    private void SetConfiguration()
    {
        switch (qosConfigurationGUI.mode)
        {
            case QoSManager.Mode.NotDegraded:
                /*QoSManager configuration*/
                break;
            case QoSManager.Mode.RandomDegradation:
                /*QoSManager configuration*/
                JitterModeling newJitterModel = new JitterModeling.Poisson(double.Parse(lambdaPoissonInputField.text), JitterModeling.Poisson.defaultPkgOutputPerCall);
                qosConfigurationGUI.jitterModelingJson = newJitterModel.Serialize();
                qosConfigurationGUI.distribution = JitterModeling.Distribution.Poisson;
                break;
            case QoSManager.Mode.RecordedDegradation:
                /*QoSManager configuration*/
                QoSManager.Recording newRecording = QoSManager.Recording.Load(qosRecordingFilePath);
                qosConfigurationGUI.recording = newRecording;
                session.qosRecordingFilePath = qosRecordingFilePath;
                break;
        }
        session.moCapQoSManager.SetConfiguration(qosConfigurationGUI);
    }

    private void GetConfiguration()
    {
        qosConfigurationGUI = session.moCapQoSManager.GetConfiguration();
        qosRecordingFilePath = session.qosRecordingFilePath;
        RefreshGUI();
    }

    private void RefreshGUI()
    {
        switch (qosConfigurationGUI.mode)
        {
            case QoSManager.Mode.NotDegraded:
                lambdaPoissonInputField.interactable = false;
                loadJsonFileButton.interactable = false;
                acceptButton.interactable = true;
                notDegradedQoSButton.GetComponent<Image>().color = Color.green;
                randomPoissonQoSButton.GetComponent<Image>().color = Color.gray;
                recordedQoSButton.GetComponent<Image>().color = Color.gray;
                break;
            case QoSManager.Mode.RandomDegradation:
                /*GUI elements*/
                lambdaPoissonInputField.interactable = true;
                loadJsonFileButton.interactable = false;
                acceptButton.interactable = true;
                notDegradedQoSButton.GetComponent<Image>().color = Color.gray;
                randomPoissonQoSButton.GetComponent<Image>().color = Color.green;
                recordedQoSButton.GetComponent<Image>().color = Color.gray;

                JitterModeling jitterModeling = JitterModeling.Deserialize(qosConfigurationGUI.distribution, qosConfigurationGUI.jitterModelingJson);
                switch (qosConfigurationGUI.distribution)
                {
                    case JitterModeling.Distribution.Constant:
                        break;
                    case JitterModeling.Distribution.Poisson:
                        lambdaPoissonInputField.text = ((JitterModeling.Poisson)jitterModeling).lambda.ToString("0.00");
                        break;
                }

                break;
            case QoSManager.Mode.RecordedDegradation:
                /*GUI elements*/
                lambdaPoissonInputField.interactable = false;
                loadJsonFileButton.interactable = true;
                notDegradedQoSButton.GetComponent<Image>().color = Color.gray;
                randomPoissonQoSButton.GetComponent<Image>().color = Color.gray;
                recordedQoSButton.GetComponent<Image>().color = Color.green;
                selectedFileText.text = qosRecordingFilePath;
                try
                {
                    QoSManager.Recording.Load(qosRecordingFilePath);
                    acceptButton.interactable = true;
                }
                catch
                {
                    Debug.Log("(VES) Could not load specified recording file");
                    acceptButton.interactable = false;
                }
                break;
        }
    }

    private void DeInit()
    {
        if (fileExplorerPanel != null)
            Destroy(fileExplorerPanel);
        Destroy(gameObject);
    }
}
