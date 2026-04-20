using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SmartLevelGenerator : NetworkBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private float mapWidth = 40f;
    [SerializeField] private float mapHeight = 40f;

    [Header("Obstacle Settings")]
    [SerializeField] private int obstacleCount = 30;
    [SerializeField] private float minObstacleSpacing = 4f;
    [SerializeField] private Vector2 obstacleWidthRange = new Vector2(1f, 3f);
    [SerializeField] private Vector2 obstacleHeightRange = new Vector2(2f, 4f);

    [Header("Prefabs (Local Objects)")]
    [SerializeField] private GameObject redWallPrefab;
    [SerializeField] private GameObject blueWallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject borderWallPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform redSpawnPoint;
    [SerializeField] private Transform blueSpawnPoint;

    // The seed is synced so clients generate the same map as the server
    public NetworkVariable<int> levelSeed = new NetworkVariable<int>(0);
    private bool isMapGenerated;

    public static bool isRed(ulong clientId)
    {
        return clientId % 2 == 0;
    }

    public override void OnNetworkSpawn()
    {
        levelSeed.OnValueChanged += OnSeedChanged;

        if (IsServer)
        {
            // Server picks a random seed
            levelSeed.Value = UnityEngine.Random.Range(1000, 9999);
        }
        else if (levelSeed.Value != 0)
        {
            // If client joins late and seed is already set
            GenerateMap(levelSeed.Value);
        }
    }

    private void OnSeedChanged(int oldSeed, int newSeed)
    {
        if (!isMapGenerated && newSeed != 0)
        {
            GenerateMap(newSeed);
        }
    }

    private void GenerateMap(int seed)
    {
        Debug.Log($"Generating procedural map with seed: {seed}");
        UnityEngine.Random.InitState(seed);

        SpawnFloor();
        SpawnBorders();
        SpawnObstacles();

        isMapGenerated = true;
    }

    private void SpawnFloor()
    {
        GameObject floor = Instantiate(floorPrefab, new Vector3(mapWidth * 0.5f, 0f, mapHeight * 0.5f), Quaternion.identity);
        floor.transform.localScale = new Vector3(mapWidth, 0.2f, mapHeight);
        floor.transform.parent = transform;
    }

    private void SpawnBorders()
    {
        float wallHeight = 5f;
        float yPos = wallHeight * 0.5f;
        float thickness = 1f;
        float centerX = mapWidth * 0.5f;
        float centerZ = mapHeight * 0.5f;

        // Top, Bottom, Left, Right walls
        SpawnWall(new Vector3(centerX, yPos, 0f), new Vector3(mapWidth + thickness * 2f, wallHeight, thickness));
        SpawnWall(new Vector3(centerX, yPos, mapHeight), new Vector3(mapWidth + thickness * 2f, wallHeight, thickness));
        SpawnWall(new Vector3(0f, yPos, centerZ), new Vector3(thickness, wallHeight, mapHeight));
        SpawnWall(new Vector3(mapWidth, yPos, centerZ), new Vector3(thickness, wallHeight, mapHeight));
    }

    private void SpawnObstacles()
    {
        float spawnSafeZone = 5f;
        Vector3 redSpawn = GetSpawnPosition(0); // Client 0
        Vector3 blueSpawn = GetSpawnPosition(1); // Client 1
        
        List<Vector3> spawnedPositions = new List<Vector3>();
        int attempts = 0;

        while (spawnedPositions.Count < obstacleCount && attempts < obstacleCount * 20)
        {
            attempts++;
            float edgeMargin = 2f;
            float x = UnityEngine.Random.Range(edgeMargin, mapWidth - edgeMargin);
            float z = UnityEngine.Random.Range(edgeMargin, mapHeight - edgeMargin);
            
            float w = UnityEngine.Random.Range(obstacleWidthRange.x, obstacleWidthRange.y);
            float h = UnityEngine.Random.Range(obstacleHeightRange.x, obstacleHeightRange.y);
            
            Vector3 pos = new Vector3(x, h * 0.5f, z);

            bool tooClose = false;
            foreach (Vector3 p in spawnedPositions)
            {
                if (Vector3.Distance(pos, p) < minObstacleSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            // Ensure we don't block spawn points
            if (!tooClose && Vector3.Distance(pos, redSpawn) > spawnSafeZone && Vector3.Distance(pos, blueSpawn) > spawnSafeZone)
            {
                GameObject prefab = (UnityEngine.Random.value > 0.5f) ? redWallPrefab : blueWallPrefab;
                GameObject obs = Instantiate(prefab, pos, Quaternion.identity);
                obs.transform.localScale = new Vector3(w, h, w);
                obs.transform.parent = transform;
                spawnedPositions.Add(pos);
            }
        }
    }

    private void SpawnWall(Vector3 pos, Vector3 scale)
    {
        GameObject wall = Instantiate(borderWallPrefab, pos, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.transform.parent = transform;
    }

    public Vector3 GetSpawnPosition(ulong clientId)
    {
        if (isRed(clientId))
        {
            return (redSpawnPoint != null) ? redSpawnPoint.position : new Vector3(5f, 2f, 5f);
        }
        else
        {
            return (blueSpawnPoint != null) ? blueSpawnPoint.position : new Vector3(35f, 2f, 35f);
        }
    }
}