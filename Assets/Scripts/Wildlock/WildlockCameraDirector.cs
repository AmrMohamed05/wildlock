using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Wildlock
{
    /// <summary>
    /// Assignment 2 Part 3 (Section 2): Camera Movements using Unity Cinemachine.
    /// Manages 4 distinct Cinemachine Virtual Cameras via priority blending:
    /// 1) Third-Person Follow Camera (with Right-Click ADS zoom)
    /// 2) First-Person POV Camera
    /// 3) Cinematic / Event Transition Camera (Central Mine Cart & Switch Cutscene)
    /// 4) Tactical Arena & Outer Vista Overview Camera
    /// </summary>
    public class WildlockCameraDirector : MonoBehaviour
    {
        public static WildlockCameraDirector Instance { get; private set; }

        public enum CameraMode
        {
            ThirdPerson = 0,
            FirstPerson = 1,
            CutsceneTransition = 2,
            ArenaOverview = 3
        }

        [Header("Cinemachine Virtual Cameras")]
        public CinemachineCamera thirdPersonVCam;
        public CinemachineCamera firstPersonVCam;
        public CinemachineCamera cutsceneVCam;
        public CinemachineCamera overviewVCam;

        [Header("Follow & Look Targets")]
        public Transform thirdPersonFollowTarget;
        public Transform firstPersonFollowTarget;
        public Transform centralMineCartTarget;
        public GameObject[] playerVisualMeshesToHideInFPS;

        [Header("State")]
        public CameraMode currentMode = CameraMode.ThirdPerson;
        public bool isAimingDownSights;

        // Verification flags for Assignment 2 HUD checklist
        public bool UsedThirdPerson { get; private set; } = true;
        public bool UsedFirstPerson { get; private set; }
        public bool UsedCutsceneTransition { get; private set; }

        private CinemachineThirdPersonFollow _tpFollow;
        private Coroutine _temporaryCutsceneRoutine;
        private CameraMode _modeBeforeCutscene = CameraMode.ThirdPerson;

        private void Awake()
        {
            Instance = this;
            DisableStraySceneCameras();
        }

        private void Start()
        {
            DisableStraySceneCameras();
            EnsureCinemachineBrainBlend();
            if (thirdPersonVCam != null)
            {
                _tpFollow = thirdPersonVCam.GetComponent<CinemachineThirdPersonFollow>();
            }
            SetCameraMode(CameraMode.ThirdPerson);
        }

        public static void DisableStraySceneCameras()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject mainGo = GameObject.Find("Main Camera");
                if (mainGo != null) mainCam = mainGo.GetComponent<Camera>();
            }

            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera cam in allCameras)
            {
                if (cam == null) continue;
                if (cam == mainCam || cam.CompareTag("MainCamera") || cam.gameObject.name == "Main Camera")
                {
                    cam.enabled = true;
                    cam.depth = 10f;
                    cam.clearFlags = CameraClearFlags.Skybox;
                    cam.cullingMask = ~0;
                    cam.nearClipPlane = 0.15f;
                    cam.farClipPlane = 2500f;
                }
                else
                {
                    // Disable embedded FBX cameras (e.g. CINEMA_4D_Editor inside Water Tower Water Tanker.fbx)
                    // which had m_Depth = 0 > Main Camera's m_Depth = -1 and overwrote the screen with underground skybox grey!
                    cam.enabled = false;
                    cam.depth = -100f;
                    AudioListener al = cam.GetComponent<AudioListener>();
                    if (al != null) al.enabled = false;
                    if (cam.GetComponent<Renderer>() == null && cam.transform.childCount == 0)
                    {
                        cam.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void EnsureCinemachineBrainBlend()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            mainCam.depth = 10f;
            CinemachineBrain brain = mainCam.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
            }
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.75f);
        }

        private void Update()
        {
            HandleCameraInput();
            UpdateThirdPersonAimZoom();
        }

        private void HandleCameraInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                // 'C' toggles between 3rd-Person and 1st-Person
                if (kb.cKey.wasPressedThisFrame)
                {
                    if (currentMode == CameraMode.ThirdPerson)
                        SetCameraMode(CameraMode.FirstPerson);
                    else
                        SetCameraMode(CameraMode.ThirdPerson);
                }
                // 'V' toggles Cinematic Transition Camera
                else if (kb.vKey.wasPressedThisFrame)
                {
                    if (currentMode == CameraMode.CutsceneTransition)
                        SetCameraMode(CameraMode.ThirdPerson);
                    else
                        SetCameraMode(CameraMode.CutsceneTransition);
                }
                else if (kb.digit1Key.wasPressedThisFrame) SetCameraMode(CameraMode.ThirdPerson);
                else if (kb.digit2Key.wasPressedThisFrame) SetCameraMode(CameraMode.FirstPerson);
                else if (kb.digit3Key.wasPressedThisFrame) SetCameraMode(CameraMode.CutsceneTransition);
                else if (kb.digit4Key.wasPressedThisFrame) SetCameraMode(CameraMode.ArenaOverview);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                isAimingDownSights = mouse.rightButton.isPressed;
            }
#endif
        }

        private void UpdateThirdPersonAimZoom()
        {
            if (thirdPersonVCam == null) return;

            float targetFov = isAimingDownSights ? 32f : 48f;
            var lens = thirdPersonVCam.Lens;
            lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, Time.deltaTime * 10f);
            thirdPersonVCam.Lens = lens;

            if (_tpFollow != null)
            {
                float targetDist = isAimingDownSights ? 2.1f : 4.2f;
                _tpFollow.CameraDistance = Mathf.Lerp(_tpFollow.CameraDistance, targetDist, Time.deltaTime * 10f);
            }
        }

        public void SetCameraMode(CameraMode mode)
        {
            if (_temporaryCutsceneRoutine != null)
            {
                StopCoroutine(_temporaryCutsceneRoutine);
                _temporaryCutsceneRoutine = null;
            }

            ApplyCameraModeInternal(mode);
        }

        private void ApplyCameraModeInternal(CameraMode mode)
        {
            currentMode = mode;

            if (mode == CameraMode.ThirdPerson) UsedThirdPerson = true;
            if (mode == CameraMode.FirstPerson) UsedFirstPerson = true;
            if (mode == CameraMode.CutsceneTransition || mode == CameraMode.ArenaOverview) UsedCutsceneTransition = true;

            SetVCamPriority(thirdPersonVCam, mode == CameraMode.ThirdPerson ? 20 : 5);
            SetVCamPriority(firstPersonVCam, mode == CameraMode.FirstPerson ? 20 : 5);
            SetVCamPriority(cutsceneVCam, mode == CameraMode.CutsceneTransition ? 20 : 5);
            SetVCamPriority(overviewVCam, mode == CameraMode.ArenaOverview ? 20 : 5);

            // In First-Person mode, keep cursor rotation synced and optionally hide intrusive headgear
            if (playerVisualMeshesToHideInFPS != null)
            {
                bool showBody = (mode != CameraMode.FirstPerson);
                foreach (GameObject go in playerVisualMeshesToHideInFPS)
                {
                    if (go != null) go.SetActive(showBody);
                }
            }
        }

        private static void SetVCamPriority(CinemachineCamera vcam, int priorityValue)
        {
            if (vcam == null) return;
            vcam.Priority = new PrioritySettings { Enabled = true, Value = priorityValue };
        }

        /// <summary>
        /// Smoothly blends to the Cinematic Virtual Camera looking at a target for a set duration,
        /// then returns to the player's active gameplay camera.
        /// </summary>
        public void TriggerEventCutscene(Transform focusTarget, float duration = 2.6f)
        {
            if (cutsceneVCam == null) return;

            if (focusTarget != null)
            {
                cutsceneVCam.LookAt = focusTarget;
            }

            if (_temporaryCutsceneRoutine != null)
            {
                StopCoroutine(_temporaryCutsceneRoutine);
            }
            else if (currentMode != CameraMode.CutsceneTransition)
            {
                _modeBeforeCutscene = currentMode;
            }

            _temporaryCutsceneRoutine = StartCoroutine(CutsceneRoutine(duration));
        }

        private IEnumerator CutsceneRoutine(float duration)
        {
            ApplyCameraModeInternal(CameraMode.CutsceneTransition);
            yield return new WaitForSeconds(duration);
            _temporaryCutsceneRoutine = null;
            ApplyCameraModeInternal(_modeBeforeCutscene);
        }

        public string GetActiveCameraLabel()
        {
            switch (currentMode)
            {
                case CameraMode.ThirdPerson:
                    return isAimingDownSights ? "Cinemachine 3rd-Person (ADS Zoom)" : "Cinemachine 3rd-Person Follow";
                case CameraMode.FirstPerson:
                    return "Cinemachine 1st-Person POV";
                case CameraMode.CutsceneTransition:
                    return "Cinemachine Cutscene / Objective Transition";
                case CameraMode.ArenaOverview:
                    return "Cinemachine Tactical Arena & Vista Overview";
                default:
                    return "Cinemachine Active";
            }
        }
    }
}
