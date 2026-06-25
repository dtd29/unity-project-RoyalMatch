using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FloodGameManager : MonoBehaviour
{
    public static FloodGameManager Instance { get; private set; }

    public LevelData[] levels;
    public int startLevelIndex;

    public BoardManager board;
    public WaterController water;
    public FloodCharacterController person;
    public DrainController drain;
    public WaterKitBridge waterKitBridge;

    [Header("Optional UI")]
    public ClearPopupUI popup;
    public LevelSelectUI levelSelectUI;

    [Header("Deprecated / Hidden UI")]
    public Text timerText;
    public Text progressText;

    [Header("Clear Timing")]
    public float finalDrainEffectSeconds = 1.6f;

    [Header("Flood Speed")]
    public float floodSpeedMultiplier = 2.0f;

    private int currentLevelIndex;
    private float flood01;
    private bool running;
    private bool levelCleared;
    private bool drainPathOpened;

    public LevelData CurrentLevel => levels != null && levels.Length > 0 ? levels[currentLevelIndex] : null;

    private void Awake()
    {
        Instance = this;
        AutoFindReferences();
        EnsureLevels();
        RuntimeSafeUI.EnsureUI(this);
    }

    private void Start()
    {
        AutoFindReferences();
        EnsureLevels();
        RuntimeSafeUI.EnsureUI(this);

        if (levelSelectUI != null)
            levelSelectUI.Refresh();

        LoadLevel(Mathf.Clamp(startLevelIndex, 0, levels.Length - 1));
    }

    private void Update()
    {
        if (popup == null || levelSelectUI == null)
            RuntimeSafeUI.EnsureUI(this);

        if (!running || levelCleared || drainPathOpened || CurrentLevel == null)
            return;

        // 시간은 UI에 표시하지 않지만, 내부적으로는 물 상승 속도에 사용한다.
        // V11: 물이 너무 느리게 차오르던 문제 수정. 기본 2배 속도.
        flood01 += (Time.deltaTime / CurrentLevel.timeLimit) * floodSpeedMultiplier;
        flood01 = Mathf.Clamp01(flood01);

        if (water != null)
            water.SetFlood01(flood01);

        if (waterKitBridge != null)
            waterKitBridge.SetFlood01(flood01);

        if (person != null)
            person.SetAnxiety(flood01, CurrentLevel.anxietyWalkSpeedMultiplier);

        if (flood01 >= 1f)
            GameOver();
    }

    private void AutoFindReferences()
    {
        if (board == null)
            board = FindFirstObjectByType<BoardManager>();

        if (water == null)
            water = FindFirstObjectByType<WaterController>();

        if (person == null)
            person = FindFirstObjectByType<FloodCharacterController>();

        if (drain == null)
            drain = FindFirstObjectByType<DrainController>();

        if (waterKitBridge == null)
            waterKitBridge = FindFirstObjectByType<WaterKitBridge>();
    }

    private void EnsureLevels()
    {
        if (levels != null && levels.Length > 0)
            return;

        levels = new LevelData[5];

        int[] sizes = { 6, 6, 6, 7, 7 };
        float[] lower = { 0.038f, 0.034f, 0.031f, 0.028f, 0.025f };

        for (int i = 0; i < 5; i++)
        {
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.stageName = $"Stage {i + 1}";
            data.stageNumber = i + 1;
            data.width = sizes[i];
            data.height = sizes[i];
            // V19: 퍼즐 사이 빈틈을 줄이기 위해 spacing을 살짝 촘촘하게 조정
            data.pieceSpacing = i < 3 ? 0.64f : 0.57f;
            data.timeLimit = 60f;
            data.waterLowerPerClearedPiece = lower[i];
            data.anxietyWalkSpeedMultiplier = Mathf.Lerp(1.3f, 2.0f, i / 4f);
            levels[i] = data;
        }
    }

    public void LoadLevel(int levelIndex)
    {
        AutoFindReferences();
        EnsureLevels();
        RuntimeSafeUI.EnsureUI(this);

        levelIndex = Mathf.Clamp(levelIndex, 0, levels.Length - 1);

        if (!LevelProgress.IsUnlocked(levelIndex))
            return;

        currentLevelIndex = levelIndex;
        flood01 = 0f;
        running = true;
        levelCleared = false;
        drainPathOpened = false;

        if (popup != null)
            popup.Hide();

        if (levelSelectUI != null)
            levelSelectUI.Hide();

        if (water != null)
            water.ResetWater();

        if (waterKitBridge != null)
            waterKitBridge.ResetBridge();

        if (person != null)
            person.ResetPerson();

        if (drain != null)
            drain.ResetDrain();

        if (board != null)
            board.SpawnBoard(CurrentLevel);
        else
            Debug.LogError("FloodGameManager: BoardManager를 찾지 못했습니다.");
    }

    public void RetryCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    public void OnPiecesCleared(int count, List<Vector3> clearedPositions)
    {
        if (!running || levelCleared || drainPathOpened || CurrentLevel == null)
            return;

        flood01 -= count * CurrentLevel.waterLowerPerClearedPiece;
        flood01 = Mathf.Clamp01(flood01);

        if (water != null)
        {
            water.SetFlood01(flood01);
            water.PulseLowerWater();
        }

        if (waterKitBridge != null)
        {
            waterKitBridge.SetFlood01(flood01);

            if (clearedPositions != null)
            {
                foreach (Vector3 pos in clearedPositions)
                    waterKitBridge.SpawnFlowAtHole(pos);
            }
        }

        // V21: 기존 하늘색 파티클 이펙트 제거.
        // 물 표현은 WaterKitBridge가 담당한다.
    }

    public void OnBoardProgressChanged(int cleared, int total)
    {
        // V10: 시간 UI와 매치/진행도 UI는 삭제했다.
        // 필요하면 Console 확인용으로만 남긴다.
        Debug.Log($"FloodGameManager V30: 열린 칸 {cleared}/{total}");
    }

    public void OnDrainPathOpened(List<Vector2Int> boardPath, List<Vector3> worldPath)
    {
        if (!running || levelCleared || drainPathOpened)
            return;

        Debug.Log("FloodGameManager V30: 최종 배수구로 이어지는 길이 열렸습니다. 배수 연출 시작.");
        StartCoroutine(DrainPathClearRoutine(worldPath));
    }

    private IEnumerator DrainPathClearRoutine(List<Vector3> worldPath)
    {
        drainPathOpened = true;
        running = false;

        if (board != null)
            board.LockBoard(true);

        if (drain != null)
        {
            drain.SetInflowActive(false);
            drain.SetOpen(true);
        }

        // V21: 클리어 시 기존 하늘색 배수 파티클은 사용하지 않는다.
        // WaterKit의 물 덩어리/폭포 프리팹만 유지한다.
        if (waterKitBridge != null)
            waterKitBridge.SpawnFlowPath(worldPath);

        if (water != null)
            water.StartDrainAll(finalDrainEffectSeconds);

        yield return new WaitForSeconds(finalDrainEffectSeconds);

        Debug.Log("FloodGameManager V30: 배수 연출 종료. 클리어 팝업 표시.");
        ClearLevelPopupOnly();
    }

    private void ClearLevelPopupOnly()
    {
        levelCleared = true;

        LevelProgress.UnlockNext(currentLevelIndex, levels.Length);

        if (levelSelectUI != null)
            levelSelectUI.Refresh();

        if (popup != null)
            popup.ShowClear(currentLevelIndex + 1, currentLevelIndex + 1 < levels.Length);
    }

    private void GameOver()
    {
        Debug.Log("FloodGameManager V30: 물이 맵 끝까지 차올라 게임오버 처리.");
        running = false;

        if (board != null)
            board.LockBoard(true);

        if (drain != null)
            drain.SetInflowActive(false);

        if (person != null)
            person.SetGameOverExpression();

        RuntimeSafeUI.EnsureUI(this);

        if (popup != null)
            popup.ShowGameOver(currentLevelIndex + 1);
    }

    public void GoNextLevel()
    {
        int next = currentLevelIndex + 1;

        if (next >= levels.Length)
            ShowLevelSelect();
        else
            LoadLevel(next);
    }

    public void ShowLevelSelect()
    {
        RuntimeSafeUI.EnsureUI(this);

        if (popup != null)
            popup.Hide();

        if (levelSelectUI != null)
        {
            levelSelectUI.Refresh();
            levelSelectUI.Show();
        }
    }
}
