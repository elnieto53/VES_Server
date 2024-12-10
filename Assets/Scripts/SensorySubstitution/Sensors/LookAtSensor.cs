using System.Collections.Generic;
using UnityEngine;

public class LookAtSensor : MonoBehaviour
{
    public static List<GameObject> goLookedAt = new List<GameObject>();
    public int counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        goLookedAt.Add(gameObject);
    }

    public void IncrementLookedAtCounter()
    {
        counter++;
    }

    public void ClearLookedAtCounter()
    {
        counter = 0;
    }

    public static void Paint()
    {
        int maxValue = 0;

        foreach (GameObject go in goLookedAt)
        {
            int currentValue = go.GetComponent<LookAtSensor>().counter;
            if (maxValue < currentValue)
                maxValue = currentValue;
        }

        foreach (GameObject go in goLookedAt)
        {
            go.GetComponent<Renderer>().material.color = new Color((float)go.GetComponent<LookAtSensor>().counter / maxValue, 0, 0, 0);
        }
    }
}
