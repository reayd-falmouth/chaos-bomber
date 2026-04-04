using System;
using System.Collections;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.MainMenu
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    public class TitleCometParticleController : MonoBehaviour
    {
        [Serializable]
        public struct CometPreset
        {
            [Header("Motion")]
            [Min(0.01f)] public float headSpeedUnitsPerSecond;
            [Tooltip("Direction in camera plane: (1,0)=right, (1,-0.35)=diagonal down-right.")]
            public Vector2 directionInCameraPlane;
            [Min(0f)] public float offscreenMarginViewport;
            [Min(0f)] public float yViewportJitter;

            [Header("Spawn cadence")]
            [Min(0f)] public float minWaitSeconds;
            [Min(0f)] public float maxWaitSeconds;

            [Header("Particle look")]
            [Min(0.01f)] public float particleLifetime;
            [Min(0f)] public float particlesPerSecond;
            [Min(0f)] public float lateralRandomnessWorld;
            public Vector2 startSizeRange;
            public Gradient colorOverLifetime;

            [Header("Stretch / trail feel")]
            [Min(0f)] public float rendererLengthScale;
            [Min(0f)] public float rendererVelocityScale;
        }

        [Header("Target camera")]
        [Tooltip("If null, uses UnityEngine.Camera.main.")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("Presets")]
        [SerializeField] private int activePresetIndex = 0;
        [SerializeField] private CometPreset[] presets =
        {
            new CometPreset
            {
                headSpeedUnitsPerSecond = 10f,
                directionInCameraPlane = new Vector2(1f, 0f),
                offscreenMarginViewport = 0.15f,
                yViewportJitter = 0.25f,
                minWaitSeconds = 0.6f,
                maxWaitSeconds = 2.4f,
                particleLifetime = 0.9f,
                particlesPerSecond = 60f,
                lateralRandomnessWorld = 0.18f,
                startSizeRange = new Vector2(0.06f, 0.14f),
                colorOverLifetime = null,
                rendererLengthScale = 3.5f,
                rendererVelocityScale = 0.2f
            },
            new CometPreset
            {
                headSpeedUnitsPerSecond = 10f,
                directionInCameraPlane = new Vector2(1f, -0.35f),
                offscreenMarginViewport = 0.15f,
                yViewportJitter = 0.25f,
                minWaitSeconds = 0.6f,
                maxWaitSeconds = 2.4f,
                particleLifetime = 0.9f,
                particlesPerSecond = 70f,
                lateralRandomnessWorld = 0.22f,
                startSizeRange = new Vector2(0.06f, 0.14f),
                colorOverLifetime = null,
                rendererLengthScale = 3.8f,
                rendererVelocityScale = 0.22f
            }
        };

        [Header("Time")]
        [SerializeField] private bool useUnscaledTime = true;

        private ParticleSystem _ps;
        private ParticleSystemRenderer _psr;

        private float _emitAccumulator;
        private Coroutine _loopRoutine;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _psr = GetComponent<ParticleSystemRenderer>();
        }

        private void OnEnable()
        {
            ApplyPreset();
            _ps.Play(true);
            _loopRoutine = StartCoroutine(Loop());
        }

        private void OnDisable()
        {
            if (_loopRoutine != null)
            {
                StopCoroutine(_loopRoutine);
                _loopRoutine = null;
            }

            // ParticleSystem is a separate component; disabling this behaviour does not stop simulation.
            if (_ps == null)
                _ps = GetComponent<ParticleSystem>();
            if (_ps != null)
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnValidate()
        {
            if (activePresetIndex < 0) activePresetIndex = 0;
            if (presets != null && presets.Length > 0 && activePresetIndex >= presets.Length)
                activePresetIndex = presets.Length - 1;
        }

        public void ApplyPreset()
        {
            if (_ps == null) _ps = GetComponent<ParticleSystem>();
            if (_psr == null) _psr = GetComponent<ParticleSystemRenderer>();

            var preset = GetActivePreset();

            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Fully script-driven emission.
            var emission = _ps.emission;
            emission.enabled = false;

            var main = _ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = useUnscaledTime;
            main.startLifetime = preset.particleLifetime;
            main.startSpeed = 0f;

            // Stretch rendering to imply a trail even without Trails module.
            if (_psr != null)
            {
                _psr.renderMode = ParticleSystemRenderMode.Stretch;
                _psr.lengthScale = preset.rendererLengthScale;
                _psr.velocityScale = preset.rendererVelocityScale;
            }

            // Fade/shrink over lifetime for a comet tail.
            var sol = _ps.sizeOverLifetime;
            sol.enabled = true;
            sol.separateAxes = false;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var col = _ps.colorOverLifetime;
            col.enabled = true;
            col.color = BuildColorGradient(preset);
        }

        public void SetPreset(int index, bool restart = true)
        {
            activePresetIndex = index;
            ApplyPreset();
            if (restart && isActiveAndEnabled)
            {
                if (_loopRoutine != null) StopCoroutine(_loopRoutine);
                _loopRoutine = StartCoroutine(Loop());
            }
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                var preset = GetActivePreset();
                var cam = targetCamera != null ? targetCamera : UnityEngine.Camera.main;
                if (cam == null)
                {
                    yield return null;
                    continue;
                }

                float wait = UnityEngine.Random.Range(preset.minWaitSeconds, Mathf.Max(preset.minWaitSeconds, preset.maxWaitSeconds));
                if (wait > 0f) yield return WaitSeconds(wait);

                yield return RunOnePass(cam, preset);
            }
        }

        private IEnumerator RunOnePass(UnityEngine.Camera cam, CometPreset preset)
        {
            _emitAccumulator = 0f;

            Vector2 dir2 = preset.directionInCameraPlane;
            if (dir2.sqrMagnitude < 0.0001f) dir2 = Vector2.right;
            dir2.Normalize();

            float depth = Vector3.Dot(transform.position - cam.transform.position, cam.transform.forward);
            if (Mathf.Abs(depth) < 0.001f) depth = 10f;

            float margin = Mathf.Max(0f, preset.offscreenMarginViewport);
            float y = 0.5f + UnityEngine.Random.Range(-preset.yViewportJitter, preset.yViewportJitter);
            y = Mathf.Clamp01(y);

            // Start slightly offscreen on the left and travel until offscreen on the right (in viewport terms).
            Vector3 start = cam.ViewportToWorldPoint(new Vector3(-margin, y, depth));
            Vector3 end = cam.ViewportToWorldPoint(new Vector3(1f + margin, y, depth));

            // Convert camera-plane direction into world direction.
            Vector3 dirWorld = (cam.transform.right * dir2.x) + (cam.transform.up * dir2.y);
            dirWorld = dirWorld.sqrMagnitude < 0.0001f ? cam.transform.right : dirWorld.normalized;

            // If direction is diagonal, re-target end point along that direction until leaving right edge.
            // We keep end.x roughly to the right edge but let y drift naturally.
            float approxDistance = Vector3.Distance(start, end);
            if (approxDistance < 0.01f) approxDistance = 10f;
            Vector3 target = start + dirWorld * approxDistance;

            transform.position = start;

            // Emit while moving.
            float maxSeconds = 10f;
            float elapsed = 0f;
            while (elapsed < maxSeconds)
            {
                float dt = DeltaTime();
                elapsed += dt;

                transform.position += dirWorld * (preset.headSpeedUnitsPerSecond * dt);
                EmitAlongHead(dirWorld, cam, preset, dt);

                // Stop once we're off the right edge (or below bottom if moving down).
                Vector3 vp = cam.WorldToViewportPoint(transform.position);
                if (vp.x > 1f + margin || vp.y < -margin || vp.y > 1f + margin)
                    break;

                yield return null;
            }
        }

        private void EmitAlongHead(Vector3 dirWorld, UnityEngine.Camera cam, CometPreset preset, float dt)
        {
            if (_ps == null) return;

            float rate = Mathf.Max(0f, preset.particlesPerSecond);
            if (rate <= 0f) return;

            _emitAccumulator += rate * dt;
            int toEmit = Mathf.FloorToInt(_emitAccumulator);
            if (toEmit <= 0) return;
            _emitAccumulator -= toEmit;

            Vector3 lateral = Vector3.Cross(cam.transform.forward, dirWorld);
            if (lateral.sqrMagnitude < 0.0001f) lateral = cam.transform.up;
            lateral.Normalize();

            for (int i = 0; i < toEmit; i++)
            {
                float lateralOffset = UnityEngine.Random.Range(-preset.lateralRandomnessWorld, preset.lateralRandomnessWorld);
                Vector3 pos = transform.position + lateral * lateralOffset;

                float size = UnityEngine.Random.Range(
                    Mathf.Min(preset.startSizeRange.x, preset.startSizeRange.y),
                    Mathf.Max(preset.startSizeRange.x, preset.startSizeRange.y)
                );

                var emit = new ParticleSystem.EmitParams
                {
                    position = pos,
                    velocity = dirWorld * UnityEngine.Random.Range(preset.headSpeedUnitsPerSecond * 0.2f, preset.headSpeedUnitsPerSecond * 0.55f),
                    startLifetime = preset.particleLifetime,
                    startSize = size
                };

                _ps.Emit(emit, 1);
            }
        }

        private ParticleSystem.MinMaxGradient BuildColorGradient(CometPreset preset)
        {
            if (preset.colorOverLifetime != null)
                return new ParticleSystem.MinMaxGradient(preset.colorOverLifetime);

            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.55f, 0.9f, 1f, 1f), 0.25f),
                    new GradientColorKey(Color.white, 0.6f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.35f),
                    new GradientAlphaKey(0f, 1f),
                }
            );
            return new ParticleSystem.MinMaxGradient(g);
        }

        private CometPreset GetActivePreset()
        {
            if (presets == null || presets.Length == 0)
            {
                return new CometPreset
                {
                    headSpeedUnitsPerSecond = 10f,
                    directionInCameraPlane = Vector2.right,
                    offscreenMarginViewport = 0.15f,
                    yViewportJitter = 0.25f,
                    minWaitSeconds = 0.6f,
                    maxWaitSeconds = 2.4f,
                    particleLifetime = 0.9f,
                    particlesPerSecond = 60f,
                    lateralRandomnessWorld = 0.18f,
                    startSizeRange = new Vector2(0.06f, 0.14f),
                    colorOverLifetime = null,
                    rendererLengthScale = 3.5f,
                    rendererVelocityScale = 0.2f
                };
            }

            int idx = Mathf.Clamp(activePresetIndex, 0, presets.Length - 1);
            var p = presets[idx];
            if (p.headSpeedUnitsPerSecond <= 0f) p.headSpeedUnitsPerSecond = 10f;
            if (p.particleLifetime <= 0.01f) p.particleLifetime = 0.9f;
            if (p.maxWaitSeconds < p.minWaitSeconds) p.maxWaitSeconds = p.minWaitSeconds;
            if (p.startSizeRange == Vector2.zero) p.startSizeRange = new Vector2(0.06f, 0.14f);
            return p;
        }

        private float DeltaTime() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        private object WaitSeconds(float seconds) =>
            useUnscaledTime ? (object)new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
    }
}

