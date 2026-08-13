using UnityEngine;

/// Attach to both objects in a suspect pair. Remove after diagnosis.
public class CollisionDebugger : MonoBehaviour
{
    void Awake()
    {
        var col = GetComponent<Collider>();
        var rb  = GetComponent<Rigidbody>();
        Debug.Log(
            $"[Setup] {name} | layer={gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)}) " +
            $"| collider={(col != null ? col.GetType().Name : "none")} " +
            $"| isTrigger={(col != null ? col.isTrigger.ToString() : "n/a")} " +
            $"| rigidbody={(rb != null ? (rb.isKinematic ? "Kinematic" : "Dynamic") : "none")} " +
            $"| active={gameObject.activeInHierarchy}",
            this);
    }

    void OnCollisionEnter(Collision col)
        => Debug.Log(
            $"[Collision] {name} hit {col.gameObject.name} | contacts: {col.contactCount} | impulse: {col.impulse.magnitude:F3}",
            this);

    void OnTriggerEnter(Collider other)
        => Debug.Log(
            $"[Trigger] {name} entered by {other.gameObject.name} | other layer: {other.gameObject.layer}",
            this);

    void OnCollisionEnter2D(Collision2D col)
        => Debug.Log(
            $"[2D Collision] {name} hit {col.gameObject.name}. This object is using 2D physics callbacks, not 3D.",
            this);
}
