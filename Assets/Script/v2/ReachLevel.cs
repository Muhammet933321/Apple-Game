using UnityEngine;

/// Shared level descriptor for all activities
[System.Serializable]
public struct ReachLevel
{
    public int   appleCount;     // number of apples
    public float height;         // metres above (+) / below (-) headset
    public float distance;       // metres forward from headset
    public float arcSpanDeg;     // arc width in degrees

    public ReachLevel(int count, float h, float d, float span)
    {
        appleCount  = count;
        height      = h;
        distance    = d;
        arcSpanDeg  = span;
    }
}