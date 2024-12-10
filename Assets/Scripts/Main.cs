using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class Main : MonoBehaviour
{
    public Boolean batteryMonitor;
    public Boolean testCalibration;
    public GameObject A, B;
    Session session;
    GameObject mainPanel;
    Body body;
    BodyPart Left_Arm;
    BodyPart Left_Forearm;
    private float firstTimestamp = -1f;

    // Clase para los datos de la batería
    [System.Serializable]
    public class BatteryData
    {
        public float chargeLevel;
        public int deviceID; // Agregar ID del dispositivo
        public float time;

        public BatteryData(float chargeLevel, int deviceID, float time)
        {
            this.chargeLevel = chargeLevel;
            this.deviceID = deviceID;
            this.time = time;
        }
    }


    // Clase para coleccionar múltiples datos de nivel de carga
    [System.Serializable]
    public class BatteryChargeDataCollection
    {
        public List<BatteryData> batteryDataPoints = new List<BatteryData>();
    }

    private BatteryChargeDataCollection batteryDataCollection = new BatteryChargeDataCollection();

    
    // Clase para los datos de la batería
    [System.Serializable]
    public class BodyPartOrientationData
    {
        public int ID;
        public Quaternion Q1;
        public Quaternion Q2;
        public float angle;
        public float time;

        public BodyPartOrientationData(int ID, Quaternion Q1, Quaternion Q2, float angle, float time)
        {
            this.ID = ID;
            this.Q1 = Q1;
            this.Q2 = Q2;
            this.angle = angle;
            this.time = time;
        }
    }

    // Clase para coleccionar múltiples datos de nivel de carga
    [System.Serializable]
    public class BodyPartOrientationCollection
    {
        public List<BodyPartOrientationData> bodyPartOrientationPoints = new List<BodyPartOrientationData>();
    }

    private BodyPartOrientationCollection bodyPartOrientationCollection = new BodyPartOrientationCollection();


    // Variables para el temporizador de guardado
    private float saveInterval = 0.08f;  // Intervalo de guardado en segundos
    private float saveTimer = 0f;
    private float dataCollectionTimerBattery = 0f; // Temporizador para la toma de datos
    private float dataCollectionIntervalBattery = 0.98f; // Intervalo de recogida de datos en segundos
    private float dataCollectionTimerCalibration = 0f; // Temporizador para la toma de datos
    private float dataCollectionIntervalCalibration = 0.08f; // Intervalo de recogida de datos en segundos

    // Start is called before the first frame update
    private void Start()
    {
        Session.InitData initData = new Session.InitData()
        {
            netPort = 11000,
            virtualScenarioPort = 11001,
            moCapPort = 11002,
            poseMoCapChannel = 4,
            orientationMoCapChannel = 3,
            moCapBodyID = 1,
        };
        initData.AddScenarioPaths(ResourcesPath.Prefabs.Environments.common, ResourcesPath.Prefabs.Environments.article3);

        session = new Session(initData);

        /*This line limits the fps. Without it, the power consumption increases several fold*/
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        mainPanel = GameObject.Find("MainPanel");
        mainPanel.GetComponent<MainGUI>().Init(session);

        System.Net.IPAddress address = TransportLayer.GetHostLocalIP();
        Debug.Log("IP: " + address);

        //se puede añadir código extra para inicialiación (lista de ip unicast? --sesion-transportmanager-lista_dispositivos)
        InvokeRepeating("ScanNetwork", 1, 1);
        InvokeRepeating("SynchronizeDevices", 1, 2);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        session.Update();

        float currentTime = Time.time;  // Tiempo actual en segundos desde el inicio del juego

        dataCollectionTimerBattery += Time.deltaTime; // Aumentar el temporizador de recolección de datos
        dataCollectionTimerCalibration += Time.deltaTime; // Aumentar el temporizador de recolección de datos

        if (batteryMonitor)
        {
            // Tomar datos cada segundo
            if (dataCollectionTimerBattery >= dataCollectionIntervalBattery) // Si ha pasado 1 segundo
            {
                if (firstTimestamp < 0f)
                {
                    firstTimestamp = currentTime;  // Establecer el tiempo de la primera toma
                }

                float time = currentTime - firstTimestamp;

                foreach (var device in session.GetAvailableNetDevices())
                {
                    // Suponiendo que el nivel de batería es un UInt32 en el rango de 0 a 100
                    float chargeLevel = device.batteryLevel;
                    // Añadir el nivel de carga a la colección
                    BatteryData batteryData = new BatteryData(chargeLevel, device.ID, time);
                    batteryDataCollection.batteryDataPoints.Add(batteryData);
                }

                dataCollectionTimerBattery = 0f; // Reiniciar el temporizador de recolección de datos
            }

            // Guardar datos
            saveTimer += Time.deltaTime;
            if (saveTimer >= saveInterval)
            {
                SaveBatteryDataToJson();
                saveTimer = 0f;  // Reiniciar el temporizador
            }
        }

        if (testCalibration)
        {
            // Tomar datos cada segundo
            if (dataCollectionTimerCalibration >= dataCollectionIntervalCalibration) // Si ha pasado 1 segundo
            {
                if (firstTimestamp < 0f)
                {
                    firstTimestamp = currentTime;  // Establecer el tiempo de la primera toma
                }

                float time = currentTime - firstTimestamp;

                float angle = Quaternion.Angle(A.transform.rotation, B.transform.rotation);

                Debug.Log($"Datos de calibracion {angle}.");

                BodyPartOrientationData orientationData1 = new BodyPartOrientationData(1, A.transform.rotation, B.transform.rotation, angle, time);
                //BodyPartOrientationData orientationData2 = new BodyPartOrientationData(2, B.transform.rotation, time);

                bodyPartOrientationCollection.bodyPartOrientationPoints.Add(orientationData1);
                //bodyPartOrientationCollection.bodyPartOrientationPoints.Add(orientationData2);

                dataCollectionTimerCalibration = 0f; // Reiniciar el temporizador de recolección de datos
            }

            // Guardar datos
            saveTimer += Time.deltaTime;
            if (saveTimer >= saveInterval)
            {
                SaveCalibrationDataToJson();
                saveTimer = 0f;  // Reiniciar el temporizador
            }
        }

        if (!batteryMonitor && !testCalibration)
        {
            firstTimestamp = -1f;
            bodyPartOrientationCollection.bodyPartOrientationPoints.Clear();
        }
    }

    private void SaveBatteryDataToJson()
    {
        // Guardar los datos en un archivo JSON
        string json1 = JsonUtility.ToJson(batteryDataCollection, true);
        File.WriteAllText(Application.dataPath + "/batteryData.json", json1);
        Debug.Log("Datos de carga de bateria guardados en JSON.");
    }

    private void SaveCalibrationDataToJson()
    {
        // Guardar los datos en un archivo JSON
        string json2 = JsonUtility.ToJson(bodyPartOrientationCollection, true);
        File.WriteAllText(Application.dataPath + "/calibrationData.json", json2);
        Debug.Log("Datos de calibración guardados en JSON.");
    }

    private void ScanNetwork() => session.ScanNetwork();

    private void SynchronizeDevices() => session.virtualScenario.GetNetworkManager().SynchronizeDevices();

    private void OnDestroy()
    {
        if (batteryMonitor)
        {
            // Guardar los datos una última vez antes de destruir
            SaveBatteryDataToJson();
        }

        if (testCalibration)
        {
            // Guardar los datos una última vez antes de destruir
            SaveCalibrationDataToJson();
        }

        //new JsonSerializer<Data>(new Data(hitPositions)).SaveDataToFile("Oh My", ResourcesPath.Files.motionData + "/Test Replay", false);
        Destroy(mainPanel);
        session.DeInit();
    }
}