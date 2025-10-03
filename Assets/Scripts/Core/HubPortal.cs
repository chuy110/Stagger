using UnityEngine;

public class HubPortal : MonoBehaviour {
    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
            GameLoop.I.EnterArena();
    }
}
