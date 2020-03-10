using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GPUSkinAnimation : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    private GPUSkinAnimationData animData = null;

    [HideInInspector]
    [SerializeField]
    private Mesh mesh = null;

    [HideInInspector]
    [SerializeField]
    private Material material = null;

    [HideInInspector]
    [SerializeField]
    private GPUSkinCullingMode cullingMode = GPUSkinCullingMode.CullUpdateTransforms;

    [HideInInspector]
    [SerializeField]
    private bool rootMotionEnabled = false;

    [System.NonSerialized]
    private Texture2D texture = null;

    [System.NonSerialized]
    private MeshRenderer meshRender = null;

    [System.NonSerialized]
    private MeshFilter meshFilter = null;

    [System.NonSerialized]
    private GPUSkinClip playingClip = null;

    [System.NonSerialized]
    private GPUSkinClip lastPlayingClip = null;

    [System.NonSerialized]
    private GPUSkinClip lastPlayedClip = null;

    [System.NonSerialized]
    private int lastPlayingFrameIndex = -1;

    [System.NonSerialized]
    private bool isPlaying = false;

    [System.NonSerialized]
    private float time = 0;

    [System.NonSerialized]
    private float crossFadeTime = -1;

    [System.NonSerialized]
    private float crossFadeProgress = 0;

    [System.NonSerialized]
    private float lastPlayedTime = 0;

    [System.NonSerialized]
    private MaterialPropertyBlock mpb = null;

    [System.NonSerialized]
    private bool visible = false;

    [System.NonSerialized]
    private int rootMotionFrameIndex = -1;

    [System.NonSerialized]
    private Vector3 rootMotionDeltaPosition = Vector3.zero;

    [System.NonSerialized]
    private Quaternion rootMotionDeltaQuat = Quaternion.identity;

    private static int shaderPropID_GPUSkin_TextureMatrix = -1;
    private static int shaderPropID_GPUSkin_TextureSize_NumPixelsPerFrame = 0;
    private static int shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation = 0;
    private static int shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade = 0;
    private static int shaderPorpID_GPUSkin_RootMotion = 0;

    private static GPUSkinAnimationManager animationMgr = new GPUSkinAnimationManager();

    public bool Visible
    {
        get
        {
            return Application.isPlaying ? visible : true;
        }
        set
        {
            visible = value;
        }
    }

    public GPUSkinCullingMode CullingMode
    {
        get
        {
            return Application.isPlaying ? cullingMode : GPUSkinCullingMode.AlwaysAnimate;
        }
        set
        {
            cullingMode = value;
        }
    }

    public bool RootMotionEnabled
    {
        get
        {
            return Application.isPlaying ? rootMotionEnabled : false;
        }
        set
        {
            rootMotionFrameIndex = -1;
            rootMotionEnabled = value;
        }
    }

    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
    }

    public GPUSkinWrapMode WrapMode
    {
        get
        {
            return playingClip == null ? GPUSkinWrapMode.Once : playingClip.wrapMode;
        }
    }

    public bool IsTimeAtTheEndOfLoop
    {
        get
        {
            if (playingClip == null)
            {
                return false;
            }
            else
            {
                return GetFrameIndex() == ((int)(playingClip.length * playingClip.frameRate) - 1);
            }
        }
    }

    public GPUSkinAnimationData AnimationData
    {
        get
        {
            return animData;
        }
    }

    public Texture2D Texture
    {
        get
        {
            return texture;
        }
        set
        {
            texture = value;
        }
    }

    public void Play(string clipName)
    {
        if (animData == null)
        {
            return;
        }

        List<GPUSkinClip> clips = animData.clips;
        int numClips = clips == null ? 0 : clips.Count;
        for (int i = 0; i < numClips; ++i)
        {
            GPUSkinClip clip = clips[i];
            if (clip.name == clipName)
            {
                if (playingClip != clip ||
                    (playingClip != null && playingClip.wrapMode == GPUSkinWrapMode.Once && IsTimeAtTheEndOfLoop) ||
                    (playingClip != null && !isPlaying))
                {
                    SetNewPlayingClip(clip);
                    crossFadeTime = 0;
                }
                return;
            }
        }
    }

    public void CrossFade(string clipName, float fadeLength)
    {
        if (playingClip == null)
        {
            Play(clipName);
        }
        else
        {
            List<GPUSkinClip> clips = animData.clips;
            int numClips = clips == null ? 0 : clips.Count;
            for (int i = 0; i < numClips; ++i)
            {
                GPUSkinClip clip = clips[i];
                if (clip.name == clipName)
                {
                    if (playingClip != clip)
                    {
                        crossFadeProgress = 0;
                        crossFadeTime = fadeLength;
                        SetNewPlayingClip(clip);
                        return;
                    }
                    if ((playingClip.wrapMode == GPUSkinWrapMode.Once && IsTimeAtTheEndOfLoop) || !isPlaying)
                    {
                        SetNewPlayingClip(clip);
                        return;
                    }
                }
            }
        }
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Resume()
    {
        if (playingClip != null)
        {
            isPlaying = true;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Start()
    {
        if (shaderPropID_GPUSkin_TextureMatrix == -1)
        {
            shaderPropID_GPUSkin_TextureMatrix = Shader.PropertyToID("_GPUSkin_TextureMatrix");
            shaderPropID_GPUSkin_TextureSize_NumPixelsPerFrame = Shader.PropertyToID("_GPUSkin_TextureSize_NumPixelsPerFrame");
            shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation = Shader.PropertyToID("_GPUSkin_FrameIndex_PixelSegmentation");
            shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade = Shader.PropertyToID("_GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade");
            shaderPorpID_GPUSkin_RootMotion = Shader.PropertyToID("_GPUSkin_RootMotion");
        }

        StartInit();
    }

    private void OnDestroy()
    {
        animationMgr.Unregister(this);
    }

    private void StartInit()
    {
        if (animData == null)
        {
            return;
        }
        if (mesh == null)
        {
            return;
        }
        if (material == null)
        {
            return;
        }

        var go = this.gameObject;

        meshRender = go.GetComponent<MeshRenderer>();
        if (meshRender == null)
        {
            meshRender = go.AddComponent<MeshRenderer>();
        }
        if (meshRender.sharedMaterial != material)
        {
            meshRender.sharedMaterial = material;
        }

        meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = go.AddComponent<MeshFilter>();
        }
        if (meshFilter.sharedMesh != mesh)
        {
            meshFilter.sharedMesh = mesh;
        }

        animationMgr.Register(this);

        mpb = new MaterialPropertyBlock();

        SetMaterialProp();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            OnDeviceLost();
        }
    }

    private void OnDeviceLost()
    {
        if (texture == null)
        {
            return;
        }

        if (animationMgr.IsDeviceLost(this, Time.time))
        {
            texture.SetPixels(animationMgr.GetColors(this), 0);
            texture.Apply(false);

            SetMaterialProp();
        }
    }

    private void SetMaterialProp()
    {
        meshRender.sharedMaterial.SetTexture(shaderPropID_GPUSkin_TextureMatrix, texture);
        meshRender.sharedMaterial.SetVector(shaderPropID_GPUSkin_TextureSize_NumPixelsPerFrame,
            new Vector4(animData.textureWidth, animData.textureHeight, animData.bones.Count * 3/*treat 3 pixels as a float3x4*/, 0));
    }

    private float GetCurrentTime()
    {
        return time;
    }

    private int GetTheLastFrameIndex(GPUSkinClip clip)
    {
        return (int)(clip.length * clip.frameRate) - 1;
    }

    private int GetFrameIndex(GPUSkinClip clip, float time)
    {
        return (int)(time * clip.frameRate) % (int)(clip.length * clip.frameRate);
    }

    private int GetFrameIndex()
    {
        float time = GetCurrentTime();
        if (playingClip.length == time)
        {
            return GetTheLastFrameIndex(playingClip);
        }
        else
        {
            return GetFrameIndex(playingClip, time);
        }
    }

    private int GetCrossFadeFrameIndex()
    {
        if (lastPlayedClip == null)
        {
            return 0;
        }

        if (lastPlayedClip.wrapMode == GPUSkinWrapMode.Once)
        {
            if (lastPlayedTime >= lastPlayedClip.length)
            {
                return GetTheLastFrameIndex(lastPlayedClip);
            }
            else
            {
                return GetFrameIndex(lastPlayedClip, lastPlayedTime);
            }
        }
        else
        {
            return GetFrameIndex(lastPlayedClip, lastPlayedTime);
        }
    }

    public bool IsCrossFadeBlending(GPUSkinClip lastPlayedClip, float crossFadeTime, float crossFadeProgress)
    {
        return lastPlayedClip != null && crossFadeTime > 0 && crossFadeProgress <= crossFadeTime;
    }

    private void SetNewPlayingClip(GPUSkinClip clip)
    {
        lastPlayedClip = playingClip;
        lastPlayedTime = GetCurrentTime();

        isPlaying = true;
        playingClip = clip;
        time = 0;
        rootMotionFrameIndex = -1;

        rootMotionDeltaPosition = Vector3.zero;
        rootMotionDeltaQuat = Quaternion.identity;
    }

    
    private void DoRootMotion(float deltaTime)
    {
        int frameIndex = GetFrameIndex();
        GPUSkinFrame frame = playingClip.frames[frameIndex];
        if (frame == null)
        {
            return;
        }

        Transform trans = transform;

        if (WrapMode == GPUSkinWrapMode.Once)
        {
            if (rootMotionFrameIndex != frameIndex)
            {
                Vector3 oldPos = rootMotionDeltaPosition;
                rootMotionDeltaPosition = frame.rootMotionDeltaPositionQ * trans.forward * frame.rootMotionDeltaPositionL;
                trans.Translate(rootMotionDeltaPosition - oldPos, Space.World);

                rootMotionFrameIndex = frameIndex;
            }
        }
        else
        {
            int totalFrameCount = Mathf.Max((int)(deltaTime * playingClip.frameRate), frameIndex - rootMotionFrameIndex);
            int lastFrameIndex = GetTheLastFrameIndex(playingClip);

            while (totalFrameCount > 0)
            {
                if (rootMotionFrameIndex + totalFrameCount >= lastFrameIndex)
                {
                    var lastFrame = playingClip.frames[lastFrameIndex];

                    Vector3 oldPos = rootMotionDeltaPosition;
                    rootMotionDeltaPosition = lastFrame.rootMotionDeltaPositionQ * trans.forward * lastFrame.rootMotionDeltaPositionL;
                    trans.Translate(rootMotionDeltaPosition - oldPos, Space.World);

                    totalFrameCount -= (lastFrameIndex - rootMotionFrameIndex);
                    rootMotionFrameIndex = 0;
                    rootMotionDeltaPosition = Vector3.zero;
                }
                else
                {
                    Vector3 oldPos = rootMotionDeltaPosition;
                    rootMotionDeltaPosition = frame.rootMotionDeltaPositionQ * trans.forward * frame.rootMotionDeltaPositionL;
                    trans.Translate(rootMotionDeltaPosition - oldPos, Space.World);

                    totalFrameCount = 0;
                }
            }

            rootMotionFrameIndex = frameIndex;
        }

        //if (doRotate)
        //{
        //    transform.rotation *= frame.rootMotionDeltaRotation;
        //}
    }

    private void UpdateMaterial(float deltaTime)
    {
        int frameIndex = GetFrameIndex();
        if (lastPlayingClip == playingClip && lastPlayingFrameIndex == frameIndex)
        {
            return;
        }

        lastPlayingClip = playingClip;
        lastPlayingFrameIndex = frameIndex;

        bool isCrossBlending        = IsCrossFadeBlending(lastPlayedClip, crossFadeTime, crossFadeProgress);
        int frameIndexCrossFade     = -1;
        GPUSkinFrame frameCrossFade = null;
        float crossFadeBlendFactor  = 1;
        bool isRootMotion           = playingClip.rootMotionEnabled && RootMotionEnabled;
        GPUSkinFrame frame          = playingClip.frames[frameIndex];

        if (isCrossBlending)
        {
            frameIndexCrossFade = GetCrossFadeFrameIndex();
            frameCrossFade = lastPlayedClip.frames[frameIndexCrossFade];
            crossFadeBlendFactor = Mathf.Clamp01(crossFadeProgress / crossFadeTime);
        }
        
        if (Visible || CullingMode == GPUSkinCullingMode.AlwaysAnimate)
        {
            mpb.SetVector(shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation, new Vector4(frameIndex, playingClip.pixelSegmentation, 0, 0));
            if (isRootMotion)
            {
                Matrix4x4 rootMotionInv = animData.rootTransformMatrix * frame.RootMotionInv(animData.rootBoneIndex);
                //Matrix4x4 rootMotionInv = frame.RootMotionInv(animData.rootBoneIndex);
                mpb.SetMatrix(shaderPorpID_GPUSkin_RootMotion, rootMotionInv);
            }
            else
            {
                mpb.SetMatrix(shaderPorpID_GPUSkin_RootMotion, Matrix4x4.identity);
            }

            if (isCrossBlending)
            {
                mpb.SetVector(shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade, new Vector4(frameIndexCrossFade, lastPlayedClip.pixelSegmentation, crossFadeBlendFactor));
            }
            else
            {
                mpb.SetVector(shaderPorpID_GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade, new Vector4(0, 0, 1));
            }

            meshRender.SetPropertyBlock(mpb);
        }

        if (isRootMotion && deltaTime > 0)
        {
            if (CullingMode != GPUSkinCullingMode.CullCompletely)
            {
                DoRootMotion(deltaTime);
            }
        }
    }

    private void UpdateInternal(float deltaTime)
    {
        if (!isPlaying || playingClip == null)
        {
            return;
        }

        if (playingClip.wrapMode == GPUSkinWrapMode.Loop)
        {
            time += deltaTime;
            UpdateMaterial(deltaTime);
        }
        else if (playingClip.wrapMode == GPUSkinWrapMode.Once)
        {
            if (time >= playingClip.length)
            {
                time = playingClip.length;
                UpdateMaterial(deltaTime);
            }
            else
            {
                UpdateMaterial(deltaTime);
                time = Mathf.Clamp(time + deltaTime, 0, playingClip.length);
            }
        }

        crossFadeProgress += deltaTime;
        lastPlayedTime += deltaTime;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            OnDeviceLost();
        }
#endif

        UpdateInternal(Time.deltaTime);
    }
}
