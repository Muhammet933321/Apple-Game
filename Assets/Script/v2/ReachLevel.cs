// ReachLevel.cs
[System.Serializable]
public struct ReachLevel
{
    public int   appleCount;    // kaç elma
    public float height;        // kafadan yukarı (+) aşağı (-) metre
    public float distance;      // kameradan ileri metre
    public float arcSpanDeg;    // yayı genişliği (örn. 60°)

    public ReachLevel(int count, float h, float d, float span)
    { appleCount = count; height = h; distance = d; arcSpanDeg = span; }
}