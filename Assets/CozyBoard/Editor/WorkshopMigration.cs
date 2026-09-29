using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace CozyBoard.Editor {
    public static class WorkshopMigration {
        const string Root = "Assets/CozyBoard";
        const string ScenePath = Root + "/Scenes/Workbench.unity";
        [Serializable] class Package { public ItemData[] items; public MeshData[] meshes; public MaterialData[] materials; }
        [Serializable] class ItemData {
            public string id,label,kind,meshId; public int stage; public bool fitted;
            public float[] position,bounds_center,bounds_size,slot; public float rotation_y;
        }
        [Serializable] class MeshData { public string id; public float[] vertices,normals,uv; public Submesh[] submeshes; public string[] materials; }
        [Serializable] class Submesh { public int[] indices; }
        [Serializable] class MaterialData { public string id,texture; public float[] color; }
        static Vector3 V(float[] a) => new(a[0],a[1],a[2]);
        static readonly Dictionary<string,Material> materials = new();
        static readonly Dictionary<string,Mesh> meshes = new();
        static readonly Dictionary<string,MeshData> meshData = new();

        [MenuItem("Cozy Board/Rebuild migrated workshop")]
        public static void Build() {
            Directory.CreateDirectory("Verification");
            var package = JsonUtility.FromJson<Package>(File.ReadAllText(Root+"/SourceData/workshop.json"));
            ConfigureTextures();
            materials.Clear(); meshes.Clear(); meshData.Clear();
            Shader paint = Shader.Find("CozyBoard/Painted");
            if (!paint) throw new Exception("Painted shader missing");
            foreach (var source in package.materials) {
                var mat = new Material(paint) { name = source.id };
                if (!string.IsNullOrEmpty(source.texture)) mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/"+source.texture));
                mat.SetColor("_BaseColor",new Color(source.color[0],source.color[1],source.color[2],source.color[3]));
                materials.Add(source.id, Save(mat,Root+"/Materials/"+source.id+".mat"));
            }
            foreach (var source in package.meshes) {
                var mesh = new Mesh { name = source.id, indexFormat = IndexFormat.UInt32 };
                var vertices = new Vector3[source.vertices.Length/3];
                var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++) {
                    vertices[i]=new Vector3(source.vertices[i*3],source.vertices[i*3+1],source.vertices[i*3+2]);
                    normals[i]=new Vector3(source.normals[i*3],source.normals[i*3+1],source.normals[i*3+2]);
                    uv[i]=new Vector2(source.uv[i*2],source.uv[i*2+1]);
                }
                mesh.vertices=vertices; mesh.normals=normals; mesh.uv=uv; mesh.subMeshCount=source.submeshes.Length;
                for(int i=0;i<source.submeshes.Length;i++) mesh.SetTriangles(source.submeshes[i].indices,i);
                mesh.RecalculateBounds(); meshes[source.id]=Save(mesh,Root+"/Meshes/"+source.id+".asset"); meshData[source.id]=source;
            }
            var quad = Save(MakeQuad(),Root+"/Meshes/GroundQuad.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var main = new GameObject("CozyBoard");
            var controller = main.AddComponent<WorkshopController>();
            controller.GroundQuad=quad;
            controller.PartsRoot=new GameObject("InteractiveParts").transform;controller.PartsRoot.SetParent(main.transform);
            var parts = new List<WorkshopItem>();
            foreach(var source in package.items) {
                var go = new GameObject(source.id) { layer=8 };
                var item = go.AddComponent<WorkshopItem>();
                item.Id=source.id; item.Label=source.label; item.Kind=source.kind; item.Stage=source.stage;
                item.Slot=V(source.slot); item.BoundsCenter=V(source.bounds_center); item.BoundsSize=V(source.bounds_size);
                item.InitialPosition=V(source.position); item.InitialYaw=source.rotation_y*Mathf.Rad2Deg;
                item.InitiallyFitted=source.fitted; item.Fitted=false;
                var visual=new GameObject("Visual");visual.transform.SetParent(go.transform,false);
                visual.AddComponent<MeshFilter>().sharedMesh=meshes[source.meshId];
                var renderer=visual.AddComponent<MeshRenderer>();
                renderer.sharedMaterials=meshData[source.meshId].materials.Select(id=>materials[id]).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var collider=go.AddComponent<BoxCollider>();collider.center=item.BoundsCenter;collider.size=item.BoundsSize;
                item.Cache();
                var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+source.id+".prefab");
                UnityEngine.Object.DestroyImmediate(go);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,controller.PartsRoot);
                item=instance.GetComponent<WorkshopItem>();item.Cache();parts.Add(item);
                instance.transform.localPosition=item.InitialPosition;
                instance.transform.localRotation=Quaternion.Euler(0,item.InitialYaw,0);
                instance.transform.localScale=Vector3.one*(source.id=="Case"?1.22f:1);
            }
            controller.Items=parts.ToArray();
            var cameraObject=new GameObject("WorkbenchCamera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true;camera.nearClipPlane=.05f;camera.farClipPlane=80;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.6f,.36f,.23f);
            camera.allowHDR=false;controller.ViewCamera=camera;
            controller.ShadowMaterial=Save(new Material(Shader.Find("CozyBoard/ContactShadow")){name="SoftContactShadow"},Root+"/Materials/ContactShadow.mat");
            var desk=new Material(paint){name="PaintedDeskBackground"};desk.SetFloat("_Shading",0);desk.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/DeskBackground.png"));
            Surface("Desk_Rug_Chair_2D",quad,Save(desk,Root+"/Materials/DeskBackground.mat"),Vector3.zero,new Vector3(21.8f,1,12.2625f),main.transform);
            var plant = new Material(Shader.Find("CozyBoard/PaintedCutout")){name="CornerPlant2D"};plant.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/CornerPlant.png"));
            plant=Save(plant,Root+"/Materials/CornerPlant.mat");
            Surface("CornerPlant2D",quad,plant,new Vector3(-9.65f,.065f,5.10f),new Vector3(4.8f,1,4.8f),main.transform);
            var plantShadow=new Material(plant){name="PlantSilhouetteShadow"};plantShadow.SetFloat("_Silhouette",1);plantShadow.SetFloat("_Opacity",.25f);plantShadow.renderQueue=3001;
            Surface("PlantShadow",quad,Save(plantShadow,Root+"/Materials/PlantShadow.mat"),new Vector3(-9.32f,.025f,4.72f),new Vector3(4.9f,1,4.9f),main.transform);
            BuildUI(controller);
            controller.Initialize();
            foreach(var item in parts.Where(p=>p.InitiallyFitted).OrderBy(p=>p.Stage)) controller.Attach(item);
            controller.ShowLayer(4);controller.UpdateCamera();
            PlayerSettings.productName="Cozy Board";PlayerSettings.companyName="CozyBoard";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            QualitySettings.vSyncCount=1;
            RecordPartOverrides(controller);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Verify();
            Debug.Log("COZY_UNITY_MIGRATION_READY: 128 parts, 61 switches, 61 caps");
        }
        static T Save<T>(T asset,string path) where T:UnityEngine.Object {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing) { EditorUtility.CopySerialized(asset,existing);UnityEngine.Object.DestroyImmediate(asset);return existing; }
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static void ConfigureTextures() {
            foreach(string path in Directory.GetFiles(Root+"/Art","*.png")) {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
                importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Trilinear;importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency=path.Contains("CornerPlant");importer.SaveAndReimport();
            }
        }
        static Mesh MakeQuad() {
            var mesh=new Mesh{name="GroundQuad"};
            mesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(.5f,0,-.5f)};
            mesh.normals=Enumerable.Repeat(Vector3.up,4).ToArray();
            mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();return mesh;
        }
        static void Surface(string name,Mesh mesh,Material mat,Vector3 position,Vector3 scale,Transform parent) {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        static RectTransform Rect(string name,Transform parent) {
            var go=new GameObject(name,typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }
        static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string value,int size) {
            var rect=Rect(name,parent);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.color=new Color(1,.94f,.84f);text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;return text;
        }
        static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,string value,float width) {
            var rect=Rect(name,parent);rect.sizeDelta=new Vector2(width,40);
            var layout=rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();layout.preferredWidth=width;layout.preferredHeight=40;
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.88f,.79f,.64f);
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var text=Text("Label",rect,font,value,17);text.alignment=TextAlignmentOptions.Center;text.color=new Color(.28f,.24f,.20f);
            var tr=text.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;
            return button;
        }
        static void BuildUI(WorkshopController controller) {
            const string fontPath=Root+"/Data/WorkshopFont.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(!font) {
                var sourceFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
                if(!sourceFont) throw new Exception("TMP source font missing");
                font=TMP_FontAsset.CreateFontAsset(sourceFont,64,8,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="WorkshopFont";
                AssetDatabase.CreateAsset(font,fontPath);
                AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            }
            string characters=new string(Enumerable.Range(32,95).Select(c=>(char)c).ToArray())+"çğıöşüÇĞİÖŞÜ ·";
            font.TryAddCharacters(characters,out string missing);
            if(!string.IsNullOrEmpty(missing))throw new Exception("Missing UI glyphs: "+missing);
            var canvasObject=new GameObject("WorkshopUI",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var bar=Rect("LayerControls",canvasObject.transform);bar.anchorMin=bar.anchorMax=bar.pivot=new Vector2(0,1);bar.anchoredPosition=new Vector2(28,-22);bar.sizeDelta=new Vector2(740,40);
            var row=bar.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();row.spacing=8;row.childControlWidth=true;row.childControlHeight=true;row.childForceExpandWidth=false;row.childForceExpandHeight=false;
            controller.LayerButtons=new[]{Button("PCB",bar,font,"PCB",100),Button("Plate",bar,font,"Plaka",100),Button("Switches",bar,font,"Switch",100),Button("Keycaps",bar,font,"Tuşlar",100)};
            controller.AssembleButton=Button("Assemble",bar,font,"Tümünü tak",130);
            controller.ResetButton=Button("Reset",bar,font,"Düzeni sıfırla",145);
            controller.StatusLabel=Text("AssemblyStatus",canvasObject.transform,font,"COZY BOARD",21);
            var status=controller.StatusLabel.rectTransform;status.anchorMin=status.anchorMax=status.pivot=Vector2.zero;status.anchoredPosition=new Vector2(30,52);status.sizeDelta=new Vector2(1150,30);
            controller.HintLabel=Text("Controls",canvasObject.transform,font,"",17);
            var hint=controller.HintLabel.rectTransform;hint.anchorMin=hint.anchorMax=hint.pivot=Vector2.zero;hint.anchoredPosition=new Vector2(30,22);hint.sizeDelta=new Vector2(1700,28);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        [MenuItem("Cozy Board/Verify migration")]
        public static void Verify() {
            var controller=UnityEngine.Object.FindFirstObjectByType<WorkshopController>();
            if(!controller) {EditorSceneManager.OpenScene(ScenePath);controller=UnityEngine.Object.FindFirstObjectByType<WorkshopController>();}
            controller.Initialize();
            var report=new List<string>();
            void Check(bool condition,string text) {if(!condition)throw new Exception("VERIFY FAILED: "+text);report.Add("PASS "+text);}
            Check(controller.Items.Length==128,"128 independent parts");
            Check(controller.Items.Count(p=>p.Kind=="switch")==61,"61 switches");Check(controller.Items.Count(p=>p.Kind=="keycap")==61,"61 keycaps");
            Check(controller.Items.Select(p=>p.Id).Distinct().Count()==128,"Unique IDs");
            Check(controller.Items.All(p=>p.Visual&&p.Hitbox&&p.Visual.sharedMaterials.All(m=>m&&m.shader)),"Meshes, materials, colliders resolved");
            var cap=controller.Lookup["Keycap_00"];var pcb=controller.Lookup["PCB"];var casePart=controller.Lookup["Case"];
            Vector3 original=cap.transform.position;
            controller.ShowLayer(1);Check(!cap.Visual.enabled&&pcb.Visual.enabled,"PCB layer hides caps and preserves PCB");controller.ShowLayer(4);
            Vector3 screen=controller.ViewCamera.WorldToScreenPoint(cap.transform.position);
            controller.BeginDrag(cap,screen);cap.transform.position+=new Vector3(3,1,1);controller.CancelDrag();
            Check(cap.Fitted&&Vector3.Distance(cap.transform.position,original)<.0001f,"Drag cancellation restores parent and transform");
            controller.BeginDrag(cap,screen);cap.transform.position=new Vector3(-7,.8f,0);controller.FinishDrag();
            Check(!cap.Fitted&&cap.transform.parent==controller.PartsRoot,"Free drop detaches cap");
            Check(Mathf.Abs(cap.transform.position.y+(cap.BoundsCenter.y-cap.BoundsSize.y*.5f)*cap.transform.lossyScale.y-.015f)<.0001f,"Free drop rests on desktop");
            controller.BeginDrag(cap,controller.ViewCamera.WorldToScreenPoint(cap.transform.position));cap.transform.position=casePart.transform.TransformPoint(cap.Slot)+Vector3.up*.2f;controller.FinishDrag();
            Check(cap.Fitted&&Vector3.Distance(cap.transform.position,original)<.0001f,"Cap snaps to matching switch");
            Vector3 caseStart=casePart.transform.position;casePart.transform.position+=Vector3.right;
            Check(Vector3.Distance(cap.transform.position,original+Vector3.right)<.0001f,"Case movement carries assembly");casePart.transform.position=caseStart;
            controller.BeginDrag(pcb,controller.ViewCamera.WorldToScreenPoint(pcb.transform.position));
            Check(!controller.CanAttach(cap),"Missing fitted PCB blocks cap attachment");controller.CancelDrag();
            controller.ResetLayout();Check(controller.Items.Where(p=>p.Stage>0).All(p=>p.Fitted),"Reset restores complete assembly");
            Physics.SyncTransforms();
            Vector3 pixel=controller.ViewCamera.WorldToScreenPoint(cap.transform.TransformPoint(cap.BoundsCenter));
            Check(Physics.Raycast(controller.ViewCamera.ScreenPointToRay(pixel),out var hit,100,1<<8)&&hit.collider.GetComponent<WorkshopItem>()==cap,"Camera ray selects visible cap");
            Check(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length==1,"Exactly one EventSystem");
            Check(controller.LayerButtons.Length==4&&controller.AssembleButton&&controller.ResetButton,"All UI controls assigned");
            foreach(var mat in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))))
                Check(!ShaderUtil.ShaderHasError(mat.shader),"Shader compiles: "+mat.name);
            RecordPartOverrides(controller);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            var restored=UnityEngine.Object.FindFirstObjectByType<WorkshopController>();restored.Initialize();
            Check(restored.Items.Where(p=>p.Stage>0).All(p=>p.Fitted),"Assembly state persists after scene reload");
            restored.ShowLayer(1);
            Check(!restored.Lookup["Keycap_00"].Visual.enabled&&restored.Lookup["PCB"].Visual.enabled,"Layer filtering survives scene reload");
            restored.ShowLayer(4);
            Directory.CreateDirectory("Verification");File.WriteAllLines("Verification/migration-checks.txt",report);
            Debug.Log("COZY_VERIFICATION_PASSED "+report.Count+" checks");
        }
        static void RecordPartOverrides(WorkshopController controller) {
            foreach(var item in controller.Items) {
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.Visual);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.Hitbox);
            }
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        }
        public static void BuildMac() {
            Verify();
            Directory.CreateDirectory("Builds/macOS");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/macOS/Cozy Board.app",target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            File.WriteAllText("Verification/build-result.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Mac build failed");
        }
    }
}
