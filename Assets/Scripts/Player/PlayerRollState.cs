using UnityEngine;

namespace Parry.PlayerClass
{
    public sealed class PlayerRollState : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (animator.TryGetComponent<PlayerCharacter>(out var character))
                character.EndRoll();
        }
    }
}
