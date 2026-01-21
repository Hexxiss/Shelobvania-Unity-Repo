using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WeatherTrigger2D : MonoBehaviour
{
    [Tooltip("WeatherEffect ID to toggle when player enters this trigger. Leave empty to turn off all weather.")]
    public string effectId;

    [Tooltip("Tag used to identify the player object.")]
    public string playerTag = "Player";

    private void Reset()
    {
        // Make sure collider is a trigger by default
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (WeatherManager.Instance == null) return;

        WeatherManager.Instance.ToggleEffect(effectId);
    }
}
