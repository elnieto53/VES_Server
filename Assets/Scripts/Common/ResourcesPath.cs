using UnityEngine;

public class ResourcesPath
{
    public class Material
    {
        public static readonly string black = "Materials/Standard/Black";
        public static readonly string blue = "Materials/Standard/Blue";
        public static readonly string brown = "Materials/Standard/Brown";
        public static readonly string gray = "Materials/Standard/Gray";
        public static readonly string green = "Materials/Standard/Green";
        public static readonly string red = "Materials/Standard/Red";

        public static readonly string unlitBlack = "Materials/Unlit/Black";
        public static readonly string unlitBlue = "Materials/Unlit/Blue";
        public static readonly string unlitGray = "Materials/Unlit/Gray";
        public static readonly string unlitGreen = "Materials/Unlit/Green";
        public static readonly string unlitRed = "Materials/Unlit/Red";
        public static readonly string unlitWhite = "Materials/Unlit/White";

        public static readonly string transparentGreen = "Materials/Transparent/Green";
        public static readonly string transparentBlue = "Materials/Transparent/Blue";
        public static readonly string transparentRed = "Materials/Transparent/Red";

        public static readonly string palette1 = "Materials/Palette/Model1";
        public static readonly string palette2 = "Materials/Palette/Model2";
    }

    public class AudioClip
    {
        public static readonly string shortWoodHit1 = "Audio/02_WoodHit_1";
        public static readonly string shortWoodHit2 = "Audio/02_WoodHit_2";
        public static readonly string shortWoodHit3 = "Audio/02_WoodHit_3";
        public static readonly string shortMetalHit = "Audio/03_MetalHit_1";
        public static readonly string longWoodHit1 = "Audio/05_Wood_1Hit";
        public static readonly string longWoodHit2 = "Audio/02_Wood_2Hit";
        public static readonly string longMetalHit = "Audio/05_MetalHit";
        public static readonly string shortClick = "Audio/FastClick";
        public static readonly string carEngine = "Audio/CarEngine";
    }

    public class Prefabs
    {
        public static readonly string audioSource = "Prefabs/AudioSource";

        public class Body
        {
            public static readonly string head = "Prefabs/Skeleton/Head";
            public static readonly string torso = "Prefabs/Skeleton/Torso";
            public static readonly string rightArm = "Prefabs/Skeleton/RightArm";
            public static readonly string leftArm = "Prefabs/Skeleton/LeftArm";
            public static readonly string rightForearm = "Prefabs/Skeleton/RightForearm";
            public static readonly string leftForearm = "Prefabs/Skeleton/LeftForearm";
            public static readonly string rightThigh = "Prefabs/Skeleton/RightThigh";
            public static readonly string leftThigh = "Prefabs/Skeleton/LeftThigh";
            public static readonly string rightCalf = "Prefabs/Skeleton/RightCalf";
            public static readonly string leftCalf = "Prefabs/Skeleton/LeftCalf";
            public static readonly string hip = "Prefabs/Skeleton/Hip";

            public static readonly string manager = "Prefabs/Skeleton/DefaultBody";
        }

        public class Environments
        {
            public static readonly string common = "Prefabs/Environments/Common";
            public static readonly string article1 = "Prefabs/Environments/Article1";
            public static readonly string article2 = "Prefabs/Environments/Article2";
            public static readonly string article3 = "Prefabs/Environments/Article3";

            public class Symbols
            {
                public static readonly string widthCompSymbol = "Prefabs/Environments/Symbols/WidthComparative";
            }
        }

        public class GUI
        {
            public static readonly string devices = "Prefabs/GUI/Menus/DevicesPanel";
            public static readonly string doubleCheck = "Prefabs/GUI/Menus/DoubleCheckPanel";
            public static readonly string evas = "Prefabs/GUI/Menus/EVASPanel";
            public static readonly string pvas = "Prefabs/GUI/Menus/PVASPanel";
            public static readonly string thevOICe = "Prefabs/GUI/Menus/ThevOICePanel";
            public static readonly string qos = "Prefabs/GUI/Menus/QoSPanel";
            public static readonly string timer = "Prefabs/GUI/Menus/TimerPanel";
            public static readonly string stimuliHeatmap = "Prefabs/GUI/Menus/StimuliHeatmapPanel";

            public static readonly string fileExplorer = "Prefabs/GUI/Menus/FileExplorerPanel";
            public static readonly string folderExplorer = "Prefabs/GUI/Menus/FolderExplorerPanel";

            public static readonly string singleChart = "Prefabs/GUI/Charts/ChartPanel";
            public static readonly string doubleChart = "Prefabs/GUI/Charts/DoubleChartPanel";
            public static readonly string resizableSingleChart = "Prefabs/GUI/Charts/ResizableChartPanel";

            public static readonly string questionnaire = "Prefabs/GUI/Menus/Environment/QuestionnairePanel";
        }
    }

    public class Files
    {
#if UNITY_EDITOR
        public static readonly string mainDirectory = Application.dataPath + "/VES-Data";
#elif UNITY_ANDROID
        public static readonly string mainDirectory = "storage/emulated/0/Documents/VES-Data";
#else
        public static readonly string mainDirectory = Application.dataPath + "/VES-Data";
        //public static readonly string mainDirectory = Application.persistentDataPath;
#endif
        public static readonly string motionData = mainDirectory + "/MotionData";
        public static readonly string pvasStimuli = mainDirectory + "/PVASData";
        public static readonly string evasStimuli = mainDirectory + "/EVASData";
        public static readonly string qosRecording = mainDirectory + "/QoSRecordings";
        public static readonly string testConfig = mainDirectory + "/TestConfigurations";
    }
}
