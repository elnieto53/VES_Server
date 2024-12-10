using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DevicesGUI : MonoBehaviour
{
    private NetworkManager networkManager;
    private InputField staticIPInputField;
    private Button addStaticIPButton;
    public static Dictionary<int, (RawImage, Text)> iconList;


    // Start is called before the first frame update
    public void Init(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        staticIPInputField = transform.Find("AddStaticIP/InputField").GetComponent<InputField>();
        addStaticIPButton = transform.Find("AddStaticIP/Button").GetComponent<Button>();
        addStaticIPButton.onClick.AddListener(AddStaticIP);

        iconList = new Dictionary<int, (RawImage, Text)>();

        iconList.Add(1, (transform.Find("Huzzah1/Icon").GetComponent<RawImage>(), GameObject.Find("Huzzah1/Text").GetComponent<Text>()));
        iconList.Add(2, (transform.Find("Huzzah2/Icon").GetComponent<RawImage>(), GameObject.Find("Huzzah2/Text").GetComponent<Text>()));
        iconList.Add(3, (transform.Find("Huzzah3/Icon").GetComponent<RawImage>(), GameObject.Find("Huzzah3/Text").GetComponent<Text>()));
        iconList.Add(4, (transform.Find("Huzzah4/Icon").GetComponent<RawImage>(), GameObject.Find("Huzzah4/Text").GetComponent<Text>()));
        iconList.Add(5, (transform.Find("Huzzah5/Icon").GetComponent<RawImage>(), GameObject.Find("Huzzah5/Text").GetComponent<Text>()));
        iconList.Add(6, (transform.Find("Glasses/Icon").GetComponent<RawImage>(), GameObject.Find("Glasses/Text").GetComponent<Text>()));

        InvokeRepeating("UpdateDevicesPanel", 1, Session.SCAN_PERIOD / 1000f);
    }

    void UpdateDevicesPanel()
    {
        //networkManager.SyncNetClocks(null);

        /*The network is scanned periodically in 'Main.cs'*/
        List<NetworkManager.NetDevice> connectedDevices = networkManager.GetAvailableDevices(Session.STAY_ALIVE_TIMEOUT);

        foreach (int deviceID in iconList.Keys)
        {
            iconList.TryGetValue(deviceID, out (RawImage icon, Text deviceInfo) entry);
            if (connectedDevices.Exists(p => p.ID == deviceID))
            {
                NetworkManager.NetDevice device = connectedDevices.Find(p => p.ID == deviceID);
                entry.icon.color = device.isSynchronized ? Color.green : Color.magenta;
                //entry.deviceInfo.text = "Battery: " + dev.info.batteryLevel + ".   IP: " + dev.address.ToString() + ".   Haptics: " + (dev.info.HapticsAvalailable == 1 ? "ON" : "OFF");
                entry.deviceInfo.text = device.batteryLevel.ToString();
                if (entry.deviceInfo.text.Length < 4)  //Workaround - DELETE!!!!
                    entry.deviceInfo.text += "\t";
                entry.deviceInfo.text += "\t\t\t" + (device.hapticsAvailable ? "ON " : "OFF");
                entry.deviceInfo.text += "\t\t\t\t" + device.address.ToString();
            }
            else
            {
                entry.icon.color = Color.red;
                entry.deviceInfo.text = "Offline";
            }
        }
    }

    private void AddStaticIP()
    {
        try
        {
            string IPAddress = staticIPInputField.text;
            networkManager.AddIPToScan(System.Net.IPAddress.Parse(IPAddress));
            staticIPInputField.text = "'" + IPAddress + "' added";
        }
        catch
        {
            staticIPInputField.text = "Error: could not add IP";
        }
    }
}
