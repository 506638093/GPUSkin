
[System.Serializable]
public class GPUSkinClip
{
    public string name = null;

    public float length = 0.0f;

    public int frameRate = 0;

    public GPUSkinWrapMode wrapMode = GPUSkinWrapMode.Once;

    public GPUSkinFrame[] frames = null;

    public int pixelSegmentation = 0;

    public bool rootMotionEnabled = false;

    public GPUSkinAnimEvent[] events = null;
}
