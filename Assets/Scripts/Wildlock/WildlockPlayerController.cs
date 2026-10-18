using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wildlock
{
    /// <summary>
    /// Full Wild West Gunslinger Controller for Wildlock (Assignment 2 Part 3).
    /// Integrates locomotion, camera look, Peacemaker Revolver shooting, Dynamite throwing,
    /// Auto-Turret & Healing Totem deployment, Lantern control, and world object interaction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class WildlockPlayerController : MonoBehaviour
    {
        public static WildlockPlayerController Instance { get; private set; }

        [Header("Locomotion Settings")]
        public float walkSpeed = 4.6f;
        public float sprintSpeed = 7.8f;
        public float jumpHeight = 1.65f;
        public float gravity = -18.0f;
        public float mouseSensitivity = 0.16f;

        [Header("Camera Targets")]
        public Transform cameraFollowTarget;
        public Transform firstPersonEyeTarget;
        public Transform weaponMuzzlePoint;

        [Header("Combat & Abilities")]
        public int maxAmmo = 6;
        public int currentAmmo = 6;
        public bool isReloading;
        public float revolverDamage = 200f;
        public float fireRateCooldown = 0.28f;
        public float turretCooldown = 0f;
        public float totemCooldown = 0f;
        public float dynamiteCooldown = 0f;

        [Header("Swappable Character Skins (Press M)")]
        public GameObject[] characterSkins;
        public int activeSkinIndex;

        public IWildlockInteractable HoveredInteractable { get; private set; }

        private CharacterController _controller;
        private Animator _animator;
        private LineRenderer _bulletTracer;
        private float _yaw;
        private float _pitch;
        private float _verticalVelocity;
        private float _fireTimer;
        private float _aimFaceTimer;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        private int _animSpeedHash;
        private int _animGroundedHash;
        private int _animJumpHash;
        private int _animFreeFallHash;
        private int _animMotionSpeedHash;

        private void Awake()
        {
            Instance = this;
            _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _yaw = transform.eulerAngles.y;

            RefreshAnimatorReference();
            SetupBulletTracer();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void RefreshAnimatorReference()
        {
            Animator[] allAnimators = GetComponentsInChildren<Animator>(true);
            foreach (Animator a in allAnimators)
            {
                if (a != null && a.GetComponent<WildlockAnimationEventReceiver>() == null)
                {
                    a.gameObject.AddComponent<WildlockAnimationEventReceiver>();
                }
            }

            _animator = GetComponentInChildren<Animator>(false);
            _animSpeedHash = Animator.StringToHash("Speed");
            _animGroundedHash = Animator.StringToHash("Grounded");
            _animJumpHash = Animator.StringToHash("Jump");
            _animFreeFallHash = Animator.StringToHash("FreeFall");
            _animMotionSpeedHash = Animator.StringToHash("MotionSpeed");
        }

        private void SetupBulletTracer()
        {
            GameObject tracerGo = new GameObject("RevolverBulletTracer");
            tracerGo.transform.SetParent(transform, false);
            _bulletTracer = tracerGo.AddComponent<LineRenderer>();
            _bulletTracer.startWidth = 0.045f;
            _bulletTracer.endWidth = 0.015f;
            _bulletTracer.material = WildlockAudioSynth.GetUrpParticleMaterial();
            _bulletTracer.startColor = new Color(1f, 0.85f, 0.25f, 1f);
            _bulletTracer.endColor = new Color(1f, 0.35f, 0.1f, 0f);
            _bulletTracer.enabled = false;
        }

        private void Update()
        {
            UpdateCooldowns();
            HandleCursorLockToggle();
            HandleLookAndMovement();
            ScanForInteractable();
            HandleCombatAndAbilityInputs();
        }

        private void UpdateCooldowns()
        {
            if (_fireTimer > 0f) _fireTimer -= Time.deltaTime;
            if (_aimFaceTimer > 0f) _aimFaceTimer -= Time.deltaTime;
            if (turretCooldown > 0f) turretCooldown -= Time.deltaTime;
            if (totemCooldown > 0f) totemCooldown -= Time.deltaTime;
            if (dynamiteCooldown > 0f) dynamiteCooldown -= Time.deltaTime;
        }

        private void HandleCursorLockToggle()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
#endif
        }

        private void HandleLookAndMovement()
        {
            Vector2 moveInput = Vector2.zero;
            Vector2 lookInput = Vector2.zero;
            bool sprintPressed = false;
            bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
                if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

                sprintPressed = kb.leftShiftKey.isPressed;
                jumpPressed = kb.spaceKey.wasPressedThisFrame;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                lookInput = mouse.delta.ReadValue() * mouseSensitivity;
            }
#endif

            // Update Camera Yaw & Pitch
            _yaw += lookInput.x;
            _pitch = Mathf.Clamp(_pitch - lookInput.y, -45f, 70f);

            if (cameraFollowTarget != null)
            {
                cameraFollowTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
            if (firstPersonEyeTarget != null)
            {
                firstPersonEyeTarget.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            // Ground check & gravity
            bool isGrounded = _controller.isGrounded;
            if (isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (jumpPressed && isGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (_animator != null) _animator.SetBool(_animJumpHash, true);
            }
            else if (isGrounded && _animator != null)
            {
                _animator.SetBool(_animJumpHash, false);
            }

            _verticalVelocity += gravity * Time.deltaTime;

            // Calculate horizontal movement relative to camera yaw
            float targetSpeed = (moveInput.sqrMagnitude > 0.01f) ? (sprintPressed ? sprintSpeed : walkSpeed) : 0f;
            Vector3 moveDirection = Vector3.zero;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                float targetRotY = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + _yaw;
                moveDirection = Quaternion.Euler(0f, targetRotY, 0f) * Vector3.forward;

                // In 3rd person, rotate character toward movement (or camera yaw when aiming/FPS/firing/backpedalling)
                bool lockToCameraYaw = _aimFaceTimer > 0f || moveInput.y < -0.1f ||
                                       (WildlockCameraDirector.Instance != null &&
                                        (WildlockCameraDirector.Instance.currentMode == WildlockCameraDirector.CameraMode.FirstPerson ||
                                         WildlockCameraDirector.Instance.isAimingDownSights));

                float desiredBodyYaw = lockToCameraYaw ? _yaw : targetRotY;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, desiredBodyYaw, 0f), Time.deltaTime * 12f);
            }
            else if (_aimFaceTimer > 0f ||
                     (WildlockCameraDirector.Instance != null &&
                      (WildlockCameraDirector.Instance.currentMode == WildlockCameraDirector.CameraMode.FirstPerson ||
                       WildlockCameraDirector.Instance.isAimingDownSights)))
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, _yaw, 0f), Time.deltaTime * 14f);
            }

            Vector3 velocity = moveDirection * targetSpeed + new Vector3(0f, _verticalVelocity, 0f);
            _controller.Move(velocity * Time.deltaTime);

            // Keep player inside the playable Western Town Arena (outer Terrain is non-playable vista)
            Vector3 p = transform.position;
            float clampedX = Mathf.Clamp(p.x, -153.5f, -97.5f);
            float clampedZ = Mathf.Clamp(p.z, -38.5f, 18.0f);
            if (Mathf.Abs(clampedX - p.x) > 0.001f || Mathf.Abs(clampedZ - p.z) > 0.001f)
            {
                _controller.enabled = false;
                transform.position = new Vector3(clampedX, p.y, clampedZ);
                _controller.enabled = true;
            }

            // Respawn if somehow below floor
            if (transform.position.y < -12f)
            {
                RespawnAtBase();
            }

            // Update Animator parameters
            if (_animator != null)
            {
                _animator.SetFloat(_animSpeedHash, targetSpeed);
                _animator.SetFloat(_animMotionSpeedHash, 1f);
                _animator.SetBool(_animGroundedHash, isGrounded);
                _animator.SetBool(_animFreeFallHash, !isGrounded && _verticalVelocity < -3f);
            }
        }

        public void LaunchUpward(float targetHeight)
        {
            _verticalVelocity = Mathf.Sqrt(targetHeight * -2f * gravity);
        }

        public void RespawnAtBase()
        {
            _controller.enabled = false;
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            _verticalVelocity = 0f;
            _controller.enabled = true;
        }

        private void ScanForInteractable()
        {
            HoveredInteractable = null;
            Camera cam = Camera.main;

            // 1) First check camera aim raycast up to 6.5m
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 6.5f))
                {
                    IWildlockInteractable interactable = hit.collider.GetComponentInParent<IWildlockInteractable>();
                    if (interactable != null)
                    {
                        HoveredInteractable = interactable;
                        return;
                    }
                }
            }

            // 2) Fallback: check closest interactable within 4.2m of player
            Collider[] nearby = Physics.OverlapSphere(transform.position + Vector3.up * 0.9f, 4.2f);
            float bestDist = 999f;
            foreach (Collider col in nearby)
            {
                if (col == null) continue;
                IWildlockInteractable inter = col.GetComponentInParent<IWildlockInteractable>();
                if (inter != null)
                {
                    float d = Vector3.Distance(transform.position, inter.GetTransform().position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        HoveredInteractable = inter;
                    }
                }
            }
        }

        private void HandleCombatAndAbilityInputs()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    TryFireRevolver();
                }
            }

            if (kb == null) return;

            // 'E' Universal Interact
            if (kb.eKey.wasPressedThisFrame && HoveredInteractable != null)
            {
                HoveredInteractable.Interact(this);
            }

            // 'R' Reload Six-Shooter
            if (kb.rKey.wasPressedThisFrame && !isReloading && currentAmmo < maxAmmo)
            {
                StartCoroutine(ReloadRoutine());
            }

            // 'G' Throw Lit Dynamite
            if (kb.gKey.wasPressedThisFrame && dynamiteCooldown <= 0f)
            {
                ThrowDynamite();
            }

            // 'Q' Deploy Sheriff Auto-Turret
            if (kb.qKey.wasPressedThisFrame && turretCooldown <= 0f)
            {
                DeployAutoTurret();
            }

            // 'H' Deploy Frontier Healing Totem
            if (kb.hKey.wasPressedThisFrame && totemCooldown <= 0f)
            {
                DeployHealingTotem();
            }

            // 'M' Cycle Character Skin (Starter Armature / Adam / Guard / Lu)
            if (kb.mKey.wasPressedThisFrame)
            {
                CycleCharacterSkin();
            }
#endif
        }

        private void TryFireRevolver()
        {
            if (isReloading || _fireTimer > 0f) return;

            if (currentAmmo <= 0)
            {
                StartCoroutine(ReloadRoutine());
                return;
            }

            currentAmmo--;
            _fireTimer = fireRateCooldown;
            _aimFaceTimer = 0.6f;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            Vector3 muzzlePos = weaponMuzzlePoint != null
                ? weaponMuzzlePoint.position
                : transform.position + Vector3.up * 1.35f + transform.forward * 0.6f;

            WildlockAudioSynth.PlaySoundAt(muzzlePos, WildlockAudioSynth.SoundType.RevolverShot, 0.8f);
            WildlockAudioSynth.SpawnParticleBurst(muzzlePos, new Color(1f, 0.85f, 0.2f), new Color(1f, 0.35f, 0.05f), 14, 4f, 0.16f, 0.22f);

            Camera cam = Camera.main;
            Vector3 targetPoint = muzzlePos + transform.forward * 50f;

            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 75f))
                {
                    targetPoint = hit.point;
                    WildlockAudioSynth.SpawnParticleBurst(hit.point, new Color(1f, 0.75f, 0.25f), Color.white, 15, 3.8f, 0.15f, 0.35f);

                    // Check what we shot!
                    WildlockDestructibleProp prop = hit.collider.GetComponentInParent<WildlockDestructibleProp>();
                    if (prop != null)
                    {
                        prop.TakeDamage(revolverDamage, hit.point);
                    }

                    WildlockNpcAgent npc = hit.collider.GetComponentInParent<WildlockNpcAgent>();
                    if (npc != null && npc.isEnemyOutlaw)
                    {
                        npc.TakeDamage(revolverDamage, hit.point);
                    }

                    WildlockEnvironmentSwitch envSwitch = hit.collider.GetComponentInParent<WildlockEnvironmentSwitch>();
                    if (envSwitch != null)
                    {
                        envSwitch.ToggleSwitch();
                    }
                }
                else
                {
                    targetPoint = ray.GetPoint(60f);
                }
            }

            StartCoroutine(ShowTracerRoutine(muzzlePos, targetPoint));

            if (currentAmmo <= 0 && !isReloading)
            {
                StartCoroutine(ReloadRoutine());
            }
        }

        private IEnumerator ShowTracerRoutine(Vector3 start, Vector3 end)
        {
            if (_bulletTracer == null) yield break;
            _bulletTracer.SetPosition(0, start);
            _bulletTracer.SetPosition(1, end);
            _bulletTracer.enabled = true;
            yield return new WaitForSeconds(0.065f);
            _bulletTracer.enabled = false;
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            WildlockAudioSynth.PlaySoundAt(transform.position, WildlockAudioSynth.SoundType.SwitchFlip, 0.55f);
            yield return new WaitForSeconds(1.0f);
            currentAmmo = maxAmmo;
            isReloading = false;
        }

        public void RefillAmmoAndCooldowns()
        {
            currentAmmo = maxAmmo;
            isReloading = false;
            turretCooldown = 0f;
            totemCooldown = 0f;
            dynamiteCooldown = 0f;
        }

        private void ThrowDynamite()
        {
            dynamiteCooldown = 5.0f;
            Vector3 spawnPos = transform.position + Vector3.up * 1.45f + transform.forward * 0.8f;

            GameObject dyn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dyn.name = "LitDynamiteStick";
            dyn.transform.position = spawnPos;
            dyn.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);
            dyn.transform.rotation = Random.rotation;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Renderer r = dyn.GetComponent<Renderer>();
            if (r != null && urpLit != null)
            {
                Material m = new Material(urpLit);
                m.color = new Color(0.85f, 0.12f, 0.10f);
                r.material = m;
            }

            Light fuseLight = dyn.AddComponent<Light>();
            fuseLight.type = LightType.Point;
            fuseLight.color = new Color(1f, 0.65f, 0.15f);
            fuseLight.intensity = 4f;
            fuseLight.range = 5f;

            Rigidbody rb = dyn.AddComponent<Rigidbody>();
            Vector3 throwDir = (Camera.main != null ? Camera.main.transform.forward : transform.forward) + Vector3.up * 0.35f;
            rb.AddForce(throwDir.normalized * 13.5f, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);

            StartCoroutine(DetonateDynamiteRoutine(dyn));
        }

        private IEnumerator DetonateDynamiteRoutine(GameObject dyn)
        {
            yield return new WaitForSeconds(1.8f);
            if (dyn == null) yield break;

            Vector3 pos = dyn.transform.position;
            WildlockAudioSynth.PlaySoundAt(pos, WildlockAudioSynth.SoundType.Explosion, 0.95f);
            WildlockAudioSynth.SpawnParticleBurst(pos, new Color(1f, 0.35f, 0.05f), new Color(1f, 0.88f, 0.2f), 50, 9f, 0.45f, 0.85f);

            Collider[] hits = Physics.OverlapSphere(pos, 6.8f);
            foreach (Collider col in hits)
            {
                if (col == null) continue;
                WildlockDestructibleProp prop = col.GetComponentInParent<WildlockDestructibleProp>();
                if (prop != null) prop.TakeDamage(320f, pos);

                WildlockNpcAgent npc = col.GetComponentInParent<WildlockNpcAgent>();
                if (npc != null && npc.isEnemyOutlaw)
                {
                    npc.StunOutlaw(3.0f);
                    npc.TakeDamage(280f, pos);
                }
            }

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordObjectInteraction("Threw & Detonated Frontier Dynamite (AoE Blast)!");
            }

            Destroy(dyn);
        }

        private void DeployAutoTurret()
        {
            turretCooldown = 10f;
            Vector3 pos = transform.position + transform.forward * 2.0f;
            pos.y = 0f;

            GameObject turretRoot = new GameObject("Deployed_Sheriff_AutoTurret");
            turretRoot.transform.position = pos;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material baseMat = new Material(urpLit) { color = new Color(0.25f, 0.28f, 0.34f) };
            Material headMat = new Material(urpLit) { color = new Color(0.85f, 0.65f, 0.18f) };

            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.transform.SetParent(turretRoot.transform, false);
            stand.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            stand.transform.localScale = new Vector3(0.45f, 0.5f, 0.45f);
            stand.GetComponent<Renderer>().material = baseMat;

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.transform.SetParent(turretRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            head.transform.localScale = new Vector3(0.55f, 0.35f, 0.75f);
            head.GetComponent<Renderer>().material = headMat;

            Light beacon = head.AddComponent<Light>();
            beacon.type = LightType.Point;
            beacon.color = Color.cyan;
            beacon.intensity = 4f;
            beacon.range = 6f;

            WildlockAutoTurret comp = turretRoot.AddComponent<WildlockAutoTurret>();
            comp.rotatingHead = head.transform;

            WildlockAudioSynth.PlaySoundAt(pos, WildlockAudioSynth.SoundType.SwitchFlip, 0.8f);
            WildlockAudioSynth.SpawnParticleBurst(pos + Vector3.up, Color.cyan, Color.white, 24, 4f, 0.2f, 0.5f);

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.RecordNpcStateChange("Deployed Autonomous Sheriff Auto-Turret (AI Targeting Active)!");
            }
        }

        private void DeployHealingTotem()
        {
            totemCooldown = 12f;
            Vector3 pos = transform.position + transform.right * 1.5f + transform.forward * 1.2f;
            pos.y = 0f;

            GameObject totemRoot = new GameObject("Deployed_Frontier_HealingTotem");
            totemRoot.transform.position = pos;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material totemMat = new Material(urpLit) { color = new Color(0.15f, 0.92f, 0.45f) };

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(totemRoot.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            pillar.transform.localScale = new Vector3(0.38f, 0.7f, 0.38f);
            pillar.GetComponent<Renderer>().material = totemMat;

            Light healLight = pillar.AddComponent<Light>();
            healLight.type = LightType.Point;
            healLight.color = new Color(0.2f, 1f, 0.45f);
            healLight.intensity = 6.5f;
            healLight.range = 7.5f;

            totemRoot.AddComponent<WildlockHealingTotem>();

            WildlockAudioSynth.PlaySoundAt(pos, WildlockAudioSynth.SoundType.HealChime, 0.85f);
            WildlockAudioSynth.SpawnParticleBurst(pos + Vector3.up, new Color(0.2f, 1f, 0.45f), Color.white, 28, 4.5f, 0.22f, 0.65f);

            if (WildlockGameManager.Instance != null)
            {
                WildlockGameManager.Instance.ModifyPlayerHealth(120, "Deployed Healing Totem (+120 Instant HP & Aura)!");
            }
        }

        public void CycleCharacterSkin()
        {
            if (characterSkins == null || characterSkins.Length == 0) return;

            activeSkinIndex = (activeSkinIndex + 1) % characterSkins.Length;
            for (int i = 0; i < characterSkins.Length; i++)
            {
                if (characterSkins[i] != null)
                {
                    characterSkins[i].SetActive(i == activeSkinIndex);
                }
            }

            RefreshAnimatorReference();
            if (WildlockGameManager.Instance != null && characterSkins[activeSkinIndex] != null)
            {
                WildlockGameManager.Instance.PostEventBanner($"Switched Hero Skin to: {characterSkins[activeSkinIndex].name}");
            }
        }
    }
}
