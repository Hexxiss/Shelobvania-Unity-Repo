using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuSpriteController : MonoBehaviour
{
    [Header("Menu Items (top-to-bottom order)")]
    [SerializeField] private MenuSpriteItem[] items;

    [Header("Selection Scale")]
    [SerializeField] private float selectedScaleMultiplier = 1.08f;
    [SerializeField] private float scaleLerpSpeed = 14f;

    [Header("Input")]
    [SerializeField] private string verticalAxisName = "Vertical";
    [SerializeField] private string submitButtonName = "Submit";
    [SerializeField] private float moveRepeatDelay = 0.18f;

    private int currentIndex = 0;
    private float nextMoveTime = 0f;
    private int lastAxisSign = 0;

    private void Awake()
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogError("MainMenuSpriteController: No menu items assigned.");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Initialize selection state
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) items[i].SetSelected(i == currentIndex);
        }

        // Snap initial scales
        ApplyScales(instant: true);
    }

    private void Update()
    {
        HandleMoveInput();
        HandleSubmitInput();

        ApplyScales(instant: false);

        // Update glow pulse with unscaled time so it keeps animating even if timescale changes
        float t = Time.unscaledTime;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) items[i].UpdateGlowVisual(t);
        }
    }

    private void HandleMoveInput()
    {
        float v = Input.GetAxisRaw(verticalAxisName);
        int sign = 0;

        if (v > 0.5f) sign = 1;         // Up
        else if (v < -0.5f) sign = -1;  // Down

        bool pressedThisFrame = (sign != 0 && lastAxisSign == 0);
        bool heldRepeatReady = (sign != 0 && Time.unscaledTime >= nextMoveTime);

        if ((pressedThisFrame || heldRepeatReady) && sign != 0)
        {
            MoveSelection(sign > 0 ? -1 : +1);
            nextMoveTime = Time.unscaledTime + moveRepeatDelay;
        }

        if (sign == 0 && lastAxisSign != 0)
        {
            nextMoveTime = 0f;
        }

        lastAxisSign = sign;
    }

    private void HandleSubmitInput()
    {
        bool submit =
            Input.GetButtonDown(submitButtonName) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter);

        if (submit)
            ActivateCurrent();
    }

    private void MoveSelection(int delta)
    {
        int prevIndex = currentIndex;

        int next = currentIndex + delta;
        if (next < 0) next = items.Length - 1;
        if (next >= items.Length) next = 0;

        currentIndex = next;

        // Toggle glow selection
        if (items[prevIndex] != null) items[prevIndex].SetSelected(false);
        if (items[currentIndex] != null) items[currentIndex].SetSelected(true);
    }

    private void ActivateCurrent()
    {
        var item = items[currentIndex];

        if (item == null)
        {
            Debug.LogError($"MainMenuSpriteController: Item at index {currentIndex} is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(item.sceneName))
        {
            Debug.LogError($"MainMenuSpriteController: sceneName is empty on {item.name}.");
            return;
        }

        SceneManager.LoadScene(item.sceneName);
    }

    private void ApplyScales(bool instant)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;

            Vector3 target = (i == currentIndex)
                ? items[i].baseScale * selectedScaleMultiplier
                : items[i].baseScale;

            if (instant)
            {
                items[i].transform.localScale = target;
            }
            else
            {
                items[i].transform.localScale = Vector3.Lerp(
                    items[i].transform.localScale,
                    target,
                    Time.unscaledDeltaTime * scaleLerpSpeed
                );
            }
        }
    }
}
