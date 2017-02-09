using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class CpuUseage : MonoBehaviour
{
    PerformanceCounter cpuCounter;
    PerformanceCounter ramCounter;
    void Start()
    {
        PerformanceCounterCategory.Exists("PerformanceCounter");
        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", Process.GetCurrentProcess().ProcessName);
        cpuCounter.CategoryName = "Processor";
        cpuCounter.CounterName = "% Processor Time";
        cpuCounter.InstanceName = "_Total";

        ramCounter = new PerformanceCounter("Memory", "Available MBytes");
    }
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "> cpu: " + getCurrentCpuUsage() + "; >ram: " + getAvailableRAM());
    }

    public string getCurrentCpuUsage()
    {
        return cpuCounter.NextValue() + "%";
    }

    public string getAvailableRAM()
    {
        return ramCounter.NextValue() + "MB";
    }
}
