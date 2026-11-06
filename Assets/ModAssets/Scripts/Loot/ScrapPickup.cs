using UnityEngine;

namespace LabyrinthOfDrones
{
    public class ScrapPickup : MonoBehaviour
    {
        public int amount = 1;
        public float lifeTime = 60f;

        void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        void OnTriggerEnter(Collider other)
        {
            // Matches common player detection patterns used in the FPS Microgame
            if (other.CompareTag("Player") ||
                other.GetComponentInParent<Unity.FPS.Gameplay.PlayerCharacterController>() != null)
            {
                Inventory.AddScrap(amount);
                // TODO: play pickup sound / VFX here
                Destroy(gameObject);
            }
        }
    }
}
