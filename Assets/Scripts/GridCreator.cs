using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates a grid of prefabs using weighted spawn chances.
/// Grid size, spacing, origin, and parenting are fully configurable.
/// </summary>
public class GridCreator : MonoBehaviour
{
    [System.Serializable]
    private struct WeightedPrefab
    {
        public GameObject prefab;

        [Min(0f)]
        [Tooltip("Relative chance. Example: 70 and 30 means 70% / 30%.")]
        public float weight;
    }

    [Header("Prefabs (weighted chance)")]
    [Tooltip("Each prefab is selected based on its weight.")]
    [SerializeField] private WeightedPrefab[] prefabs;

    [Header("Grid Settings")]
    [Min(1)]
    [SerializeField] private int rowCount = 5;

    [Min(1)]
    [SerializeField] private int columnCount = 10;

    [Header("Spacing (World Units)")]
    [SerializeField] private Vector2 spacing = new Vector2(1f, 1f);

    [Header("Placement")]
    [Tooltip("Optional parent for instantiated prefabs")]
    [SerializeField] private Transform parent;

    [Tooltip("Grid origin point (top-left by default)")]
    [SerializeField] private Vector3 origin = Vector3.zero;

    [Tooltip("Centers the grid around the origin point")]
    [SerializeField] private bool centerGridOnOrigin = true;

    [Tooltip("If true, row 0 starts at the top and grows downward")]
    [SerializeField] private bool topToBottom = true;

    [Header("Lifecycle")]
    [SerializeField] private bool spawnOnStart = true;

    [SerializeField] private bool clearPreviousOnSpawn = true;

    // Cached references to spawned instances
    private readonly List<GameObject> _spawnedObjects = new();

    private void Start()
    {
        if (spawnOnStart)
            SpawnGrid();
    }

    /// <summary>
    /// Instantiates the prefab grid using the current configuration.
    /// </summary>
    [ContextMenu("Spawn Grid")]
    public void SpawnGrid()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError($"{name}: No prefabs assigned.");
            return;
        }

        if (clearPreviousOnSpawn)
            ClearGrid();

        // Offset used to center the grid around the origin
        Vector3 centeringOffset = Vector3.zero;

        if (centerGridOnOrigin)
        {
            float width = (columnCount - 1) * spacing.x;
            float height = (rowCount - 1) * spacing.y;
            float yDirection = topToBottom ? -1f : 1f;

            centeringOffset = new Vector3(
                -width * 0.5f,
                yDirection * height * 0.5f,
                0f
            );
        }

        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                GameObject prefab = GetRandomPrefabByWeight();

                if (prefab == null)
                    continue;

                float yDirection = topToBottom ? -1f : 1f;
                Vector3 position = origin + centeringOffset +
                                   new Vector3(column * spacing.x, row * spacing.y * yDirection, 0f);

                Transform targetParent = parent != null ? parent : transform;
                GameObject instance = Instantiate(prefab, position, Quaternion.identity, targetParent);
                _spawnedObjects.Add(instance);
            }
        }
    }

    private GameObject GetRandomPrefabByWeight()
    {
        float totalWeight = 0f;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i].prefab == null || prefabs[i].weight <= 0f)
                continue;

            totalWeight += prefabs[i].weight;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogError($"{name}: All prefab weights are zero/invalid or prefabs are missing.");
            return null;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i].prefab == null || prefabs[i].weight <= 0f)
                continue;

            cumulative += prefabs[i].weight;

            if (randomValue <= cumulative)
                return prefabs[i].prefab;
        }

        // Fallback due to float precision.
        for (int i = prefabs.Length - 1; i >= 0; i--)
        {
            if (prefabs[i].prefab != null && prefabs[i].weight > 0f)
                return prefabs[i].prefab;
        }

        return null;
    }

    /// <summary>
    /// Destroys all previously spawned prefabs.
    /// </summary>
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (_spawnedObjects[i] == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_spawnedObjects[i]);
            else
                Destroy(_spawnedObjects[i]);
#else
            Destroy(_spawnedObjects[i]);
#endif
        }

        _spawnedObjects.Clear();
    }
}
