using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAnimation : MonoBehaviour
{
    Animator animator;
    GPUSkinAnimation animation;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        animation = GetComponent<GPUSkinAnimation>();
    }

    // Update is called once per frame
    float ttime = 0;
    void Update()
    {
        ttime += Time.deltaTime;
        if (ttime > 1f)
        {
            var rd = UnityEngine.Random.RandomRange(0.0f, 1.0f);
            if (rd < 0.15f)
            {
                if (animator != null)
                {
                    animator.Play("walk");
                }
                else
                {
                    animation.Play("walk");
                }
                
            }
            else if (rd < 0.3f)
            {
                if (animator != null)
                {
                    animator.Play("dodge");
                }
                else
                {
                    animation.Play("dodge");
                }
            }
            else if (rd < 0.45f)
            {
                if (animator != null)
                {
                    animator.Play("fly");
                }
                else
                {
                    animation.Play("fly");
                }
            }
            else if (rd < 0.6f)
            {
                if (animator != null)
                {
                    animator.CrossFade("walk", UnityEngine.Random.RandomRange(0.0f, 1.0f));
                }
                else
                {
                    animation.CrossFade("walk", UnityEngine.Random.RandomRange(0.0f, 1.0f));
                }
            }
            else if (rd < 0.75f)
            {
                if (animator != null)
                {
                    animator.CrossFade("dodge", UnityEngine.Random.RandomRange(0.0f, 1.0f));
                }
                else
                {
                    animation.CrossFade("dodge", UnityEngine.Random.RandomRange(0.0f, 1.0f));
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.CrossFade("fly", UnityEngine.Random.RandomRange(0.0f, 1.0f));
                }
                else
                {
                    animation.CrossFade("fly", UnityEngine.Random.RandomRange(0.0f, 1.0f));
                }
            }
            ttime = 0;
        }
    }
}
