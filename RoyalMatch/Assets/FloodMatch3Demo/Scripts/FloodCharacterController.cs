using UnityEngine;

public class FloodCharacterController : MonoBehaviour
{
    [Header("Move Limit By X Coordinate")]
    public float minX = -1.5f;
    public float maxX = 1.5f;
    public float fixedY = 2.55f;
    public float fixedZ = -2.2f;
    public float baseMoveSpeed = 0.55f;
    public float turnSpeed = 8f;

    [Header("Animation")]
    [Tooltip("걷기 애니메이션이 기본 상태로 들어있는 Animator를 넣으세요.")]
    public Animator characterAnimator;

    [Tooltip("비워두면 Animator의 기본 상태를 그대로 사용합니다. 특정 걷기 State를 강제로 재생하려면 이름을 적으세요.")]
    public string walkStateName = "";

    [Tooltip("체크하면 Start 때 walkStateName 상태를 한 번 강제로 재생합니다.")]
    public bool forcePlayWalkOnStart = false;

    [Header("Model Rotation")]
    [Tooltip("FBX 모델 오브젝트를 여기에 넣으면 부모가 아니라 모델만 좌우 회전합니다.")]
    public Transform visualRoot;

    [Tooltip("Mixamo 모델 방향이 이상할 때 조절하세요. 보통 Y 0, 90, 180, -90 중 하나입니다.")]
    public Vector3 visualRotationOffsetEuler = Vector3.zero;

    [Header("X Only Movement")]
    public float targetReachDistance = 0.10f;

    [Header("Floating")]
    public WaterKitBridge waterKitBridge;
    public WaterController legacyWaterController;
    public bool floatOnWaterSurface = true;
    public float floatStartOffset = 0.4f;
    public float floatHeightOffset = 0.1f;
    public float floatFollowSpeed = 4.0f;
    public float floatBobAmount = 0.055f;
    public float floatBobSpeed = 5.4f;

    [Header("Visual - Old Capsule Expression")]
    public TextMesh expressionText;
    public Renderer bodyRenderer;
    public Material calmMaterial;
    public Material worriedMaterial;
    public Material panicMaterial;

    private float targetX;
    private float nextTargetTime;
    private float currentSpeedMultiplier = 1f;
    private float baseY;

    private float LeftX => Mathf.Min(minX, maxX);
    private float RightX => Mathf.Max(minX, maxX);

    private void Start()
    {
        FindReferences();
        PlayWalkAnimation();
        ResetPerson();
    }

    private void Update()
    {
        FindReferences();

        if (Time.time >= nextTargetTime || Mathf.Abs(transform.position.x - targetX) < targetReachDistance)
            PickNewTarget();

        Vector3 pos = transform.position;

        float dx = targetX - pos.x;
        float move = 0f;

        if (Mathf.Abs(dx) > 0.001f && currentSpeedMultiplier > 0f)
        {
            move = Mathf.Sign(dx) * baseMoveSpeed * currentSpeedMultiplier * Time.deltaTime;

            if (Mathf.Abs(move) > Mathf.Abs(dx))
                move = dx;
        }

        pos.x += move;
        pos.x = Mathf.Clamp(pos.x, LeftX, RightX);

        float desiredY = baseY;

        if (floatOnWaterSurface)
        {
            float waterTopY = GetWaterSurfaceY();

            if (waterTopY > baseY + floatStartOffset)
            {
                float bob = Mathf.Sin(Time.time * floatBobSpeed) * floatBobAmount;
                desiredY = waterTopY + floatHeightOffset + bob;
            }
        }

        pos.y = Mathf.Lerp(pos.y, desiredY, Time.deltaTime * floatFollowSpeed);
        pos.z = fixedZ;

        transform.position = pos;

        UpdateFacingDirection(dx);
    }

    public void ResetPerson()
    {
        baseY = fixedY;

        float startX = (LeftX + RightX) * 0.5f;
        transform.position = new Vector3(startX, fixedY, fixedZ);

        currentSpeedMultiplier = 1f;

        SetExpression(":|", calmMaterial);
        PickNewTarget();
        PlayWalkAnimation();
    }

    public void SetAnxiety(float water01, float maxSpeedMultiplier)
    {
        currentSpeedMultiplier = Mathf.Lerp(1f, maxSpeedMultiplier, water01);

        if (water01 < 0.35f)
            SetExpression(":|", calmMaterial);
        else if (water01 < 0.72f)
            SetExpression("o_o", worriedMaterial);
        else
            SetExpression("T_T", panicMaterial);
    }

    public void SetGameOverExpression()
    {
        SetExpression("X_X", panicMaterial);

        // 캐릭터 이동만 멈춤.
        // 애니메이션은 걷기 기본 상태를 계속 유지함.
        currentSpeedMultiplier = 0f;
    }

    private void PickNewTarget()
    {
        targetX = Random.Range(LeftX, RightX);
        nextTargetTime = Time.time + Random.Range(1f, 2.2f);
    }

    private void FindReferences()
    {
        if (waterKitBridge == null)
            waterKitBridge = FindFirstObjectByType<WaterKitBridge>();

        if (legacyWaterController == null)
            legacyWaterController = FindFirstObjectByType<WaterController>();

        if (characterAnimator == null && visualRoot != null)
            characterAnimator = visualRoot.GetComponentInChildren<Animator>();
    }

    private void PlayWalkAnimation()
    {
        if (characterAnimator == null)
            return;

        characterAnimator.enabled = true;

        if (forcePlayWalkOnStart && !string.IsNullOrEmpty(walkStateName))
            characterAnimator.Play(walkStateName, 0, 0f);
    }

    private float GetWaterSurfaceY()
    {
        if (waterKitBridge != null)
            return waterKitBridge.GetUpperWaterTopY();

        if (legacyWaterController != null)
            return legacyWaterController.CurrentTopY;

        return baseY - 999f;
    }

    private void UpdateFacingDirection(float dx)
    {
        if (Mathf.Abs(dx) <= 0.02f)
            return;

        Vector3 direction = dx > 0f ? Vector3.right : Vector3.left;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        targetRotation *= Quaternion.Euler(visualRotationOffsetEuler);

        Transform rotateTarget = visualRoot != null ? visualRoot : transform;

        rotateTarget.rotation = Quaternion.Slerp(
            rotateTarget.rotation,
            targetRotation,
            Time.deltaTime * turnSpeed
        );
    }

    private void SetExpression(string expression, Material material)
    {
        if (expressionText != null)
            expressionText.text = expression;

        // if (bodyRenderer != null && material != null)
        //     bodyRenderer.sharedMaterial = material;
    }
}