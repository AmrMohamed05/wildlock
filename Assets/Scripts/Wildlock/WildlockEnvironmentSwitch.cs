using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.3: Changing an Environmental State (Flipping the Town Generator Lever Switch
    /// raises the Central Sniper Platform & Tactical Iron Cover Barriers, and powers on Town Floodlights).
    /// </summary>
    public class WildlockEnvironmentSwitch : MonoBehaviour, IWildlockInteractable
    {
        public string switchName = "Arena Generator & Defense Lever";
        public Transform leverArm;
        public Renderer indicatorOrbRenderer;
        public Transform[] risingPlatformsAndBarriers;
        public float raisedHeightOffset = 2.4f;
        public bool isActivated;

        private Vector3[] _basePositions;

        private void Start()
        {
            if (risingPlatformsAndBarriers != null)
            {
                _basePositions = new Vector3[risingPlatformsAndBarriers.Length];
                for (int i = 0; i < risingPlatformsAndBarriers.Length; i++)
                {
                    if (risingPlatformsAndBarriers[i] != null)
                    {
                        _basePositions[i] = risingPlatformsAndBarriers[i].position;
                    }
                }
            }
            UpdateSwitchVisuals();
        }

        private void Update()
        {
            if (risingPlatformsAndBarriers == null || _basePositions == null) return;

            for (int i = 0; i < risingPlatformsAndBarriers.Length; i++)
            {
                Transform t = risingPlatformsAndBarriers[i];
                if (t == null) continue;

                // First element is the Sniper Platform (rises 2.6m), others are Iron Cover Walls (rise 1.65m)
                float h = (i == 0) ? raisedHeightOffset : 1.65f;
                Vector3 targetPos = _basePositions[i] + (isActivated ? new Vector3(0f, h, 0f) : Vector3.zero);
                t.position = Vector3.MoveTowards(t.position, targetPos, Time.deltaTime * 3.2f);
            }

            if (leverArm != null)
            {
                Quaternion targetRot = Quaternion.Euler(isActivated ? -42f : 42f, 0f, 0f);
                leverArm.localRotation = Quaternion.Slerp(leverArm.localRotation, targetRot, Time.deltaTime * 8f);
            }
        }

        public string GetInteractionPrompt()
        {
            return isActivated
                ? $"[E / Shoot] {switchName}: Lower Barriers & Turn Off Floodlights"
                : $"[E / Shoot] {switchName}: Raise Sniper Platform, Cover Walls & Floodlights";
        }

        public void Interact(WildlockPlayerController player)
        {
            ToggleSwitch();
        }

        public void ToggleSwitch()
        {
            isActivated = !isActivated;
            WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.SwitchFlip, 0.9f);
            WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up * 1.2f, Color.cyan, Color.yellow, 26, 5f, 0.22f, 0.65f);

            UpdateSwitchVisuals();

            if (WildlockLightingManager.Instance != null)
            {
                WildlockLightingManager.Instance.SetGeneratorFloodlights(isActivated);
            }

            if (WildlockCameraDirector.Instance != null && risingPlatformsAndBarriers != null && risingPlatformsAndBarriers.Length > 0)
            {
                WildlockCameraDirector.Instance.TriggerEventCutscene(risingPlatformsAndBarriers[0], 2.2f);
            }

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordEnvironmentalStateChange(
                    isActivated
                        ? "Generator Activated: Raised Central Sniper Platform, Tactical Cover Barriers & Arena Floodlights!"
                        : "Generator Deactivated: Lowered Sniper Platform & Turned Off Floodlights!");
            }
        }

        private void UpdateSwitchVisuals()
        {
            if (indicatorOrbRenderer != null && indicatorOrbRenderer.material != null)
            {
                Color c = isActivated ? new Color(0.15f, 1f, 0.35f) : new Color(1f, 0.18f, 0.15f);
                indicatorOrbRenderer.material.color = c;
                if (indicatorOrbRenderer.material.HasProperty("_EmissionColor"))
                {
                    indicatorOrbRenderer.material.EnableKeyword("_EMISSION");
                    indicatorOrbRenderer.material.SetColor("_EmissionColor", c * 2.5f);
                }
            }
        }

        public Transform GetTransform() => transform;
    }
}
