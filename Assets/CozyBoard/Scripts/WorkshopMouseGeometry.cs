using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyBoard {
    // Native meshes and the existing Painted shader, with the same restrained atelier palette.
    public sealed partial class WorkshopMouseProduct {
        readonly List<Object> owned=new();
        readonly List<WorkshopItem> parts=new();
        public IReadOnlyList<WorkshopItem> Parts=>parts;
        public Mesh BaseMesh{get;private set;}
        public Material[] BaseMaterials{get;private set;}
        readonly Dictionary<WorkshopItem,Transform> films=new();
        readonly Dictionary<WorkshopItem,Transform> guides=new();
        Transform tray;Material cream,sage,ink,metal,copper,pcb,film;
        public static Vector3 SkateSlot(int i)=>new Vector3(i%2==0?-.45f:.45f,-.018f,i<2?-.91f:.87f);
        public static Vector3 ScrewSlot(int i){var p=SkateSlot(i);p.y=.015f;return p;}
        Material Paint(string name,Color color){var m=new Material(Shader.Find("CozyBoard/Painted")){name=name};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
        Mesh Own(Mesh mesh){owned.Add(mesh);return mesh;}
        Mesh Box(Vector3 size,Vector3 center,float radius=.06f){var m=Own(WorkshopPropMesh.RoundedBox(size,radius));var v=m.vertices;for(int i=0;i<v.Length;i++)v[i]+=center;m.vertices=v;m.RecalculateBounds();return m;}
        Mesh Combine(string name,params Mesh[] meshes){var m=Own(new Mesh{name=name});m.CombineMeshes(meshes.Select(x=>new CombineInstance{mesh=x,transform=Matrix4x4.identity}).ToArray(),false);return m;}
        static float HalfWidth(float z){
            if(z<-.80f)return .80f*Mathf.Sqrt(Mathf.Clamp01(1-Mathf.Pow((z+.80f)/.62f,2)));
            if(z>.95f)return .73f*Mathf.Sqrt(Mathf.Clamp01(1-Mathf.Pow((z-.95f)/.47f,2)));
            return Mathf.Lerp(.80f,.73f,(z+.80f)/1.75f)-.035f*Mathf.Exp(-Mathf.Pow((z+.15f)/.42f,2));
        }
        Mesh OvalRing(string name,Vector2 outer,Vector2 inner,float height,Vector3 center){
            const int n=64;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;var r=ring<2?outer:inner;var p=new Vector3(Mathf.Cos(a)*r.x,ring==1||ring==2?height*.5f:-height*.5f,Mathf.Sin(a)*r.y);v.Add(p+center);uv.Add(new Vector2(p.x/(outer.x*2)+.5f,p.z/(outer.y*2)+.5f));}
            for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n,c=((ring+1)%4)*n+i,d=((ring+1)%4)*n+(i+1)%n;t.AddRange(new[]{a,c,b,b,c,d});}
            var m=Own(new Mesh{name=name});m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static float ShellHeight(float x,float z){float hump=Mathf.Pow(Mathf.Max(0,Mathf.Sin((z+1.42f)/2.84f*Mathf.PI)),.7f);return .18f+.55f*hump*Mathf.Sqrt(Mathf.Clamp01(1-Mathf.Pow(x/Mathf.Max(.02f,HalfWidth(z)),2)));}
        Mesh ShellPatch(string name,float minZ,float maxZ,int side,Vector3 slot){
            const int rows=24,cols=16;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int lower=0;lower<2;lower++)for(int row=0;row<=rows;row++){float z=Mathf.Lerp(minZ,maxZ,(float)row/rows),w=HalfWidth(z)-.006f;float inner=Mathf.Max(z>1.16f?.085f:.012f,.012f+.108f*Mathf.Sqrt(Mathf.Clamp01(1-Mathf.Pow((z-.76f)/.37f,2))));float minX=side==1?inner:-w,maxX=side==-1?-inner:w;for(int col=0;col<=cols;col++){float x=Mathf.Lerp(minX,maxX,(float)col/cols);bool outer=side==0?(col==0||col==cols):(side==1?col==cols:col==0);float y=lower==1&&outer?.135f:ShellHeight(x,z)-lower*.05f;v.Add(new Vector3(x,y,z)-slot);uv.Add(new Vector2(x/1.7f+.5f,z/2.84f+.5f));}}
            int n=(rows+1)*(cols+1);for(int row=0;row<rows;row++)for(int col=0;col<cols;col++){int a=row*(cols+1)+col,b=a+cols+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1,a+n,a+1+n,b+n,a+1+n,b+1+n,b+n});}
            var edge=new List<int>();for(int c=0;c<=cols;c++)edge.Add(c);for(int r=1;r<=rows;r++)edge.Add(r*(cols+1)+cols);for(int c=cols-1;c>=0;c--)edge.Add(rows*(cols+1)+c);for(int r=rows-1;r>0;r--)edge.Add(r*(cols+1));for(int i=0;i<edge.Count;i++){int a=edge[i],b=edge[(i+1)%edge.Count];t.AddRange(new[]{a,b,b+n,a,b+n,a+n});}
            var m=Own(new Mesh{name=name});m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        Mesh Cradle(){const int n=96;var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,z=Mathf.Sin(a)*1.42f,x=(Mathf.Cos(a)<0?-1:1)*HalfWidth(z);if(ring>=2){x=Mathf.Cos(a)*.30f;z=Mathf.Sin(a)*.33f;}var p=new Vector3(x,ring==1||ring==2?.13f:.012f,z);v.Add(p);uv.Add(new Vector2(x/1.7f+.5f,z/2.84f+.5f));}for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n,c=((ring+1)%4)*n+i,d=((ring+1)%4)*n+(i+1)%n;t.AddRange(new[]{a,c,b,b,c,d});}var m=Own(new Mesh{name="Contoured mouse cradle with open optical window"});m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        Mesh Wheel(){
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int n=48;
            for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;float r=(ring==0||ring==3?.205f:.235f)+(i%2==0?.007f:0);v.Add(new Vector3(new[]{-.09f,-.065f,.065f,.09f}[ring],Mathf.Cos(a)*r,Mathf.Sin(a)*r));uv.Add(new Vector2((float)ring/3,(float)i/n));}
            for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n;t.AddRange(new[]{a,b,a+n,b,b+n,a+n});}
            for(int side=0;side<2;side++){int c=v.Count;v.Add(new Vector3(side==0?-.09f:.09f,0,0));uv.Add(Vector2.one*.5f);for(int i=0;i<n;i++){int a=side*3*n+i,b=side*3*n+(i+1)%n;t.AddRange(side==0?new[]{c,b,a}:new[]{c,a,b});}}
            var m=Own(new Mesh{name="Fine ribbed scroll wheel"});m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        Mesh Wire(Vector3[] points,float radius){
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();const int n=8;
            for(int i=0;i<points.Length;i++){var axis=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(i-1,0)]).normalized;var across=Vector3.Cross(axis,Vector3.up).normalized;var normal=Vector3.Cross(axis,across).normalized;for(int j=0;j<=n;j++){float a=j*Mathf.PI*2/n;v.Add(points[i]+(across*Mathf.Cos(a)+normal*Mathf.Sin(a))*radius);uv.Add(new Vector2((float)j/n,(float)i/points.Length));}}
            for(int i=0;i<points.Length-1;i++)for(int j=0;j<n;j++){int a=i*(n+1)+j,b=a+n+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var m=Own(new Mesh{name="Soft braided mouse lead"});m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        Mesh Positioned(Mesh mesh,Vector3 position,Quaternion rotation){var v=mesh.vertices;var n=mesh.normals;for(int i=0;i<v.Length;i++){v[i]=rotation*v[i]+position;n[i]=rotation*n[i];}mesh.vertices=v;mesh.normals=n;mesh.RecalculateBounds();return mesh;}
        void UpdatePlacementGuides(bool visible){
            var held=Game.Controller.Dragged;var next=held&&held.Id.StartsWith("Mouse_")?held:Game.Stock.FirstOrDefault();
            foreach(var pair in guides)pair.Value.gameObject.SetActive(visible&&!Busy&&!pair.Key.Fitted&&next&&(next.Stage==5?pair.Key.Stage==5:pair.Key==next)&&((pair.Key.Stage==5)==State.underbody));
        }
        WorkshopItem Part(string id,string label,string kind,int stage,Vector3 slot,Mesh mesh,params Material[] materials){
            var go=new GameObject(id);go.layer=8;go.transform.SetParent(Game.Controller.PartsRoot,false);var item=go.AddComponent<WorkshopItem>();item.Id=id;item.Label=label;item.Kind=kind;item.Stage=stage;item.Slot=slot;item.BoundsCenter=mesh.bounds.center;item.BoundsSize=mesh.bounds.size;
            var body=new GameObject("Painted native part");body.transform.SetParent(go.transform,false);body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterials=materials;
            var hit=go.AddComponent<BoxCollider>();hit.center=item.BoundsCenter;hit.size=Vector3.Max(item.BoundsSize,new Vector3(.12f,.08f,.12f));item.Cache();parts.Add(item);Game.Controller.RegisterPart(item);return item;
        }
        void BuildGeometry(){
            cream=Paint("Paint_key_mouse_ivory",new Color(.89f,.84f,.69f));sage=Paint("Paint_key_mouse_sage",new Color(.39f,.51f,.43f));ink=Paint("Mouse graphite recess",new Color(.18f,.24f,.22f));metal=Paint("Mouse brushed contacts",new Color(.61f,.66f,.61f));copper=Paint("Mouse copper traces",new Color(.77f,.67f,.38f));pcb=Paint("Paint_pcb_mouse",new Color(.24f,.42f,.32f));film=Paint("Mouse skate release paper",new Color(.96f,.91f,.77f));
            // The sensor opening is real; four base rails surround it.
            BaseMesh=Resources.Load<Mesh>("Mouse/BASE");if(!BaseMesh)BaseMesh=Cradle();BaseMaterials=new[]{cream};
            var circuit=new List<Mesh>{Box(new Vector3(1.30f,.045f,2.13f),Vector3.zero,.17f),Box(new Vector3(.23f,.07f,.28f),new Vector3(-.13f,.06f,-.30f),.02f)};var circuitMats=new List<Material>{pcb,ink};
            for(int i=0;i<6;i++){circuit.Add(Box(new Vector3(.02f,.006f,1.2f),new Vector3(-.50f+i*.2f,.027f,0),.004f));circuitMats.Add(copper);circuit.Add(Box(new Vector3(.045f,.008f,.06f),new Vector3(-.5f+i*.2f,.030f,.72f),.008f));circuitMats.Add(copper);}
            var authored=Resources.Load<Mesh>("Mouse/PCB");Part("Mouse_PCB","Mouse devresi","mouse-part",1,new Vector3(0,.20f,0),authored?authored:Combine("Mouse PCB, controller and copper routes",circuit.ToArray()),authored?new[]{pcb,ink,copper,metal,cream}:circuitMats.ToArray());
            var sensorMeshes=new List<Mesh>{Box(new Vector3(.30f,.075f,.34f),Vector3.zero,.025f),Box(new Vector3(.12f,.032f,.15f),new Vector3(0,-.075f,0),.035f),Box(new Vector3(.06f,.026f,.05f),new Vector3(0,-.063f,.145f),.015f)};
            var sensorMats=new List<Material>{ink,Paint("Optical sensor aperture",new Color(.10f,.19f,.18f)),Paint("Optical sensor light",new Color(.66f,.29f,.18f))};
            for(int side=0;side<2;side++)for(int pin=0;pin<4;pin++){sensorMeshes.Add(Box(new Vector3(.068f,.018f,.036f),new Vector3(side==0?-.168f:.168f,.009f,-.12f+pin*.08f),.004f));sensorMats.Add(metal);}
            Part("Mouse_Sensor","Optik sensör modülü","mouse-sensor",2,new Vector3(0,.064f,0),Combine("Removable optical sensor module with contact pins",sensorMeshes.ToArray()),sensorMats.ToArray());
            Part("Mouse_Lens","Sensör merceği","mouse-part",2,new Vector3(0,-.012f,0),Combine("Optical lens rim and polished light guide",OvalRing("Sensor lens rim",new Vector2(.19f,.22f),new Vector2(.075f,.10f),.03f,Vector3.zero),OvalRing("Polished light guide",new Vector2(.09f,.115f),new Vector2(.061f,.082f),.019f,new Vector3(0,-.019f,0))),ink,metal);
            var points=new Vector3[49];for(int i=0;i<points.Length;i++){float f=(float)i/(points.Length-1);points[i]=new Vector3(.38f*Mathf.Sin(f*Mathf.PI*1.2f)+f*.35f,.017f,.04f+f*2.0f);}
            var tangent=(points[48]-points[47]).normalized;var plugRotation=Quaternion.LookRotation(tangent,Vector3.up);var cableEnd=points[48];
            Part("Mouse_Cable","USB kablosu","mouse-part",2,new Vector3(0,.075f,1.23f),Combine("Relaxed cable, tangent-aligned USB plug and strain relief",Wire(points,.019f),Box(new Vector3(.12f,.10f,.22f),new Vector3(0,.018f,.075f)),Positioned(Box(new Vector3(.20f,.10f,.25f),Vector3.zero,.04f),cableEnd+tangent*.13f,plugRotation),Positioned(Box(new Vector3(.16f,.074f,.15f),Vector3.zero,.012f),cableEnd+tangent*.32f,plugRotation),Positioned(Box(new Vector3(.11f,.008f,.035f),Vector3.zero,.006f),cableEnd+tangent*.33f+Vector3.up*.04f,plugRotation)),ink,ink,sage,metal,ink);
            for(int i=0;i<2;i++)Part(i==0?"Mouse_LeftSwitch":"Mouse_RightSwitch",i==0?"Sol mikro switch":"Sağ mikro switch","mouse-switch",3,new Vector3(i==0?-.40f:.40f,.26f,.75f),Combine("Small click switch with copper terminals",Box(new Vector3(.18f,.11f,.38f),Vector3.zero,.02f),Box(new Vector3(.09f,.06f,.14f),new Vector3(0,.075f,.07f),.02f),Box(new Vector3(.25f,.018f,.22f),new Vector3(0,-.04f,0),.005f)),ink,cream,copper);
            for(int i=0;i<2;i++){
                Part("Mouse_SideSwitch_"+i,"Yan düğme mikro switch "+(i+1),"mouse-side-switch",3,new Vector3(-.64f,.25f,i==0?-.06f:.29f),Combine("Side click mechanism",Box(new Vector3(.14f,.12f,.25f),Vector3.zero,.018f),Box(new Vector3(.06f,.08f,.13f),new Vector3(-.09f,0,0),.012f),Box(new Vector3(.08f,.024f,.29f),new Vector3(.06f,-.042f,0),.004f)),ink,cream,metal);
            }
            Part("Mouse_Wheel","Kaydırma tekerleği","mouse-part",3,new Vector3(0,.46f,.76f),Wheel(),sage);
            Part("Mouse_Shell","Mouse üst gövdesi","mouse-shell",4,new Vector3(0,.14f,-.57f),Resources.Load<Mesh>("Mouse/PALM")??ShellPatch("Continuous mouse palm shell",-1.416f,.26f,0,new Vector3(0,.14f,-.57f)),cream);
            for(int i=0;i<2;i++)Part(i==0?"Mouse_LeftButton":"Mouse_RightButton",i==0?"Sol düğme kapağı":"Sağ düğme kapağı","mouse-button",4,new Vector3(i==0?-.43f:.43f,.30f,.64f),Resources.Load<Mesh>(i==0?"Mouse/LEFT":"Mouse/RIGHT")??ShellPatch("Curved mouse button",.285f,1.416f,i==0?-1:1,new Vector3(i==0?-.43f:.43f,.30f,.64f)),cream);
            for(int i=0;i<2;i++)Part("Mouse_SideButton_"+i,"Makro yan düğme "+(i+1),"mouse-button",4,new Vector3(-.752f,.255f,i==0?-.06f:.29f),Combine("Moulded macro button and recessed collar",Box(new Vector3(.055f,.112f,.285f),Vector3.zero,.04f),Box(new Vector3(.030f,.085f,.245f),new Vector3(-.032f,0,0),.035f)),ink,sage);
            var ptfe=Paint("Warm white PTFE glide pads",new Color(.94f,.94f,.88f));
            for(int i=0;i<4;i++){
                var foot=Part("Mouse_Skate_"+i,"Kaydırıcı ayak "+(i+1),"mouse-skate",5,SkateSlot(i),Combine("PTFE pad and thin recessed backing",Box(new Vector3(.41f,.018f,.31f),new Vector3(0,.015f,0),.11f),Box(new Vector3(.38f,.034f,.28f),new Vector3(0,-.011f,0),.10f)),ink,ptfe);
                var paper=new GameObject("Peelable protective film");paper.transform.SetParent(foot.transform,false);paper.AddComponent<MeshFilter>().sharedMesh=Box(new Vector3(.39f,.008f,.29f),new Vector3(.01f,-.037f,.01f),.10f);paper.AddComponent<MeshRenderer>().sharedMaterial=film;films[foot]=paper.transform;
            }
            tray=new GameObject("Mouse component tray").transform;tray.SetParent(Game.Controller.transform,false);tray.localPosition=new Vector3(-4.55f,.02f,.55f);tray.gameObject.AddComponent<MeshFilter>().sharedMesh=Box(new Vector3(3.0f,.065f,3.50f),Vector3.zero,.24f);tray.gameObject.AddComponent<MeshRenderer>().sharedMaterial=Paint("Mouse kit stoneware",new Color(.78f,.77f,.65f));
            var guideMaterial=Paint("Sage mouse fitting guides",new Color(.48f,.63f,.50f));
            foreach(var item in parts){var marker=new GameObject("Placement guide "+item.Id).transform;marker.SetParent(Game.Controller.Lookup["Case"].transform,false);marker.localPosition=item.Slot+Vector3.up*(item.Stage==5?-.020f:.016f);var size=item.Stage==5?new Vector2(.235f,.185f):new Vector2(Mathf.Min(.5f,item.BoundsSize.x*.53f+.07f),Mathf.Min(.7f,item.BoundsSize.z*.53f+.07f));marker.gameObject.AddComponent<MeshFilter>().sharedMesh=OvalRing("Subtle fitting outline",size,size-Vector2.one*.022f,.006f,Vector3.zero);marker.gameObject.AddComponent<MeshRenderer>().sharedMaterial=guideMaterial;marker.gameObject.SetActive(false);guides[item]=marker;}
        }
        public void RefreshPalette(){if(!cream)return;var colors=new[]{new Color(.89f,.84f,.69f),new Color(.59f,.69f,.51f),new Color(.68f,.59f,.76f)};cream.SetColor("_BaseColor",colors[Mathf.Clamp(Game.Shop.Data.mouseColor,0,2)]);}
        void OnDestroy(){foreach(var value in owned)if(value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}}
    }
}
