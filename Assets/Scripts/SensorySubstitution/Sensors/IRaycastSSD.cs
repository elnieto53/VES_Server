using System;
using UnityEngine;

public interface IRaycastSSD
{
    /*Provides 'hit' data and a timestamp*/
    public Action<RaycastHit> onDetectionCallback { get; set; }
}
