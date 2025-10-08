using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour {
    public string nodeId;          // e.g., "sanctum", "boss1"
    public string sceneName;       // scene to load
    public bool unlockedOnStart;   // first node (sanctum) true, others false

    [Header("Visuals")]
    public Image icon;
    public Color lockedColor = new Color(0.4f,0.4f,0.4f,0.6f);
    public Color unlockedColor = Color.white;
    public Color currentColor = new Color(1f,0.9f,0.3f,1f);

    Button _btn;
    bool _unlocked;

    void Awake() {
        _btn = GetComponent<Button>();
        if (!icon) icon = GetComponent<Image>();
    }

    void Start() {
        bool saved = PlayerPrefs.GetInt("node_" + nodeId, unlockedOnStart ? 1 : 0) == 1;
        SetUnlocked(saved);
        _btn.onClick.AddListener(() => {
            if (_unlocked) MapHubUI.I.RequestTeleport(this);
        });
    }

    public void SetUnlocked(bool v) {
        _unlocked = v;
        _btn.interactable = v;
        if (icon) icon.color = v ? unlockedColor : lockedColor;
    }

    public void MarkCurrent(bool isCurrent) {
        if (!icon) return;
        icon.color = isCurrent ? currentColor : (_unlocked ? unlockedColor : lockedColor);
    }

    public void UnlockAndSave() {
        PlayerPrefs.SetInt("node_" + nodeId, 1);
        SetUnlocked(true);
    }
}