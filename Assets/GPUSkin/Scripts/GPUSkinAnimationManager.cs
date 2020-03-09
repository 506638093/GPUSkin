using System;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinAnimationManager
{
    public class GPUSkinAnimationItem
    {
        public List<GPUSkinAnimation> animations = new List<GPUSkinAnimation>();
        public Texture2D texture = null;
    }

    private Dictionary<GPUSkinAnimationData, GPUSkinAnimationItem> allAnimations = new Dictionary<GPUSkinAnimationData, GPUSkinAnimationItem>();

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
            item.texture = GPUSkinUtil.CreateTexture2D(anim.AnimationData);
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
