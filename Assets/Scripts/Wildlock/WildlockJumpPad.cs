using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.2 & Movement Mechanic: Steam-Powered Balcony Jump Pad that launches the player
    /// onto elevated Saloon and General Store rooftops.
    /// </summary>
    public class WildlockJumpPad : MonoBehaviour
    {
        public string padName = "Saloon Balcony Steam Jump Pad";
        public float launchHeight = 7.2f;
        private float _lastLaunchTime;

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - _lastLaunchTime < 0.6f) return;

            WildlockPlayerController player = other.GetComponentInParent<WildlockPlayerController>();
            if (player != null)
            {
                _lastLaunchTime = Time.time;
                player.LaunchUpward(launchHeight);
                WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.JumpPad, 0.9f);
                WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up * 0.3f, new Color(0.3f, 0.95f, 1f), Color.white, 32, 6f, 0.28f, 0.65f);

                if (WildlockGameManager.Instance != null)
                {
                    WildlockGameManager.Instance.RecordScriptedEvent($"Triggered {padName}: Launched to High Ground!");
                }
            }
        }
    }
}
