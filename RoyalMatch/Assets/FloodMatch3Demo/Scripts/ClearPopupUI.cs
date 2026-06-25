using UnityEngine;
using UnityEngine.UI;

public class ClearPopupUI : MonoBehaviour
{
    public GameObject root;
    public Text titleText;
    public Text bodyText;
    public Button levelSelectButton;
    public Button nextLevelButton;
    public Button retryButton;

    private bool bound;

    private void Start()
    {
        BindButtons();
    }

    public void BindButtons()
    {
        if (bound)
            return;

        if (levelSelectButton != null)
        {
            levelSelectButton.onClick.RemoveAllListeners();
            levelSelectButton.onClick.AddListener(() => FloodGameManager.Instance?.ShowLevelSelect());
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(() => FloodGameManager.Instance?.GoNextLevel());
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => FloodGameManager.Instance?.RetryCurrentLevel());
        }

        bound = true;
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void ShowClear(int stageNumber, bool hasNext)
    {
        BindButtons();

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = $"Stage {stageNumber} Clear!";

        if (bodyText != null)
            bodyText.text = "최종 배수구로 이어지는 길이 열려 물이 빠져나갔습니다.";

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(hasNext);

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);
    }

    public void ShowGameOver(int stageNumber)
    {
        BindButtons();

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = $"Stage {stageNumber} Game Over";

        if (bodyText != null)
            bodyText.text = "물이 사람 공간까지 차올랐습니다.";

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);

        if (retryButton != null)
            retryButton.gameObject.SetActive(true);
    }
}
