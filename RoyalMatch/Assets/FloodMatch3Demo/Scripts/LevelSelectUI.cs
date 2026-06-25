using UnityEngine;
using UnityEngine.UI;

public class LevelSelectUI : MonoBehaviour
{
    public GameObject root;
    public Button[] levelButtons;

    private bool bound;

    private void Start()
    {
        BindButtons();
    }

    public void BindButtons()
    {
        if (bound)
            return;

        if (levelButtons == null)
            return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int index = i;

            if (levelButtons[i] == null)
                continue;

            levelButtons[i].onClick.RemoveAllListeners();
            levelButtons[i].onClick.AddListener(() => FloodGameManager.Instance?.LoadLevel(index));
        }

        bound = true;
    }

    public void Show()
    {
        BindButtons();

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (levelButtons == null)
            return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button button = levelButtons[i];
            if (button == null)
                continue;

            bool unlocked = LevelProgress.IsUnlocked(i);
            button.interactable = unlocked;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = unlocked ? $"Stage {i + 1}" : $"Stage {i + 1}\nLOCK";
        }
    }
}
