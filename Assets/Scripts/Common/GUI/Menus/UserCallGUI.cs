using UnityEngine;
using UnityEngine.UI;

public class UserCallGUI : MonoBehaviour
{
    private AudioSource userCallAudioSource;
    public Toggle userCallToggle;

    // Start is called before the first frame update
    void Start()
    {
        userCallToggle = transform.Find("Toggle").GetComponent<Toggle>();
        userCallAudioSource = gameObject.AddComponent<AudioSource>();
        userCallToggle.onValueChanged.AddListener(delegate { UpdateUserCallToggle(userCallToggle.isOn); });
    }

    // This function is used for sending verbal intructions to the user through the headphones.
    // However, it assumes that the headphones are connected to the SERVER, and the server do have a microphone
    public void UpdateUserCallToggle(bool enable)
    {
        if (enable)
        {
            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(Microphone.devices[0], out minFreq, out maxFreq);

            Destroy(userCallAudioSource.clip);  //Frees memory from previous call
            userCallAudioSource.clip = Microphone.Start(Microphone.devices[0], true, 60, minFreq);    //The call has a buffer of 60 sec
            userCallAudioSource.PlayDelayed(0.1f);  //Once the data is recorded, it is conveyed to the headphones (after a delay)

        }
        else
        {
            if (Microphone.IsRecording(Microphone.devices[0]))
            {
                Microphone.End(Microphone.devices[0]);
                Debug.Log(Microphone.IsRecording(Microphone.devices[0]));
            }
        }
    }
}
