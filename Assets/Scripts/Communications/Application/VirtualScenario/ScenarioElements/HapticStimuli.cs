using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class HapticStimuli : ScenarioElement
{
    private byte vibrationAmplitude = 128;
    //private byte vibrationAmplitude = 13;
    public bool isOn = false;

    private int impulseTimestamp = 0;

    private byte txVibrationAmplitude
    {
        set { vibrationAmplitude = value; }
        //set { vibrationAmplitude = (byte)(value / 10); }
        get { return isOn && ((int)(Time.time * 1000) < impulseTimestamp) ? vibrationAmplitude : (byte)128; }
    }

    public void Impulse(byte intensity, int duration)
    {
        vibrationAmplitude = intensity;
        impulseTimestamp = (int)(Time.time * 1000) + duration;
        //impulseTimestamp = (int)(Time.time * 1000) + duration/10;
    }

    public HapticStimuli() : base() { }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct PkgData
    {
        public byte vibrationAmplitude;
        public PkgData(byte vibrationAmplitude)
        {
            this.vibrationAmplitude = vibrationAmplitude;
        }

        public PkgData(byte[] data) : this() => this = PkgSerializer.GetStruct<PkgData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public enum SpawnableHapticStimuli
    {
        Default = 1,
    }

    //private static readonly Dictionary<int, GameObject> spawnableHapticElement = new Dictionary<int, GameObject> {
    //    { (int)SpawnableHapticStimuli.Default, (GameObject)Resources.Load("Haptic/DefaultHapticManager") },
    //};

    private protected override void LoadPrefab(byte prefabID)
    {
        //if (spawnableHapticElement.TryGetValue(prefabID, out GameObject prefab))
        //{
        //    /*For now I won't use this. Maybe later I could make a haptic renderer in which haptic stimuli are generated according to
        //     * the position of 'haptic elements' in the scene and the user's body movement. For example, the distance between the haptic
        //     * interface and the 'haptic element' could be coded in vibration intensity*/
        //    go = MonoBehaviour.Instantiate(prefab);
        //}
    }

    public override void Config(byte[] updateData)
    {
        PkgData remote = new PkgData(updateData);
        vibrationAmplitude = remote.vibrationAmplitude;
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        data = BuildPkg(new PkgData((byte)txVibrationAmplitude).GetBytes());
        return true;
    }
}

//public enum HapticEffect
//{
//    drv2605L_effect_stop = 0x00,

//    // Strong Clicks
//    drv2605L_effect_strongClick_100P,
//    drv2605L_effect_strongClick_60P,
//    drv2605L_effect_strongClick_30P,

//    // Sharp Clicks
//    drv2605L_effect_sharpClick_100P,
//    drv2605L_effect_sharpClick_60P,
//    drv2605L_effect_sharpClick_30P,

//    // Soft Bumps
//    drv2605L_effect_softBump_100P,
//    drv2605L_effect_softBump_60P,
//    drv2605L_effect_softBump_30P,

//    // Double/Triple Clicks
//    drv2605L_effect_doubleClick_100P,
//    drv2605L_effect_doubleClick_60P,
//    drv2605L_effect_tripleClick_100P,

//    // Soft Fuzz
//    drv2605L_effect_softFuzz_100P,

//    // Strong Buzz
//    drv2605L_effect_strongBuzz_100P,

//    // Alerts
//    drv2605L_effect_750msAlert_100P,
//    drv2605L_effect_1sAlert_100P,

//    // Strong Clicks (Expanded)
//    drv2605L_effect_strongClick1_100P,
//    drv2605L_effect_strongClick2_80P,
//    drv2605L_effect_strongClick3_60P,
//    drv2605L_effect_strongClick4_30P,

//    // Medium Clicks
//    drv2605L_effect_mediumClick1_100P,
//    drv2605L_effect_mediumClick2_80P,
//    drv2605L_effect_mediumClick3_60P,

//    // Sharp Ticks
//    drv2605L_effect_sharpTick1_100P,
//    drv2605L_effect_sharpTick2_80P,
//    drv2605L_effect_sharpTick3_60P,

//    // Short Double Clicks (Strong)
//    drv2605L_effect_shortDoubleClickStrong1_100P,
//    drv2605L_effect_shortDoubleClickStrong2_80P,
//    drv2605L_effect_shortDoubleClickStrong3_60P,
//    drv2605L_effect_shortDoubleClickStrong4_30P,

//    // Short Double Clicks (Medium)
//    drv2605L_effect_shortDoubleClickMedium1_100P,
//    drv2605L_effect_shortDoubleClickMedium2_80P,
//    drv2605L_effect_shortDoubleClickMedium3_60P,

//    // Short Double Sharp Ticks
//    drv2605L_effect_shortDoubleSharpTick1_100P,
//    drv2605L_effect_shortDoubleSharpTick2_80P,
//    drv2605L_effect_shortDoubleSharpTick3_60P,

//    // Long Double Sharp Clicks (Strong)
//    drv2605L_effect_longDoubleSharpClickStrong1_100P,
//    drv2605L_effect_longDoubleSharpClickStrong2_80P,
//    drv2605L_effect_longDoubleSharpClickStrong3_60P,
//    drv2605L_effect_longDoubleSharpClickStrong4_30P,

//    // Long Double Sharp Clicks (Medium)
//    drv2605L_effect_longDoubleSharpClickMedium1_100P,
//    drv2605L_effect_longDoubleSharpClickMedium2_100P,
//    drv2605L_effect_longDoubleSharpClickMedium3_60P,

//    // Long Double Sharp Ticks
//    drv2605L_effect_longDoubleSharpTick1_100P,
//    drv2605L_effect_longDoubleSharpTick2_80P,
//    drv2605L_effect_longDoubleSharpTick3_60P,

//    // Buzzes
//    drv2605L_effect_buzz1_100P,
//    drv2605L_effect_buzz2_80P,
//    drv2605L_effect_buzz3_60P,
//    drv2605L_effect_buzz4_40P,
//    drv2605L_effect_buzz5_20P,

//    // Strong Pulses
//    drv2605L_effect_pulsingStrong1_100P,
//    drv2605L_effect_pulsingStrong2_60P,

//    // Medium Pulses
//    drv2605L_effect_pulsingMedium1_100P,
//    drv2605L_effect_pulsingMedium2_60P,

//    // Sharp Pulses
//    drv2605L_effect_pulsingSharp1_100P,
//    drv2605L_effect_pulsingSharp2_60P,

//    // Transition Clicks
//    drv2605L_effect_transitionClick1_100P,
//    drv2605L_effect_transitionClick2_80P,
//    drv2605_effect_transitionClick3_60P,
//    drv2605L_effect_transitionClick4_40P,
//    drv2605L_effect_transitionClick5_20P,
//    drv2605L_effect_transitionClick6_10P,

//    // Transition Hums
//    drv2605L_effect_transitionHum1_100P,
//    drv2605L_effect_transitionHum2_80P,
//    drv2605L_effect_transitionHum3_60P,
//    drv2605L_effect_transitionHum4_40P,
//    drv2605L_effect_transitionHum5_20P,
//    drv2605L_effect_transitionHum6_10P,

//    // Transition Ramp Downs (Smooth) Full Scale
//    drv2605L_effect_transitionRampDownLongSmooth1_100Pto0P,
//    drv2605L_effect_transitionRampDownLongSmooth2_100Pto0P,
//    drv2605L_effect_transitionRampDownMediumSmooth1_100Pto0P,
//    drv2605L_effect_transitionRampDownMediumSmooth2_100Pto0P,
//    drv2605L_effect_transitionRampDownShortSmooth1_100Pto0P,
//    drv2605L_effect_transitionRampDownShortSmooth2_100Pto0P,

//    // Transition Ramp Downs (Sharp) Full Scale
//    drv2605L_effect_transitionRampDownLongSharp1_100Pto0P,
//    drv2605L_effect_transitionRampDownLongSharp2_100Pto0P,
//    drv2605L_effect_transitionRampDownMediumSharp1_100Pto0P,
//    drv2605L_effect_transitionRampDownMediumSharp2_100Pto0P,
//    drv2605L_effect_transitionRampDownShortSharp1_100Pto0P,
//    drv2605L_effect_transitionRampDownShortSharp2_100Pto0P,

//    // Transition Ramp Ups (Smooth) Full Scale
//    drv2605L_effect_transitionRampUpLongSmooth1_0Pto100P,
//    drv2605L_effect_transitionRampUpLongSmooth2_0Pto100P,
//    drv2605L_effect_transitionRampUpMediumSmooth1_0Pto100P,
//    drv2605L_effect_transitionRampUpMediumSmooth2_0Pto100P,
//    drv2605L_effect_transitionRampUpShortSmooth1_0Pto100P,
//    drv2605L_effect_transitionRampUpShortSmooth2_0Pto100P,

//    // Transition Ramp Ups (Sharp) Full Scale
//    drv2605L_effect_transitionRampUpLongSharp1_0Pto100P,
//    drv2605L_effect_transitionRampUpLongSharp2_0Pto100P,
//    drv2605L_effect_transitionRampUpMediumSharp1_0Pto100P,
//    drv2605L_effect_transitionRampUpMediumSharp2_0Pto100P,
//    drv2605L_effect_transitionRampUpShortSharp1_0Pto100P,
//    drv2605L_effect_transitionRampUpShortSharp2_0Pto100P,

//    // Transition Ramp Downs (Smooth) Half Scale
//    drv2605L_effect_transitionRampDownLongSmooth1_50Pto0P,
//    drv2605L_effect_transitionRampDownLongSmooth2_50Pto0P,
//    drv2605L_effect_transitionRampDownMediumSmooth1_50Pto0P,
//    drv2605L_effect_transitionRampDownMediumSmooth2_50Pto0P,
//    drv2605L_effect_transitionRampDownShortSmooth1_50Pto0P,
//    drv2605L_effect_transitionRampDownShortSmooth2_50Pto0P,

//    // Transition Ramp Downs (Sharp) Half Scale
//    drv2605L_effect_transitionRampDownLongSharp1_50Pto0P,
//    drv2605L_effect_transitionRampDownLongSharp2_50Pto0P,
//    drv2605L_effect_transitionRampDownMediumSharp1_50Pto0P,
//    drv2605L_effect_transitionRampDownMediumSharp2_50Pto0P,
//    drv2605L_effect_transitionRampDownShortSharp1_50Pto0P,
//    drv2605L_effect_transitionRampDownShortSharp2_50Pto0P,

//    // Transition Ramp Ups (Smooth) Half Scale
//    drv2605L_effect_transitionRampUpLongSmooth1_0Pto50P,
//    drv2605L_effect_transitionRampUpLongSmooth2_0Pto50P,
//    drv2605L_effect_transitionRampUpMediumSmooth1_0Pto50P,
//    drv2605L_effect_transitionRampUpMediumSmooth2_0Pto50P,
//    drv2605L_effect_transitionRampUpShortSmooth1_0Pto50P,
//    drv2605L_effect_transitionRampUpShortSmooth2_0Pto50P,

//    // Transition Ramp Ups (Sharp) Half Scale
//    drv2605L_effect_transitionRampUpLongSharp1_0Pto50P,
//    drv2605L_effect_transitionRampUpLongSharp2_0Pto50P,
//    drv2605L_effect_transitionRampUpMediumSharp1_0Pto50P,
//    drv2605L_effect_transitionRampUpMediumSharp2_0Pto50P,
//    drv2605L_effect_transitionRampUpShortSharp1_0Pto50P,
//    drv2605L_effect_transitionRampUpShortSharp2_0Pto50P,

//    // Long Buzz for Programmatic Stopping
//    drv2605L_effect_LongBuzzForProgrammaticstopping,

//    // Smooth Hums (No Kick or Brake Pulses)
//    drv2605L_effect_smoothHum1_50P,
//    drv2605L_effect_smoothHum2_40P,
//    drv2605L_effect_smoothHum3_30P,
//    drv2605L_effect_smoothHum4_20P,
//    drv2605L_effect_smoothHum5_10P
//}

