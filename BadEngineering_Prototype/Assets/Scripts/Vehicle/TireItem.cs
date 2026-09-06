using BadEngineering.Interaction;
using BadEngineering.Player;
using BadEngineering.Weapons;
using UnityEngine;

namespace BadEngineering.Vehicle
{
    /// <summary>Worldでは1輪、装着時は車両全体のタイヤ種類を交換するItem。</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TireItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private TireDefinition definition;
        public TireDefinition Definition => definition;
        public PlayerWeaponSlots Carrier { get; private set; }
        private Rigidbody body;
        private PhysicsMaterial contactMaterial;
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = definition != null ? definition.Mass : 18f;
            PrototypeCollision.SetLayer(gameObject, PrototypeCollision.TireItem);
            contactMaterial = new PhysicsMaterial("Tire Item Contact");
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.sharedMaterial = contactMaterial;
        }
        private void OnCollisionStay(Collision collision)
        {
            if (Carrier != null || definition == null || collision.contactCount == 0) return;
            float friction = definition.FrictionForNormal(collision.GetContact(0).normal, transform.right);
            contactMaterial.dynamicFriction = friction;
            contactMaterial.staticFriction = friction;
        }
        private void OnDestroy()
        {
            Carrier?.RemoveItem(this);
            if (contactMaterial != null) Destroy(contactMaterial);
        }
        public bool CanInteract(GameObject user) => Carrier == null && user.GetComponent<PlayerWeaponSlots>() != null;
        public bool TryInteract(GameObject user) => CanInteract(user) && user.GetComponent<PlayerWeaponSlots>().AddPart(this);
        public void Hold(PlayerWeaponSlots carrier)
        {
            Carrier = carrier;
            body.isKinematic = true;
            body.detectCollisions = false;
            Transform root = carrier.GetComponent<WeaponHost>().WeaponAttachRoot;
            transform.SetParent(root, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
        public void SetSelected(bool selected)
        {
            foreach (Renderer visual in GetComponentsInChildren<Renderer>(true)) visual.enabled = selected;
        }
        public void ApplyReplica(PlayerWeaponSlots carrier, TireDefinition tire)
        {
            if (definition != tire) { definition = tire; RefreshVisual(); }
            if (Carrier == carrier) return;
            Carrier?.RemoveItem(this);
            Carrier = carrier;
            transform.SetParent(carrier != null ? carrier.GetComponent<WeaponHost>().WeaponAttachRoot : null, true);
            body.detectCollisions = carrier == null;
        }
        public void Drop(Vector3 position, Vector3 velocity)
        {
            Carrier?.RemoveItem(this);
            Carrier = null;
            transform.SetParent(null, true);
            transform.position = position;
            gameObject.SetActive(true);
            body.isKinematic = false;
            body.detectCollisions = true;
            body.linearVelocity = velocity;
        }
        /// <summary>新Itemの枠を旧タイヤへ置き換えるため、満杯でも交換可能。</summary>
        public bool Install(WheelSystem wheels)
        {
            if (Carrier == null || wheels == null || definition == null) return false;
            TireDefinition old = wheels.CurrentTire;
            if (old == definition) return false;
            wheels.ReplaceTire(definition);
            if (old != null)
            {
                definition = old;
                body.mass = old.Mass;
                RefreshVisual();
            }
            else
            {
                Carrier.RemoveItem(this);
                Carrier = null;
                Destroy(gameObject);
            }
            return true;
        }
        public void RefreshVisual()
        {
            Transform oldVisual = transform.Find("Visual");
            if (oldVisual != null) { oldVisual.gameObject.SetActive(false); Destroy(oldVisual.gameObject); }
            if (definition != null && definition.VisualPrefab != null)
            {
                GameObject visual = Instantiate(definition.VisualPrefab, transform);
                visual.name = "Visual";
                var collider = visual.AddComponent<MeshCollider>();
                collider.sharedMesh = visual.GetComponent<MeshFilter>().sharedMesh;
                collider.convex = true;
                collider.sharedMaterial = contactMaterial;
                PrototypeCollision.SetLayer(visual, PrototypeCollision.TireItem);
            }
        }
    }
}
