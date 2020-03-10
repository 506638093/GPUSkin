using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class GPUSkinGeneratorAnimationData
{
    public AnimationClip clip;
    public string name;
    public int frameRate;
    public GPUSkinWrapMode wrapMode = GPUSkinWrapMode.Once;
    public bool rootMotion = false;
}


[ExecuteInEditMode]
public class GPUSkinGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [HideInInspector]
    [SerializeField]
    public string animName = null;

    [HideInInspector]
    [System.NonSerialized]
    public AnimationClip animClip = null;

    [HideInInspector]
    [SerializeField]
    public List<GPUSkinGeneratorAnimationData> animClips = new List<GPUSkinGeneratorAnimationData>();

    [HideInInspector]
    [SerializeField]
    public GPUSkinAnimationData animData = null;

    [HideInInspector]
    [SerializeField]
    public Mesh savedMesh = null;

    [HideInInspector]
    [SerializeField]
    public GPUSkinQuality skinQuality = GPUSkinQuality.Bone4;

    [HideInInspector]
    [SerializeField]
    public Transform rootBoneTransform = null;

    [HideInInspector]
    [System.NonSerialized]
    public bool isSampling = false;

    [HideInInspector]
    [System.NonSerialized]
    public int samplingClipIndex = -1;

    [HideInInspector]
    [System.NonSerialized]
    public int samplingFrameIndex = 0;

    [HideInInspector]
    [System.NonSerialized]
    public int samplingTotalFrams = 0;

    [HideInInspector]
    [SerializeField]
    public Matrix4x4 rootTransformMatrix;

    private new Animation animation = null;
    private Animator animator = null;
    private RuntimeAnimatorController runtimeAnimatorController = null;

    private SkinnedMeshRenderer smr = null;

    private GPUSkinAnimationData gpuSkinAnimData = null;

    private GPUSkinClip gpuSkinClip = null;

    private Vector3 rootMotionPosition;

    private Quaternion rootMotionRotation;


    public const string TEMP_SAVED_ANIM_PATH = "GPUSkin_Temp_Save_AnimData_Path";
    public const string TEMP_SAVED_MESH_PATH = "GPUSkin_Temp_Save_Mesh_Path";


    private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
    {
        public int index = 0;

        public float weight = 0;

        public int CompareTo(BoneWeightSortData b)
        {
            return weight > b.weight ? -1 : 1;
        }
    }

    private void Awake()
    {
        animation = GetComponent<Animation>();
        animator = GetComponent<Animator>();
        if (animator == null && animation == null)
        {
            DestroyImmediate(this);
            ShowDialog("Cannot find Animator Or Animation Component");
            return;
        }
        if (animator != null && animation != null)
        {
            DestroyImmediate(this);
            ShowDialog("Animation is not coexisting with Animator");
            return;
        }

        if (animator != null)
        {
            if (animator.runtimeAnimatorController == null)
            {
                DestroyImmediate(this);
                ShowDialog("Missing RuntimeAnimatorController");
                return;
            }
            if (animator.runtimeAnimatorController is AnimatorOverrideController)
            {
                DestroyImmediate(this);
                ShowDialog("RuntimeAnimatorController could not be a AnimatorOverrideController");
                return;
            }
            runtimeAnimatorController = animator.runtimeAnimatorController;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            CollectAnimationClips();
            InitTransform();
            return;
        }
        else if (animation != null)
        {
            CollectAnimationClips();
            animation.Stop();
            animation.cullingType = AnimationCullingType.AlwaysAnimate;
            InitTransform();
            return;
        }
    }

    public static void ShowDialog(string msg)
    {
        EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
    }

    private void SaveUserPreferDir(string dirPath)
    {
        PlayerPrefs.SetString("GPUSkinning_UserPreferDir", dirPath);
    }

    private string GetUserPreferDir()
    {
        return PlayerPrefs.GetString("GPUSkinning_UserPreferDir", Application.dataPath);
    }

    private void InitTransform()
    {
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
    }

    public bool IsAnimatorOrAnimation()
    {
        return animator != null;
    }

    private bool ContainsClip(AnimationClip clip)
    {
        for(int i=0; i<animClips.Count; ++i)
        {
            if (animClips[i].clip == clip)
            {
                return true;
            }
        }
        return false;
    }

    public void CollectAnimationClips()
    {
        AnimationClip[] clips = null;
        if (animator != null)
        {
            clips = runtimeAnimatorController.animationClips;
        }
        else if (animation == null)
        {
            clips = AnimationUtility.GetAnimationClips(gameObject);
        }

        for (int i = 0; i < clips.Length; ++i)
        {
            AnimationClip clip = clips[i];
            if (clip != null)
            {
                if (!ContainsClip(clip))
                {
                    animClips.Add(new GPUSkinGeneratorAnimationData()
                    {
                        name = clip.name,
                        clip = clip,
                        frameRate = (int)clip.frameRate,
                        wrapMode = clip.isLooping ? GPUSkinWrapMode.Loop : GPUSkinWrapMode.Once
                    });
                }
            }
        }

        for (int i = animClips.Count - 1; i >= 0; --i)
        {
            if (System.Array.IndexOf(clips, animClips[i].clip) == -1)
            {
                animClips.RemoveAt(i);
            }
        }
    }

    private int GetClipFrameRate(AnimationClip clip, int clipIndex)
    {
        return animClips[clipIndex].frameRate == 0 ? (int)clip.frameRate : animClips[clipIndex].frameRate;
    }

    private void SetCurrentAnimationClip()
    {
        if (animation == null)
        {
            AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController();
            AnimationClip[] clips = runtimeAnimatorController.animationClips;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            for (int i = 0; i < clips.Length; ++i)
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(clips[i], animClip));
            }
            animatorOverrideController.runtimeAnimatorController = runtimeAnimatorController;
            animatorOverrideController.ApplyOverrides(overrides);
            animator.runtimeAnimatorController = animatorOverrideController;
        }
    }

    private void PrepareRecordAnimator()
    {
        if (animator != null)
        {
            int numFrames = (int)(gpuSkinClip.frameRate * gpuSkinClip.length);

            animator.applyRootMotion = gpuSkinClip.rootMotionEnabled;
            animator.Rebind();
            animator.recorderStartTime = 0;
            animator.StartRecording(numFrames);
            for (int i = 0; i < numFrames; ++i)
            {
                animator.Update(1.0f / gpuSkinClip.frameRate);
            }
            animator.StopRecording();
            animator.StartPlayback();
        }
    }

    private void CollectBones(List<GPUSkinBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, GPUSkinBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
    {
        GPUSkinBone currentBone = new GPUSkinBone();
        bones_result.Add(currentBone);

        int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
        currentBone.transform = currentBoneTransform;
        currentBone.name = currentBone.transform.gameObject.name;
        currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
        currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);

        if (parentBone != null)
        {
            parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
        }

        int numChildren = currentBone.transform.childCount;
        if (numChildren > 0)
        {
            currentBone.childrenBonesIndices = new int[numChildren];
            for (int i = 0; i < numChildren; ++i)
            {
                CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i), i);
            }
        }
    }

    public void StartSample()
    {
        if (isSampling)
        {
            return;
        }

        if (string.IsNullOrEmpty(animName.Trim()))
        {
            ShowDialog("Animation name is empty.");
            return;
        }

        if (rootBoneTransform == null)
        {
            ShowDialog("Please set Root Bone.");
            return;
        }

        if (animClips.Count == 0)
        {
            ShowDialog("Please set Animation Clips.");
            return;
        }

        animClip = animClips[samplingClipIndex].clip;
        if (animClip == null)
        {
            isSampling = false;
            return;
        }

        int numFrames = (int)(GetClipFrameRate(animClip, samplingClipIndex) * animClip.length);
        if (numFrames == 0)
        {
            isSampling = false;
            return;
        }

        smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            ShowDialog("Cannot find SkinnedMeshRenderer.");
            return;
        }
        Mesh mesh = smr.sharedMesh;
        if (mesh == null)
        {
            ShowDialog("Missing Mesh");
            return;
        }

        samplingFrameIndex = 0;

        gpuSkinAnimData = animData == null ? ScriptableObject.CreateInstance<GPUSkinAnimationData>() : animData;
        gpuSkinAnimData.name = animName;

        List<GPUSkinBone> bones_result = new List<GPUSkinBone>();
        CollectBones(bones_result, smr.bones, mesh.bindposes, null, rootBoneTransform, 0);
        gpuSkinAnimData.bones = bones_result;
        gpuSkinAnimData.rootBoneIndex = 0;
        gpuSkinAnimData.rootTransformMatrix = rootTransformMatrix;

        int numClips = gpuSkinAnimData.clips == null ? 0 : gpuSkinAnimData.clips.Count;
        int overrideClipIndex = -1;
        for (int i = 0; i < numClips; ++i)
        {
            if (gpuSkinAnimData.clips[i].name == animClips[samplingClipIndex].name)
            {
                overrideClipIndex = i;
                break;
            }
        }

        gpuSkinClip = new GPUSkinClip();
        gpuSkinClip.name = animClips[samplingClipIndex].name;
        gpuSkinClip.frameRate = GetClipFrameRate(animClip, samplingClipIndex);
        gpuSkinClip.length = animClip.length;
        gpuSkinClip.wrapMode = animClips[samplingClipIndex].wrapMode;
        gpuSkinClip.frames = new GPUSkinFrame[numFrames];
        gpuSkinClip.rootMotionEnabled = animClips[samplingClipIndex].rootMotion;

        if (gpuSkinAnimData.clips == null)
        {
            gpuSkinAnimData.clips = new List<GPUSkinClip>();
            gpuSkinAnimData.clips.Add(gpuSkinClip);
        }
        else
        {
            if (overrideClipIndex == -1)
            {
                gpuSkinAnimData.clips.Add(gpuSkinClip);
            }
            else
            {
                GPUSkinClip overridedClip = gpuSkinAnimData.clips[overrideClipIndex];
                //RestoreCustomClipData(overridedClip, gpuSkinningClip);
                gpuSkinAnimData.clips[overrideClipIndex] = gpuSkinClip;
            }
        }

        SetCurrentAnimationClip();
        PrepareRecordAnimator();

        isSampling = true;
    }

    private void CalculateTextureSize(int numPixels, out int texWidth, out int texHeight)
    {
        texWidth = 1;
        texHeight = 1;
        while (true)
        {
            if (texWidth * texHeight >= numPixels) break;
            texWidth *= 2;
            if (texWidth * texHeight >= numPixels) break;
            texHeight *= 2;
        }
    }

    private void SetTextureInfo(GPUSkinAnimationData data)
    {
        int numPixels = 0;

        var clips = data.clips;
        int numClips = clips.Count;
        for (int clipIndex = 0; clipIndex < numClips; ++clipIndex)
        {
            GPUSkinClip clip = clips[clipIndex];
            clip.pixelSegmentation = numPixels;

            numPixels += data.bones.Count * 3/*treat 3 pixels as a float3x4*/ * clip.frames.Length;
        }

        CalculateTextureSize(numPixels, out data.textureWidth, out data.textureHeight);
    }

    private Mesh CreateNewMesh(Mesh mesh, string meshName)
    {
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        Color[] colors = mesh.colors;
        Vector2[] uv = mesh.uv;

        Mesh newMesh = new Mesh();
        newMesh.name = meshName;
        newMesh.vertices = mesh.vertices;
        if (normals != null && normals.Length > 0)
        {
            newMesh.normals = normals;
        }
        if (tangents != null && tangents.Length > 0)
        {
            newMesh.tangents = tangents;
        }
        if (colors != null && colors.Length > 0)
        {
            newMesh.colors = colors;
        }
        if (uv != null && uv.Length > 0)
        {
            newMesh.uv = uv;
        }

        int numVertices = mesh.vertexCount;
        BoneWeight[] boneWeights = mesh.boneWeights;
        Vector4[] uv2 = new Vector4[numVertices];
        Vector4[] uv3 = new Vector4[numVertices];
        Transform[] smrBones = smr.bones;
        for (int i = 0; i < numVertices; ++i)
        {
            BoneWeight boneWeight = boneWeights[i];

            BoneWeightSortData[] weights = new BoneWeightSortData[4];
            weights[0] = new BoneWeightSortData() { index = boneWeight.boneIndex0, weight = boneWeight.weight0 };
            weights[1] = new BoneWeightSortData() { index = boneWeight.boneIndex1, weight = boneWeight.weight1 };
            weights[2] = new BoneWeightSortData() { index = boneWeight.boneIndex2, weight = boneWeight.weight2 };
            weights[3] = new BoneWeightSortData() { index = boneWeight.boneIndex3, weight = boneWeight.weight3 };
            System.Array.Sort(weights);

            GPUSkinBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
            GPUSkinBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
            GPUSkinBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
            GPUSkinBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

            Vector4 skinData_01 = new Vector4();
            skinData_01.x = GetBoneIndex(bone0);
            skinData_01.y = weights[0].weight;
            skinData_01.z = GetBoneIndex(bone1);
            skinData_01.w = weights[1].weight;
            uv2[i] = skinData_01;

            Vector4 skinData_23 = new Vector4();
            skinData_23.x = GetBoneIndex(bone2);
            skinData_23.y = weights[2].weight;
            skinData_23.z = GetBoneIndex(bone3);
            skinData_23.w = weights[3].weight;
            uv3[i] = skinData_23;
        }
        newMesh.SetUVs(1, new List<Vector4>(uv2));
        newMesh.SetUVs(2, new List<Vector4>(uv3));

        newMesh.triangles = mesh.triangles;
        return newMesh;
    }

    public void BeginSample()
    {
        samplingClipIndex = 0;
    }

    public void EndSample()
    {
        samplingClipIndex = -1;
    }

    public bool IsSamplingProgress()
    {
        return samplingClipIndex != -1;
    }

    private void Update()
    {
        if (!isSampling)
        {
            return;
        }

        int totalFrams = (int)(gpuSkinClip.length * gpuSkinClip.frameRate);
        samplingTotalFrams = totalFrams;

        if (samplingFrameIndex >= totalFrams)
        {
            if (animator != null)
            {
                animator.StopPlayback();
            }

            string savePath = null;
            if (animData == null)
            {
                savePath = EditorUtility.SaveFolderPanel("GPUSkinning Animation Data Save", GetUserPreferDir(), string.Empty);
            }
            else
            {
                string animPath = AssetDatabase.GetAssetPath(animData);
                savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
            }

            if (!string.IsNullOrEmpty(savePath))
            {
                if (!savePath.Contains(Application.dataPath.Replace('\\', '/')))
                {
                    ShowDialog("Must select a directory in the project's Asset folder.");
                }
                else
                {
                    SaveUserPreferDir(savePath);

                    string dir = "Assets" + savePath.Substring(Application.dataPath.Length);

                    string savedAnimPath = dir + "/GPUSKinAnimData_" + animName + ".asset";
                    SetTextureInfo(gpuSkinAnimData);
                    EditorUtility.SetDirty(gpuSkinAnimData);
                    if (animData != gpuSkinAnimData)
                    {
                        AssetDatabase.CreateAsset(gpuSkinAnimData, savedAnimPath);
                    }
                    WriteTempData(TEMP_SAVED_ANIM_PATH, savedAnimPath);
                    animData = gpuSkinAnimData;

                    if (samplingClipIndex == 0)
                    {
                        Mesh newMesh = CreateNewMesh(smr.sharedMesh, "GPUSkinning_Mesh");
                        if (savedMesh != null)
                        {
                            newMesh.bounds = savedMesh.bounds;
                        }
                        string savedMeshPath = dir + "/GPUSKinMesh_" + animName + ".asset";
                        AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                        WriteTempData(TEMP_SAVED_MESH_PATH, savedMeshPath);
                        savedMesh = newMesh;

                        //    CreateShaderAndMaterial(dir);

                        //    CreateLODMeshes(newMesh.bounds, dir);
                    }

                    AssetDatabase.Refresh();
                    AssetDatabase.SaveAssets();
                }
            }

            isSampling = false;
            return;
        }


        float time = gpuSkinClip.length * ((float)samplingFrameIndex / totalFrams);
        GPUSkinFrame frame = new GPUSkinFrame();
        gpuSkinClip.frames[samplingFrameIndex] = frame;
        frame.matrices = new Matrix4x4[gpuSkinAnimData.bones.Count];
        if (animation == null)
        {
            animator.playbackTime = time;
            animator.Update(0);
        }
        else
        {
            animation.Stop();
            AnimationState animState = animation[animClip.name];
            if (animState != null)
            {
                animState.time = time;
                animation.Sample();
                animation.Play();
            }
        }
        StartCoroutine(SamplingCoroutine(frame, totalFrams));

    }

    private GPUSkinBone GetBoneByTransform(Transform transform)
    {
        List<GPUSkinBone> bones = gpuSkinAnimData.bones;
        int numBones = bones.Count;
        for (int i = 0; i < numBones; ++i)
        {
            if (bones[i].transform == transform)
            {
                return bones[i];
            }
        }
        return null;
    }

    private int GetBoneIndex(GPUSkinBone bone)
    {
        List<GPUSkinBone> bones = gpuSkinAnimData.bones;
        int numBones = bones.Count;
        for (int i = 0; i < numBones; ++i)
        {
            if (bone == bones[i])
            {
                return i;
            }
        }
        return -1;
    }

    private IEnumerator SamplingCoroutine(GPUSkinFrame frame, int totalFrames)
    {
        yield return new WaitForEndOfFrame();

        List<GPUSkinBone> bones = gpuSkinAnimData.bones;
        int numBones = bones.Count;
        for (int i = 0; i < numBones; ++i)
        {
            GPUSkinBone currentBone = bones[i];
            frame.matrices[i] = currentBone.bindpose;
            do
            {
                Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                frame.matrices[i] = mat * frame.matrices[i];
                if (currentBone.parentBoneIndex == -1)
                {
                    break;
                }
                else
                {
                    currentBone = bones[currentBone.parentBoneIndex];
                }
            }
            while (true);
        }

        int rootBoneIndex = gpuSkinAnimData.rootBoneIndex;
        if (samplingFrameIndex == 0)
        {
            rootMotionPosition = bones[rootBoneIndex].transform.localPosition;
            rootMotionRotation = bones[rootBoneIndex].transform.localRotation;
        }
        else
        {
            Vector3 newPosition = bones[rootBoneIndex].transform.localPosition;
            Quaternion newRotation = bones[rootBoneIndex].transform.localRotation;
            Vector3 deltaPosition = newPosition - rootMotionPosition;
            frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(transform.forward.normalized)) * Quaternion.Euler(deltaPosition.normalized);
            frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
            frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;

            if (samplingFrameIndex == 1)
            {
                gpuSkinClip.frames[0].rootMotionDeltaPositionQ = gpuSkinClip.frames[1].rootMotionDeltaPositionQ;
                gpuSkinClip.frames[0].rootMotionDeltaPositionL = gpuSkinClip.frames[1].rootMotionDeltaPositionL;
                gpuSkinClip.frames[0].rootMotionDeltaRotation = gpuSkinClip.frames[1].rootMotionDeltaRotation;
            }
        }

        ++samplingFrameIndex;
    }


    public static void WriteTempData(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    public static string ReadTempData(string key)
    {
        return PlayerPrefs.GetString(key, string.Empty);
    }

    public static void DeleteTempData(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

#endif
}
