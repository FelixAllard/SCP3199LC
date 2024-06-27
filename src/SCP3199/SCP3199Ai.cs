using System;
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
    internal MeshRenderer mainEggRenderer;

    [SerializeField] 
    internal ParticleSystem spawningParticles;
    // 0-1 : child : Doesn't lay egg and won't attack
    // 1-2 : Teenager : Lay egg but won't attack
    // 2++ : Adult : Will attack and Lay egg
    

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
        MakeEggMouthVisible(false);
        self = this;
        SCP682Objects.Add(gameObject);
        InitialState = new InEgg();
        if (enemyType.isOutsideEnemy)
        {
            var scale = 4f;
            gameObject.transform.Find("CrocodileModel").localScale = new(scale, scale, scale);
        }
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
            StopCoroutine(searchCoroutine);
            KillEnemyOnOwnerClient();
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
            if (RoundManager.Instance.IsHost)
            {
                self.SetDestinationToPosition(RoundManager.Instance.outsideAINodes[
                    RandomNumberGenerator.GetInt32(RoundManager.Instance.outsideAINodes.Length)].transform.position);
            }
            
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
                Plugin.Logger.LogInfo((Vector3.Distance(self.agent.destination, self.transform.position)<1f).ToString());
                if (Vector3.Distance(self.agent.destination, self.transform.position)<1f)
                    return true;
                return false;
            }

            public override AIBehaviorState NextState()
            {
                if (RandomNumberGenerator.GetInt32(3) == 0 && self.stageOfGrowth >= 1)
                {
                    return new LayingEgg();
                }
                Plugin.Logger.LogInfo("Hello there!");
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
            [];
        //TODO Transition must be done through animation toward walking state

        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetTrigger(Anim.doLayEgg);
            
        }

        public override void OnStateExit(Animator creatureAnimator)
        {

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
            self.agent.speed = 6f;
            self.agent.autoBraking = false;
            self.SynchronisedTargetPlayer = self.CheckLineOfSightForPlayer();
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            if (Vector3.Distance(self.SynchronisedTargetPlayer.transform.position, self.transform.position) < 2)
            {
                if (self.canAttack)
                {
                    self.canAttack = false;
                    creatureAnimator.SetTrigger(Anim.doAttack);
                }
            }
            self.SetDestinationToPosition(self.SynchronisedTargetPlayer.transform.position);
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isRunning, false);
            
            self.agent.speed = 4f;
        }

        public class DontSeePlayer : AIStateTransition
        {
            private int timeWithoutSeeing = 10;
            private int time = 10;
            public override bool CanTransitionBeTaken()
            {
                if (!self.CheckLineOfSightForPlayer())
                {
                    if (timeWithoutSeeing <=0)
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
}