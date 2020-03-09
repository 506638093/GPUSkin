using System;
using System.Collections.Generic;
using UnityEngine;


public class GPUSkinAnimatorData : ScriptableObject
{
    public List<GPUSkinBone> bones = null;

    public int rootBoneIndex = 0;

    public Matrix4x4 rootTransformMatrix;
}
