
using UnityEngine;

[System.Serializable]
public class GPUSkinBone
{
    [System.NonSerialized]
    public Transform transform = null;

    public string name = null;

    public Matrix4x4 bindpose;

    public int parentBoneIndex = -1;

    public int[] childrenBonesIndices = null;

}
