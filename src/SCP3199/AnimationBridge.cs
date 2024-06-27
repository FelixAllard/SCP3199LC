using UnityEngine;

namespace SCP3199.SCP3199;

public class AnimationBridge : MonoBehaviour
{
    public SCP3199AI mainScript;
    public void FootPrintsAnimationHandle()
    {
        
    }

    public void ThrowUpAnimationHandle()
    {
        
    }

    public void FinishThrowingEggAnimationHandle()
    {
        
    }

    public void MakeEggVisibleAnimationHandle()
    {
        mainScript.MakeEggMouthVisible(true);
    }
    public void MakeEggInvisibleAnimationHandle()
    {
        mainScript.MakeEggMouthVisible(false);
    }
}