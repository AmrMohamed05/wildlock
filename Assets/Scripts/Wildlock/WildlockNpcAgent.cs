using System.Collections;
using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Action 1.4: Affecting an NPC's Behavior or State Through an Action.
    /// Drives both Enemy Outlaw Bots (Guard & Lu from Adam Character Pack) and
    /// Friendly Duo Partner (Adam from Adam Character Pack).
    /// </summary>
    public class WildlockNpcAgent : MonoBehaviour, IWildlockInteractable
    {
        public enum NpcState
        {
            Patrol,
            AlertChase,
            FollowPlayer,
            DefendMineCart,
            Stunned,
            Eliminated
        }

        public string npcDisplayName = "Outlaw Guard (Adam Pack)";
        public bool isEnemyOutlaw = true;
        public float maxHealth = 600f;
        public float currentHealth = 600f;
        public float moveSpeed = 3.2f;
        public NpcState currentState = NpcState.Patrol;
        public Vector3[] patrolWaypoints;

        private int _waypointIndex;
        private Animator _animator;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _stateTimer;
        private float _attackCooldown;
        private TextMesh _overheadLabel;

        // Animator parameter hashes matching StarterAssetsThirdPerson.controller
        private int _animSpeedHash;
        private int _animMotionSpeedHash;
        private int _animGroundedHash;

        private void Start()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            currentHealth = maxHealth;

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && _animator.GetComponent<WildlockAnimationEventReceiver>() == null)
            {
                _animator.gameObject.AddComponent<WildlockAnimationEventReceiver>();
            }
            _animSpeedHash = Animator.StringToHash("Speed");
            _animMotionSpeedHash = Animator.StringToHash("MotionSpeed");
            _animGroundedHash = Animator.StringToHash("Grounded");

            CreateOverheadStatusLabel();
            UpdateOverheadText();
        }

        private void CreateOverheadStatusLabel()
        {
            GameObject labelGo = new GameObject("NPC_StatusLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.25f, 0f);

            _overheadLabel = labelGo.AddComponent<TextMesh>();
            _overheadLabel.characterSize = 0.085f;
            _overheadLabel.fontSize = 36;
            _overheadLabel.anchor = TextAnchor.MiddleCenter;
            _overheadLabel.alignment = TextAlignment.Center;
            _overheadLabel.color = isEnemyOutlaw ? new Color(1f, 0.35f, 0.3f) : new Color(0.3f, 0.95f, 1f);
        }

        private void Update()
        {
            if (_overheadLabel != null && Camera.main != null)
            {
                _overheadLabel.transform.rotation = Quaternion.LookRotation(_overheadLabel.transform.position - Camera.main.transform.position);
            }

            if (currentState == NpcState.Eliminated)
            {
                UpdateAnimatorSpeed(0f);
                return;
            }

            if (currentState == NpcState.Stunned)
            {
                _stateTimer -= Time.deltaTime;
                UpdateAnimatorSpeed(0f);
                if (_stateTimer <= 0f)
                {
                    currentState = isEnemyOutlaw ? NpcState.AlertChase : NpcState.FollowPlayer;
                    UpdateOverheadText();
                }
                return;
            }

            WildlockPlayerController player = WildlockPlayerController.Instance;
            Vector3 targetPos = transform.position;
            bool shouldMove = false;
            float desiredSpeed = moveSpeed;

            if (isEnemyOutlaw)
            {
                float distToPlayer = player != null ? Vector3.Distance(transform.position, player.transform.position) : 999f;

                // Automatically alert if player gets within 14m or if 10-gem countdown is active
                bool showdownActive = WildlockGameManager.Instance != null && WildlockGameManager.Instance.isCountdownActive;
                if ((distToPlayer < 13.5f || showdownActive) && currentState == NpcState.Patrol)
                {
                    currentState = NpcState.AlertChase;
                    WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.NpcAlert, 0.7f);
                    UpdateOverheadText();
                }
                else if (distToPlayer > 22f && !showdownActive && currentState == NpcState.AlertChase)
                {
                    currentState = NpcState.Patrol;
                    UpdateOverheadText();
                }

                if (currentState == NpcState.AlertChase && player != null)
                {
                    if (distToPlayer > 5.5f)
                    {
                        targetPos = player.transform.position;
                        shouldMove = true;
                        desiredSpeed = moveSpeed * 1.3f;
                    }
                    else
                    {
                        // Face player and fire warning shots
                        FacePosition(player.transform.position);
                        TryFireAtTarget(player.transform.position + Vector3.up * 1.1f);
                    }
                }
                else if (patrolWaypoints != null && patrolWaypoints.Length > 0)
                {
                    targetPos = patrolWaypoints[_waypointIndex];
                    if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(targetPos.x, 0f, targetPos.z)) < 1.0f)
                    {
                        _waypointIndex = (_waypointIndex + 1) % patrolWaypoints.Length;
                        targetPos = patrolWaypoints[_waypointIndex];
                    }
                    shouldMove = true;
                }
            }
            else
            {
                // Friendly Duo Partner (Adam)
                if (currentState == NpcState.FollowPlayer && player != null)
                {
                    float dist = Vector3.Distance(transform.position, player.transform.position);
                    if (dist > 3.6f)
                    {
                        targetPos = player.transform.position;
                        shouldMove = true;
                        desiredSpeed = dist > 7f ? moveSpeed * 1.45f : moveSpeed;
                    }
                }
                else if (currentState == NpcState.DefendMineCart)
                {
                    Vector3 mineCenter = new Vector3(-131.5f, 0f, 3.5f);
                    if (Vector3.Distance(transform.position, mineCenter) > 2.0f)
                    {
                        targetPos = mineCenter;
                        shouldMove = true;
                    }
                }

                // Friendly Duo Partner shoots at nearby enemy outlaws
                WildlockNpcAgent nearestEnemy = FindNearestActiveEnemy(18f);
                if (nearestEnemy != null)
                {
                    if (!shouldMove) FacePosition(nearestEnemy.transform.position);
                    TryFireAtTarget(nearestEnemy.transform.position + Vector3.up * 1.1f, nearestEnemy);
                }
            }

            if (shouldMove)
            {
                Vector3 flatTarget = new Vector3(targetPos.x, transform.position.y, targetPos.z);
                Vector3 dir = (flatTarget - transform.position);
                if (dir.sqrMagnitude > 0.04f)
                {
                    FacePosition(flatTarget);
                    transform.position = Vector3.MoveTowards(transform.position, flatTarget, desiredSpeed * Time.deltaTime);
                    UpdateAnimatorSpeed(desiredSpeed);
                }
                else
                {
                    UpdateAnimatorSpeed(0f);
                }
            }
            else
            {
                UpdateAnimatorSpeed(0f);
            }
        }

        private void FacePosition(Vector3 worldPos)
        {
            Vector3 lookDir = worldPos - transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }
        }

        private void TryFireAtTarget(Vector3 targetPoint, WildlockNpcAgent enemyTarget = null)
        {
            _attackCooldown -= Time.deltaTime;
            if (_attackCooldown > 0f) return;

            _attackCooldown = isEnemyOutlaw ? 2.1f : 1.6f;
            Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.5f;

            WildlockAudioSynth.PlaySoundAt(muzzle, WildlockAudioSynth.SoundType.RevolverShot, 0.35f);
            WildlockAudioSynth.SpawnParticleBurst(muzzle, new Color(1f, 0.75f, 0.2f), Color.red, 8, 2.5f, 0.14f, 0.2f);

            if (isEnemyOutlaw && WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.ModifyPlayerHealth(-45, $"{npcDisplayName} grazed you (-45 HP)!");
            }
            else if (!isEnemyOutlaw && enemyTarget != null)
            {
                enemyTarget.TakeDamage(85f, enemyTarget.transform.position + Vector3.up * 1.2f);
            }
        }

        private WildlockNpcAgent FindNearestActiveEnemy(float maxRange)
        {
            WildlockNpcAgent[] all = FindObjectsByType<WildlockNpcAgent>(FindObjectsSortMode.None);
            WildlockNpcAgent best = null;
            float bestDist = maxRange;

            foreach (WildlockNpcAgent a in all)
            {
                if (a == null || !a.isEnemyOutlaw || a.currentState == NpcState.Eliminated) continue;
                float d = Vector3.Distance(transform.position, a.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = a;
                }
            }
            return best;
        }

        private void UpdateAnimatorSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat(_animSpeedHash, speed);
            _animator.SetFloat(_animMotionSpeedHash, 1f);
            _animator.SetBool(_animGroundedHash, true);
        }

        public string GetInteractionPrompt()
        {
            if (isEnemyOutlaw)
            {
                return $"[Left-Click Shoot / E Stun] {npcDisplayName} ({currentHealth:F0}/{maxHealth:F0} HP | State: {currentState})";
            }
            return $"[E] Give Tactical Order to Duo Partner {npcDisplayName} (Current: {currentState})";
        }

        public void Interact(WildlockPlayerController player)
        {
            if (isEnemyOutlaw)
            {
                // E performs a close-range Sheriff Pistol-Whip / Stun on Enemy Outlaw
                StunOutlaw(3.5f);
                TakeDamage(150f, transform.position + Vector3.up * 1.2f);
            }
            else
            {
                // Cycle Duo Partner tactical behavior state
                if (currentState == NpcState.FollowPlayer)
                    currentState = NpcState.DefendMineCart;
                else if (currentState == NpcState.DefendMineCart)
                    currentState = NpcState.Patrol;
                else
                    currentState = NpcState.FollowPlayer;

                WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.NpcAlert, 0.75f);
                WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up * 1.2f, Color.cyan, Color.white, 20, 4f, 0.2f, 0.55f);
                UpdateOverheadText();

                if (WildlockGameManager.Instance != null)
                {
                    WildlockGameManager.Instance.RecordNpcStateChange($"Commanded Duo Partner {npcDisplayName} -> State: {currentState}!");
                }
            }
        }

        public void StunOutlaw(float duration)
        {
            if (currentState == NpcState.Eliminated) return;
            currentState = NpcState.Stunned;
            _stateTimer = duration;
            UpdateOverheadText();

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordNpcStateChange($"Stunned {npcDisplayName} for {duration:F1}s!");
            }
        }

        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (currentState == NpcState.Eliminated) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            WildlockAudioSynth.SpawnParticleBurst(hitPoint, Color.red, new Color(1f, 0.6f, 0.1f), 16, 4f, 0.18f, 0.4f);

            if (isEnemyOutlaw && currentState == NpcState.Patrol)
            {
                currentState = NpcState.AlertChase;
            }

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordNpcStateChange($"Hit {npcDisplayName} (-{damage:F0} HP | State: {currentState})");
            }

            if (currentHealth <= 0f)
            {
                StartCoroutine(EliminatedAndRespawnRoutine());
            }
            else
            {
                UpdateOverheadText();
            }
        }

        private IEnumerator EliminatedAndRespawnRoutine()
        {
            currentState = NpcState.Eliminated;
            UpdateOverheadText();

            WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.Explosion, 0.75f);
            WildlockAudioSynth.SpawnParticleBurst(transform.position + Vector3.up, Color.red, Color.yellow, 32, 6f, 0.28f, 0.75f);

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordNpcStateChange($"Eliminated {npcDisplayName}! Outlaw dropped 2 Red Gems!");
                WildlockGameManager.Instance.SpawnCollectibleGemAt(transform.position + new Vector3(0.4f, 0.6f, 0f));
                WildlockGameManager.Instance.SpawnCollectibleGemAt(transform.position + new Vector3(-0.4f, 0.6f, 0.4f));
            }

            // Lay down while eliminated
            transform.rotation = Quaternion.Euler(-85f, transform.eulerAngles.y, 0f);
            yield return new WaitForSeconds(7.5f);

            // Respawn at original patrol start
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            currentHealth = maxHealth;
            currentState = isEnemyOutlaw ? NpcState.Patrol : NpcState.FollowPlayer;
            UpdateOverheadText();
        }

        private void UpdateOverheadText()
        {
            if (_overheadLabel == null) return;
            if (isEnemyOutlaw)
            {
                _overheadLabel.text = $"{npcDisplayName}\n[{currentState}] HP: {currentHealth:F0}/{maxHealth:F0}";
            }
            else
            {
                _overheadLabel.text = $"DUO PARTNER: {npcDisplayName}\n[Order: {currentState}] (Press E)";
            }
        }

        public Transform GetTransform() => transform;
    }
}
