using Parry.PlayerClass;
using Unity.Cinemachine;
using UnityEngine;

namespace Parry.BossClass
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class BossEntrance : MonoBehaviour
    {
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private BossSpawner spawner;
        [SerializeField] private CinemachineCamera bossCamera;
        private bool entered;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (entered)
                return;

            PlayerCharacter player = other.GetComponentInParent<PlayerCharacter>();
            if (player == null || player.IsDead)
                return;

            entered = true;
            player.GetComponent<PlayerMovement>().Teleport(playerSpawn.position);

            // 줌·추적·경계는 보스 카메라의 Inspector에서 설정한다.
            // 높은 Priority의 보스 카메라를 켜면 Brain이 Cut 방식으로 즉시 전환한다.
            bossCamera.gameObject.SetActive(true);

            spawner.Spawn();
        }
    }
}
