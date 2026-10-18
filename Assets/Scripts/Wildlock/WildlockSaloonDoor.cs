using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.2: Triggering a Scripted Event (Opening Saloon Swinging Doors / Unlocking Sheriff Armory Vault).
    /// </summary>
    public class WildlockSaloonDoor : MonoBehaviour, IWildlockInteractable
    {
        public string doorTitle = "Saloon Batwing Doors";
        public Transform leftDoorWing;
        public Transform rightDoorWing;
        public Light[] interiorLights;
        public int requiredGemsToUnlock = 0;
        public bool isOpen;

        private Quaternion _leftClosedRot;
        private Quaternion _rightClosedRot;

        private void Start()
        {
            if (leftDoorWing != null) _leftClosedRot = leftDoorWing.localRotation;
            if (rightDoorWing != null) _rightClosedRot = rightDoorWing.localRotation;
            ApplyInteriorLights();
        }

        private void Update()
        {
            float angle = isOpen ? 95f : 0f;
            if (leftDoorWing != null)
            {
                Quaternion targetL = _leftClosedRot * Quaternion.Euler(0f, -angle, 0f);
                leftDoorWing.localRotation = Quaternion.Slerp(leftDoorWing.localRotation, targetL, Time.deltaTime * 6.5f);
            }
            if (rightDoorWing != null)
            {
                Quaternion targetR = _rightClosedRot * Quaternion.Euler(0f, angle, 0f);
                rightDoorWing.localRotation = Quaternion.Slerp(rightDoorWing.localRotation, targetR, Time.deltaTime * 6.5f);
            }
        }

        public string GetInteractionPrompt()
        {
            if (requiredGemsToUnlock > 0 && WildlockGameManager.Instance != null && WildlockGameManager.Instance.playerGems < requiredGemsToUnlock && !isOpen)
            {
                return $"[Locked] {doorTitle} — Requires {requiredGemsToUnlock} Gems (Have {WildlockGameManager.Instance.playerGems})";
            }
            return isOpen ? $"[E] Close {doorTitle}" : $"[E] Swing Open {doorTitle} (Scripted Event)";
        }

        public void Interact(WildlockPlayerController player)
        {
            if (requiredGemsToUnlock > 0 && WildlockGameManager.Instance != null && WildlockGameManager.Instance.playerGems < requiredGemsToUnlock && !isOpen)
            {
                WildlockGameManager.Instance.PostEventBanner($"Need {requiredGemsToUnlock} Red Gems to unlock {doorTitle}!");
                return;
            }
            SetDoorOpen(!isOpen, true);
        }

        public void SetDoorOpen(bool open, bool notifyManager)
        {
            isOpen = open;
            WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.DoorSwing, 0.85f);
            WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.8f, 0.3f), Color.white, 18, 3.8f, 0.2f, 0.55f);
            ApplyInteriorLights();

            if (notifyManager && WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordScriptedEvent(
                    isOpen ? $"Scripted Event: Opened {doorTitle} & Ignited Saloon Chandeliers!" : $"Closed {doorTitle}");
            }
        }

        private void ApplyInteriorLights()
        {
            if (interiorLights == null) return;
            foreach (Light l in interiorLights)
            {
                if (l != null)
                {
                    l.intensity = isOpen ? 9.5f : 2.2f;
                }
            }
        }

        public Transform GetTransform() => transform;
    }
}
