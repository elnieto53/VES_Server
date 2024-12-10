using System;
using UnityEngine;
using UnityEngine.UI;

public class TimerGUI : MonoBehaviour
{
    private Button playPauseButton;
    private Button resetButton;
    private Text text;

    private int time;
    private bool running;

    public Action playCallback { get; set; }
    public Action stopCallback { get; set; }
    public Action resetCallback { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        playPauseButton = transform.Find("PlayPauseButton").GetComponent<Button>();
        text = transform.Find("PlayPauseButton/Text").GetComponent<Text>();
        resetButton = transform.Find("ResetButton").GetComponent<Button>();

        playPauseButton.onClick.AddListener(delegate { running = !running; text.color = running ? Color.red : Color.black; });
        playPauseButton.onClick.AddListener(delegate { if (running) { playCallback?.Invoke(); } else { stopCallback?.Invoke(); } });
        resetButton.onClick.AddListener(delegate { time = 0; text.text = GetTimeString(time); });
        resetButton.onClick.AddListener(delegate { resetCallback?.Invoke(); });

        running = false;
        time = 0;
        text.text = GetTimeString(time);

        InvokeRepeating("TimerTask", 1, 1);
    }

    public void TimerTask()
    {
        if (running)
            text.text = GetTimeString(++time);
    }

    public void PlayTimer()
    {
        running = true;
        text.color = Color.red;
    }

    public void StopTimer()
    {
        running = false;
        text.color = Color.black;
    }

    public void ResetTimer()
    {
        time = 0;
        text.text = GetTimeString(time);
    }

    public string GetTimeString(int value)
    {
        return (value / 60).ToString() + ":" + (value % 60 < 10 ? "0" : "") + (value % 60).ToString();
    }

    public void OnDestroy() => CancelInvoke();
}
