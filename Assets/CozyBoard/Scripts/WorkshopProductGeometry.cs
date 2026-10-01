using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyBoard {
    // Product-specific native geometry; original authored keyboard meshes are never edited.
    public sealed class WorkshopProductGeometry : MonoBehaviour {
        readonly Dictionary<WorkshopItem,Vector3> slots=new();
        readonly Dictionary<string,Mesh> originals=new(),macroMeshes=new();
        readonly Dictionary<string,(Vector3 center,Vector3 size)> bounds=new();
        readonly Dictionary<Transform,(Vector3 position,Vector3 scale)> details=new();
        WorkshopGameMode game;bool initialized,macro;int lastProduct;readonly Dictionary<string,Material[]> originalMaterials=new();
        public void Apply(WorkshopGameMode owner){
            game=owner;
            if(!initialized){
                initialized=true;foreach(var item in game.Controller.Items)slots[item]=item.Slot;
                foreach(string id in new[]{"Case","PCB","Plate"}){var item=game.Controller.Lookup[id];originals[id]=item.Visual.GetComponent<MeshFilter>().sharedMesh;originalMaterials[id]=item.Visual.sharedMaterials;bounds[id]=(item.BoundsCenter,item.BoundsSize);}
                foreach(Transform child in game.Controller.Lookup["Case"].transform)if(child.name is "DarkCushion" or "LowerShell" or "MakerBadge")details[child]=(child.localPosition,child.localScale);
                macroMeshes["Case"]=Rim();macroMeshes["PCB"]=Pcb(game.Controller.Lookup["PCB"],slots[game.Controller.Lookup["Switch_01"]]);macroMeshes["Plate"]=Plate();
            }
            foreach(var pair in slots)pair.Key.Slot=pair.Value;
            if(game.IsMacro)for(int i=1;i<=6;i++){float x=((i-1)%3-1)*.48f,z=(i<=3?1:-1)*.27f;foreach(string prefix in new[]{"Keycap_","Switch_"}){var item=game.Controller.Lookup[$"{prefix}{i:00}"];item.Slot=new Vector3(x,slots[item].y,z);}}
            foreach(var id in originals.Keys){
                var item=game.Controller.Lookup[id];item.Visual.GetComponent<MeshFilter>().sharedMesh=game.IsMouse&&id=="Case"?game.MouseProduct.BaseMesh:game.IsMacro?macroMeshes[id]:originals[id];item.Visual.sharedMaterials=game.IsMouse&&id=="Case"?game.MouseProduct.BaseMaterials:originalMaterials[id];
                var b=game.IsMouse&&id=="Case"?game.MouseProduct.BaseMesh.bounds:game.IsMacro?macroMeshes[id].bounds:new Bounds(bounds[id].center,bounds[id].size);item.BoundsCenter=b.center;item.BoundsSize=b.size;item.Hitbox.center=b.center;item.Hitbox.size=b.size;
                if(lastProduct!=(game.IsMouse?2:game.IsMacro?1:0))game.Controller.RefreshShadowMesh(item);
            }
            foreach(var pair in details){pair.Key.gameObject.SetActive(!game.IsMouse);var p=pair.Value.position;var scale=pair.Value.scale;if(game.IsMacro){p.x*=1.85f/6.64f;p.z*=1.43f/2.67f;scale.x*=1.85f/6.64f;scale.z*=1.43f/2.67f;}pair.Key.localPosition=p;pair.Key.localScale=scale;}
            macro=game.IsMacro;lastProduct=game.IsMouse?2:game.IsMacro?1:0;
        }
        static Mesh Box(Vector3 size,Vector3 center){var mesh=WorkshopPropMesh.RoundedBox(size,.09f);var vertices=mesh.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]+=center;mesh.vertices=vertices;mesh.RecalculateBounds();return mesh;}
        static Mesh Pcb(WorkshopItem item,Vector3 sourceSlot){
            // Reuse the authored keyboard's actual pads, holes, traces and silkscreen.
            // Keeping its material slots also avoids drawing every material over one box.
            var source=item.Visual.GetComponent<MeshFilter>().sharedMesh;
            var materials=item.Visual.sharedMaterials;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var triangles=Enumerable.Range(0,source.subMeshCount).Select(_=>new List<int>()).ToArray();
            int MaterialSlot(string name)=>System.Array.FindIndex(materials,m=>m.name=="Paint_"+name);
            void Add(Mesh mesh,int sub,int target,Vector3 offset,System.Func<Vector3,bool> inside=null){
                var v=mesh.vertices;var n=mesh.normals;var u=mesh.uv;var indices=mesh.GetTriangles(sub);
                var copied=new Dictionary<int,int>();
                for(int i=0;i<indices.Length;i+=3){
                    if(inside!=null&&(!inside(v[indices[i]])||!inside(v[indices[i+1]])||!inside(v[indices[i+2]])))continue;
                    for(int j=0;j<3;j++){
                        int old=indices[i+j];
                        if(!copied.TryGetValue(old,out int index)){index=vertices.Count;copied[old]=index;vertices.Add(v[old]+offset);normals.Add(n[old]);uv.Add(u[old]);}
                        triangles[target].Add(index);
                    }
                }
            }
            void Layer(string material,Vector3 size,Vector3 center,float radius){
                var mesh=WorkshopPropMesh.RoundedBox(size,radius);Add(mesh,0,MaterialSlot(material),center);Release(mesh);
            }
            Layer("pcb_edge",new Vector3(1.57f,.064f,1.10f),new Vector3(0,.032f,0),.04f);
            Layer("pcb",new Vector3(1.50f,.012f,1.03f),new Vector3(0,.068f,0),.005f);
            for(int key=0;key<6;key++){
                var offset=new Vector3((key%3-1)*.48f-sourceSlot.x,0,(key<3?1:-1)*.27f-sourceSlot.z);
                for(int sub=0;sub<source.subMeshCount;sub++){
                    if(sub==MaterialSlot("pcb")||sub==MaterialSlot("pcb_edge"))continue;
                    Add(source,sub,sub,offset,p=>Mathf.Abs(p.x-sourceSlot.x)<.20f&&Mathf.Abs(p.z-sourceSlot.z)<.175f);
                }
            }
            // Three small controller components along the rear edge, at their original scale.
            for(int component=0;component<3;component++){
                float x=-2.85f+component*.25f;
                foreach(int sub in new[]{MaterialSlot("black"),MaterialSlot("metal")})
                    Add(source,sub,sub,new Vector3((-.25f+component*.25f)-x,0,-.545f),
                        p=>Mathf.Abs(p.x-x)<.07f&&Mathf.Abs(p.z-1.005f)<.03f);
            }
            var result=new Mesh{name="Six-key PCB with authored keyboard circuitry"};
            result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.subMeshCount=triangles.Length;
            for(int sub=0;sub<triangles.Length;sub++)result.SetTriangles(triangles[sub],sub);
            result.RecalculateBounds();return result;
        }
        static Mesh Rim(){
            var v=new List<Vector3>();var t=new List<int>();var rings=new[]{new Vector4(1.85f,1.43f,.08f,.16f),new Vector4(1.85f,1.43f,.245f,.16f),new Vector4(1.80f,1.38f,.30f,.14f),new Vector4(1.57f,1.10f,.30f,.07f),new Vector4(1.53f,1.06f,.16f,.06f)};
            foreach(var r in rings)for(int corner=0;corner<4;corner++)for(int j=0;j<12;j++){float a=(corner*90+j*90f/11)*Mathf.Deg2Rad;v.Add(new Vector3((corner==0||corner==3?1:-1)*(r.x/2-r.w)+Mathf.Cos(a)*r.w,r.z,(corner<2?1:-1)*(r.y/2-r.w)+Mathf.Sin(a)*r.w));}
            const int n=48;for(int k=0;k<rings.Length-1;k++)for(int j=0;j<n;j++){int a=k*n+j,b=k*n+(j+1)%n;t.AddRange(new[]{a,a+n,b,b,a+n,b+n});}
            var mesh=new Mesh{name="Rolled six-key macro pad rim"};mesh.SetVertices(v);mesh.SetUVs(0,v.Select(p=>new Vector2(p.x,p.z)).ToList());mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh Plate(){
            // Thin rails leave six real openings rather than hiding sockets under a solid slab.
            var parts=new List<CombineInstance>();var owned=new List<Mesh>();
            void Rail(Vector3 position,Vector3 size){var mesh=Box(size,Vector3.zero);owned.Add(mesh);parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.Translate(position)});}
            foreach(float z in new[]{-.51f,0,.51f})Rail(new Vector3(0,.03f,z),new Vector3(1.57f,.06f,.09f));
            foreach(float x in new[]{-.745f,-.24f,.24f,.745f})Rail(new Vector3(x,.03f,0),new Vector3(.09f,.06f,1.10f));
            var result=new Mesh{name="Six hot-swap openings"};result.CombineMeshes(parts.ToArray());foreach(var mesh in owned)Release(mesh);return result;
        }
        static void Release(UnityEngine.Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void OnDestroy(){foreach(var mesh in macroMeshes.Values)if(mesh)Release(mesh);}
    }
}
