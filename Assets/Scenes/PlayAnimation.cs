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

        if (animator != null)
        {
            animator.Play("walk");
        }
        else
        {
            animation.Play("walk");
        }
    }

    // Update is called once per frame
    float ttime = 0;
    void Update()
    {
        ttime += Time.deltaTime;
        if (ttime > 1f)
        {
            var rd = UnityEngine.Random.RandomRange(0.0f, 1.0f);
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
            ttime = 0;
        }
    }
}
