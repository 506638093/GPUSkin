
[System.Serializable]
public class GPUSkinAnimEvent : System.IComparable<GPUSkinAnimEvent>
{
    public int frameIndex = 0;

    public int eventId = 0;

    public int CompareTo(GPUSkinAnimEvent other)
    {
        return frameIndex > other.frameIndex ? -1 : 1;
    }
}
