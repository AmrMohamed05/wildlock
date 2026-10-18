using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Trigger volume placed at the Gold Mine entrance and Saloon interior to demonstrate
    /// environment-triggered lighting changes (Section 3.3).
    /// </summary>
    public class WildlockLightZoneTrigger : MonoBehaviour
    {
        public string zoneDisplayName = "Gold Mine Cavern";
        public Light[] zoneSpecificLights;

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                if (WildlockLightingManager.Instance != null)
                {
                    WildlockLightingManager.Instance.SetDarkZoneState(true, zoneDisplayName);
                }
                SetZoneLightsBoost(true);
                if (WildlockGameManager.Instance != null)
                {
                    WildlockGameManager.Instance.PostEventBanner($"Entered {zoneDisplayName}: Ambient Light Darkened & Local Torches Boosted!");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                if (WildlockLightingManager.Instance != null)
                {
                    WildlockLightingManager.Instance.SetDarkZoneState(false, "");
                }
                SetZoneLightsBoost(false);
            }
        }

        private void SetZoneLightsBoost(bool boosted)
        {
            if (zoneSpecificLights == null) return;
            foreach (Light l in zoneSpecificLights)
            {
                if (l != null)
                {
                    l.range = boosted ? 18f : 11f;
                }
            }
        }

        private static bool IsPlayerCollider(Collider col)
        {
            if (col == null) return false;
            if (col.CompareTag("Player")) return true;
            if (col.GetComponentInParent<WildlockPlayerController>() != null) return true;
            return false;
        }
    }
}
