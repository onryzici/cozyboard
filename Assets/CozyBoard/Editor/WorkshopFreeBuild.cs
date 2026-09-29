using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
namespace CozyBoard.Editor {
    public static class WorkshopFreeBuild {
        const string Root="Assets/CozyBoard";
        static T Ensure<T>(GameObject go) where T:Component {var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
        static TMP_FontAsset font;
        static Texture2D icons;
        static Material Mat(string name,string hex){string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("CozyBoard/Painted"));AssetDatabase.CreateAsset(m,path);}ColorUtility.TryParseHtmlString("#"+hex,out var c);m.SetColor("_BaseColor",c);m.SetTexture("_BaseMap",Texture2D.whiteTexture);EditorUtility.SetDirty(m);return m;}
        static Mesh SaveMesh(string name,Mesh mesh){string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
        public static void FinalizeVersion(){WorkshopGameBuild.Configure();WorkshopRenderReview.ReviewAndBuild();}
        public static void Apply(WorkshopController c) {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Data/WorkshopFont.asset");font.TryAddCharacters("çğıöşüÇĞİÖŞÜ–%:0123456789",out _);
            foreach(var path in new[]{"PaintedIcons.png","OrderClipboard.png"}){var imp=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/UI/"+path);imp.textureType=TextureImporterType.Default;imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.maxTextureSize=4096;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
            icons=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/PaintedIcons.png");
            var shadow=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PaintedSilhouette.mat");if(!shadow){shadow=new Material(Shader.Find("CozyBoard/PaintedSilhouette"));AssetDatabase.CreateAsset(shadow,Root+"/Materials/PaintedSilhouette.mat");}c.ShadowMaterial=shadow;
            BuildCase(c);BuildSupplies(c,shadow);BuildUI(c.Game);
            foreach(var item in c.Items.Where(p=>p.Stage==0&&p.Id!="Case")) {
                int index=item.Id=="Screwdriver"?0:item.Id=="KeyPuller"?1:2;
                item.InitialPosition=new Vector3(7.2f,.065f,-2.8f-index*.64f);item.InitialYaw=90;
            }
        }
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material material,float bevel=.035f) {
            var tr=parent.Find(name);var go=tr?tr.gameObject:new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;
            var mf=Ensure<MeshFilter>(go);mf.sharedMesh=SaveMesh(parent.name+"_"+name,Rounded(size,bevel));
            var mr=Ensure<MeshRenderer>(go);mr.sharedMaterial=material;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
        static void BuildCase(WorkshopController c) {
            var item=c.Lookup["Case"];var vertices=new List<Vector3>();var indices=new List<int>();
            // A continuous rounded rim, gently rolled over at the top.
            var rings=new[]{new Vector4(6.64f,2.67f,.08f,.24f),new Vector4(6.64f,2.67f,.245f,.24f),new Vector4(6.57f,2.60f,.30f,.22f),new Vector4(6.21f,2.24f,.30f,.12f),new Vector4(6.15f,2.18f,.16f,.10f)};
            foreach(var r in rings)Ring(vertices,r.x,r.y,r.z,r.w);
            const int n=48;for(int k=0;k<rings.Length-1;k++)for(int j=0;j<n;j++){int a=k*n+j,b=k*n+(j+1)%n;indices.AddRange(new[]{a,a+n,b,b,a+n,b+n});}
            var mesh=new Mesh{name="Rolled ceramic keyboard case"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x,v.z)).ToList());mesh.RecalculateBounds();
            var filter=item.Visual.GetComponent<MeshFilter>();filter.sharedMesh=SaveMesh("RoundedCaseRim",mesh);item.Visual.sharedMaterials=new[]{Mat("CaseIvory","D8C5A7")};
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);PrefabUtility.RecordPrefabInstancePropertyModifications(item.Visual);
            Part(item.transform,"DarkCushion",new Vector3(0,.11f,0),new Vector3(6.21f,.09f,2.23f),Mat("CaseLiner","394A42"),.10f);
            Part(item.transform,"LowerShell",new Vector3(0,.04f,0),new Vector3(6.60f,.08f,2.63f),Mat("CaseLower","A68265"),.15f);
            Part(item.transform,"MakerBadge",new Vector3(2.40f,.302f,-1.205f),new Vector3(.43f,.012f,.075f),Mat("BrassBadge","B79764"),.015f);
        }
        static void Ring(List<Vector3> v,float w,float h,float y,float r){for(int corner=0;corner<4;corner++)for(int j=0;j<12;j++){float a=(corner*90+j*90f/11)*Mathf.Deg2Rad;v.Add(new Vector3((corner==0||corner==3?1:-1)*(w/2-r)+Mathf.Cos(a)*r,y,(corner<2?1:-1)*(h/2-r)+Mathf.Sin(a)*r));}}
        static void BuildSupplies(WorkshopController c,Material shadow) {
            var root=c.transform.Find("SupplyPackages");if(!root){root=new GameObject("SupplyPackages").transform;root.SetParent(c.transform,false);}
            var cardboard=Mat("BoxCardboard","BA8356");var rim=Mat("BoxEdge","D4A270");var paper=Mat("BoxTissue","D4DFDF");var dark=Mat("BoxCrease","845D45");
            for(int stage=1;stage<=4;stage++) {
                string name="SupplyBox"+stage;var box=root.Find(name);if(!box){box=new GameObject(name).transform;box.SetParent(root,false);}
                box.localPosition=stage<3?new Vector3(stage==1?-2.4f:2.4f,0,3.82f):new Vector3(stage==3?-7.35f:7.35f,0,-.15f);
                float w=stage<3?4.25f:2.85f,h=stage<3?1.68f:3.55f;
                Part(box,"Floor",new Vector3(0,.055f,0),new Vector3(w,.11f,h),cardboard,.055f);
                Part(box,"LeftWall",new Vector3(-w/2,.20f,0),new Vector3(.075f,.34f,h),rim,.024f);
                Part(box,"RightWall",new Vector3(w/2,.20f,0),new Vector3(.075f,.34f,h),rim,.024f);
                Part(box,"FrontWall",new Vector3(0,.16f,-h/2),new Vector3(w,.24f,.075f),rim,.022f);
                Part(box,"BackWall",new Vector3(0,.19f,h/2),new Vector3(w,.31f,.075f),cardboard,.018f);
                // Open attached lid with a shallow crease, deliberately simple silhouettes.
                var lid=Part(box,"OpenLid",new Vector3(0,.045f,h/2+.36f),new Vector3(w,.055f,.70f),cardboard,.028f);lid.transform.localRotation=Quaternion.Euler(-8,0,0);
                Part(box,"Hinge",new Vector3(0,.10f,h/2+.05f),new Vector3(w,.026f,.035f),dark,.008f);
                var fill=Part(box,"Tissue",new Vector3(0,.125f,0),new Vector3(w-.16f,.095f,h-.13f),paper,.045f);
                fill.GetComponent<MeshFilter>().sharedMesh=SaveMesh(name+"_Tissue",CrumpledPaper(w-.18f,h-.16f,stage));
                var collider=Ensure<BoxCollider>(box.gameObject);collider.center=new Vector3(0,.12f,0);collider.size=new Vector3(w,.24f,h);box.gameObject.layer=8;
                var supply=Ensure<WorkshopSupply>(box.gameObject);supply.Stage=stage;
                foreach(var renderer in box.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.EndsWith("Shadow")&&!r.GetComponent<TMP_Text>()).ToArray()) {
                    var st=box.Find(renderer.name+"Shadow");var go=st?st.gameObject:new GameObject(renderer.name+"Shadow");go.transform.SetParent(box,false);go.transform.localPosition=renderer.transform.localPosition;go.transform.localRotation=renderer.transform.localRotation;
                    (Ensure<MeshFilter>(go)).sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;(Ensure<MeshRenderer>(go)).sharedMaterial=shadow;
                }
                var label=box.Find("Label");if(!label){label=new GameObject("Label",typeof(RectTransform)).transform;label.SetParent(box,false);}
                var text=Ensure<TextMeshPro>(label.gameObject);text.font=font;text.text=stage switch{1=>"PCB",2=>"PLAKA",3=>"SWITCH",_=>"TUŞLAR"};text.fontSize=2.1f;text.color=new Color(.26f,.23f,.21f);text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(w,.28f);label.localPosition=new Vector3(0,.295f,-h/2-.025f);label.localRotation=Quaternion.Euler(90,0,0);
            }
        }
        static Mesh CrumpledPaper(float width,float depth,int seed) {
            var vertices=new List<Vector3>();var indices=new List<int>();var uv=new List<Vector2>();const int nx=30,nz=24;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++) {
                float px=((float)x/nx-.5f)*width,pz=((float)z/nz-.5f)*depth;
                float edge=Mathf.Max(Mathf.Abs(px)/(width/2),Mathf.Abs(pz)/(depth/2));
                float fold=Mathf.PerlinNoise(px*9+seed*10,pz*8+4);
                float y=.035f+(fold-.5f)*.025f+Mathf.Pow(edge,6)*(.035f+fold*.12f);
                vertices.Add(new Vector3(px,y,pz));uv.Add(new Vector2(px,pz));
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){int a=z*(nx+1)+x,b=a+1,c=a+nx+1;indices.AddRange(new[]{a,c,b,b,c,c+1});}
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh Rounded(Vector3 size,float radius) {
            radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.49f);var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int steps=6;
            var axes=new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            foreach(var normal in axes){var u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var w=Vector3.Cross(normal,u);int start=v.Count;
                for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++){var p=Vector3.Scale(normal*.5f+u*((float)x/steps-.5f)+w*((float)y/steps-.5f),size);var core=new Vector3(Mathf.Clamp(p.x,-size.x/2+radius,size.x/2-radius),Mathf.Clamp(p.y,-size.y/2+radius,size.y/2-radius),Mathf.Clamp(p.z,-size.z/2+radius,size.z/2-radius));var d=(p-core).normalized;v.Add(core+d*radius);n.Add(d);uv.Add(new Vector2((float)x/steps,(float)y/steps));}
                for(int y=0;y<steps;y++)for(int x=0;x<steps;x++){int a=start+y*(steps+1)+x,b=a+1,c=a+steps+1,d=c+1;tris.AddRange(new[]{a,b,c,b,d,c});}
            }
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
        }
        static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size){var existing=parent.Find(name) as RectTransform;var r=existing?existing:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        static TMP_Text Label(string name,Transform parent,string value,int size,Vector2 anchor,Vector2 p,Vector2 dimensions){var r=Rect(name,parent,anchor,p,dimensions);var t=Ensure<TextMeshProUGUI>(r.gameObject);t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.23f,.25f,.23f);t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;return t;}
        static UnityEngine.UI.Button Icon(string name,Transform parent,int index,Vector2 anchor,Vector2 p,int size=86){var r=Rect(name,parent,anchor,p,new Vector2(size,size));var raw=Ensure<UnityEngine.UI.RawImage>(r.gameObject);raw.texture=icons;
            // Atlas cells retain their source alpha; UV windows remove only transparent padding.
            float cellW=icons.width/4f,cellH=icons.height/2f;int col=index%4,row=index/4;
            raw.uvRect=new Rect((68+col*400f)/icons.width,(icons.height-76-row*425f-348)/icons.height,348f/icons.width,348f/icons.height);
            var b=Ensure<UnityEngine.UI.Button>(r.gameObject);b.targetGraphic=raw;return b;}
        static void BuildUI(WorkshopGameMode game) {
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var hud=Rect("PaintedHUD",canvas.transform,Vector2.zero,Vector2.zero,Vector2.zero);hud.anchorMax=Vector2.one;hud.offsetMax=Vector2.zero;
            var menu=Ensure<WorkshopMenu>(game.gameObject);game.Menu=menu;menu.Game=game;
            game.TestButton.gameObject.SetActive(false);game.NewOrderButton.gameObject.SetActive(false);
            menu.SaveButton=Icon("Save",hud,0,Vector2.one,new Vector2(-224,-25));menu.OrderButton=Icon("Order",hud,1,Vector2.one,new Vector2(-123,-25));menu.SettingsButton=Icon("Settings",hud,2,Vector2.one,new Vector2(-22,-25));
            menu.PointerButton=Icon("Pointer",hud,4,Vector2.zero,new Vector2(25,25));menu.MoveButton=Icon("Move",hud,5,Vector2.zero,new Vector2(126,25));menu.RotateButton=Icon("Rotate",hud,6,Vector2.zero,new Vector2(227,25));menu.UndoButton=Icon("Undo",hud,7,Vector2.zero,new Vector2(328,25));
            menu.ToastLabel=Label("Toast",hud,"",18,new Vector2(.5f,0),new Vector2(0,35),new Vector2(820,30));menu.ToastLabel.alignment=TextAlignmentOptions.Center;menu.ToastLabel.gameObject.SetActive(false);
            game.Objective.fontSize=21;game.Objective.rectTransform.anchoredPosition=new Vector2(0,167);game.Progress.fontSize=16;game.Progress.rectTransform.anchoredPosition=new Vector2(0,134);game.CompletionLabel.rectTransform.anchoredPosition=new Vector2(0,193);
            game.ProgressFill.transform.parent.gameObject.SetActive(false);game.Controller.HintLabel.rectTransform.anchoredPosition=new Vector2(28,125);game.Controller.HintLabel.fontSize=15;
            var order=Rect("OrderCard",hud,new Vector2(1,.5f),new Vector2(-80,0),new Vector2(580,870));menu.OrderPanel=order.gameObject;order.localScale=Vector3.one*.88f;
            var art=Ensure<UnityEngine.UI.RawImage>(order.gameObject);art.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/OrderClipboard.png");
            menu.OrderName=Label("Customer",order,"",25,new Vector2(0,1),new Vector2(65,-185),new Vector2(250,125));
            menu.OrderBody=Label("Request",order,"",22,new Vector2(0,1),new Vector2(65,-395),new Vector2(455,360));
            menu.ConfirmButton=Icon("Confirm",order,3,new Vector2(1,0),new Vector2(-50,58),64);menu.OrderAction=Label("ActionLabel",order,"Atölyeye dön",19,new Vector2(0,0),new Vector2(65,77),new Vector2(330,32));
            menu.CloseOrder=Icon("CloseOrder",order,7,new Vector2(1,1),new Vector2(30,30),58);
            var settings=Rect("SettingsCard",hud,new Vector2(.5f,.5f),Vector2.zero,new Vector2(490,300));menu.SettingsPanel=settings.gameObject;
            var bg=Ensure<UnityEngine.UI.Image>(settings.gameObject);bg.color=new Color(.91f,.84f,.70f,.98f);
            Label("Title",settings,"ATÖLYENİN SESİ",25,new Vector2(.5f,1),new Vector2(0,-35),new Vector2(350,40)).alignment=TextAlignmentOptions.Center;
            game.MusicSlider.transform.SetParent(settings,false);game.MusicSlider.GetComponent<RectTransform>().anchoredPosition=new Vector2(-45,-125);
            game.EffectsSlider.transform.SetParent(settings,false);game.EffectsSlider.GetComponent<RectTransform>().anchoredPosition=new Vector2(-45,-190);
            menu.CloseSettings=Icon("CloseSettings",settings,7,Vector2.one,new Vector2(22,22),60);
            order.gameObject.SetActive(false);settings.gameObject.SetActive(false);
        }
    }
}
