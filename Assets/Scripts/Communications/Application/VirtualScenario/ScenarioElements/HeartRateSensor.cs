using System;
using System.Runtime.InteropServices;

public class HeartRateSensor : ScenarioElement
{
    public UInt32 rawMeasurement1 { get; set; }
    public UInt32 rawMeasurement2 { get; set; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PkgData
    {
        public UInt32 rawMeasurement1;
        public UInt32 rawMeasurement2;

        public PkgData(UInt32 rawMeasurement1, UInt32 rawMeasurement2)
        {
            this.rawMeasurement1 = rawMeasurement1;
            this.rawMeasurement2 = rawMeasurement2;
        }

        public PkgData(byte[] data) : this() => this = PkgSerializer.GetStruct<PkgData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public enum Spawnable
    {
        defaultElement = 1
    }

    public HeartRateSensor() : base()
    {

    }

    private protected override void LoadPrefab(byte prefabID)
    {
        /*This element only contains heart rate data; it does no require a prefab*/
    }

    public override void Config(byte[] updateData)
    {
        PkgData data = new PkgData(updateData);
        rawMeasurement1 = data.rawMeasurement1;
        rawMeasurement2 = data.rawMeasurement2;
        //Debug.Log("ID: " + elementID + ". Orientation: " + orientation);
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        data = BuildPkg(new PkgData(rawMeasurement1, rawMeasurement2).GetBytes());
        return true;
    }
}


//using System.Net;
//using System.Runtime.InteropServices;

//public class PhysSensorsManager : ApplicationLayer
//{
//    public enum ChannelCommands
//    {
//        HeartRateConfig = 0
//    }

//    [StructLayout(LayoutKind.Sequential, Pack = 1)]
//    public struct MAX30102ConfigData
//    {
//        public byte led1PulseAmplitude;
//        public byte led2PulseAmplitude;
//        public byte ledsPulseWidth;
//        public byte sampleRate;
//        public byte averagedSamples;
//        public byte adcRange;
//        public byte operationMode;

//        public enum PulseWidth
//        {
//            PW_0 = 0,
//            PW_1 = 1,
//            PW_2 = 2,
//            PW_3 = 3
//        }
//        public enum SampleRate
//        {
//            SR_50Hz = 0,
//            SR_100Hz = 1,
//            SR_200Hz = 2,
//            SR_400Hz = 3,
//            SR_800Hz = 4,
//            SR_1000Hz = 5,
//            SR_1600Hz = 6,
//            SR_3200Hz = 7,
//        }

//        public enum AveragedSamples
//        {
//            AvSam_0 = 0,
//            AvSam_2 = 1,
//            AvSam_4 = 2,
//            AvSam_8 = 3,
//            AvSam_16 = 4,
//            AvSam_32 = 5,
//        }

//        public enum ADCRange
//        {
//            Range_0 = 0,
//            Range_1 = 1,
//            Range_2 = 2,
//            Range_3 = 3
//        }

//        public enum OperationMode
//        {
//            HeartRate = 2,
//            SpO2Mode = 3,
//            MultiLedMode = 7
//        }

//        public MAX30102ConfigData(byte led1PA, byte led2PA, PulseWidth pw, SampleRate sr, AveragedSamples avSam, ADCRange range, OperationMode mode)
//        {
//            led1PulseAmplitude = led1PA;
//            led2PulseAmplitude = led2PA;
//            ledsPulseWidth = (byte)pw;
//            sampleRate = (byte)sr;
//            averagedSamples = (byte)avSam;
//            adcRange = (byte)range;
//            operationMode = (byte)mode;
//        }

//        public MAX30102ConfigData(byte[] data) : this() => this = PkgSerializer.GetStruct<MAX30102ConfigData>(data);
//        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
//    }


//    public PhysSensorsManager(int hostPort) : base(hostPort)
//    {

//    }

//    public override void ExecuteNetCommand(IPAddress origin, byte[] data)
//    {
//        throw new System.NotImplementedException();
//    }
//}
