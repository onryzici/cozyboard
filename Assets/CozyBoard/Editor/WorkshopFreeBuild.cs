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
        static Texture2D icons,paintIcon;
        static Material Mat(string name,string hex){string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("CozyBoard/Painted"));AssetDatabase.CreateAsset(m,path);}ColorUtility.TryParseHtmlString("#"+hex,out var c);m.SetColor("_BaseColor",c);m.SetTexture("_BaseMap",Texture2D.whiteTexture);EditorUtility.SetDirty(m);return m;}
        static Mesh SaveMesh(string name,Mesh mesh){string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
        public static void FinalizeVersion(){WorkshopGameBuild.Configure();WorkshopRenderReview.ReviewAndBuild();}
        public static void Apply(WorkshopController c) {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Data/WorkshopHandwritten.asset")??CozyFont();font.TryAddCharacters("çğıöşüÇĞİÖŞÜ–·%:0123456789",out _);
            foreach(var path in new[]{"PaintedIcons.png","OrderClipboard.png","PaintBrushIcon.png"}){var imp=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/UI/"+path);imp.textureType=TextureImporterType.Default;imp.npotScale=TextureImporterNPOTScale.None;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.maxTextureSize=4096;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
            var cursorImporter=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/UI/WorkshopCursor.png");cursorImporter.textureType=TextureImporterType.Cursor;cursorImporter.alphaIsTransparency=true;cursorImporter.mipmapEnabled=false;cursorImporter.isReadable=true;cursorImporter.maxTextureSize=64;cursorImporter.textureCompression=TextureImporterCompression.Uncompressed;cursorImporter.SaveAndReimport();
            icons=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/PaintedIcons.png");
            paintIcon=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/PaintBrushIcon.png");
            var shadow=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/PaintedSilhouette.mat");if(!shadow){shadow=new Material(Shader.Find("CozyBoard/PaintedSilhouette"));AssetDatabase.CreateAsset(shadow,Root+"/Materials/PaintedSilhouette.mat");}c.ShadowMaterial=shadow;
            BuildCase(c);BuildSupplies(c,shadow);BuildUI(c.Game);WorkshopExperienceBuild.Configure();var cursor=Ensure<WorkshopCursor>(c.Game.gameObject);cursor.Pointer=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/UI/WorkshopCursor.png");
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None)){text.font=font;text.fontStyle|=FontStyles.Bold;}
            foreach(var item in c.Items.Where(p=>p.Stage==0&&p.Id!="Case")) {
                int index=item.Id=="Screwdriver"?0:item.Id=="KeyPuller"?1:2;
                item.InitialPosition=new Vector3(7.2f,.065f,-2.8f-index*.64f);item.InitialYaw=90;
            }
        }
        static TMP_FontAsset CozyFont() {
            const string assetPath=Root+"/Data/CozyFredoka.asset";
            var result=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);if(result)return result;
            var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/Fredoka-Variable.ttf");if(!source)throw new Exception("Fredoka font has not been imported yet");
            result=TMP_FontAsset.CreateFontAsset(source,72,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            result.name="Cozy Fredoka";AssetDatabase.CreateAsset(result,assetPath);AssetDatabase.AddObjectToAsset(result.material,result);
            foreach(var texture in result.atlasTextures)AssetDatabase.AddObjectToAsset(texture,result);AssetDatabase.SaveAssets();return result;
        }
        static Sprite RoundedKeySprite(){
            const string path=Root+"/Data/RoundedKeyMask.asset";var found=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(found)return found;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture){const int size=128;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Rounded keycap mask",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var pixels=new Color32[size*size];const float radius=19,half=64;for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=Mathf.Max(Mathf.Abs(x+.5f-half)-(half-radius),0),dy=Mathf.Max(Mathf.Abs(y+.5f-half)-(half-radius),0),distance=Mathf.Sqrt(dx*dx+dy*dy);byte alpha=(byte)Mathf.RoundToInt(255*(1-Mathf.SmoothStep(radius-1,radius+1,distance)));pixels[y*size+x]=new Color32(255,255,255,alpha);}texture.SetPixels32(pixels);texture.Apply(false,true);AssetDatabase.CreateAsset(texture,path);}
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(22,22,22,22));sprite.name="Rounded keycap UI mask";AssetDatabase.AddObjectToAsset(sprite,texture);AssetDatabase.SaveAssets();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
        static Sprite SoftUISprite()=>AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        static Sprite BrushRingSprite(){
            const string path=Root+"/Data/BrushRing.asset";var found=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();if(found)return found;
            const int size=128;var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(!texture){texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Brush footprint ring",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var pixels=new Color32[size*size];for(int y=0;y<size;y++)for(int x=0;x<size;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*64),edge=Mathf.Abs(d-57);byte alpha=(byte)Mathf.RoundToInt(255*(1-Mathf.SmoothStep(1.6f,3.5f,edge)));pixels[y*size+x]=new Color32(255,255,255,alpha);}texture.SetPixels32(pixels);texture.Apply(false,true);AssetDatabase.CreateAsset(texture,path);}var sprite=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100);sprite.name="Brush footprint ring";AssetDatabase.AddObjectToAsset(sprite,texture);AssetDatabase.SaveAssets();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
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
            // Keep the lower shell tucked under the ceramic rim. The previous footprint
            // exposed square tabs at the two near corners in the almost top-down camera.
            Part(item.transform,"LowerShell",new Vector3(0,.045f,0),new Vector3(6.42f,.075f,2.45f),Mat("CaseLower","A68265"),.20f);
            Part(item.transform,"MakerBadge",new Vector3(2.40f,.302f,-1.205f),new Vector3(.43f,.012f,.075f),Mat("BrassBadge","B79764"),.015f);
        }
        static void Ring(List<Vector3> v,float w,float h,float y,float r){for(int corner=0;corner<4;corner++)for(int j=0;j<12;j++){float a=(corner*90+j*90f/11)*Mathf.Deg2Rad;v.Add(new Vector3((corner==0||corner==3?1:-1)*(w/2-r)+Mathf.Cos(a)*r,y,(corner<2?1:-1)*(h/2-r)+Mathf.Sin(a)*r));}}
        static void BuildSupplies(WorkshopController c,Material shadow) {
            var root=c.transform.Find("SupplyPackages");if(!root){root=new GameObject("SupplyPackages").transform;root.SetParent(c.transform,false);}
            Material edge=Mat("BoxEdge","725946"),canvas=Mat("BoxCanvas","E1D2B4"),labelMat=Mat("BoxLabel","F4E8CD"),tape=Mat("BoxTape","CBB58B");
            var shellHex=new[]{"A97C59","B08A62","718F85","A66F61"};
            var accentHex=new[]{"D39A63","D3B06F","91B0A4","CF8976"};
            for(int stage=1;stage<=4;stage++) {
                var shell=Mat("BoxShell"+stage,shellHex[stage-1]);var accent=Mat("BoxAccent"+stage,accentHex[stage-1]);
                string name="SupplyBox"+stage;var box=root.Find(name);if(!box){box=new GameObject(name).transform;box.SetParent(root,false);}
                box.localPosition=stage<3?new Vector3(stage==1?-2.4f:2.4f,0,3.82f):new Vector3(stage==3?-7.35f:7.35f,0,-.15f);
                float w=stage<3?4.25f:2.85f,h=stage<3?1.68f:3.55f;
                foreach(var child in box.Cast<Transform>().Where(t=>t.name.EndsWith("Shadow")||new[]{"Footprint","Floor","LeftWall","RightWall","FrontWall","BackWall","OpenLid","Hinge","Tissue","LabelPlate","DividerV","DividerH1","DividerH2"}.Contains(t.name)).ToArray())child.gameObject.SetActive(false);
                Part(box,"TrayBase",new Vector3(.045f,.045f,-.045f),new Vector3(w+.14f,.075f,h+.14f),edge,.14f);
                Part(box,"TrayShell",new Vector3(0,.095f,0),new Vector3(w,.13f,h),shell,.14f);
                Part(box,"Liner",new Vector3(0,.175f,0),new Vector3(w-.25f,.045f,h-.25f),canvas,.10f);
                Part(box,"LeftRail",new Vector3(-w/2+.065f,.245f,0),new Vector3(.13f,.27f,h-.10f),shell,.06f);
                Part(box,"RightRail",new Vector3(w/2-.065f,.245f,0),new Vector3(.13f,.27f,h-.10f),shell,.06f);
                Part(box,"FrontRail",new Vector3(0,.23f,-h/2+.065f),new Vector3(w-.10f,.24f,.13f),shell,.055f);
                Part(box,"BackRail",new Vector3(0,.245f,h/2-.065f),new Vector3(w-.10f,.27f,.13f),shell,.055f);
                Part(box,"FrontAccent",new Vector3(0,.354f,-h/2+.045f),new Vector3(w*.54f,.025f,.11f),accent,.035f);
                var lid=Part(box,"RaisedLid",new Vector3(0,.095f,h/2+.46f),new Vector3(w+.01f,.085f,.86f),shell,.12f);lid.transform.localRotation=Quaternion.Euler(-6,0,0);
                var inset=Part(box,"LidInset",new Vector3(0,.147f,h/2+.46f),new Vector3(w-.23f,.025f,.63f),canvas,.09f);inset.transform.localRotation=Quaternion.Euler(-6,0,0);
                var ribbon=Part(box,"LidRibbon",new Vector3(0,.165f,h/2+.46f),new Vector3(w*.30f,.014f,.65f),accent,.035f);ribbon.transform.localRotation=Quaternion.Euler(-6,0,0);
                Part(box,"LidHinge",new Vector3(0,.145f,h/2+.045f),new Vector3(w-.24f,.045f,.07f),edge,.022f);
                Part(box,"TapeLeft",new Vector3(-w*.31f,.172f,h/2+.45f),new Vector3(.32f,.012f,.18f),tape,.025f).transform.localRotation=Quaternion.Euler(-6,8,0);
                Part(box,"TapeRight",new Vector3(w*.31f,.172f,h/2+.45f),new Vector3(.32f,.012f,.18f),tape,.025f).transform.localRotation=Quaternion.Euler(-6,-8,0);
                if(stage>=3){
                    Part(box,"PocketBand1",new Vector3(0,.205f,-(h-.3f)/6),new Vector3(w-.40f,.035f,.045f),accent,.018f);
                    Part(box,"PocketBand2",new Vector3(0,.205f,(h-.3f)/6),new Vector3(w-.40f,.035f,.045f),accent,.018f);
                }
                var collider=Ensure<BoxCollider>(box.gameObject);collider.center=new Vector3(0,.12f,0);collider.size=new Vector3(w,.24f,h);box.gameObject.layer=8;
                var supply=Ensure<WorkshopSupply>(box.gameObject);supply.Stage=stage;
                foreach(var oldShadow in box.Cast<Transform>().Where(t=>t.name.EndsWith("Shadow")).ToArray())UnityEngine.Object.DestroyImmediate(oldShadow.gameObject);
                foreach(var renderer in box.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.EndsWith("Shadow")&&!r.GetComponent<TMP_Text>()).ToArray()) {
                    var st=box.Find(renderer.name+"Shadow");var go=st?st.gameObject:new GameObject(renderer.name+"Shadow");go.transform.SetParent(box,false);go.transform.localPosition=renderer.transform.localPosition;go.transform.localRotation=renderer.transform.localRotation;
                    (Ensure<MeshFilter>(go)).sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;(Ensure<MeshRenderer>(go)).sharedMaterial=shadow;
                }
                Part(box,"NamePlate",new Vector3(0,.365f,-h/2+.025f),new Vector3(Mathf.Min(1.58f,w-.35f),.025f,.23f),labelMat,.06f);
                var label=box.Find("Label");if(!label){label=new GameObject("Label",typeof(RectTransform)).transform;label.SetParent(box,false);}
                var text=Ensure<TextMeshPro>(label.gameObject);text.font=font;text.text=stage switch{1=>"PCB",2=>"PLAKA",3=>"SWITCH",_=>"TUŞLAR"};text.fontSize=1.8f;text.fontStyle=FontStyles.Bold;text.color=new Color(.25f,.22f,.18f);text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(w,.25f);label.localPosition=new Vector3(0,.385f,-h/2+.012f);label.localRotation=Quaternion.Euler(90,0,0);
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
        static UnityEngine.UI.Button StandaloneIcon(string name,Transform parent,Texture texture,Vector2 anchor,Vector2 p,int size=86){var r=Rect(name,parent,anchor,p,new Vector2(size,size));var oldImage=r.GetComponent<UnityEngine.UI.Image>();if(oldImage)UnityEngine.Object.DestroyImmediate(oldImage);var oldLabel=r.Find("Label");if(oldLabel)UnityEngine.Object.DestroyImmediate(oldLabel.gameObject);var raw=Ensure<UnityEngine.UI.RawImage>(r.gameObject);raw.texture=texture;raw.uvRect=new Rect(0,0,1,1);raw.color=Color.white;var button=Ensure<UnityEngine.UI.Button>(r.gameObject);button.targetGraphic=raw;return button;}
        static UnityEngine.UI.Button Pill(string name,Transform parent,string value,Vector2 anchor,Vector2 p,Vector2 size,Color color) {
            var r=Rect(name,parent,anchor,p,size);var image=Ensure<UnityEngine.UI.Image>(r.gameObject);image.sprite=SoftUISprite();image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;
            var outline=Ensure<UnityEngine.UI.Outline>(r.gameObject);outline.effectColor=new Color(.20f,.22f,.20f,.55f);outline.effectDistance=new Vector2(2,-2);
            var button=Ensure<UnityEngine.UI.Button>(r.gameObject);button.targetGraphic=image;
            var label=Label("Label",r,value,18,new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*8);label.alignment=TextAlignmentOptions.Center;label.color=new Color(.97f,.92f,.82f);label.fontStyle=FontStyles.Bold;return button;
        }
        static void BuildPaintPalette(WorkshopGameMode game,WorkshopMenu menu,RectTransform hud) {
            var painter=Ensure<WorkshopKeyPainter>(game.gameObject);game.Painter=painter;painter.Game=game;painter.StudioShader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Shaders/KeycapStudio.shader");
            menu.PaintButton=StandaloneIcon("Paint",hud,paintIcon,Vector2.zero,new Vector2(429,25));
            var panel=Rect("PaintPalette",hud,new Vector2(0,.5f),new Vector2(270,0),new Vector2(356,930));foreach(Transform child in panel.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var background=Ensure<UnityEngine.UI.Image>(panel.gameObject);background.sprite=SoftUISprite();background.type=UnityEngine.UI.Image.Type.Sliced;background.color=new Color(.105f,.145f,.14f,.995f);
            var border=Ensure<UnityEngine.UI.Outline>(panel.gameObject);border.effectColor=new Color(.04f,.055f,.05f,.9f);border.effectDistance=new Vector2(4,-4);
            var title=Label("Title",panel,"KEYCAP BOYA STÜDYOSU",20,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(310,34));title.alignment=TextAlignmentOptions.Center;title.color=new Color(.96f,.90f,.78f);title.fontStyle=FontStyles.Bold;title.textWrappingMode=TextWrappingModes.NoWrap;
            var colorCard=Rect("ColorCard",panel,new Vector2(.5f,1),new Vector2(0,-184),new Vector2(314,268));var colorCardImage=Ensure<UnityEngine.UI.Image>(colorCard.gameObject);colorCardImage.sprite=SoftUISprite();colorCardImage.type=UnityEngine.UI.Image.Type.Sliced;colorCardImage.color=new Color(.15f,.205f,.19f,1);colorCardImage.raycastTarget=false;
            var brushCard=Rect("BrushCard",panel,new Vector2(.5f,1),new Vector2(0,-430),new Vector2(314,224));var brushCardImage=Ensure<UnityEngine.UI.Image>(brushCard.gameObject);brushCardImage.sprite=SoftUISprite();brushCardImage.type=UnityEngine.UI.Image.Type.Sliced;brushCardImage.color=new Color(.15f,.205f,.19f,1);brushCardImage.raycastTarget=false;
            var controlCard=Rect("ControlCard",panel,new Vector2(.5f,1),new Vector2(0,-699),new Vector2(314,304));var controlCardImage=Ensure<UnityEngine.UI.Image>(controlCard.gameObject);controlCardImage.sprite=SoftUISprite();controlCardImage.type=UnityEngine.UI.Image.Type.Sliced;controlCardImage.color=new Color(.15f,.205f,.19f,1);controlCardImage.raycastTarget=false;
            var current=Rect("CurrentColor",panel,new Vector2(.5f,1),new Vector2(0,-48),new Vector2(286,6));painter.CurrentColor=Ensure<UnityEngine.UI.Image>(current.gameObject);painter.CurrentColor.color=WorkshopKeyPainter.Palette[4];painter.CurrentColor.raycastTarget=false;
            var field=Rect("ColorField",panel,new Vector2(.5f,1),new Vector2(0,-126),new Vector2(250,146));painter.ColorField=Ensure<UnityEngine.UI.RawImage>(field.gameObject);painter.ColorField.raycastTarget=true;
            var colorCursor=Rect("ColorCursor",field,new Vector2(.5f,.5f),Vector2.zero,new Vector2(14,14));var colorCursorImage=Ensure<UnityEngine.UI.Image>(colorCursor.gameObject);colorCursorImage.color=Color.white;var colorCursorOutline=Ensure<UnityEngine.UI.Outline>(colorCursor.gameObject);colorCursorOutline.effectColor=Color.black;colorCursorOutline.effectDistance=new Vector2(2,-2);colorCursorImage.raycastTarget=false;painter.ColorCursor=colorCursor;
            var hue=Rect("HueField",panel,new Vector2(.5f,1),new Vector2(0,-211),new Vector2(250,18));painter.HueField=Ensure<UnityEngine.UI.RawImage>(hue.gameObject);painter.HueField.raycastTarget=true;
            var hueCursor=Rect("HueCursor",hue,new Vector2(.5f,.5f),Vector2.zero,new Vector2(7,30));var hueCursorImage=Ensure<UnityEngine.UI.Image>(hueCursor.gameObject);hueCursorImage.color=Color.white;var hueCursorOutline=Ensure<UnityEngine.UI.Outline>(hueCursor.gameObject);hueCursorOutline.effectColor=Color.black;hueCursorOutline.effectDistance=new Vector2(2,-2);hueCursorImage.raycastTarget=false;painter.HueCursor=hueCursor;
            painter.SwatchButtons=new UnityEngine.UI.Button[WorkshopKeyPainter.Palette.Length];
            for(int i=0;i<painter.SwatchButtons.Length;i++) {
                var r=Rect("Swatch"+i,panel,new Vector2(0,1),new Vector2(37+(i%4)*70,-245-(i/4)*40),new Vector2(52,30));
                var image=Ensure<UnityEngine.UI.Image>(r.gameObject);image.color=WorkshopKeyPainter.Palette[i];
                var outline=Ensure<UnityEngine.UI.Outline>(r.gameObject);outline.effectColor=new Color(.18f,.20f,.18f,.7f);outline.effectDistance=new Vector2(1,-1);
                var button=Ensure<UnityEngine.UI.Button>(r.gameObject);button.targetGraphic=image;painter.SwatchButtons[i]=button;
            }
            var toolLabel=Label("ToolLabel",panel,"FIRÇA UÇLARI",12,new Vector2(.5f,1),new Vector2(0,-327),new Vector2(290,22));toolLabel.alignment=TextAlignmentOptions.Center;toolLabel.color=new Color(.85f,.80f,.69f);
            var toolColor=new Color(.27f,.39f,.35f);
            painter.RoundButton=Pill("RoundBrush",panel,"DETAY",new Vector2(0,1),new Vector2(28,-361),new Vector2(92,36),toolColor);
            painter.SquareButton=Pill("SquareBrush",panel,"YASSI",new Vector2(.5f,1),new Vector2(0,-361),new Vector2(92,36),toolColor);
            painter.AirbrushButton=Pill("Airbrush",panel,"SPREY",new Vector2(1,1),new Vector2(-28,-361),new Vector2(92,36),toolColor);
            painter.SpongeButton=Pill("Sponge",panel,"SÜNGER",new Vector2(0,1),new Vector2(28,-403),new Vector2(92,36),toolColor);
            painter.DryBrushButton=Pill("DryBrush",panel,"KURU",new Vector2(.5f,1),new Vector2(0,-403),new Vector2(92,36),toolColor);
            painter.SplatterButton=Pill("Splatter",panel,"SIÇRAT",new Vector2(1,1),new Vector2(-28,-403),new Vector2(92,36),toolColor);
            painter.EraserButton=Pill("Eraser",panel,"SİLGİ",new Vector2(.5f,1),new Vector2(0,-445),new Vector2(290,34),new Color(.45f,.32f,.29f));
            var shapeLabel=Label("ShapeLabel",panel,"ŞEKİL ARAÇLARI",12,new Vector2(.5f,1),new Vector2(0,-480),new Vector2(290,20));shapeLabel.alignment=TextAlignmentOptions.Center;shapeLabel.color=new Color(.85f,.80f,.69f);
            var shapeColor=new Color(.31f,.38f,.43f);
            painter.LineButton=Pill("Line",panel,"ÇİZGİ",new Vector2(0,1),new Vector2(28,-512),new Vector2(92,34),shapeColor);
            painter.RectangleButton=Pill("Rectangle",panel,"KUTU",new Vector2(.5f,1),new Vector2(0,-512),new Vector2(92,34),shapeColor);
            painter.EllipseButton=Pill("Ellipse",panel,"ELİPS",new Vector2(1,1),new Vector2(-28,-512),new Vector2(92,34),shapeColor);
            var sizeLabel=Label("SizeLabel",panel,"BOYUT",11,new Vector2(.5f,1),new Vector2(0,-548),new Vector2(290,18));sizeLabel.alignment=TextAlignmentOptions.Center;sizeLabel.color=new Color(.85f,.80f,.69f);
            var slider=Rect("BrushSize",panel,new Vector2(.5f,1),new Vector2(0,-573),new Vector2(270,24));painter.BrushSize=Ensure<UnityEngine.UI.Slider>(slider.gameObject);
            var track=Rect("Track",slider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,7));var trackImage=Ensure<UnityEngine.UI.Image>(track.gameObject);trackImage.color=new Color(.75f,.70f,.59f,.65f);
            var handle=Rect("Handle",slider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24));var handleImage=Ensure<UnityEngine.UI.Image>(handle.gameObject);handleImage.color=new Color(.97f,.91f,.79f);painter.BrushSize.handleRect=handle;painter.BrushSize.targetGraphic=handleImage;
            painter.BrushReadout=Label("BrushReadout",panel,"DETAY · 25",13,new Vector2(.5f,1),new Vector2(0,-604),new Vector2(270,22));painter.BrushReadout.alignment=TextAlignmentOptions.Center;painter.BrushReadout.color=new Color(.96f,.90f,.78f);
            var hardnessLabel=Label("HardnessLabel",panel,"KENAR SERTLİĞİ",11,new Vector2(.5f,1),new Vector2(0,-632),new Vector2(270,18));hardnessLabel.alignment=TextAlignmentOptions.Center;hardnessLabel.color=new Color(.85f,.80f,.69f);
            var hardnessSlider=Rect("Hardness",panel,new Vector2(.5f,1),new Vector2(0,-657),new Vector2(270,22));painter.Hardness=Ensure<UnityEngine.UI.Slider>(hardnessSlider.gameObject);var hardnessTrack=Rect("Track",hardnessSlider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,7));var hardnessTrackImage=Ensure<UnityEngine.UI.Image>(hardnessTrack.gameObject);hardnessTrackImage.color=new Color(.75f,.70f,.59f,.65f);var hardnessHandle=Rect("Handle",hardnessSlider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20));var hardnessHandleImage=Ensure<UnityEngine.UI.Image>(hardnessHandle.gameObject);hardnessHandleImage.color=new Color(.97f,.91f,.79f);painter.Hardness.handleRect=hardnessHandle;painter.Hardness.targetGraphic=hardnessHandleImage;
            var opacityLabel=Label("OpacityLabel",panel,"BOYA YOĞUNLUĞU",11,new Vector2(.5f,1),new Vector2(0,-686),new Vector2(270,18));opacityLabel.alignment=TextAlignmentOptions.Center;opacityLabel.color=new Color(.85f,.80f,.69f);
            var opacitySlider=Rect("Opacity",panel,new Vector2(.5f,1),new Vector2(0,-711),new Vector2(270,22));painter.Opacity=Ensure<UnityEngine.UI.Slider>(opacitySlider.gameObject);var opacityTrack=Rect("Track",opacitySlider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,7));var opacityTrackImage=Ensure<UnityEngine.UI.Image>(opacityTrack.gameObject);opacityTrackImage.color=new Color(.75f,.70f,.59f,.65f);var opacityHandle=Rect("Handle",opacitySlider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20));var opacityHandleImage=Ensure<UnityEngine.UI.Image>(opacityHandle.gameObject);opacityHandleImage.color=new Color(.97f,.91f,.79f);painter.Opacity.handleRect=opacityHandle;painter.Opacity.targetGraphic=opacityHandleImage;
            var stabilizationLabel=Label("StabilizationLabel",panel,"ÇİZGİ DÜZELTME",11,new Vector2(.5f,1),new Vector2(0,-740),new Vector2(270,18));stabilizationLabel.alignment=TextAlignmentOptions.Center;stabilizationLabel.color=new Color(.85f,.80f,.69f);
            var stabilizationSlider=Rect("Stabilization",panel,new Vector2(.5f,1),new Vector2(0,-765),new Vector2(270,22));painter.Stabilization=Ensure<UnityEngine.UI.Slider>(stabilizationSlider.gameObject);var stabilizationTrack=Rect("Track",stabilizationSlider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,7));var stabilizationTrackImage=Ensure<UnityEngine.UI.Image>(stabilizationTrack.gameObject);stabilizationTrackImage.color=new Color(.75f,.70f,.59f,.65f);var stabilizationHandle=Rect("Handle",stabilizationSlider,new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20));var stabilizationHandleImage=Ensure<UnityEngine.UI.Image>(stabilizationHandle.gameObject);stabilizationHandleImage.color=new Color(.97f,.91f,.79f);painter.Stabilization.handleRect=stabilizationHandle;painter.Stabilization.targetGraphic=stabilizationHandleImage;
            painter.RedoButton=Pill("RedoPaint",panel,"YİNELE",new Vector2(0,1),new Vector2(34,-811),new Vector2(136,36),new Color(.34f,.40f,.35f));
            painter.ClearButton=Pill("ClearPaint",panel,"TUŞU TEMİZLE",new Vector2(1,1),new Vector2(-34,-811),new Vector2(136,36),new Color(.45f,.32f,.29f));
            var shortcut=Label("Shortcut",panel,"Ctrl+Z geri al  ·  Ctrl+Y yinele  ·  Alt renk al",11,new Vector2(.5f,1),new Vector2(0,-850),new Vector2(310,20));shortcut.alignment=TextAlignmentOptions.Center;shortcut.color=new Color(.68f,.68f,.60f);
            var cursorRect=Rect("BrushCursor",hud,new Vector2(.5f,.5f),Vector2.zero,new Vector2(32,32));var oldCursorText=cursorRect.GetComponent<TMP_Text>();if(oldCursorText)UnityEngine.Object.DestroyImmediate(oldCursorText);painter.BrushCursor=Ensure<UnityEngine.UI.Image>(cursorRect.gameObject);painter.BrushCursor.sprite=BrushRingSprite();painter.BrushCursor.raycastTarget=false;painter.BrushCursor.gameObject.SetActive(false);
            var editor=Rect("KeycapPaintEditor",hud,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1920,1080));var shade=Ensure<UnityEngine.UI.Image>(editor.gameObject);shade.color=new Color(.10f,.13f,.12f,.84f);painter.EditorPanel=editor.gameObject;
            var card=Rect("StudioCard",editor,new Vector2(.5f,.5f),new Vector2(190,52),new Vector2(1320,800));var cardImage=Ensure<UnityEngine.UI.Image>(card.gameObject);cardImage.sprite=SoftUISprite();cardImage.type=UnityEngine.UI.Image.Type.Sliced;cardImage.color=new Color(.88f,.81f,.68f,.995f);var cardOutline=Ensure<UnityEngine.UI.Outline>(card.gameObject);cardOutline.effectColor=new Color(.12f,.15f,.13f,.9f);cardOutline.effectDistance=new Vector2(5,-5);
            painter.EditorTitle=Label("EditorTitle",card,"TUŞ ATÖLYESİ",27,new Vector2(.5f,1),new Vector2(-170,-34),new Vector2(930,48));painter.EditorTitle.alignment=TextAlignmentOptions.Center;painter.EditorTitle.fontStyle=FontStyles.Bold;painter.EditorTitle.textWrappingMode=TextWrappingModes.NoWrap;
            painter.LegendButton=Pill("LegendVisibility",card,"HARF  AÇIK",new Vector2(1,1),new Vector2(-202,-31),new Vector2(172,54),new Color(.35f,.43f,.39f));painter.LegendButtonLabel=painter.LegendButton.transform.Find("Label").GetComponent<TMP_Text>();
            painter.DoneButton=Pill("Done",card,"TAMAM",new Vector2(1,1),new Vector2(-24,-31),new Vector2(150,54),new Color(.32f,.48f,.40f));
            var mount=Rect("KeycapMount",card,new Vector2(.5f,.5f),new Vector2(0,6),new Vector2(1170,550));var mountImage=Ensure<UnityEngine.UI.Image>(mount.gameObject);mountImage.sprite=SoftUISprite();mountImage.type=UnityEngine.UI.Image.Type.Sliced;mountImage.color=new Color(.17f,.22f,.20f,1);var mountOutline=Ensure<UnityEngine.UI.Outline>(mount.gameObject);mountOutline.effectColor=new Color(.07f,.08f,.07f,.85f);mountOutline.effectDistance=new Vector2(5,-5);
            var surface=Rect("PaintSurface",mount,new Vector2(.5f,.5f),Vector2.zero,new Vector2(430,430));var legacyRaw=surface.GetComponent<UnityEngine.UI.RawImage>();if(legacyRaw)UnityEngine.Object.DestroyImmediate(legacyRaw);var keyMask=Ensure<UnityEngine.UI.Image>(surface.gameObject);keyMask.sprite=RoundedKeySprite();keyMask.type=UnityEngine.UI.Image.Type.Sliced;keyMask.color=Color.white;var keyShadow=Ensure<UnityEngine.UI.Shadow>(surface.gameObject);keyShadow.effectColor=new Color(.04f,.05f,.04f,.8f);keyShadow.effectDistance=new Vector2(10,-12);var mask=Ensure<UnityEngine.UI.Mask>(surface.gameObject);mask.showMaskGraphic=false;
            var pixels=Rect("PaintPixels",surface,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);pixels.anchorMin=Vector2.zero;pixels.anchorMax=Vector2.one;pixels.offsetMin=pixels.offsetMax=Vector2.zero;painter.EditorSurface=Ensure<UnityEngine.UI.RawImage>(pixels.gameObject);painter.EditorSurface.color=Color.white;painter.EditorSurface.raycastTarget=true;var roundedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/RoundedKeyUI.mat");if(!roundedMaterial){roundedMaterial=new Material(Shader.Find("CozyBoard/RoundedKeyUI"));AssetDatabase.CreateAsset(roundedMaterial,Root+"/Materials/RoundedKeyUI.mat");}painter.EditorSurface.material=roundedMaterial;
            var maskedLegend=surface.Find("KeyLegend");if(maskedLegend)UnityEngine.Object.DestroyImmediate(maskedLegend.gameObject);
            painter.EditorLegend=Label("KeyLegend",mount,"A",42,new Vector2(.5f,.5f),Vector2.zero,new Vector2(360,110));painter.EditorLegend.alignment=TextAlignmentOptions.Center;painter.EditorLegend.color=new Color(.16f,.19f,.18f,.78f);painter.EditorLegend.fontStyle=FontStyles.Bold;
            painter.EditorHint=Label("EditorHint",card,"Klavyeden boyamak istediğin tuşa tıkla",18,new Vector2(.5f,0),new Vector2(0,28),new Vector2(1050,38));painter.EditorHint.alignment=TextAlignmentOptions.Center;painter.EditorHint.fontStyle=FontStyles.Bold;
            editor.gameObject.SetActive(false);editor.SetAsLastSibling();panel.SetAsLastSibling();painter.BrushCursor.rectTransform.SetAsLastSibling();
            painter.PalettePanel=panel.gameObject;panel.gameObject.SetActive(false);
        }
        static void BuildUI(WorkshopGameMode game) {
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var hud=Rect("PaintedHUD",canvas.transform,Vector2.zero,Vector2.zero,Vector2.zero);hud.anchorMax=Vector2.one;hud.offsetMax=Vector2.zero;
            var menu=Ensure<WorkshopMenu>(game.gameObject);game.Menu=menu;menu.Game=game;
            game.TestButton.gameObject.SetActive(false);game.NewOrderButton.gameObject.SetActive(false);
            menu.SaveButton=Icon("Save",hud,0,Vector2.one,new Vector2(-224,-25));menu.OrderButton=Icon("Order",hud,1,Vector2.one,new Vector2(-123,-25));menu.SettingsButton=Icon("Settings",hud,2,Vector2.one,new Vector2(-22,-25));
            menu.PointerButton=Icon("Pointer",hud,4,Vector2.zero,new Vector2(25,25));menu.MoveButton=Icon("Move",hud,5,Vector2.zero,new Vector2(126,25));menu.RotateButton=Icon("Rotate",hud,6,Vector2.zero,new Vector2(227,25));menu.UndoButton=Icon("Undo",hud,7,Vector2.zero,new Vector2(328,25));
            BuildPaintPalette(game,menu,hud);
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
