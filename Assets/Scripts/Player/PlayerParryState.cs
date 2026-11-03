using UnityEngine;

namespace Parry.PlayerClass
{
    public sealed class PlayerParryState : StateMachineBehaviour
    {
        private PlayerCharacter character;

        public override void OnStateEnter(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (character == null)
                animator.TryGetComponent(out character);
        }

        public override void OnStateExit(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (character != null)
                character.EndParry();
        }
    }
}
