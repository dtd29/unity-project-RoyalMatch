using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Flood Match 3/Level Data")]
public class LevelData : ScriptableObject
{
    public string stageName = "Stage 1";
    [Range(1, 5)] public int stageNumber = 1;

    [Header("Board")]
    [Min(3)] public int width = 6;
    [Min(3)] public int height = 6;
    [Min(0.4f)] public float pieceSpacing = 0.72f;

    [Header("Manual Layout")]
    public bool useManualLayout = true;

    [TextArea(6, 12)]
    public string manualLayout =
        "RYBRYB\n" +
        "YBRYBR\n" +
        "BRYBRY\n" +
        "RYBRYB\n" +
        "YBRYBR\n" +
        "BRYBRY";

    [Header("Flood")]
    [Min(5f)] public float timeLimit = 60f;
    [Min(0f)] public float waterLowerPerClearedPiece = 0.032f;

    [Header("Character")]
    public float anxietyWalkSpeedMultiplier = 1.6f;
}