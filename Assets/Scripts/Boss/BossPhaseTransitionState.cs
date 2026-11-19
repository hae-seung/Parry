using Parry.AnimationClass;
using Parry.MonsterClass;
using UnityEngine;

namespace Parry.BossClass
{
    public sealed class BossPhaseTransitionState : IMonsterState
    {
        private readonly BossBrain brain;
        private readonly AnimationClipPlayer clipPlayer;
        private bool dialogueStarted;
        private bool isWakingUp;

        public BossPhaseTransitionState(BossBrain brain)
        {
            this.brain = brain;
            clipPlayer = brain.GetComponent<AnimationClipPlayer>();
        }

        public void Enter()
        {
            brain.Attack.Stop();
            brain.Movement.Stop();
            dialogueStarted = false;
            isWakingUp = false;
            
            clipPlayer.Play(brain.Data.Flinch.faintClip, true);
        }

        public void Tick()
        {
            if (isWakingUp)
            {
                if (clipPlayer.IsPlayingClip(brain.Data.Flinch.wakeUpClip))
                    return;

                isWakingUp = false;
                brain.PhaseTransitionComplete();
                
                return;
            }

            if (dialogueStarted || !clipPlayer.HasReachedEnd)
                return;

            dialogueStarted = true;
            brain.StartDialogue();
        }

        public void StartWakeUp()
        {
            isWakingUp = true;
            dialogueStarted = false;
            
            clipPlayer.Play(brain.Data.Flinch.wakeUpClip);
        }

        public void Exit() => clipPlayer.Stop();

    }
}
