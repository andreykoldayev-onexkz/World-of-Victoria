using UnityEngine;

namespace WorldOfVictoria.Rendering.Character
{
    [DisallowMultipleComponent]
    public sealed class ZombieContactDebug : MonoBehaviour
    {
        [SerializeField] private float hitCooldown = 1f;

        private float lastHitTime = -999f;

        public void NotifyPlayerHit()
        {
            if (Time.time - lastHitTime < hitCooldown)
            {
                return;
            }

            lastHitTime = Time.time;
            Debug.Log("Player hit by zombie!", this);
        }
    }
}
