using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wildlock
{
    /// <summary>
    /// Central Game Manager & HUD Controller for Wildlock (Assignment 2 Part 3 Prototype).
    /// Implements the 2v2 Gem Grab core loop (7-second central cart gem spawner, 10-gem threshold,
    /// 30-second countdown, Armory gate unlock, and live Assignment 2 requirement tracker).
    /// </summary>
    public class WildlockGameManager : MonoBehaviour
    {
        public static WildlockGameManager Instance { get; private set; }

        [Header("Player Stats & Gem Grab Rules (From Assignment 1)")]
        public int playerMaxHealth = 1000;
        public int playerHealth = 1000;
        public int playerGems = 0;
        public int targetGemsForCountdown = 10;
        public float gemSpawnInterval = 7.0f;
        public float showdownCountdownDuration = 30.0f;

        [Header("World References")]
        public Transform centralMineCartTransform;
        public GameObject gemPrefabTemplate;
        public WildlockSaloonDoor sheriffArmoryGate;

        [Header("Match State")]
        public bool isCountdownActive;
        public float remainingCountdownSeconds = 30.0f;
        public bool matchWon;
        public bool showChecklistOverlay = true;

        // Assignment 2 Part 3 Verification Flags (5 Player Actions)
        public bool Action1_ObjectInteraction { get; private set; }
        public bool Action2_ScriptedEvent { get; private set; }
        public bool Action3_EnvironmentState { get; private set; }
        public bool Action4_NpcBehavior { get; private set; }
        public bool Action5_StatsAndHud { get; private set; }

        private float _gemSpawnTimer = 3.5f;
        private float _sirenBeepTimer;
        private string _latestEventBanner = "Welcome to WILDLOCK! Collect 10 Red Gems from the Central Mine Cart & Crates!";
        private float _bannerTimer = 8f;

        private Texture2D _solidTex;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            playerHealth = playerMaxHealth;
            remainingCountdownSeconds = showdownCountdownDuration;

            // Spawn initial cluster of collectible gems around the central mine cart
            Vector3 center = centralMineCartTransform != null ? centralMineCartTransform.position : new Vector3(-134f, 0.2f, 5.1f);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * 3.8f, 0.65f, Mathf.Sin(angle) * 3.8f);
                SpawnCollectibleGemAt(pos);
            }
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                showChecklistOverlay = !showChecklistOverlay;
            }
#endif

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.deltaTime;
            }

            // 7-Second Central Mine Cart Gem Spawner (Core Rule from Assignment 1)
            _gemSpawnTimer -= Time.deltaTime;
            if (_gemSpawnTimer <= 0f)
            {
                _gemSpawnTimer = gemSpawnInterval;
                WildlockGemPickup[] existingGems = FindObjectsByType<WildlockGemPickup>(FindObjectsSortMode.None);
                if (existingGems.Length < 16)
                {
                    Vector3 center = centralMineCartTransform != null ? centralMineCartTransform.position : new Vector3(-134f, 0.2f, 5.1f);
                    Vector2 randCircle = Random.insideUnitCircle.normalized * Random.Range(2.4f, 5.2f);
                    Vector3 spawnPos = new Vector3(center.x + randCircle.x, 0.7f, center.z + randCircle.y);
                    SpawnCollectibleGemAt(spawnPos);
                }
            }

            // 30-Second Showdown Countdown Logic
            if (isCountdownActive && !matchWon)
            {
                remainingCountdownSeconds -= Time.deltaTime;
                _sirenBeepTimer -= Time.deltaTime;

                if (_sirenBeepTimer <= 0f)
                {
                    _sirenBeepTimer = 2.5f;
                    if (centralMineCartTransform != null)
                    {
                        WildlockAudioSynth.PlaySoundAt(centralMineCartTransform.position, WildlockAudioSynth.SoundType.CountdownSiren, 0.45f);
                    }
                }

                if (remainingCountdownSeconds <= 0f)
                {
                    remainingCountdownSeconds = 0f;
                    matchWon = true;
                    PostEventBanner("VICTORY! You held 10+ Gems for 30 Seconds and Won the Showdown!");
                }
            }
        }

        public void SpawnCollectibleGemAt(Vector3 worldPos)
        {
            GameObject gemGo = new GameObject("Collectible_RedRubyGem");
            gemGo.transform.position = worldPos;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material rubyMat = new Material(urpLit);
            rubyMat.color = new Color(1f, 0.08f, 0.22f);
            if (rubyMat.HasProperty("_Smoothness")) rubyMat.SetFloat("_Smoothness", 0.9f);
            if (rubyMat.HasProperty("_EmissionColor"))
            {
                rubyMat.EnableKeyword("_EMISSION");
                rubyMat.SetColor("_EmissionColor", new Color(0.95f, 0.05f, 0.18f) * 2.2f);
            }

            // Build faceted crystal visual
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cube);
            core.name = "RubyCrystalMesh";
            core.transform.SetParent(gemGo.transform, false);
            core.transform.localScale = new Vector3(0.42f, 0.58f, 0.42f);
            core.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            Destroy(core.GetComponent<Collider>());
            core.GetComponent<Renderer>().material = rubyMat;

            Light glow = gemGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.15f, 0.28f);
            glow.intensity = 2.6f;
            glow.range = 3.8f;

            SphereCollider trigger = gemGo.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.85f;

            gemGo.AddComponent<WildlockGemPickup>();
            gemGo.AddComponent<gemanimation>();
        }

        public void AddPlayerGems(int delta, string reason)
        {
            playerGems = Mathf.Max(0, playerGems + delta);
            Action1_ObjectInteraction = true;
            Action5_StatsAndHud = true;

            PostEventBanner($"{reason} (Total Gems: {playerGems}/{targetGemsForCountdown})");

            // Unlock Sheriff Armory Gate at 5 Gems (Action 1.5 World Consequence)
            if (playerGems >= 5 && sheriffArmoryGate != null && !sheriffArmoryGate.isOpen)
            {
                sheriffArmoryGate.SetDoorOpen(true, false);
                Action2_ScriptedEvent = true;
                PostEventBanner("5 GEMS COLLECTED! Sheriff's Armory Gate Unlocked Automatically!");
            }

            // Trigger 30-Second Showdown Countdown at 10 Gems
            if (playerGems >= targetGemsForCountdown && !isCountdownActive && !matchWon)
            {
                isCountdownActive = true;
                remainingCountdownSeconds = showdownCountdownDuration;
                Action2_ScriptedEvent = true;

                if (centralMineCartTransform != null)
                {
                    WildlockAudioSynth.PlaySoundAt(centralMineCartTransform.position, WildlockAudioSynth.SoundType.CountdownSiren, 0.9f);
                }

                if (WildlockLightingManager.Instance != null)
                {
                    WildlockLightingManager.Instance.SetShowdownAlarmLights(true);
                }

                if (WildlockCameraDirector.Instance != null && centralMineCartTransform != null)
                {
                    WildlockCameraDirector.Instance.TriggerEventCutscene(centralMineCartTransform, 2.5f);
                }

                PostEventBanner("10 GEMS REACHED! 30-SECOND SHOWDOWN COUNTDOWN STARTED! SURVIVE!");
            }
        }

        public void ModifyPlayerHealth(int delta, string reason)
        {
            playerHealth = Mathf.Clamp(playerHealth + delta, 0, playerMaxHealth);
            if (delta > 0)
            {
                Action5_StatsAndHud = true;
            }
            PostEventBanner(reason);

            if (playerHealth <= 0)
            {
                // Player eliminated: drop half of gems and reset countdown if below 10
                int dropped = Mathf.Min(playerGems, 4);
                playerGems -= dropped;
                playerHealth = playerMaxHealth;

                if (WildlockPlayerController.Instance != null)
                {
                    Vector3 deathPos = WildlockPlayerController.Instance.transform.position;
                    for (int i = 0; i < dropped; i++)
                    {
                        SpawnCollectibleGemAt(deathPos + new Vector3((i - 1.5f) * 0.7f, 0.6f, 0f));
                    }
                    WildlockPlayerController.Instance.RespawnAtBase();
                }

                if (playerGems < targetGemsForCountdown && isCountdownActive)
                {
                    isCountdownActive = false;
                    remainingCountdownSeconds = showdownCountdownDuration;
                    if (WildlockLightingManager.Instance != null)
                    {
                        WildlockLightingManager.Instance.SetShowdownAlarmLights(false);
                    }
                    PostEventBanner("Eliminated! Dropped Gems — Showdown Countdown Reset!");
                }
            }
        }

        public void RecordObjectInteraction(string message)
        {
            Action1_ObjectInteraction = true;
            PostEventBanner(message);
        }

        public void RecordScriptedEvent(string message)
        {
            Action2_ScriptedEvent = true;
            PostEventBanner(message);
        }

        public void RecordEnvironmentalStateChange(string message)
        {
            Action3_EnvironmentState = true;
            Action2_ScriptedEvent = true;
            PostEventBanner(message);
        }

        public void RecordNpcStateChange(string message)
        {
            Action4_NpcBehavior = true;
            PostEventBanner(message);
        }

        public void RecordStatModification(string message)
        {
            Action5_StatsAndHud = true;
            PostEventBanner(message);
        }

        public void PostEventBanner(string message)
        {
            _latestEventBanner = message;
            _bannerTimer = 5.5f;
        }

        private void EnsureTexture()
        {
            if (_solidTex == null)
            {
                _solidTex = new Texture2D(1, 1);
                _solidTex.SetPixel(0, 0, Color.white);
                _solidTex.Apply();
            }
        }

        private void DrawBox(Rect rect, Color color)
        {
            EnsureTexture();
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _solidTex);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            EnsureTexture();

            Matrix4x4 prevMatrix = GUI.matrix;
            float uiScale = Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 810f), 0.50f, 1.0f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            float vw = Screen.width / uiScale;
            float vh = Screen.height / uiScale;

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = new Color(1f, 0.88f, 0.42f);

            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            // 1. TOP-CENTER GEM GRAB & COUNTDOWN BANNER
            float topW = 540f;
            Rect topRect = new Rect((vw - topW) * 0.5f, 10f, topW, 96f);
            DrawBox(topRect, new Color(0.08f, 0.06f, 0.05f, 0.86f));

            GUI.Label(new Rect(topRect.x, topRect.y + 4f, topW, 26f),
                $"WILDLOCK — 2v2 GEM GRAB ARENA   |   RUBIES: {playerGems} / {targetGemsForCountdown}", headerStyle);

            // Gem progress bar
            Rect barBg = new Rect(topRect.x + 25f, topRect.y + 32f, topW - 50f, 14f);
            DrawBox(barBg, new Color(0.22f, 0.18f, 0.16f, 1f));
            float gemFill = Mathf.Clamp01((float)playerGems / targetGemsForCountdown);
            DrawBox(new Rect(barBg.x, barBg.y, barBg.width * gemFill, barBg.height),
                isCountdownActive ? new Color(1f, 0.2f, 0.25f, 1f) : new Color(0.95f, 0.15f, 0.35f, 1f));

            string statusLine;
            if (matchWon)
            {
                statusLine = "★ SHOWDOWN VICTORY! TEAM 1 HELD 10+ GEMS FOR 30 SECONDS! ★";
            }
            else if (isCountdownActive)
            {
                statusLine = $"⚠ SHOWDOWN COUNTDOWN ACTIVE: {remainingCountdownSeconds:F1}s REMAINING! DEFEND GEMS! ⚠";
            }
            else
            {
                statusLine = $"Next Central Cart Gem Spawn: {_gemSpawnTimer:F1}s   |   Armory Gate Unlock: {Mathf.Min(playerGems, 5)}/5 Gems";
            }

            GUIStyle centerSub = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
            centerSub.normal.textColor = isCountdownActive ? new Color(1f, 0.45f, 0.45f) : new Color(0.9f, 0.85f, 0.75f);
            GUI.Label(new Rect(topRect.x, topRect.y + 48f, topW, 24f), statusLine, centerSub);

            string camLabel = WildlockCameraDirector.Instance != null ? WildlockCameraDirector.Instance.GetActiveCameraLabel() : "Cinemachine";
            string lightLabel = WildlockLightingManager.Instance != null ? WildlockLightingManager.Instance.GetTimeOfDayLabel() : "High Noon";
            GUI.Label(new Rect(topRect.x, topRect.y + 70f, topW, 24f), $"Cam: {camLabel}  |  Time: {lightLabel}", centerSub);

            // 2. EVENT NOTIFICATION BANNER
            if (_bannerTimer > 0f && !string.IsNullOrEmpty(_latestEventBanner))
            {
                Rect bannerRect = new Rect((vw - 620f) * 0.5f, 112f, 620f, 32f);
                DrawBox(bannerRect, new Color(0.18f, 0.10f, 0.05f, 0.90f));
                GUIStyle bannerStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
                bannerStyle.normal.textColor = new Color(1f, 0.92f, 0.35f);
                GUI.Label(bannerRect, _latestEventBanner, bannerStyle);
            }

            // 3. CENTER CROSSHAIR & INTERACTION PROMPT
            float cx = vw * 0.5f;
            float cy = vh * 0.5f;
            bool hasInteractable = WildlockPlayerController.Instance != null && WildlockPlayerController.Instance.HoveredInteractable != null;
            Color reticleCol = hasInteractable ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.9f, 0.5f, 0.85f);
            DrawBox(new Rect(cx - 8f, cy - 1f, 16f, 2f), reticleCol);
            DrawBox(new Rect(cx - 1f, cy - 8f, 2f, 16f), reticleCol);

            if (hasInteractable)
            {
                string prompt = WildlockPlayerController.Instance.HoveredInteractable.GetInteractionPrompt();
                Rect promptRect = new Rect(cx - 250f, cy + 22f, 500f, 32f);
                DrawBox(promptRect, new Color(0.05f, 0.14f, 0.08f, 0.9f));
                GUIStyle pStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
                pStyle.normal.textColor = new Color(0.4f, 1f, 0.55f);
                GUI.Label(promptRect, prompt, pStyle);
            }

            // 4. BOTTOM-LEFT GUNSLINGER STATS & ABILITIES HUD
            Rect statRect = new Rect(14f, vh - 136f, 415f, 122f);
            DrawBox(statRect, new Color(0.08f, 0.06f, 0.05f, 0.86f));

            GUI.Label(new Rect(statRect.x + 12f, statRect.y + 6f, 390f, 24f),
                $"GUNSLINGER HP: {playerHealth} / {playerMaxHealth}", textStyle);
            Rect hpBg = new Rect(statRect.x + 12f, statRect.y + 30f, 390f, 13f);
            DrawBox(hpBg, new Color(0.25f, 0.1f, 0.1f, 1f));
            DrawBox(new Rect(hpBg.x, hpBg.y, hpBg.width * Mathf.Clamp01((float)playerHealth / playerMaxHealth), hpBg.height),
                new Color(0.2f, 0.88f, 0.35f, 1f));

            WildlockPlayerController pc = WildlockPlayerController.Instance;
            string ammoStr = pc != null ? (pc.isReloading ? "RELOADING..." : $"{pc.currentAmmo} / {pc.maxAmmo} Rounds ([R] Reload)") : "6 / 6";
            bool lanternOn = WildlockLightingManager.Instance != null && WildlockLightingManager.Instance.isLanternOn;
            GUI.Label(new Rect(statRect.x + 12f, statRect.y + 46f, 390f, 24f),
                $"Six-Shooter: {ammoStr}   |   [F] Lantern: {(lanternOn ? "ON" : "OFF")}", textStyle);

            string qStr = (pc != null && pc.turretCooldown > 0f) ? $"{pc.turretCooldown:F1}s" : "READY";
            string hStr = (pc != null && pc.totemCooldown > 0f) ? $"{pc.totemCooldown:F1}s" : "READY";
            string gStr = (pc != null && pc.dynamiteCooldown > 0f) ? $"{pc.dynamiteCooldown:F1}s" : "READY";
            GUI.Label(new Rect(statRect.x + 12f, statRect.y + 69f, 390f, 24f),
                $"[Q] Auto-Turret: {qStr}  |  [H] Heal Totem: {hStr}  |  [G] Dynamite: {gStr}", textStyle);
            GUI.Label(new Rect(statRect.x + 12f, statRect.y + 92f, 390f, 24f),
                "[Left-Click] Shoot  |  [Right-Click] ADS Zoom  |  [E] Interact  |  [M] Swap Hero", textStyle);

            // 5. TOP-RIGHT ASSIGNMENT 2 PROTOTYPE CHECKLIST (Toggleable with TAB)
            if (showChecklistOverlay)
            {
                Rect chkRect = new Rect(vw - 388f, 10f, 374f, 312f);
                DrawBox(chkRect, new Color(0.06f, 0.08f, 0.12f, 0.86f));

                GUI.Label(new Rect(chkRect.x + 8f, chkRect.y + 6f, 358f, 24f),
                    "ASSIGNMENT 2 PART 3 CHECKLIST ([Tab] Hide)", headerStyle);

                float y = chkRect.y + 32f;
                DrawCheckLine(chkRect.x + 10f, ref y, true, "--- 1. PLAYER ACTIONS (Min 5) ---", true);
                DrawCheckLine(chkRect.x + 10f, ref y, Action1_ObjectInteraction, "1.1 Pick Up Gems / Shoot Crates & Barrels");
                DrawCheckLine(chkRect.x + 10f, ref y, Action2_ScriptedEvent, "1.2 Open Saloon Doors [E] / Jump Pads / 10-Gem Siren");
                DrawCheckLine(chkRect.x + 10f, ref y, Action3_EnvironmentState, "1.3 Flip Generator Lever [E] -> Raise Platform & Lights");
                DrawCheckLine(chkRect.x + 10f, ref y, Action4_NpcBehavior, "1.4 Shoot Outlaws / Command Duo Adam [E] / Turret [Q]");
                DrawCheckLine(chkRect.x + 10f, ref y, Action5_StatsAndHud, "1.5 Collect Gems/Elixirs -> Update HUD & Unlock Gate");

                WildlockCameraDirector cd = WildlockCameraDirector.Instance;
                DrawCheckLine(chkRect.x + 10f, ref y, true, "--- 2. CINEMACHINE CAMERAS (Min 3) ---", true);
                DrawCheckLine(chkRect.x + 10f, ref y, cd != null && cd.UsedThirdPerson, "2.1 3rd-Person Follow Cam ([1] / [C] / Right-Click)");
                DrawCheckLine(chkRect.x + 10f, ref y, cd != null && cd.UsedFirstPerson, "2.2 1st-Person POV Cam ([2] or [C] to Toggle)");
                DrawCheckLine(chkRect.x + 10f, ref y, cd != null && cd.UsedCutsceneTransition, "2.3 Cutscene / Overview Blend ([3], [4], or [V])");

                WildlockLightingManager lm = WildlockLightingManager.Instance;
                DrawCheckLine(chkRect.x + 10f, ref y, true, "--- 3. LIGHTING BEHAVIORS (Min 3) ---", true);
                DrawCheckLine(chkRect.x + 10f, ref y, lm != null && lm.ObservedDayNightShift, "3.1 Day/Night Shift ([T] Step Noon/Sunset/Night, [Y] Auto)");
                DrawCheckLine(chkRect.x + 10f, ref y, lm != null && lm.UsedPlayerLantern, "3.2 Player Western Lantern & Spotlight ([F] Toggle)");
                DrawCheckLine(chkRect.x + 10f, ref y, lm != null && lm.TriggeredEnvironmentLighting, "3.3 Enter Mine/Saloon Zone or Flip Generator Switch");
            }

            GUI.matrix = prevMatrix;
        }

        private void DrawCheckLine(float x, ref float y, bool done, string label, bool isSection = false)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = isSection ? FontStyle.Bold : FontStyle.Normal
            };
            if (isSection)
            {
                s.normal.textColor = new Color(0.55f, 0.85f, 1f);
                GUI.Label(new Rect(x, y, 355f, 20f), label, s);
            }
            else
            {
                s.normal.textColor = done ? new Color(0.35f, 1f, 0.48f) : new Color(0.88f, 0.88f, 0.88f);
                string prefix = done ? "[✓] " : "[  ] ";
                GUI.Label(new Rect(x, y, 355f, 20f), prefix + label, s);
            }
            y += 20f;
        }
    }
}
