﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameNetcodeStuff;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SCP3199.SCP3199;

public partial class  SCP3199AI : ModEnemyAI
{
    // We use this list to destroy loaded game objects when plugin is reloaded
    internal static List<GameObject> SCP682Objects = [];
    internal bool finishedEggPhase = false;
    internal float stageOfGrowth = 0f;
    internal bool canAttack = true;
    [SerializeField]
    internal MeshRenderer eggRendererMouth;
    [SerializeField]
    internal GameObject mainEgg;
    [SerializeField]
    internal Transform mouthEggTransform;
    [SerializeField]
    internal MeshRenderer mainEggRenderer;

    [SerializeField] 
    internal bool switchOffLayingEgg = false;

    [SerializeField] 
    internal Transform AttackArea;

    internal Coroutine AttackCoroutine;
    
    
    [SerializeField] 
    internal ParticleSystem spawningParticles;
    // 0-1 : child : Doesn't lay egg and won't attack
    // 1-2 : Teenager : Lay egg but won't attack
    // 2++ : Adult : Will attack and Lay egg
    [Header("Audio")]
    [SerializeField] 
    internal AudioClip _hatchingSound;
    [SerializeField] 
    internal AudioClip huntScream;
    [SerializeField] 
    internal AudioClip gettingHit;

    [SerializeField] 
    internal AudioClip attackPreSound;
    [SerializeField] 
    internal AudioClip layingEgg;
    [SerializeField] 
    internal AudioClip[] footStepSound;
    [SerializeField] 
    internal AudioClip[] growlSound;
    
    
    

    public enum Speed
    {
        Walking = 4,
        Running = 6
    }
    
    static class Anim
    {
        // do: trigger
        // is: boolean
        internal const string doKillEnemy = "KillEnemy"; // base game thing, gets called automatically
        internal const string isWalking = "isWalking";
        internal const string isRunning = "isRunning";
        internal const string doHurtEnemy = "doHurtEnemy";
        internal const string doLayEgg = "doLayEgg";
        internal const string doAttack = "doAttack";
    }

    public override void Start()
    {
        self = this;
        InitialState = new InEgg();
        self.finishedEggPhase = false;
        MakeEggMouthVisible(false);
        SCP682Objects.Add(gameObject);
        // agent.radius = 0.5f;
        base.Start();

    }

    [ClientRpc]
    public void ExitEggPhaseClientRpc()
    {
        self.stageOfGrowth = 1;
        self.spawningParticles.Play();
        self.finishedEggPhase = true;
        self.mainEggRenderer.enabled = false;
    }
    public override void HitEnemy(
        int force = 1,
        PlayerControllerB? playerWhoHit = null,
        bool playHitSFX = false,
        int hitID = -1
    )
    {
        if (self.stageOfGrowth < 1)
        {
            return;
        }
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if (isEnemyDead)
            return;

        enemyHP -= force;
        if (enemyHP <= 0 && !isEnemyDead)
        {
            // Our death sound will be played through creatureVoice when KillEnemy() is called.
            // KillEnemy() will also attempt to call creatureAnimator.SetTrigger("KillEnemy"),
            // so we don't need to call a death animation ourselves.

            // We need to stop our search coroutine, because the game does not do that by default.
            KillEnemyOnOwnerClient();
            /*if(searchCoroutine!=null)
            StopCoroutine(searchCoroutine);*/
        }
        //if it gets attackes and does not see the player!
        if (ActiveState is not Chase)
            OverrideState(new Chase());
    }
    

    private class InEgg : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new FinishedEgg()];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            self.mainEgg.GetComponent<MeshRenderer>().enabled = false;
            self.spawningParticles.Play();
        }
        internal class FinishedEgg : AIStateTransition
        {
            public override bool CanTransitionBeTaken()
            {
                //IfIsClose enough to destination
                if(self.finishedEggPhase)
                    return true;
                return false;
            }

            public override AIBehaviorState NextState()
            {
                
                return new WanderState();
            }
        }
    }
    private class WanderState : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new ArrivedAtDestination(), new SawPlayer()];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            
            creatureAnimator.SetBool(Anim.isWalking, true);

            self.SetDestinationToPosition(RoundManager.Instance.outsideAINodes[
                RandomNumberGenerator.GetInt32(RoundManager.Instance.outsideAINodes.Length)].transform.position);

            
            self.agent.autoBraking = true;
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            agent.ResetPath();
            self.agent.autoBraking = false;
            creatureAnimator.SetBool(Anim.isWalking, false);
        }
        internal class ArrivedAtDestination : AIStateTransition
        {
            public override bool CanTransitionBeTaken()
            {
                //IfIsClose enough to destination
                //Plugin.Logger.LogInfo((Vector3.Distance(self.agent.destination, self.transform.position)<1f).ToString());
                if (Vector3.Distance(self.agent.destination, self.transform.position)<1f)
                    return true;
                return false;
            }

            public override AIBehaviorState NextState()
            {
                if (UnityEngine.Random.RandomRangeInt(0,3) == 0 && self.stageOfGrowth == 2)
                {
                    return new LayingEgg();
                }
                self.OverrideState(new WanderState());
                return new WanderState();
            }
        }
        internal class SawPlayer : AIStateTransition
        {
            public override bool CanTransitionBeTaken()
            {
                //IfIsClose enough to destination
                if (self.CheckLineOfSightForPlayer())
                    return true;
                return false;
            }
            public override AIBehaviorState NextState()
            {
                return new Chase();
            }
        }
    }
    private class LayingEgg : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new FinishedLaying()];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            agent.ResetPath();
            agent.isStopped = true;
            self.PlayAnimationClientRpc(Anim.doLayEgg);
            
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            self.switchOffLayingEgg = false;
            agent.ResetPath();
            agent.isStopped = false;
        }
        internal class FinishedLaying : AIStateTransition
        {
            public override bool CanTransitionBeTaken()
            {
                //IfIsClose enough to destination
                if (self.switchOffLayingEgg)
                    return true;
                return false;
            }
            public override AIBehaviorState NextState()
            {
                return new WanderState();
            }
        }
        //DONE
    }

    private class Chase : AIBehaviorState
    {
        // Note: We add one more transition to this afterwards!
        public override List<AIStateTransition> Transitions { get; set; } =
            [new DontSeePlayer()];
        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isRunning, true);
            self.agent.ResetPath();
            self.agent.speed = 6f;
            self.agent.autoBraking = false;
            self.targetPlayer = self.CheckLineOfSightForPlayer();
            self.creatureVoice.PlayOneShot(self.growlSound[UnityEngine.Random.RandomRangeInt(0,self.growlSound.Length)]);
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            if (self.targetPlayer == null)
            {
                self.targetPlayer = self.CheckLineOfSightForPlayer();
                if (self.targetPlayer == null)
                {
                    self.OverrideState(new WanderState());
                }
                return;
            } 
            if (Vector3.Distance(self.targetPlayer.transform.position, self.transform.position) < 2)
            {
                if (self.canAttack)
                {
                    self.canAttack = false;
                    self.PlayAnimationClientRpc(Anim.doAttack);
                }
            }
            self.SetDestinationToPosition(self.targetPlayer.transform.position);
        }
        public override void OnStateExit(Animator creatureAnimator)
        {
            self.PlayAnimationClientRpc(Anim.isRunning, false);
            self.agent.speed = 4f;
        }
        public class DontSeePlayer : AIStateTransition
        {
            private int timeWithoutSeeing = 200;
            private int time = 200;

            public DontSeePlayer()
            {
                timeWithoutSeeing = 200;
                time = 200;
            }
            public override bool CanTransitionBeTaken()
            {
                if (!self.CheckLineOfSightForPlayer())
                {
                    if (timeWithoutSeeing ==0)
                    {
                        return true;
                    }
                    timeWithoutSeeing -= 1;
                }
                else
                {
                    timeWithoutSeeing = time;
                }

                return false;
            }

            public override AIBehaviorState NextState()
            {
                return new WanderState();
            }
        }
    }
    /// <summary>
    /// Called by animation and resolve damage on player.
    /// </summary>
    [ClientRpc]
    public void SwingAttackHitClientRpc() {
        int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
        Collider[] hitColliders = Physics.OverlapBox(self.AttackArea.position, AttackArea.localScale, Quaternion.identity, playerLayer);
        if(hitColliders.Length > 0){
            foreach (var player in hitColliders){
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                if (playerControllerB != null)
                {
                    self.AttackCoroutine = StartCoroutine(DamagePlayerCoroutine(playerControllerB));
                }
            }
        }
    }
    /// <summary>
    /// Called by attack in VitalCalls
    /// </summary>
    /// <param name="playerControllerB">The player which will loose hp</param>
    /// <returns></returns>
    IEnumerator DamagePlayerCoroutine(PlayerControllerB playerControllerB)
    {
        yield return new WaitForSeconds(0.1f);
        playerControllerB.DamagePlayer(self.stageOfGrowth==2?40:20);
        StopCoroutine(self.AttackCoroutine);
    }
    [ClientRpc]
    public void PlayAnimationClientRpc(string animationName, bool value)
    {
        creatureAnimator.SetBool(animationName,value);
    }
    [ClientRpc]
    public void PlayAnimationClientRpc(string animationName)
    {
        creatureAnimator.SetTrigger(animationName);
    }

}