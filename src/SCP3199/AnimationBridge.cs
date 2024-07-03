using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace SCP3199.SCP3199;

public class AnimationBridge : MonoBehaviour
{
    public SCP3199AI mainScript;
    public void FootPrintsAnimationHandle()
    {
        mainScript.self.creatureSFX.PlayOneShot(mainScript.self.footStepSound[UnityEngine.Random.RandomRangeInt(0,2)]);
    }

    public void ThrowUpAnimationHandle()
    {
        var allEnemiesList = new List<SpawnableEnemyWithRarity>();
        allEnemiesList.AddRange(RoundManager.Instance.currentLevel.Enemies);
        allEnemiesList.AddRange(RoundManager.Instance.currentLevel.OutsideEnemies);
        var enemyToSpawn = allEnemiesList.Find(x => x.enemyType.enemyName.Equals("scp3199"));
        RoundManager.Instance.SpawnEnemyGameObject(
            mainScript.self.mouthEggTransform.position,
            0f,
            RoundManager.Instance.currentLevel.OutsideEnemies.IndexOf(enemyToSpawn),
            enemyToSpawn.enemyType
        );
    }

    public void FinishThrowingEggAnimationHandle()
    {
        mainScript.switchOffLayingEgg = true;
    }

    public void MakeEggVisibleAnimationHandle()
    {
        mainScript.MakeEggMouthVisible(true);
    }
    public void MakeEggInvisibleAnimationHandle()
    {
        mainScript.MakeEggMouthVisible(false);
    }

    public void AttackAnimationHandle()
    {
        mainScript.self.SwingAttackHitClientRpc();
    }

    public void EndAttackAnimationHandle()
    {
        mainScript.canAttack = true;
    }
    /// <summary>
    /// I guess I should rename it to smt else, but this is for when he starts attack sequence
    /// </summary>
    public void FallDownDeathAnimationHandle()
    {
        mainScript.self.creatureVoice.PlayOneShot(mainScript.self.attackPreSound);
    }
}