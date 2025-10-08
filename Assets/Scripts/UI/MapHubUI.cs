using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class MapHubUI : MonoBehaviour {
    public static MapHubUI I { get; private set; }

    [Header("Structure")]
    public GameObject mapCanvasRoot;   // assign MapCanvas (or MapRoot)
    public RectTransform maskToReveal; // assign MapRoot (has RectMask2D)
    public Transform nodesRoot;        // assign Nodes
    public string sanctumNodeId = "sanctum";

    [Header("Behavior")]
    public float unscrollDuration = 0.8f;
    public KeyCode toggleKey = KeyCode.M;

    MapNode[] _nodes;
    bool _firstOpenDone;
    bool _open;
    string _currentNodeId = "sanctum";

    void Awake() { I = this; }

    void Start() {
        _nodes = nodesRoot.GetComponentsInChildren<MapNode>(true);
        SetOpen(false, immediate:true);
        MarkCurrent(_currentNodeId);
    }

    void Update() {
        if (Input.GetKeyDown(toggleKey)) Toggle();
        if (_open && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
    }

    public void Toggle() => SetOpen(!_open);

    public void SetOpen(bool open, bool immediate = false) {
        _open = open;
        mapCanvasRoot.SetActive(open);

        Time.timeScale = open ? 0f : 1f; // pause while map open

        if (open && !_firstOpenDone && !immediate) {
            _firstOpenDone = true;
            StartCoroutine(UnscrollCR());
        }
    }

    IEnumerator UnscrollCR() {
        // Reveal by animating mask height from 0 to full
        var rt = maskToReveal;
        if (!rt) yield break;
        float t = 0f;
        float fullH = rt.rect.height;
        while (t < unscrollDuration) {
            t += Time.unscaledDeltaTime;
            float h = Mathf.Lerp(0f, fullH, t / unscrollDuration);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            yield return null;
        }
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fullH);
    }

    public void RequestTeleport(MapNode target) {
        SetOpen(false);
        Time.timeScale = 1f;
        _currentNodeId = target.nodeId;
        MarkCurrent(_currentNodeId);
        GameLoop.I.EnterArena(target.sceneName); // works for sanctum or arenas
    }

    void MarkCurrent(string nodeId) {
        foreach (var n in _nodes) n.MarkCurrent(n.nodeId == nodeId);
    }

    // Called by GameLoop when a boss is defeated
    public void UnlockNode(string nodeId) {
        var node = _nodes.FirstOrDefault(n => n.nodeId == nodeId);
        if (node) node.UnlockAndSave();
    }
}
