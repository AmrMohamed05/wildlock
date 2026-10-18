using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.1: Destructible Wooden Crate or Explosive Red Barrel.
    /// Can be shot with Left-Click, hit with Dynamite (G), or smashed with E.
    /// Drops a Collectible Red Gem on destruction; Explosive Barrels detonate in an AoE blast!
    /// </summary>
    public class WildlockDestructibleProp : MonoBehaviour, IWildlockInteractable
    {
        public string propDisplayName = "Wooden Supply Crate";
        public float maxHealth = 200f;
        public float currentHealth = 200f;
        public bool isExplosiveBarrel;
        public float explosionRadius = 6.5f;
        public float explosionDamage = 280f;
        public bool dropsGemOnBreak = true;

        private bool _destroyed;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public string GetInteractionPrompt()
        {
            return isExplosiveBarrel
                ? $"[Left-Click Shoot / E Kick] Detonate {propDisplayName} ({currentHealth:F0} HP)"
                : $"[Left-Click Shoot / E Smash] Break {propDisplayName} ({currentHealth:F0} HP)";
        }

        public void Interact(WildlockPlayerController player)
        {
            TakeDamage(maxHealth, transform.position + Vector3.up * 0.5f);
        }

        public void TakeDamage(float amount, Vector3 hitPoint)
        {
            if (_destroyed) return;
            currentHealth -= amount;

            WildlockAudioSynth.SpawnParticleBurst(
                hitPoint,
                isExplosiveBarrel ? new Color(1f, 0.45f, 0.1f) : new Color(0.68f, 0.48f, 0.24f),
                new Color(0.35f, 0.25f, 0.12f),
                12, 3.5f, 0.16f, 0.45f);

            if (currentHealth <= 0f)
            {
                Shatter();
            }
        }

        private void Shatter()
        {
            if (_destroyed) return;
            _destroyed = true;

            Vector3 center = transform.position + Vector3.up * 0.6f;

            if (isExplosiveBarrel)
            {
                WildlockAudioSynth.PlaySoundAt(center, WildlockAudioSynth.SoundType.Explosion, 0.95f);
                WildlockAudioSynth.SpawnParticleBurst(center, new Color(1f, 0.35f, 0.05f), new Color(1f, 0.85f, 0.15f), 48, 8.5f, 0.42f, 0.85f);

                // Damage nearby crates & NPCs
                Collider[] hits = Physics.OverlapSphere(center, explosionRadius);
                foreach (Collider col in hits)
                {
                    if (col == null || col.gameObject == gameObject) continue;

                    WildlockDestructibleProp otherProp = col.GetComponentInParent<WildlockDestructibleProp>();
                    if (otherProp != null && otherProp != this)
                    {
                        otherProp.TakeDamage(explosionDamage, col.transform.position);
                    }

                    WildlockNpcAgent npc = col.GetComponentInParent<WildlockNpcAgent>();
                    if (npc != null && npc.isEnemyOutlaw)
                    {
                        npc.TakeDamage(explosionDamage, center);
                    }
                }
            }
            else
            {
                WildlockAudioSynth.PlaySoundAt(center, WildlockAudioSynth.SoundType.CrateBreak, 0.85f);
                WildlockAudioSynth.SpawnParticleBurst(center, new Color(0.72f, 0.52f, 0.25f), new Color(0.45f, 0.30f, 0.15f), 30, 5.5f, 0.25f, 0.7f);
            }

            // Spawn flying wooden/metal debris chunks
            SpawnDebrisChunks(center);

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordObjectInteraction(
                    isExplosiveBarrel
                        ? $"Detonated {propDisplayName} in fiery blast!"
                        : $"Destroyed {propDisplayName} & revealed hidden loot!");

                if (dropsGemOnBreak)
                {
                    WildlockGameManager.Instance.SpawnCollectibleGemAt(center + Vector3.up * 0.25f);
                }
            }

            gameObject.SetActive(false);
        }

        private void SpawnDebrisChunks(Vector3 center)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Color chunkColor = isExplosiveBarrel ? new Color(0.65f, 0.14f, 0.12f) : new Color(0.58f, 0.40f, 0.20f);

            for (int i = 0; i < 6; i++)
            {
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.name = "DebrisChunk";
                chunk.transform.position = center + Random.insideUnitSphere * 0.4f;
                chunk.transform.localScale = new Vector3(0.28f, 0.14f, 0.42f);
                chunk.transform.rotation = Random.rotation;

                Renderer r = chunk.GetComponent<Renderer>();
                if (r != null && urpLit != null)
                {
                    Material m = new Material(urpLit);
                    m.color = chunkColor;
                    r.material = m;
                }

                Rigidbody rb = chunk.AddComponent<Rigidbody>();
                rb.mass = 0.4f;
                rb.AddExplosionForce(220f, center - Vector3.up * 0.3f, 3f);
                Destroy(chunk, 3.2f);
            }
        }

        public Transform GetTransform() => transform;
    }
}
