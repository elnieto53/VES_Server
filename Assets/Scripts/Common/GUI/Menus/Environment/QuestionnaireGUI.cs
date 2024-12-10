using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class QuestionnaireGUI : MonoBehaviour, IRecordable
{
    /*GUI elements*/
    private Button acceptButton;
    private Text progressText;
    private Text questionText;
    private Dropdown optionDropdown;
    private Text hitRateText;

    /*State*/
    private int currentSetupIndex;
    private List<int> userAnswers;
    private List<int> answersTimeStamp;
    private bool isRecording;

    /*References*/
    public Questionnaire questionnaire;
    public EnvironmentManager environmentManager;
    Func<int> GetTimeStamp;

    [Serializable]
    public class Record
    {
        public string question;
        public List<string> options;
        public List<int> userAnswers;
        public List<int> correctAnswer;
        public List<int> answersTimeStamp;

        public Record(){
            userAnswers = new List<int>();
            correctAnswer = new List<int>();
            answersTimeStamp = new List<int>();
        }
        public Record(QuestionnaireGUI gui){
            question = gui.questionnaire.question;
            options = gui.questionnaire.GetOptions();
            userAnswers = new List<int>(gui.userAnswers);
            correctAnswer = new List<int>(gui.questionnaire.setups.Select(o => o.answer));
            answersTimeStamp = new List<int>(gui.answersTimeStamp);
            Debug.Log("HEey " + answersTimeStamp.Count);
        }
    }

    public void Init(Questionnaire questionnaire)
    {
        acceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        progressText = transform.Find("ProgressText").GetComponent<Text>();
        questionText = transform.Find("QuestionText").GetComponent<Text>();
        optionDropdown = transform.Find("OptionDropdown").GetComponent<Dropdown>();
        hitRateText = transform.Find("HitRateText").GetComponent<Text>();

        optionDropdown.ClearOptions();
        optionDropdown.AddOptions(questionnaire.GetOptions());
        acceptButton.onClick.AddListener(OnClickAccept);
        optionDropdown.onValueChanged.AddListener(delegate { optionDropdown.GetComponent<Image>().color = Color.white; });

        this.questionnaire = questionnaire;
        questionText.text = questionnaire.question;

        currentSetupIndex = 0;
        userAnswers = new List<int>();
        answersTimeStamp = new List<int>();
        for (int i = 0; i < questionnaire.setups.Count; i++)
            userAnswers.Add(-1);

        questionnaire.setups[currentSetupIndex].Init();
        RecordingManager.Add(this);
        UpdateGUI();
    }

    private void OnClickAccept()
    {
        SetAnswer(currentSetupIndex, optionDropdown.value);
        questionnaire.setups[currentSetupIndex].DeInit();
        if (currentSetupIndex == userAnswers.Count-1)
        {
            acceptButton.interactable = false;
            optionDropdown.interactable = false;
        }
        else
        {
            questionnaire.setups[++currentSetupIndex].Init();
        }
        UpdateGUI();
    }

    private void SetAnswer(int questionIndex, int newAnswer)
    {
        if(optionDropdown.value != userAnswers[questionIndex])
        {
            userAnswers[questionIndex] = newAnswer;
            if (isRecording)
                answersTimeStamp.Add(GetTimeStamp());
        }
    }

    private void UpdateGUI()
    {
        progressText.text = (currentSetupIndex + 1) + "/" + questionnaire.setups.Count;
        optionDropdown.value = userAnswers[currentSetupIndex];
        float sum = 0;
        for(int i = 0; i < questionnaire.setups.Count; i++)
        {
            if (userAnswers[i] == questionnaire.setups[i].answer)
                sum++;
        }

        hitRateText.text = (100 * sum / questionnaire.setups.Count).ToString("0.00") + "%";
    }

    void OnDestroy() => RecordingManager.Remove(this);

    public void StartRecording(Func<int> GetTimeStamp)
    {
        answersTimeStamp.Clear();
        this.GetTimeStamp = GetTimeStamp;
        isRecording = true;
    }

    public void StopRecording() => isRecording = false;


    public void SaveRecording(string folderPath, bool overwrite) => SaveRecording("Symbol Questionnaire", folderPath, overwrite);

    public void SaveRecording(string filename, string folderPath, bool overwrite)
        => new JsonSerializer<Record>(new Record(this)).SaveDataToFile(filename, folderPath, overwrite);
}

public abstract class Questionnaire : MonoBehaviour
{
    private GameObject GUI;
    public EnvironmentManager environmentManager;
    public string question;
    public List<Setup> setups { get; protected set; }
    
    public interface Setup
    {
        public int answer { get; }
        public void Init();
        public void DeInit();
    }

    private void Start()
    {
        Init();
        GUI = Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.questionnaire), GameObject.Find("UIFrame").transform, false);
        GUI.GetComponent<QuestionnaireGUI>().Init(this);
    }

    public void OnDestroy() => Destroy(GUI);


    protected abstract void Init();
    public abstract List<string> GetOptions();
}



