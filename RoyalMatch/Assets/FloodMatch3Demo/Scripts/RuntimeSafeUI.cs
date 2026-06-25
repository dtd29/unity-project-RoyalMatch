using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class RuntimeSafeUI
{
    public static void EnsureUI(FloodGameManager game)
    {
        if (game == null)
            return;

        bool missing =
            game.popup == null ||
            game.levelSelectUI == null;

        if (!missing)
            return;

        Debug.LogWarning("RuntimeSafeUI V10: 팝업/단계선택 UI가 비어 있어 UI Canvas를 새로 생성합니다.");

        EnsureEventSystem();

        Canvas canvas = CreateCanvas();

        CreateBottomGuide(canvas.transform);

        game.popup = CreatePopup(canvas.transform);
        game.levelSelectUI = CreateLevelSelect(canvas.transform);

        game.timerText = null;
        game.progressText = null;

        if (game.popup != null)
        {
            game.popup.BindButtons();
            game.popup.Hide();
        }

        if (game.levelSelectUI != null)
        {
            game.levelSelectUI.BindButtons();
            game.levelSelectUI.Hide();
            game.levelSelectUI.Refresh();
        }

        Debug.Log("RuntimeSafeUI V10: 하단 안내문, 다시하기 포함 팝업, 단계 선택 UI 생성 완료");
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
            return;

        GameObject eventSystemObj = new GameObject("Runtime EventSystem V10");
        eventSystemObj.AddComponent<EventSystem>();

        System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            Component inputModule = eventSystemObj.AddComponent(inputSystemModuleType);
            System.Reflection.MethodInfo assignDefaultActions =
                inputSystemModuleType.GetMethod(
                    "AssignDefaultActions",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic
                );

            if (assignDefaultActions != null)
                assignDefaultActions.Invoke(inputModule, null);
        }
        else
        {
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Main UI Canvas V10 Runtime Fallback");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private static Font GetFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static void CreateBottomGuide(Transform parent)
    {
        GameObject bgObj = new GameObject("UI Bottom Guide");
        bgObj.transform.SetParent(parent, false);

        RectTransform rect = bgObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 45);
        rect.sizeDelta = new Vector2(900, 72);

        Image image = bgObj.AddComponent<Image>();
        image.color = new Color(0.03f, 0.04f, 0.06f, 0.68f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bgObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = "빈칸을 가장자리에서 아래 배수구까지 연결하세요";
        text.font = GetFont();
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private static ClearPopupUI CreatePopup(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "UI Clear GameOver Popup", new Vector2(0.5f, 0.5f), new Vector2(780, 540));

        ClearPopupUI popup = panel.AddComponent<ClearPopupUI>();
        popup.root = panel;
        popup.titleText = CreateCenteredText(panel.transform, "Title", "Stage Clear!", new Vector2(0, 160), new Vector2(700, 90), 48);
        popup.bodyText = CreateCenteredText(panel.transform, "Body", "물이 모두 빠졌습니다.", new Vector2(0, 55), new Vector2(700, 110), 30);

        popup.levelSelectButton = CreateButton(panel.transform, "Level Select Button", "단계 선택", new Vector2(-170, -145), new Vector2(220, 90));
        popup.nextLevelButton = CreateButton(panel.transform, "Next Button", "다음 단계", new Vector2(90, -145), new Vector2(220, 90));
        popup.retryButton = CreateButton(panel.transform, "Retry Button", "다시하기", new Vector2(170, -145), new Vector2(220, 90));

        return popup;
    }

    private static LevelSelectUI CreateLevelSelect(Transform parent)
    {
        GameObject panel = CreatePanel(parent, "UI Level Select Panel", new Vector2(0.5f, 0.5f), new Vector2(860, 580));

        LevelSelectUI ui = panel.AddComponent<LevelSelectUI>();
        ui.root = panel;

        CreateCenteredText(panel.transform, "Title", "단계 선택", new Vector2(0, 200), new Vector2(700, 90), 48);

        ui.levelButtons = new Button[5];

        for (int i = 0; i < 5; i++)
        {
            float x = -300 + i * 150;
            ui.levelButtons[i] = CreateButton(panel.transform, "Stage " + (i + 1) + " Button", "Stage " + (i + 1), new Vector2(x, -35), new Vector2(130, 130));
        }

        return ui;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.04f, 0.06f, 0.09f, 0.94f);

        return panel;
    }

    private static Text CreateCenteredText(Transform parent, string name, string textValue, Vector2 pos, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.text = textValue;
        text.font = GetFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.45f, 0.9f, 1f);

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = GetFont();
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        return button;
    }
}
