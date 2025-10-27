using UnityEngine;
using UnityEngine.InputSystem;

public class InputProbe : MonoBehaviour {
    PlayerInput pi;
    Vector2 move;
    void Awake() {
        pi = GetComponent<PlayerInput>();
        Debug.Log("Probe: current map = " + (pi.currentActionMap != null ? pi.currentActionMap.name : "<null>"));
    }
    public void OnMove(InputValue v) {
        move = v.Get<Vector2>();
        Debug.Log("Probe: OnMove " + move);
    }
}