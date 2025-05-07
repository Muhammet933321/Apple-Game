using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AppleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject applePrefab;
    
    public GameObject basketHealthy;
    public GameObject basketRotten;
    public GameObject originPos;

    public LevelManager levelManager;
    public float spawnInterval = 3f;
    private int maxAppleCount = 1;
    private int targetAppleCount = 10;
    
    public float defaultScale = 0.2f;

    public int rom = 60;
    public int holdDuration = 5;
    public int progress = 5;

    [Header("UI Settings")]
    public TextMeshProUGUI appleCounterText;

    public Slider slider;
    public TextMeshProUGUI LevelTitle; 
    public TextMeshProUGUI LevelInfo; 
    public TextMeshProUGUI playTypeText; 

    private float nextSpawnTime = 0f;
    
    private FirestoreAppointmentManager appointmentManager;

    private int maxX = 3;
    private int minX = 0;
    private int maxY = 3;
    private int minY = 0;
    private int maxZ = 3;
    private int minZ = 1;
    
    private int direction = 1; // 1 for right, -1 for left
    private int level1Index = 0;
    
    private int level2Index = 0; // For XZ Diagonal
    private int level2Direction = 1;

    private int level3Index = 0; // For XY Diagonal
    private int level3Direction = 1;

    private int level4Index = 0; // For XYZ Diagonal
    private int level4Direction = 1;
    
    public int calibrationIndex = 0; 

    private bool[,,] availableLocations;
    private List<Vector3Int> availableList;
    private List<GameObject> spawnedApples;

    private int index;

    private int interactedAppleCount = 0;  // NEW: number of apples player interacted with
    private int totalAppleCount = 0; 
    
    public enum SpawnLevel
    {
        Idle,
        Level1_XLine,
        Level2_XZDiagonal,
        Level3_XYDiagonal,
        Level4_3DDiagonal,
        Level5,
        Level6,
        Level7,
        Level8,
        Level9
    }
    
    private static readonly Dictionary<SpawnLevel, string> LevelNames = new Dictionary<SpawnLevel, string>
    {
        { SpawnLevel.Idle, "Kalibrasyon" },
        { SpawnLevel.Level1_XLine, "Yatay Toplar" },
        { SpawnLevel.Level2_XZDiagonal, "X-Z Çapraz Toplar" },
        { SpawnLevel.Level3_XYDiagonal, "X-Y Çapraz Toplar" },
        { SpawnLevel.Level4_3DDiagonal, "3D Çapraz Toplar" },
        { SpawnLevel.Level5, "Tut ve Bırak" },
        { SpawnLevel.Level6, "Tut ve Bırak" },
        { SpawnLevel.Level7, "Sepete Koy" },
        { SpawnLevel.Level8, "Sağlam ve Çürük" },
        { SpawnLevel.Level9, "Dinamik" }
    };

    public SpawnLevel spawnLevel;
    
    public static string GetLevelName(SpawnLevel level)
    {
        return LevelNames.TryGetValue(level, out string name) ? name : "Unknown Level";
    }
    private void Awake()
    {
        index = 0;
        int sizeX = maxX - minX + 1;
        int sizeY = maxY - minY + 1;
        int sizeZ = maxZ - minZ + 1;

        availableLocations = new bool[sizeX, sizeY, sizeZ];
        availableList = new List<Vector3Int>(sizeX * sizeY * sizeZ);
        spawnedApples = new List<GameObject>();
        LevelTitle.text = GetLevelName(spawnLevel);
        for (int x = 0; x <= maxX - minX; x++)
        {
            for (int y = 0; y <= maxY - minY; y++)
            {
                for (int z = 0; z <= maxZ - minZ; z++)
                {
                    availableLocations[x, y, z] = true;
                    availableList.Add(new Vector3Int(x + minX, y + minY, z + minZ));
                }
            }
        }

        UpdateAppleCounter();  // Initialize counter display
        
        nextSpawnTime = Time.time + spawnInterval;
        
        if(appointmentManager == null)
            appointmentManager = FindAnyObjectByType<FirestoreAppointmentManager>();
        if(appointmentManager.freePlay)
            playTypeText.text = "Serbest Oynama";
        else
        {
            playTypeText.text = "Randevulu Oynama";
        }
    }

    private void Update()
    {
        if(appointmentManager == null)
            appointmentManager = FindAnyObjectByType<FirestoreAppointmentManager>();
        if (interactedAppleCount >= targetAppleCount)
        {
            SetLevel(++spawnLevel);
        }
        
        if (Time.time >= nextSpawnTime)
        {
            if (spawnedApples.Count <= maxAppleCount && spawnLevel != SpawnLevel.Idle)
            {
                Debug.Log(spawnedApples.Count);
                SpawnApple();
            }
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void SetLevel(SpawnLevel _spawnLevel)
    {
        if ((int)spawnLevel == 10)
        {
            RestartGame();
        }
        ClearApples();
        spawnLevel = _spawnLevel;
        interactedAppleCount = 0;
        LevelTitle.text = "Level " + ((int)_spawnLevel).ToString();
        if (LevelNames.TryGetValue(spawnLevel, out string levelName))
        {
            LevelInfo.text = levelName;
        }
        else
        {
            LevelInfo.text = "Unknown Level";
        }
        UpdateAppleCounter();
        nextSpawnTime = Time.time + spawnInterval;
        if (spawnLevel < SpawnLevel.Level7)
        {
            basketHealthy.SetActive(false);
            basketRotten.SetActive(false);
        }
        else if (_spawnLevel == SpawnLevel.Level9)
        {
            appointmentManager.StartLevel9();
            basketHealthy.SetActive(true);
            basketRotten.SetActive(false);
        }
        else if (spawnLevel == SpawnLevel.Level7)
        {
            basketRotten.SetActive(false);
            basketHealthy.SetActive(true);
        }
        else if (spawnLevel == SpawnLevel.Level8)
        {
            basketHealthy.SetActive(true);
            basketRotten.SetActive(true);
        }
    }
    
    private Vector3Int Level2_XZDiagonalPositions()
    {
        int y = 1;
        int size = Mathf.Min(maxX - minX + 1, maxZ - minZ + 1);
        int i = Mathf.Clamp(level2Index, 0, size - 1);

        int x = minX + i;
        int z = minZ + i;
    
        Vector3Int gridPos = new Vector3Int(x, y, z);

        level2Index += level2Direction;
        if (level2Index >= size)
        {
            level2Index = size - 2;
            level2Direction = -1;
        }
        else if (level2Index < 0)
        {
            level2Index = 1;
            level2Direction = 1;
        }

        return gridPos;
    }
    
    private Vector3Int Level3_XYDiagonalPositions()
    {
        int z = 1;
        int size = Mathf.Min(maxX - minX + 1, maxY - minY + 1);
        int i = Mathf.Clamp(level3Index, 0, size - 1);

        int x = minX + i;
        int y = minY + i;

        Vector3Int gridPos = new Vector3Int(x, y, z);

        level3Index += level3Direction;
        if (level3Index >= size)
        {
            level3Index = size - 2;
            level3Direction = -1;
        }
        else if (level3Index < 0)
        {
            level3Index = 1;
            level3Direction = 1;
        }

        return gridPos;
    }
    
    private Vector3Int Level4_3DDiagonalPositions()
    {
        int size = Mathf.Min(maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);
        int i = Mathf.Clamp(level4Index, 0, size - 1);

        int x = minX + i;
        int y = minY + i;
        int z = minZ + i;

        Vector3Int gridPos = new Vector3Int(x, y, z);

        level4Index += level4Direction;
        if (level4Index >= size)
        {
            level4Index = size - 2;
            level4Direction = -1;
        }
        else if (level4Index < 0)
        {
            level4Index = 1;
            level4Direction = 1;
        }

        return gridPos;
    }
    
    private Vector3Int Level1Positions()
    {
        int y = 1, z = 1;
        int x = minX + level1Index;
    
        Vector3Int gridPos = new Vector3Int(x, y, z);
    
        level1Index += direction;
        if (level1Index > maxX - minX)
        {
            level1Index = maxX - minX - 1;
            direction = -1;
        }
        else if (index < 0)
        {
            level1Index = 1;
            direction = 1;
        }

        return gridPos;
    }
    
    public Vector3Int GetRandomAvailablePosition()
    {
        List<Vector3Int> filteredPositions = new List<Vector3Int>();
        Vector3Int chosen;
        foreach (var pos in availableList)
        {
            switch (spawnLevel)
            {
                case SpawnLevel.Level1_XLine:
                    return Level1Positions();
                case SpawnLevel.Level2_XZDiagonal:
                    return Level2_XZDiagonalPositions();
                case SpawnLevel.Level3_XYDiagonal:
                    return Level3_XYDiagonalPositions();
                case SpawnLevel.Level4_3DDiagonal:
                    return Level4_3DDiagonalPositions();
                default:
                    filteredPositions.Add(pos);
                    break;
            }
        }

        if (filteredPositions.Count == 0)
        {
            Debug.LogWarning("No available positions for this level!");
            return Vector3Int.zero;
        }

        int randomIndex = Random.Range(0, filteredPositions.Count);
        chosen = filteredPositions[randomIndex];
        return chosen;
    }

    Vector3 GridToPos(Vector3 chosen)
    {
        return new Vector3(chosen.x * defaultScale, chosen.y * defaultScale, chosen.z * defaultScale);
    }

    private void SpawnApple()
    {
        var gridPos = GetRandomAvailablePosition();  // Get both world offset and grid index
        
        Vector3 spawnPosition = GridToPos(gridPos);
        Vector3 origin = originPos.transform.position;
        Vector3 spawnWorldPosition = origin + spawnPosition;
        GameObject newApple = Instantiate(applePrefab, spawnWorldPosition, Quaternion.identity);
        newApple.transform.localScale = Vector3.zero;
        newApple.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 1f).SetEase(Ease.InCubic);
        spawnedApples.Add(newApple);

        // Directly use the grid position returned from GetRandomAvailablePosition
        OccupyPosition(gridPos.x, gridPos.y, gridPos.z);

        UpdateAppleCounter(); // Update UI when spawn
    }
    public GameObject SpawnAppleCalibration()
        {
            if (applePrefab == null)
            {
                Debug.LogError("Apple prefab not assigned!");
                return null;
            }
            if(index != 0)
                defaultScale *= 0.8f;

            rom = Mathf.RoundToInt(defaultScale * 3 * 100);
            
            Vector3 spawnPosition = GridToPos(new Vector3(3, 3, 3));
            Vector3 origin = originPos.transform.position;
            Vector3 spawnWorldPosition = origin + spawnPosition;
            GameObject newApple = Instantiate(applePrefab, spawnWorldPosition, Quaternion.identity);
            newApple.transform.localScale = Vector3.zero;
            newApple.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 1f).SetEase(Ease.InCubic);
            spawnedApples.Add(newApple);
            index++;
            Debug.Log(defaultScale + " " + index);
            return newApple;
        }

    public void OccupyPosition(int worldX, int worldY, int worldZ)
    {
        var (i, j, k) = WorldToGrid(worldX, worldY, worldZ);

        if (availableLocations[i, j, k])
        {
            availableLocations[i, j, k] = false;
            availableList.Remove(new Vector3Int(worldX, worldY, worldZ));
        }
    }

    private (int, int, int) WorldToGrid(int x, int y, int z)
    {
        return (x - minX, y - minY, z - minZ);
    }

    // Clears all apples and resets everything
    public void ClearApples()
    {
        foreach (GameObject apple in spawnedApples)
        {
            if (apple != null)
            {
                Destroy(apple);
            }
        }
        spawnedApples.Clear();
        interactedAppleCount = 0;  // Reset interaction count
        UpdateAppleCounter();
    }

    // Removes a specific apple without increasing interacted count
    public void RemoveApple(GameObject apple)
    {
        if (apple != null && spawnedApples.Contains(apple))
        {
            spawnedApples.Remove(apple);
            Destroy(apple);
            UpdateAppleCounter();
        }
        else
        {
            Debug.LogWarning("Tried to remove an apple that doesn't exist in the list!");
        }
    }

    // Interact with an apple: remove + increment interaction counter
    public void InteractApple(GameObject apple)
    {
        if (apple != null && spawnedApples.Contains(apple))
        {
            RemoveApple(apple);
            
            Debug.Log("Interacted apple");
            levelManager.AppleInteracted();

            interactedAppleCount++;  // Increase interaction counter
            totalAppleCount++;
            UpdateAppleCounter();
            GetComponent<AudioSource>().Play();
        }
        else
        {
            Debug.LogWarning("Tried to interact with an apple that doesn't exist in the list!");
        }
    }

    // --- UI Update ---
    private void UpdateAppleCounter()
    {
        if (appleCounterText != null)
        {
            appleCounterText.text = interactedAppleCount.ToString();
            slider.value = interactedAppleCount;
        }
    }

    public int GetProgress()
    {
        return Mathf.RoundToInt((float)totalAppleCount * 100f / 90f);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("LoginScene");
    }
    
    void OnApplicationQuit()
    {
        Debug.Log("Application is quitting...");
        // Optionally call any save/cleanup logic here
        appointmentManager.SendProgress();
    }
    
    void OnApplicationPause()
    {
        Debug.Log("Application is paused...");
        // Optionally call any save/cleanup logic here
        if(appointmentManager != null)
            appointmentManager.SendProgress();
    }
    
}