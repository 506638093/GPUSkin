using System;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinAnimationManager
{
    public class GPUSkinAnimationItem
    {
        public List<GPUSkinAnimation> animations = new List<GPUSkinAnimation>();
        public Texture2D texture = null;
        public Color[] pixels = null;
        public float time = 0;
    }

    private Dictionary<GPUSkinAnimationData, GPUSkinAnimationItem> allAnimations = new Dictionary<GPUSkinAnimationData, GPUSkinAnimationItem>();

    public bool IsDeviceLost(GPUSkinAnimation anim, float time)
    {
        if (allAnimations.TryGetValue(anim.AnimationData, out GPUSkinAnimationItem item))
        {
            if (item.time != time)
            {
                item.time = time;
                return true;
            }
        }
        return false;
    }

    public Color[] GetColors(GPUSkinAnimation anim)
    {
        if (allAnimations.TryGetValue(anim.AnimationData, out GPUSkinAnimationItem item))
        {
            return item.pixels;
        }
        return null;
    }

    public void Register(GPUSkinAnimation anim)
    { 
        if (anim == null)
        {
            return;
        }

        GPUSkinAnimationItem item = null;
        if(!allAnimations.TryGetValue(anim.AnimationData, out item))
        {
            item = new GPUSkinAnimationItem();
            allAnimations.Add(anim.AnimationData, item);
        }
        item.animations.Add(anim);

        if (item.texture == null)
        {
            item.texture = GPUSkinUtil.CreateTexture2D(anim.AnimationData, out item.pixels);
        }

        anim.Texture = item.texture;
    }

    public void Unregister(GPUSkinAnimation anim)
    {
        if (anim == null)
        {
            return;
        }

        GPUSkinAnimationItem item = null;
        if (!allAnimations.TryGetValue(anim.AnimationData, out item))
        {
            return;
        }

        anim.Texture = null;
        item.animations.Remove(anim);

        if(item.animations.Count == 0)
        {
            UnityEngine.Object.DestroyImmediate(item.texture);
            item.texture = null;

            allAnimations.Remove(anim.AnimationData);
        }
        
    }
}
