using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Procedural sound effect synthesizer and URP particle effect helper for Wildlock.
    /// Ensures zero external audio file dependencies while providing rich audio-visual feedback.
    /// </summary>
    public static class WildlockAudioSynth
    {
        private static Material _cachedParticleMaterial;

        public static Material GetUrpParticleMaterial()
        {
            if (_cachedParticleMaterial != null) return _cachedParticleMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            _cachedParticleMaterial = new Material(shader);
            _cachedParticleMaterial.name = "Wildlock_URP_ParticleMat";
            return _cachedParticleMaterial;
        }

        public static void PlaySoundAt(Vector3 position, SoundType type, float volume = 0.7f)
        {
            AudioClip clip = CreateClip(type);
            if (clip == null) return;

            GameObject tempGo = new GameObject("TempAudio_" + type);
            tempGo.transform.position = position;
            AudioSource source = tempGo.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 0.35f; // Mostly 2D so player always hears it clearly
            source.minDistance = 4f;
            source.maxDistance = 60f;
            source.Play();
            Object.Destroy(tempGo, clip.length + 0.1f);
        }

        public enum SoundType
        {
            RevolverShot,
            GemPickup,
            CrateBreak,
            Explosion,
            SwitchFlip,
            DoorSwing,
            JumpPad,
            CountdownSiren,
            NpcAlert,
            HealChime,
            LanternToggle
        }

        private static AudioClip CreateClip(SoundType type)
        {
            int sampleRate = 22050;
            float duration = 0.35f;

            switch (type)
            {
                case SoundType.RevolverShot: duration = 0.32f; break;
                case SoundType.GemPickup: duration = 0.28f; break;
                case SoundType.CrateBreak: duration = 0.25f; break;
                case SoundType.Explosion: duration = 0.65f; break;
                case SoundType.SwitchFlip: duration = 0.30f; break;
                case SoundType.DoorSwing: duration = 0.35f; break;
                case SoundType.JumpPad: duration = 0.40f; break;
                case SoundType.CountdownSiren: duration = 0.55f; break;
                case SoundType.NpcAlert: duration = 0.25f; break;
                case SoundType.HealChime: duration = 0.40f; break;
                case SoundType.LanternToggle: duration = 0.15f; break;
            }

            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            System.Random rng = new System.Random((int)(Time.realtimeSinceStartup * 1000f) + (int)type);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = (float)i / sampleCount;
                float env = Mathf.Pow(1f - norm, 1.8f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

                switch (type)
                {
                    case SoundType.RevolverShot:
                        float bang = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 55f, norm) * t);
                        samples[i] = (noise * 0.7f + bang * 0.6f) * Mathf.Pow(1f - norm, 2.5f);
                        break;

                    case SoundType.GemPickup:
                        float freq = norm < 0.45f ? 880f : 1318.5f;
                        samples[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f +
                                      Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * 0.25f) * env;
                        break;

                    case SoundType.CrateBreak:
                        float crack = Mathf.Sin(2f * Mathf.PI * 140f * t);
                        samples[i] = (noise * 0.8f + crack * 0.3f) * Mathf.Pow(1f - norm, 2.2f);
                        break;

                    case SoundType.Explosion:
                        float rumble = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(110f, 28f, norm) * t);
                        samples[i] = (noise * 0.75f + rumble * 0.65f) * Mathf.Pow(1f - norm, 1.4f);
                        break;

                    case SoundType.SwitchFlip:
                        float clankFreq = norm < 0.5f ? 320f : 540f;
                        samples[i] = (Mathf.Sin(2f * Mathf.PI * clankFreq * t) * 0.7f + noise * 0.2f) * env;
                        break;

                    case SoundType.DoorSwing:
                        float creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(190f, 260f, norm) * t);
                        samples[i] = creak * 0.5f * env;
                        break;

                    case SoundType.JumpPad:
                        float whooshFreq = Mathf.Lerp(180f, 760f, norm);
                        samples[i] = (Mathf.Sin(2f * Mathf.PI * whooshFreq * t) * 0.55f + noise * 0.25f) * env;
                        break;

                    case SoundType.CountdownSiren:
                        float sirenFreq = Mathf.Lerp(520f, 780f, Mathf.PingPong(norm * 2f, 1f));
                        samples[i] = Mathf.Sin(2f * Mathf.PI * sirenFreq * t) * 0.6f * (1f - norm * 0.4f);
                        break;

                    case SoundType.NpcAlert:
                        float alertFreq = norm < 0.5f ? 440f : 620f;
                        samples[i] = Mathf.Sin(2f * Mathf.PI * alertFreq * t) * 0.55f * env;
                        break;

                    case SoundType.HealChime:
                        float healFreq = norm < 0.33f ? 523.25f : (norm < 0.66f ? 659.25f : 783.99f);
                        samples[i] = Mathf.Sin(2f * Mathf.PI * healFreq * t) * 0.5f * env;
                        break;

                    case SoundType.LanternToggle:
                        samples[i] = (Mathf.Sin(2f * Mathf.PI * 950f * t) * 0.5f + noise * 0.3f) * env;
                        break;
                }
            }

            AudioClip clip = AudioClip.Create("Synth_" + type, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static void SpawnParticleBurst(Vector3 position, Color startColor, Color endColor, int count = 22, float speed = 4.5f, float size = 0.22f, float lifetime = 0.65f)
        {
            GameObject fxGo = new GameObject("Wildlock_ParticleBurst");
            fxGo.transform.position = position;

            ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size * 1.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(startColor, endColor);
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = false;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var psRenderer = fxGo.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = GetUrpParticleMaterial();

            ps.Play();
            ps.Emit(count);
            Object.Destroy(fxGo, lifetime + 0.5f);
        }
    }
}


