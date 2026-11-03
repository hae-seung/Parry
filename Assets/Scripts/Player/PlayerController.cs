using UnityEngine;

namespace Parry.PlayerClass
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerController : MonoBehaviour
    {
        protected PlayerCharacter Character { get; private set; }

        protected void Awake()
        {
            Character = GetComponent<PlayerCharacter>();
        }
    }
}
