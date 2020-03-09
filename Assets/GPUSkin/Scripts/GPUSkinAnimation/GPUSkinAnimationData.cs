using System.Collections.Generic;
using UnityEngine;

public class GPUSkinAnimationData : ScriptableObject
{
    public string guid = null;

    public new string name = null;

    public List<GPUSkinBone> bones = null;

    public List<GPUSkinClip> clips = null;

    public int textureWidth = 0;

    public int textureHeight = 0;

    public int rootBoneIndex = 0;

    public Matrix4x4 rootTransformMatrix;
}
