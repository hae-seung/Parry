using UnityEngine;



namespace Parry.PlayerClass
{
    [RequireComponent(typeof(Animator), typeof(PlayerCharacter))]
    public class PlayerAnimation : MonoBehaviour
    {
        private Animator animator;
        private PlayerCharacter character;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int IsDeadId = Animator.StringToHash("IsDead");
        private static readonly int IsGroundId = Animator.StringToHash("IsGround");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");


        private bool bIsDead;
        private bool bIsGrounded;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            character = GetComponent<PlayerCharacter>();
        }

        private void Update()
        {
            Vector2 velocity = character.Velocity;
            bIsDead = character.IsDead;
            bIsGrounded = character.IsGrounded;
            
            
            animator.SetFloat(SpeedId, Mathf.Abs(velocity.x));
            animator.SetFloat(VerticalSpeedId, velocity.y);
            animator.SetBool(IsDeadId, bIsDead);
            animator.SetBool(IsGroundId, bIsGrounded);
        }

    }
}

