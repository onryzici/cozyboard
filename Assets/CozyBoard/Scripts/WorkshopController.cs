using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace CozyBoard {
    public sealed class WorkshopController : MonoBehaviour {
        public Camera ViewCamera;
        public WorkshopGameMode Game;
        public Transform PartsRoot;
        public WorkshopItem[] Items;
        public Material ShadowMaterial;
        public Mesh GroundQuad;
        public TMP_Text StatusLabel, HintLabel;
        public UnityEngine.UI.Button[] LayerButtons;
        public UnityEngine.UI.Button AssembleButton, ResetButton;
        public int Layer = 4;
        public float Yaw, Pitch = 81, ViewWidth = 21;
        public Vector3 ViewTarget;
        public WorkshopItem Selected { get; private set; }
        public WorkshopItem Dragged { get; private set; }
        readonly Dictionary<string, WorkshopItem> byId = new();
        readonly Dictionary<string, Renderer> shadows = new();
        MaterialPropertyBlock shadowProperties;
        Transform dragParent;
        Vector3 dragPosition, dragScale, dragOffset;
        Quaternion dragRotation;
        bool dragFitted;
        Vector2 pickupScreen;
        bool clickCarry;
        float nextStamp;
        Mesh previewOriginal;
        Material[] previewMaterials;
        float dragHeight;
        public IReadOnlyDictionary<string, WorkshopItem> Lookup => byId;

        void Awake() { Initialize(); }
        public void Initialize() {
            shadowProperties ??= new MaterialPropertyBlock();
            byId.Clear();
            foreach (var item in Items) { item.Cache(); byId.Add(item.Id, item); }
            UpdateCamera();
            ShowLayer(Layer);
        }
        void Start() {
            foreach (var item in Items) CreateShadow(item);
            for (int i = 0; i < LayerButtons.Length; i++) {
                int value = i + 1;
                LayerButtons[i].onClick.AddListener(() => { CancelDrag(); ShowLayer(value); });
            }
            if(Game)Game.BeginSession();
            else {AssembleButton.onClick.AddListener(Assemble);ResetButton.onClick.AddListener(ResetLayout);}
        }
        void Update() {
            var mouse=Mouse.current;var keyboard=Keyboard.current;if(mouse==null)return;
            if(Game&&Game.Menu&&Game.Menu.InputBlocked){if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)Game.Menu.ClosePanels();return;}
            Vector2 point=mouse.position.ReadValue();bool overUI=EventSystem.current&&EventSystem.current.IsPointerOverGameObject();
            if(mouse.leftButton.wasPressedThisFrame&&!overUI) {
                if(Dragged&&clickCarry){MoveDrag(point);FinishDrag();clickCarry=false;}
                else {
                    pickupScreen=point;clickCarry=false;BeginDragAt(point);
                    if(!Dragged&&Game&&Game.ActiveSupply>=3&&(!Game.Menu||Game.Menu.ToolMode==0)) {
                        var item=Game.SupplyItem(Game.ActiveSupply);
                        if(item&&FindSocket(item.Stage,MousePlane(point,byId["Case"].transform.position.y+.5f))) {BeginDrag(item,point);dragOffset=Vector3.zero;}
                    }
                }
            }
            if(Dragged) {
                MoveDrag(point);
                if(!overUI&&mouse.leftButton.isPressed&&Dragged.Stage>=3&&Vector2.Distance(point,pickupScreen)>7&&Time.unscaledTime>=nextStamp) {
                    if(SnapCandidate(Dragged)) {
                        int stage=Dragged.Stage;FinishDrag();nextStamp=Time.unscaledTime+.065f;
                        var next=Game?Game.SupplyItem(stage):null;
                        if(next){BeginDrag(next,point);dragOffset=Vector3.zero;MoveDrag(point);}
                    }
                }
                if(mouse.leftButton.wasReleasedThisFrame&&!clickCarry&&Dragged){if(Vector2.Distance(point,pickupScreen)>7)FinishDrag();else clickCarry=true;}
            }
            if(!Dragged&&!overUI) {
                Vector2 delta=mouse.delta.ReadValue();
                if(mouse.rightButton.isPressed){Yaw=Mathf.Clamp(Yaw+delta.x*.06f,-5,5);Pitch=Mathf.Clamp(Pitch-delta.y*.06f,78,85);}
                else if(mouse.middleButton.isPressed){ViewTarget.x=Mathf.Clamp(ViewTarget.x-delta.x*.008f,-.15f,.15f);ViewTarget.z=Mathf.Clamp(ViewTarget.z-delta.y*.008f,-.10f,.10f);}
                ViewWidth=Mathf.Clamp(ViewWidth-mouse.scroll.ReadValue().y*.006f,18,21);
            }
            if(keyboard!=null){if(keyboard.escapeKey.wasPressedThisFrame)CancelDrag();if(Selected&&!Selected.Fitted){if(keyboard.qKey.wasPressedThisFrame)Selected.transform.Rotate(0,15,0,Space.World);if(keyboard.eKey.wasPressedThisFrame)Selected.transform.Rotate(0,-15,0,Space.World);}}
            UpdateCamera();
        }
        public void UpdateCamera() {
            float elevation = Pitch * Mathf.Deg2Rad, azimuth = Yaw * Mathf.Deg2Rad;
            ViewCamera.transform.position = ViewTarget + new Vector3(Mathf.Sin(azimuth) * Mathf.Cos(elevation), Mathf.Sin(elevation), -Mathf.Cos(azimuth) * Mathf.Cos(elevation)) * 20.6f;
            ViewCamera.transform.LookAt(ViewTarget, Vector3.forward);
            // With a nearly vertical camera, world +Z is screen-up.
            ViewCamera.orthographicSize = ViewWidth / (2 * Mathf.Max(ViewCamera.aspect, .1f));
        }
        public Vector3 MousePlane(Vector2 point, float height) {
            var ray = ViewCamera.ScreenPointToRay(point);
            return new Plane(Vector3.up, new Vector3(0, height, 0)).Raycast(ray, out float distance) ? ray.GetPoint(distance) : Vector3.zero;
        }
        public void BeginDragAt(Vector2 point) {
            if(!Physics.Raycast(ViewCamera.ScreenPointToRay(point),out var hit,100,1<<8))return;
            var supply=hit.collider.GetComponent<WorkshopSupply>();
            var item=supply&&Game?Game.SupplyItem(supply.Stage):hit.collider.GetComponent<WorkshopItem>();
            if(!item)return;
            if(Game&&Game.Menu&&Game.Menu.ToolMode==2&&item.Stage==0){item.transform.Rotate(0,15,0);Selected=item;return;}
            BeginDrag(item,point);
        }
        public void BeginDrag(WorkshopItem item, Vector2 point) {
            if(Game&&Game.SessionActive&&!Game.CanPick(item))return;
            if (Dragged) CancelDrag();
            Selected = Dragged = item;
            previewOriginal=item.Visual.GetComponent<MeshFilter>().sharedMesh;previewMaterials=item.Visual.sharedMaterials;
            var t = item.transform;
            dragParent = t.parent; dragPosition = t.position; dragRotation = t.rotation;
            dragScale = t.localScale; dragFitted = item.Fitted;
            item.Fitted = false;
            t.SetParent(PartsRoot, true);
            dragHeight = item.Stage>0?byId["Case"].transform.TransformPoint(item.Slot).y+.24f:Mathf.Max(t.position.y+.16f,.38f);
            if(Game&&Game.SessionActive&&item.Stage>0){t.rotation=byId["Case"].transform.rotation;t.localScale=Vector3.one*1.22f;}
            dragOffset = item.Stage>0?Vector3.zero:t.position - MousePlane(point, dragHeight); dragOffset.y = 0;
            if (HintLabel) HintLabel.text = item.Label + " · Klavyenin üzerine bırak veya tıkla · Esc: geri koy";
            if(Game&&Game.SessionActive)Game.Picked(item);
        }
        public void MoveDrag(Vector2 point) {
            if(!Dragged)return;
            Vector3 p=MousePlane(point,dragHeight)+dragOffset;
            Dragged.transform.position=new Vector3(Mathf.Clamp(p.x,-10.3f,10.3f),dragHeight,Mathf.Clamp(p.z,-4.5f,4.9f));
            var candidate=SnapCandidate(Dragged);
            if(Dragged.Stage==4&&candidate&&candidate!=Dragged){Dragged.Visual.GetComponent<MeshFilter>().sharedMesh=candidate.Visual.GetComponent<MeshFilter>().sharedMesh;Dragged.Visual.sharedMaterials=candidate.Visual.sharedMaterials;}
            else RestorePreview();
        }
        void RestorePreview(){if(Dragged&&previewOriginal){Dragged.Visual.GetComponent<MeshFilter>().sharedMesh=previewOriginal;Dragged.Visual.sharedMaterials=previewMaterials;}}
        public bool CanAttach(WorkshopItem item)=>item&&item.Stage>0&&!item.Fitted;
        public void Attach(WorkshopItem item) {
            var board=byId["Case"].transform;item.transform.SetParent(board,false);
            item.transform.localPosition=item.Slot;item.transform.localRotation=Quaternion.identity;
            item.transform.localScale=Vector3.one;item.Fitted=true;
        }
        public WorkshopItem FindSocket(int stage,Vector3 world) {
            var local=byId["Case"].transform.InverseTransformPoint(world);
            if(stage<3){var item=byId[stage==1?"PCB":"Plate"];return Mathf.Abs(local.x)<3.3f&&Mathf.Abs(local.z)<1.3f&&!item.Fitted?item:null;}
            WorkshopItem nearest=null;float best=float.MaxValue;
            foreach(var cap in Items.Where(p=>p.Kind=="keycap")) {
                float dx=Mathf.Abs(local.x-cap.Slot.x)/(cap.BoundsSize.x*.5f+.055f),dz=Mathf.Abs(local.z-cap.Slot.z)/.245f;
                if(dx>1||dz>1)continue;float score=dx*dx+dz*dz;
                if(score<best){best=score;nearest=stage==4?cap:byId[cap.Id.Replace("Keycap_","Switch_")];}
            }
            return nearest&&!nearest.Fitted?nearest:null;
        }
        public WorkshopItem SnapCandidate(WorkshopItem item)=>item&&CanAttach(item)?FindSocket(item.Stage,item.transform.position):null;
        public bool PaintAt(int stage,Vector3 world) {
            var candidate=FindSocket(stage,world);if(!candidate)return false;
            Attach(candidate);if(Game)Game.InstalledPart(candidate,world+Vector3.up*.18f);return true;
        }
        public void FinishDrag() {
            if (!Dragged) return;
            var item = Dragged;
            var candidate=SnapCandidate(item);
            Vector3 from=item.transform.position;RestorePreview();
            if(candidate) {
                if(candidate!=item) {
                    item.transform.SetPositionAndRotation(dragPosition,dragRotation);
                    item.transform.localScale=dragScale;
                }
                Attach(candidate); Dragged=null;clickCarry=false;ShowLayer(Mathf.Max(Layer,candidate.Stage));
                if(Game&&Game.SessionActive)Game.InstalledPart(candidate,from);
                return;
            }
            if(Game&&Game.SessionActive&&item.Stage>0) {CancelDrag();return;}
            Vector3 p=item.transform.position;
            float bottom=(item.BoundsCenter.y-item.BoundsSize.y*.5f)*item.transform.lossyScale.y;
            p.y=.015f-bottom;item.transform.position=p;
            Dragged=null;clickCarry=false;RefreshStatus();
            if(Game&&Game.SessionActive)Game.Dropped();
        }
        public void CancelDrag() {
            if (!Dragged) return;
            var item = Dragged;RestorePreview();
            item.transform.SetParent(dragParent, false);
            item.transform.localScale = dragScale;
            item.transform.SetPositionAndRotation(dragPosition, dragRotation);
            item.Fitted = dragFitted; Dragged = null; clickCarry=false; RefreshStatus();
        }
        public void ResetItem(WorkshopItem item) {
            item.transform.SetParent(PartsRoot, false);
            item.transform.localPosition = item.InitialPosition;
            item.transform.localRotation = Quaternion.Euler(0, item.InitialYaw, 0);
            item.transform.localScale = Vector3.one * (item.Id == "Case" ? 1.22f : 1);
            item.Fitted = false;
            if (item.InitiallyFitted) Attach(item);
            ShowLayer(Layer);
        }
        public void ResetLayout() {
            if(Game&&Game.SessionActive) {Game.NewOrder();return;}
            CancelDrag();
            foreach (var item in Items.OrderBy(p => p.Stage)) ResetItem(item);
            Yaw = 0; Pitch = 81; ViewWidth = 21; ViewTarget = Vector3.zero;
            UpdateCamera(); ShowLayer(4);
        }
        public void Assemble() {
            CancelDrag();
            foreach (var item in Items.Where(p => p.Stage > 0).OrderBy(p => p.Stage)) Attach(item);
            ShowLayer(4);
        }
        public void ShowLayer(int value) {
            Layer = Mathf.Clamp(value, 1, 4);
            if(Game&&Game.SessionActive) {Game.Refresh();return;}
            foreach (var item in Items) {
                bool visible = !item.Fitted || item.Stage <= Layer;
                // Disable only the renderer/collider: child transforms still form the assembly.
                if (item.Visual) item.Visual.enabled = visible;
                if (item.Hitbox) item.Hitbox.enabled = visible;
            }
            for (int i = 0; i < LayerButtons.Length; i++) {
                var image = LayerButtons[i].GetComponent<UnityEngine.UI.Image>();
                if (image) image.color = i + 1 == Layer ? new Color(.29f,.42f,.37f) : new Color(.88f,.79f,.64f);
                var label = LayerButtons[i].GetComponentInChildren<TMP_Text>();
                if (label) label.color = i + 1 == Layer ? new Color(1,.94f,.84f) : new Color(.28f,.24f,.20f);
            }
            RefreshStatus();
        }
        public void RefreshStatus() {
            if(Game&&Game.SessionActive) {Game.Refresh();return;}
            if (StatusLabel) StatusLabel.text = $"COZY BOARD  ·  Switch {Items.Count(p => p.Kind == "switch" && p.Fitted)} / 61  ·  Tuş {Items.Count(p => p.Kind == "keycap" && p.Fitted)} / 61";
            if (HintLabel) HintLabel.text = "Sol tuş: taşı  ·  Sağ tuş: bakış  ·  Orta tuş: kaydır  ·  Tekerlek: yakınlaş  ·  Q / E: çevir  ·  R: sıfırla";
        }
        void CreateShadow(WorkshopItem item) {
            var go = new GameObject(item.Id + "_ContactShadow");
            go.transform.SetParent(transform, false);
            var mesh=Instantiate(item.Visual.GetComponent<MeshFilter>().sharedMesh);var indices=mesh.triangles;mesh.subMeshCount=1;mesh.SetTriangles(indices,0);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = ShadowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false; shadows.Add(item.Id, renderer);
        }
        void LateUpdate() {
            foreach (var item in Items) {
                if (!shadows.TryGetValue(item.Id, out var renderer)) continue;
                bool mountedCap = item.Fitted && item.Kind == "keycap";
                renderer.enabled = item.Visual.enabled && (!item.Fitted || mountedCap);
                if (!renderer.enabled) continue;
                renderer.transform.SetPositionAndRotation(item.Visual.transform.position,item.Visual.transform.rotation);
                renderer.transform.localScale=item.Visual.transform.lossyScale;
                shadowProperties.Clear();
                shadowProperties.SetFloat("_GroundY",mountedCap?item.transform.position.y-.012f:.018f);
                shadowProperties.SetFloat("_Opacity",mountedCap?.27f:.30f);
                renderer.SetPropertyBlock(shadowProperties);
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
    }
}
