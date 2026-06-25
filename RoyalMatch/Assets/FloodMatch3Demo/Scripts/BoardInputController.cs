using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BoardInputController : MonoBehaviour
{
    public BoardManager board;
    public Camera targetCamera;

    [Header("Input")]
    public float minDragPixels = 20f;

    private Match3Tile pressedTile;
    private Vector2 pressScreenPosition;
    private Vector2 lastScreenPosition;
    private bool pointerWasDown;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (board == null)
            board = FindFirstObjectByType<BoardManager>();
    }

    private void Update()
    {
        if (board == null)
            return;

        // 입력 잠겼을 때 선택 표시가 남지 않게 정리
        if (board.inputLocked)
        {
            ClearPressedTile();
            pointerWasDown = IsPointerDown();
            return;
        }

        bool isDown = IsPointerDown();
        Vector2 screenPosition = GetPointerScreenPosition();

        // 터치/마우스를 누르고 있는 동안 마지막 위치 저장
        if (isDown)
        {
            lastScreenPosition = screenPosition;
        }
        // 손을 뗀 프레임에는 터치 위치가 이상하게 0으로 잡힐 수 있어서 마지막 위치 사용
        else if (pointerWasDown)
        {
            screenPosition = lastScreenPosition;
        }

        // 누르기 시작
        if (isDown && !pointerWasDown)
        {
            pressedTile = RaycastTile(screenPosition);
            pressScreenPosition = screenPosition;
            lastScreenPosition = screenPosition;

            if (pressedTile != null)
                pressedTile.SetSelected(true);
        }

        // 손 떼기
        if (!isDown && pointerWasDown)
        {
            if (pressedTile != null)
            {
                Vector2 screenDelta = screenPosition - pressScreenPosition;

                if (screenDelta.magnitude >= minDragPixels)
                {
                    Vector2Int direction = GetDirection(screenDelta);
                    board.TryMovePieceByDirection(pressedTile, direction);
                }

                pressedTile.SetSelected(false);
            }

            pressedTile = null;
        }

        pointerWasDown = isDown;
    }

    private void ClearPressedTile()
    {
        if (pressedTile != null)
        {
            pressedTile.SetSelected(false);
            pressedTile = null;
        }
    }

    private Match3Tile RaycastTile(Vector2 screenPosition)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return null;

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            return hit.collider.GetComponentInParent<Match3Tile>();

        return null;
    }

    private Vector2Int GetDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0 ? Vector2Int.right : Vector2Int.left;

        return delta.y >= 0 ? Vector2Int.up : Vector2Int.down;
    }

    private bool IsPointerDown()
    {
#if ENABLE_INPUT_SYSTEM
        // 모바일 터치를 먼저 확인해야 함
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return true;

        // PC / 에디터 마우스
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            return true;

        return false;

#elif ENABLE_LEGACY_INPUT_MANAGER
        // 모바일 터치를 먼저 확인
        if (Input.touchCount > 0)
            return true;

        return Input.GetMouseButton(0);
#else
        return false;
#endif
    }

    private Vector2 GetPointerScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        // 모바일 터치 위치 먼저 사용
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        // PC / 에디터 마우스 위치
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return lastScreenPosition;

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;

        return Input.mousePosition;
#else
        return lastScreenPosition;
#endif
    }
}