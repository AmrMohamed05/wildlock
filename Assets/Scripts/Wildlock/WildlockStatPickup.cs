using System.Collections;
using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.5: Collectible Health Elixir (+250 HP) or Revolver Ammo Crate (+Full Ammo & Cooldowns).
    /// </summary>
    public class WildlockStatPickup : MonoBehaviour, IWildlockInteractable
    {
        public enum PickupKind
        {
            HealthElixir,
            AmmoAndEnergyCrate
        }

        public PickupKind kind = PickupKind.HealthElixir;
        public int amount = 250;
        public float respawnTime = 12f;

        private Vector3 _startPos;
        private bool _isAvailable = true;
        private Renderer[] _renderers;
        private Collider _collider;
        private Light _glowLight;

        private void Start()
        {
            _startPos = transform.position;
            _renderers = GetComponentsInChildren<Renderer>();
            _collider = GetComponent<Collider>();
            _glowLight = GetComponentInChildren<Light>();
        }

        private void Update()
        {
            if (!_isAvailable) return;
            transform.Rotate(0f, 65f * Time.deltaTime, 0f, Space.World);
            transform.position = _startPos + new Vector3(0f, Mathf.Sin(Time.time * 2.5f) * 0.15f, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isAvailable) return;
            WildlockPlayerController player = other.GetComponentInParent<WildlockPlayerController>();
            if (player != null)
            {
                Consume(player);
            }
        }

        public string GetInteractionPrompt()
        {
            if (!_isAvailable) return "Pickup Respawning...";
            return kind == PickupKind.HealthElixir
                ? $"[Walk Over / E] Drink Frontier Health Elixir (+{amount} HP)"
                : "[Walk Over / E] Grab Six-Shooter Ammo & Ability Charge";
        }

        public void Interact(WildlockPlayerController player)
        {
            if (_isAvailable && player != null)
            {
                Consume(player);
            }
        }

        private void Consume(WildlockPlayerController player)
        {
            _isAvailable = false;
            WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.HealChime, 0.8f);

            if (kind == PickupKind.HealthElixir)
            {
                WildlockAudioSynth.SpawnParticleBurst(transform.position, new Color(0.15f, 1f, 0.35f), Color.white, 24, 4f, 0.22f, 0.65f);
                if (WildlockGameManager.Instance != null)
                {
                    WildlockGameManager.Instance.ModifyPlayerHealth(amount, "Used Frontier Health Elixir (+250 HP)");
                }
            }
            else
            {
                WildlockAudioSynth.SpawnParticleBurst(transform.position, new Color(1f, 0.78f, 0.15f), Color.white, 24, 4f, 0.22f, 0.65f);
                player.RefillAmmoAndCooldowns();
                if (WildlockGameManager.Instance != null)
                {
                    WildlockGameManager.Instance.RecordStatModification("Restored Full Revolver Ammo (6/6) & Ability Cooldowns!");
                }
            }

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            SetVisible(false);
            yield return new WaitForSeconds(respawnTime);
            _isAvailable = true;
            SetVisible(true);
        }

        private void SetVisible(bool vis)
        {
            if (_collider != null) _collider.enabled = vis;
            if (_glowLight != null) _glowLight.enabled = vis;
            if (_renderers != null)
            {
                foreach (Renderer r in _renderers)
                {
                    if (r != null) r.enabled = vis;
                }
            }
        }

        public Transform GetTransform() => transform;
    }
}
