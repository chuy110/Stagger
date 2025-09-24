using UnityEngine;

public class BossAttackMarker : MonoBehaviour {
    public System.Action onParried;
    public void OnParried() => onParried?.Invoke();
}