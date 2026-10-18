using System.Collections;
using UnityEngine;

namespace Wildlock
{
    public interface IWildlockInteractable
    {
        string GetInteractionPrompt();
        void Interact(WildlockPlayerController player);
        Transform GetTransform();
    }

    /// <summary>
    /// Autonomous Sheriff Auto-Turret (Deployed with 'Q') that targets and shoots Enemy Outlaws.
    /// Matches Assignment 1's "Deployable auto-turrets (combat AI targeting)".
    /// </summary>
    public class WildlockAutoTurret : MonoBehaviour
    {
        public Transform rotatingHead;
        public float range = 20f;
        public float fireInterval = 0.75f;
        public float damagePerShot = 110f;
        public float duration = 22f;

        private float _fireTimer;
        private LineRenderer _laserBeam;

        private void Start()
        {
            _laserBeam = gameObject.AddComponent<LineRenderer>();
            _laserBeam.startWidth = 0.04f;
            _laserBeam.endWidth = 0.02f;
            _laserBeam.material = WildlockAudioSynth.GetUrpParticleMaterial();
            _laserBeam.startColor = Color.cyan;
            _laserBeam.endColor = new Color(0.2f, 0.8f, 1f, 0f);
            _laserBeam.enabled = false;

            Destroy(gameObject, duration);
        }

        private void Update()
        {
            WildlockNpcAgent target = FindTarget();
            if (target == null)
            {
                if (rotatingHead != null) rotatingHead.Rotate(0f, 45f * Time.deltaTime, 0f);
                return;
            }

            Vector3 aimPos = target.transform.position + Vector3.up * 1.15f;
            if (rotatingHead != null)
            {
                Vector3 dir = aimPos - rotatingHead.position;
                if (dir.sqrMagnitude > 0.01f)
                {
                    rotatingHead.rotation = Quaternion.Slerp(rotatingHead.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 12f);
                }
            }

            _fireTimer -= Time.deltaTime;
            if (_fireTimer <= 0f)
            {
                _fireTimer = fireInterval;
                StartCoroutine(FireBeamRoutine(aimPos, target));
            }
        }

        private IEnumerator FireBeamRoutine(Vector3 hitPos, WildlockNpcAgent target)
        {
            Vector3 start = (rotatingHead != null ? rotatingHead.position : transform.position + Vector3.up * 1.1f);
            WildlockAudioSynth.PlaySoundAt(start, WildlockAudioSynth.SoundType.RevolverShot, 0.45f);
            target.TakeDamage(damagePerShot, hitPos);

            if (_laserBeam != null)
            {
                _laserBeam.SetPosition(0, start);
                _laserBeam.SetPosition(1, hitPos);
                _laserBeam.enabled = true;
                yield return new WaitForSeconds(0.08f);
                _laserBeam.enabled = false;
            }
        }

        private WildlockNpcAgent FindTarget()
        {
            WildlockNpcAgent[] npcs = FindObjectsByType<WildlockNpcAgent>(FindObjectsSortMode.None);
            WildlockNpcAgent closest = null;
            float best = range;
            foreach (WildlockNpcAgent n in npcs)
            {
                if (n == null || !n.isEnemyOutlaw || n.currentState == WildlockNpcAgent.NpcState.Eliminated) continue;
                float d = Vector3.Distance(transform.position, n.transform.position);
                if (d < best)
                {
                    best = d;
                    closest = n;
                }
            }
            return closest;
        }
    }

    /// <summary>
    /// Autonomous Healing Totem (Deployed with 'H') that restores Player HP inside an emerald radius.
    /// Matches Assignment 1's "Healing totems (autonomous AoE radius logic)".
    /// </summary>
    public class WildlockHealingTotem : MonoBehaviour
    {
        public float healRadius = 6.5f;
        public int healPerTick = 60;
        public float tickInterval = 1.0f;
        public float duration = 18f;

        private float _timer;

        private void Start()
        {
            Destroy(gameObject, duration);
        }

        private void Update()
        {
            transform.Rotate(0f, 50f * Time.deltaTime, 0f);
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = tickInterval;
                if (WildlockPlayerController.Instance != null &&
                    Vector3.Distance(transform.position, WildlockPlayerController.Instance.transform.position) <= healRadius)
                {
                    WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up * 0.6f, new Color(0.2f, 1f, 0.45f), Color.white, 12, 2.5f, 0.18f, 0.5f);
                    if (WildlockGameManager.Instance != null)
                    {
                        WildlockGameManager.Instance.ModifyPlayerHealth(healPerTick, "Healing Totem Aura (+60 HP)");
                    }
                }
            }
        }
    }
}
