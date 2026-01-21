using UnityEngine;
 // For Light2D

public class FlickeringLight : MonoBehaviour
{
    public UnityEngine.Rendering.Universal.Light2D targetLight; // Reference to the 2D Light component
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.0f;
    public float flickerSpeed = 0.1f; // How quickly the intensity changes

    private float timer;

    void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= flickerSpeed)
        {
            targetLight.intensity = Random.Range(minIntensity, maxIntensity);
            timer = 0f;
        }
    }
}