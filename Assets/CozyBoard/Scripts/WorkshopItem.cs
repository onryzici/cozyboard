using UnityEngine;
namespace CozyBoard {
    [DisallowMultipleComponent]
    public sealed class WorkshopItem : MonoBehaviour {
        public string Id, Label, Kind;
        public int Stage;
        public Vector3 Slot, BoundsCenter, BoundsSize, InitialPosition;
        public float InitialYaw;
        public bool InitiallyFitted, Fitted;
        [HideInInspector] public MeshRenderer Visual;
        [HideInInspector] public BoxCollider Hitbox;
        public void Cache() {
            Visual = GetComponentInChildren<MeshRenderer>(true);
            Hitbox = GetComponent<BoxCollider>();
        }
    }
}
