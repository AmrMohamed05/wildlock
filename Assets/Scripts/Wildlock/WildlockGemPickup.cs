using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.1 & 1.5: Collectible Red Gem that rotates, floats, updates Player HUD Gem Inventory,
    /// unlocks the Armory Gate at 5 gems, and triggers the 30-second Showdown Countdown at 10 gems.
    /// </summary>
    public class WildlockGemPickup : MonoBehaviour, IWildlockInteractable
    {
        public int gemValue = 1;
        public bool isCentralPermanentCart;
        public float rotationSpeed = 75f;
        public float bobAmplitude = 0.22f;
        public float bobFrequency = 2.4f;

        private Vector3 _basePosition;
        private float _extractCooldown;
        private bool _collected;

        private void Start()
        {
            _basePosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
            if (!isCentralPermanentCart)
            {
                float yOffset = Mathf.Sin(Time.time * bobFrequency + GetInstanceID()) * bobAmplitude;
                transform.position = _basePosition + new Vector3(0f, yOffset, 0f);
            }

            if (_extractCooldown > 0f)
            {
                _extractCooldown -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCentralPermanentCart || _collected) return;

            WildlockPlayerController player = other.GetComponentInParent<WildlockPlayerController>();
            if (player != null)
            {
                CollectGem(player);
            }
        }

        public string GetInteractionPrompt()
        {
            if (isCentralPermanentCart)
            {
                return _extractCooldown <= 0f
                    ? "[E] Mine Ruby Vein from Central Cart (+1 Gem)"
                    : $"Central Mine Cart Recharging ({_extractCooldown:F1}s)";
            }
            return $"[Walk Over or Press E] Collect Red Ruby (+{gemValue} Gem)";
        }

        public void Interact(WildlockPlayerController player)
        {
            if (isCentralPermanentCart)
            {
                if (_extractCooldown <= 0f)
                {
                    _extractCooldown = 4.0f;
                    WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.GemPickup, 0.85f);
                    WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up * 1.1f, new Color(1f, 0.1f, 0.25f), new Color(1f, 0.75f, 0.2f), 28, 5f, 0.25f, 0.75f);
                    if (WildlockGameManager.Instance != null)
                    {
                        WildlockGameManager.Instance.AddPlayerGems(1, "Extracted Ruby directly from Central Mine Cart!");
                    }
                }
            }
            else
            {
                CollectGem(player);
            }
        }

        private void CollectGem(WildlockPlayerController player)
        {
            if (_collected) return;
            _collected = true;

            WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.GemPickup, 0.85f);
            WildlockAudioSynth.SpawnParticleBurst(transform.position, new Color(1f, 0.12f, 0.28f), new Color(1f, 0.85f, 0.3f), 26, 4.8f, 0.22f, 0.65f);

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.AddPlayerGems(gemValue, "Picked up Red Gem (+1 Gem Inventory)");
            }

            Destroy(gameObject);
        }

        public Transform GetTransform() => transform;
    }
}
