using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    public Transform[] backgroundLayers; // Assign your background layers here
    public float[] parallaxSpeeds; // Adjust speed for each layer
    private float[] startPositions;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        startPositions = new float[backgroundLayers.Length];
        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            startPositions[i] = backgroundLayers[i].position.x;
        }
    }

    void Update()
    {
        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            float distance = (mainCamera.transform.position.x * parallaxSpeeds[i]);
            backgroundLayers[i].position = new Vector3(startPositions[i] + distance, backgroundLayers[i].position.y, backgroundLayers[i].position.z);
        }
    }
}