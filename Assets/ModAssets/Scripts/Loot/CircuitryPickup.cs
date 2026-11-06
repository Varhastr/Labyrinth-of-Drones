using UnityEngine;

namespace LabyrinthOfDrones
{
    public class CircuitryPickup : MonoBehaviour
    {
        public int amount = 1;
        public float lifeTime = 60f;

        void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") ||
                other.GetComponentInParent<Unity.FPS.Gameplay.PlayerCharacterController>() != null)
            {
                Inventory.AddCircuitry(amount);
                // TODO: play pickup sound / VFX here
                Destroy(gameObject);
            }
        }
    }
}
