using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace SCP3199.SCP3199;

class SCP3199AI : ModEnemyAI<SCP3199AI>
{
    // We use this list to destroy loaded game objects when plugin is reloaded
    internal static List<GameObject> SCP682Objects = [];
    
    

    public enum Speed
    {
        Walking = 4,
        Running = 6
    }

    public void SetAgentSpeed(Speed speed)
    {
        agent.speed = (int)speed;

        bool newIsRunning = speed == Speed.Running;
        if (creatureAnimator.GetBool(Anim.isRunning) != newIsRunning)
            creatureAnimator.SetBool(Anim.isRunning, newIsRunning);
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
        SCP682Objects.Add(gameObject);
        InitialState = new WanderState();
        if (enemyType.isOutsideEnemy)
        {
            var scale = 4f;
            gameObject.transform.Find("CrocodileModel").localScale = new(scale, scale, scale);
        }
        // agent.radius = 0.5f;
        base.Start();
    }
    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);
        if (ActiveState is AttackPlayerState state)
            state.AttackCollideWithPlayer(other);
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

        if (ActiveState is not AttackPlayerState)
            OverrideState(new AttackPlayerState());
    }
    

    private class WanderState : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new LayingEgg().ArrivedAtShipTransition(), new SawPlayer()];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isWalking, true);
            self.SetDestinationToPosition(RoundManager.Instance.outsideAINodes[
                RandomNumberGenerator.GetInt32(RoundManager.Instance.outsideAINodes.Length)].transform.position);
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isWalking, false);
        }
       
    }

    private class LayingEgg : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [];
        //TODO Transition must be done through animation toward walking state

        public override void OnStateEntered(Animator creatureAnimator)
        {

        }

        public override void OnStateExit(Animator creatureAnimator)
        {

        }
        //DONE
        internal class ArrivedAtDestination : AIStateTransition
        {
            public override bool CanTransitionBeTaken()
            {
                //IfIsClose enough to destination
                if (Vector3.Distance(self.agent.destination, self.transform.position)<1f)
                    return true;
                return false;
            }

            public override AIBehaviorState NextState()
            {
                if (self.isOutside)
                    return new WanderState();
                else
                    return new LayingEgg();
            }
        }
    }

    private class DetectedPlayer : AIBehaviorState
    {
        // Note: We add one more transition to this afterwards!
        public override List<AIStateTransition> Transitions { get; set; } =
            [new SawPlayer()];

        public override void OnStateEntered(Animator creatureAnimator)
        {

        }

        public override void AIInterval(Animator creatureAnimator)
        {

        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            
        }

        public class CheckIfPlayerVisible : AIStateTransition
        {

            public override bool CanTransitionBeTaken()
            {
                if (self.CheckLineOfSightForPlayer())
                {
                    
                }
            }

            public override AIBehaviorState NextState()
            {
                
            }
        }
    }
    

    private class AtFacilityWanderingState : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [
                new BoredOfFacilityTransition(),
                new AtFacilityEatNoisyJesterState.FindNoisyJesterTransition(),
                new SawPlayer()
            ];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isMoving, true);

            self.StartSearch(self.transform.position);
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            self.StopSearch(self.currentSearch);
        }

        private class BoredOfFacilityTransition : AIStateTransition
        {
            float debugMSGTimer = defaultBoredOfWanderingFacilityTimer;

            public override bool CanTransitionBeTaken()
            {
                self.boredOfWanderingFacilityTimer -= Time.deltaTime;
                if (debugMSGTimer - self.boredOfWanderingFacilityTimer > 1)
                {
                    debugMSGTimer = self.boredOfWanderingFacilityTimer;
                    self.LogDebug(
                        $"[{nameof(BoredOfFacilityTransition)}] Time until bored: {self.boredOfWanderingFacilityTimer}"
                    );
                }
                if (self.boredOfWanderingFacilityTimer <= 0)
                {
                    return true;
                }
                else
                    return false;
            }

            public override AIBehaviorState NextState()
            {
                self.boredOfWanderingFacilityTimer = defaultBoredOfWanderingFacilityTimer;
                self.currentTravelDirection = TravelingTo.Ship;
                return new WanderThroughEntranceState();
            }
        }
    }
    

    private class AtFacilityEatNoisyJesterState : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new NoisyJesterEatenTransition()];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isMoving, true);
            self.SetAgentSpeed(Speed.Running);
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            if (self.targetJester is null)
                return;

            var jesterPos = self.targetJester.agent.transform.position;

            if (Vector3.Distance(jesterPos, self.transform.position) < 3)
                self.targetJester.KillEnemy(true);

            self.SetDestinationToPosition(jesterPos);
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            self.SetAgentSpeed(Speed.Walking);
        }

        internal class FindNoisyJesterTransition : AIStateTransition
        {
            public override bool CanTransitionBeTaken()
            {
                if (self.isOutside)
                    return false;

                for (int i = 0; i < JesterListHook.jesterEnemies.Count; i++)
                {
                    var jester = JesterListHook.jesterEnemies[i];
                    if (jester is null)
                    {
                        JesterListHook.jesterEnemies.RemoveAt(i);
                        i--;
                        continue;
                    }
                    if (jester.farAudio.isPlaying)
                    {
                        self.targetJester = jester;
                        JesterListHook.jesterEnemies.RemoveAt(i);
                        return true;
                    }
                }
                return false;
            }

            public override AIBehaviorState NextState() => new AtFacilityEatNoisyJesterState();
        }

        private class NoisyJesterEatenTransition : AIStateTransition
        {
            public override bool CanTransitionBeTaken() => self.targetJester is null;

            public override AIBehaviorState NextState() => new AtFacilityWanderingState();
        }
    }


    private class InvestigatePlayerState : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new AttackPlayerTransition(), new LostPlayerTransition()];

        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isMoving, true);
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            self.targetPlayer = self.FindNearestPlayer();
            self.SetDestinationToPosition(self.targetPlayer.transform.position);
        }

        public override void OnStateExit(Animator creatureAnimator) { }
    }

    private class AttackPlayerState : AIBehaviorState
    {
        public override List<AIStateTransition> Transitions { get; set; } =
            [new LostPlayerTransition()];

        const float defaultCooldown = 0.5f;
        float attackCooldown = defaultCooldown;

        public override void OnStateEntered(Animator creatureAnimator)
        {
            creatureAnimator.SetBool(Anim.isMoving, true);
            self.SetAgentSpeed(Speed.Running);
        }

        public override void UpdateBehavior(Animator creatureAnimator)
        {
            attackCooldown -= Time.deltaTime;
        }

        public override void AIInterval(Animator creatureAnimator)
        {
            self.targetPlayer = self.FindNearestPlayer();
            self.SetDestinationToPosition(self.targetPlayer.transform.position);
        }

        public override void OnStateExit(Animator creatureAnimator)
        {
            self.SetAgentSpeed(Speed.Walking);
        }

        internal void AttackCollideWithPlayer(Collider other)
        {
            if (attackCooldown > 0)
                return;

            PlayerControllerB? player = self.MeetsStandardPlayerCollisionConditions(other);
            if (player is not null)
            {
                int damageToDeal;
                if (player.health > 45) // At min do 15 damage
                    damageToDeal = player.health + 30; // Set health to 30
                else
                    damageToDeal = 15;
                player.DamagePlayer(damageToDeal);
                self.creatureAnimator.SetTrigger(Anim.doBite);

                attackCooldown = defaultCooldown;
            }
        }
    }
    

    private class AttackPlayerTransition : AIStateTransition
    {

        public override bool CanTransitionBeTaken()
        {
            aggressionTimer -= Time.deltaTime;
            if (aggressionTimer <= 0)
                return true;
            return false;
        }
        public override AIBehaviorState NextState() => new AttackPlayerState();
    }
    public void SetLocationState(bool toOutside)
    {
        LogDebug("Set location state to OUTSIDE?: " + toOutside);
        if (toOutside)
        {
            // Maybe using MyValidState was a mistake?
            // Just results in writing more code without really advantages.
            MyValidState = PlayerState.Outside;
            SetEnemyOutside(true);
        }
        else
        {
            MyValidState = PlayerState.Inside;
            SetEnemyOutside(false);
        }
    }

    [ClientRpc]
    public void TeleportSelfToOtherEntranceClientRpc(bool wasInside)
    {
        TeleportSelfToOtherEntrance(!wasInside);
    }

    private void TeleportSelfToOtherEntrance(bool isOutside)
    {
        var targetEntrance = RoundManager.FindMainEntranceScript(!isOutside);

        Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(
            targetEntrance.entrancePoint.position
        );
        if (IsOwner)
        {
            agent.enabled = false;
            transform.position = navMeshPosition;
            agent.enabled = true;
        }
        else
            transform.position = navMeshPosition;

        serverPosition = navMeshPosition;
        SetLocationState(!isOutside);

        PlayEntranceOpeningSound(targetEntrance);
    }

    public void PlayEntranceOpeningSound(EntranceTeleport entrance)
    {
        if (entrance.doorAudios == null || entrance.doorAudios.Length == 0)
            return;
        entrance.entrancePointAudio.PlayOneShot(entrance.doorAudios[0]);
        WalkieTalkie.TransmitOneShotAudio(entrance.entrancePointAudio, entrance.doorAudios[0]);
    }

    #endregion
    #region Debug Stuff
#if DEBUG
    public static IEnumerator DrawPath(LineRenderer line, NavMeshAgent agent)
    {
        if (!agent.enabled)
            yield break;
        yield return new WaitForEndOfFrame();
        line.SetPosition(0, agent.transform.position); //set the line's origin

        line.positionCount = agent.path.corners.Length; //set the array of positions to the amount of corners
        for (var i = 1; i < agent.path.corners.Length; i++)
        {
            line.SetPosition(i, agent.path.corners[i]); //go through each corner and set that to the line renderer's position
        }
    }



    class DebugNewSearchRoutineAction(SCP682AI self) : MMButtonAction("New Search Routine")
    {
        protected override void OnClick()
        {
            self.StopSearch(self.currentSearch);
            self.StartSearch(self.transform.position);
        }
    }

    class DebugOverrideState(SCP682AI self, Type state) : MMButtonAction($"To {state.Name}")
    {
        protected override void OnClick()
        {
            var stateInstance = (AIBehaviorState)Activator.CreateInstance(state);
            self.OverrideState(stateInstance);
        }
    }

#endif
    #endregion
}