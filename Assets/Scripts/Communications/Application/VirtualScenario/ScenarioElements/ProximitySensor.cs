using System;
using System.Runtime.InteropServices;
using UnityEngine;
using VESCharts;

public class ProximitySensor : ScenarioElement
{
    public UInt64 rawDistance { get; set; }
    public float distance { get => rawDistance / 56.2f; } /*This is a rough calibration curve; it needs to be tuned*/
    ChartGUI gui;
    public LineChart2D chart;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PkgData
    {
        public UInt64 rawDistance;

        public PkgData(UInt64 rawDistance)
        {
            this.rawDistance = rawDistance;
        }

        public PkgData(byte[] data) : this() => this = PkgSerializer.GetStruct<PkgData>(data);
        public byte[] GetBytes() => PkgSerializer.GetBytes(this);
    }

    public enum Spawnable
    {
        defaultElement = 1
    }

    public ProximitySensor() : base()
    {
        gui = MonoBehaviour.Instantiate((GameObject)Resources.Load(ResourcesPath.Prefabs.GUI.resizableSingleChart), GameObject.Find("MainPanel").transform).GetComponent<ChartGUI>();
        gui.Init("Time [ms]", "Distance [m]");
        chart = new LineChart2D(gui.Axes, 2, 2);
        chart.SetMaterial((Material)Resources.Load(ResourcesPath.Material.blue));
        //gui.AddChart(chart, 2);
    }

    private protected override void LoadPrefab(byte prefabID)
    {
        /*This element only contains heart rate data; it does no require a prefab*/
    }

    public override void Config(byte[] updateData)
    {
        PkgData data = new PkgData(updateData);
        rawDistance = data.rawDistance;
        chart.points.Add(new Vector2(netClock.GetTimeStamp(), distance));
        //chart.points.Add(Vector2.zero);
        while(chart.points.Count > 200)
        {
            chart.points.RemoveAt(0);
        }
        Debug.Log("Distance data: " + rawDistance);
    }

    public override bool UpdateAvailable(out byte[] data)
    {
        data = BuildPkg(new PkgData(rawDistance).GetBytes());
        return true;
    }
}
