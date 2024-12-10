using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Session
{
    [Serializable]
    public class Configuration
    {
        public InitData mainConfig; /*This is only saved but not loaded*/
        public string environment;
        public List<DetectionArea<ProjectedVAS.Configuration>> pvasConfig;
        public List<DetectionArea<EnvelopingVAS.Configuration>> evasConfig;
        public List<DetectionArea<ThevOICe.Configuration>> thevOICeConfig;

        public QoSManager.Configuration moCapQoSConfiguration;

        [Serializable]
        public class DetectionArea<T> where T : new()
        {
            public int sensorAnchorID;
            public T configuration;

            public DetectionArea(int sensorAnchorID, T configuration)
            {
                this.sensorAnchorID = sensorAnchorID;
                this.configuration = configuration;
            }
        }

        public Configuration()
        {
            pvasConfig = new List<DetectionArea<ProjectedVAS.Configuration>>();
            evasConfig = new List<DetectionArea<EnvelopingVAS.Configuration>>();
            thevOICeConfig = new List<DetectionArea<ThevOICe.Configuration>>();
        }

        public Configuration(Session session)
        {
            mainConfig = new InitData()
            {
                netPort = session.virtualScenario.networkManager.hostPort,
                virtualScenarioPort = session.virtualScenario.hostPort,
                moCapPort = session.moCap.hostPort,
                poseMoCapChannel = session.moCap.poseChannel,
                orientationMoCapChannel = session.moCap.orientationChannel,
                moCapBodyID = session.body.ID,
            };

            environment = session.environmentLoaded.name;

            pvasConfig = new List<DetectionArea<ProjectedVAS.Configuration>>();
            foreach ((Transform anchor, ProjectedVAS pvas) entry in session.pvasDetectionAreas)
            {
                pvasConfig.Add(new DetectionArea<ProjectedVAS.Configuration>(session.body.GetSensorAnchorID(entry.anchor), entry.pvas.GetConfig()));
            }

            evasConfig = new List<DetectionArea<EnvelopingVAS.Configuration>>();
            foreach ((Transform anchor, EnvelopingVAS evas) entry in session.evasDetectionAreas)
            {
                evasConfig.Add(new DetectionArea<EnvelopingVAS.Configuration>(session.body.GetSensorAnchorID(entry.anchor), entry.evas.GetConfig()));
            }

            thevOICeConfig = new List<DetectionArea<ThevOICe.Configuration>>();
            foreach ((Transform anchor, ThevOICe thevOICe) entry in session.thevOICeDetectionAreas)
            {
                thevOICeConfig.Add(new DetectionArea<ThevOICe.Configuration>(session.body.GetSensorAnchorID(entry.anchor), entry.thevOICe.GetConfig()));
            }

            moCapQoSConfiguration = session.moCapQoSManager.GetConfiguration();
        }

        public static Configuration LoadDataFromFile(string folderPath, string name) =>
            JsonSerializer<Configuration>.LoadDataFromFile(folderPath, name);

        public static Configuration LoadDataFromFile(string filePath) =>
            JsonSerializer<Configuration>.LoadDataFromFile(filePath);

        public void SaveDataToFile(string name, string folderPath, bool overwrite) =>
            new JsonSerializer<Configuration>(this).SaveDataToFile(name, folderPath, overwrite);

        public void SaveDataToFile(string filePath, bool overwrite) =>
            new JsonSerializer<Configuration>(this).SaveDataToFile(filePath, overwrite);
    }
}
