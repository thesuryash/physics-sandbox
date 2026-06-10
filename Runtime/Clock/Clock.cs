using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class Clock : MonoBehaviour
{
    [Header("Initial State")]
    [SerializeField] private bool startPaused = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool logFixedUpdateOncePerSecond = true;

    private float lastTimeScale;
    private float fixedLogTimer;
    private float defaultFixedDeltaTime; // Automatically grabs your project's default step

    [Header("Live Editor Controls")]
    [Tooltip("Check/Uncheck this during Play Mode to instantly pause or play.")]
    [SerializeField] private bool togglePauseInEditor;

    // Runs BEFORE any scene objects Awake/OnEnable.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ForcePauseBeforeSceneLoad()
    {
        Time.timeScale = 0f;
        // Note: cannot read instance fields here; this is a global safety net.
        Debug.Log("[Clock] BeforeSceneLoad → FORCED timeScale=0");
    }

    private void Awake()
    {
        // Capture the default fixed timestep from Project Settings so we can scale it later
        defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (debugLogs)
            Debug.Log($"[Clock] Awake on '{gameObject.name}' (id={GetInstanceID()}) startPaused={startPaused} (timeScale currently {Time.timeScale})");

        // Apply the intended initial state
        SetSpeed(startPaused ? 0f : 1f);

        if (debugLogs)
            Debug.Log($"[Clock] Awake end on '{gameObject.name}' (id={GetInstanceID()}) → timeScale={Time.timeScale}");
    }

    // This runs automatically whenever you change a value in the Inspector
    private void OnValidate()
    {
        // We only want this to work while the game is actually running
        if (Application.isPlaying)
        {
            if (togglePauseInEditor && Time.timeScale > 0f)
            {
                Pause();
            }
            else if (!togglePauseInEditor && Time.timeScale == 0f)
            {
                Play();
            }
        }
    }

    private void OnEnable()
    {
        if (debugLogs)
            Debug.Log($"[Clock] OnEnable on '{gameObject.name}' (id={GetInstanceID()}) startPaused={startPaused} (timeScale currently {Time.timeScale})");

        // Re-assert (helps if something changes it between Awake and first frame).
        if (startPaused) SetSpeed(0f);
    }

    private void Start()
    {
        // The "Last Word": Re-assert the pause after all other scripts have run their Awakes/OnEnables.
        // This is highly likely to catch whatever was stealing your timeScale.
        if (startPaused)
        {
            SetSpeed(0f);
            if (debugLogs) Debug.Log($"[Clock] Start re-enforced timeScale=0");
        }
    }

    private void Update()
    {
        // Detect external changes
        if (!Mathf.Approximately(Time.timeScale, lastTimeScale))
        {
            Debug.LogError(
                $"[Clock] EXTERNAL CHANGE detected on '{gameObject.name}' (id={GetInstanceID()}): " +
                $"{lastTimeScale} → {Time.timeScale}\nStack:\n{System.Environment.StackTrace}"
            );

            // Update lastTimeScale so it doesn't spam the console every frame
            lastTimeScale = Time.timeScale;
        }
    }

    private void FixedUpdate()
    {
        if (!debugLogs) return;

        if (!logFixedUpdateOncePerSecond)
        {
            Debug.Log($"[Clock] FixedUpdate on '{gameObject.name}' (id={GetInstanceID()}) → timeScale={Time.timeScale}, fixedDeltaTime={Time.fixedDeltaTime}");
            return;
        }

        fixedLogTimer += Time.unscaledDeltaTime; // unscaled so it works even if paused
        if (fixedLogTimer >= 1f)
        {
            fixedLogTimer = 0f;
            Debug.Log($"[Clock] FixedUpdate (1s) on '{gameObject.name}' (id={GetInstanceID()}) → timeScale={Time.timeScale}, fixedDeltaTime={Time.fixedDeltaTime}");
        }
    }

    // Manual testing buttons (call from UI / inspector)
    [ContextMenu("⏸️ Force Pause")]
    public void Pause()
    {
        SetSpeed(0f);
        if (debugLogs) Debug.Log($"[Clock] Pause() on '{gameObject.name}' (id={GetInstanceID()}) → timeScale=0");
    }

    [ContextMenu("▶️ Force Play")]
    public void Play()
    {
        SetSpeed(1f);
        if (debugLogs) Debug.Log($"[Clock] Play() on '{gameObject.name}' (id={GetInstanceID()}) → timeScale=1");
    }

    public void SetSpeed(float speed)
    {
        Time.timeScale = speed;
        lastTimeScale = speed;

        // Scale fixedDeltaTime for smooth physics in slow-mo/fast-forward.
        // If paused (speed == 0), keep the default step to avoid 0 timestep engine errors.
        Time.fixedDeltaTime = defaultFixedDeltaTime * (speed > 0f ? speed : 1f);

        if (debugLogs) Debug.Log($"[Clock] SetSpeed({speed}) on '{gameObject.name}' (id={GetInstanceID()}) → fixedDeltaTime={Time.fixedDeltaTime}");
    }
}