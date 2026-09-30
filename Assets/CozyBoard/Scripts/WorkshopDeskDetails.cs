using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyBoard {
    public sealed class WorkshopDeskDetails:MonoBehaviour {
        readonly List<Object> owned=new();
        Material shadow;
        public static void Create(WorkshopGameMode game){
            if(game.Controller.transform.Find("Quiet desk details"))return;
            var root=new GameObject("Quiet desk details");root.transform.SetParent(game.Controller.transform,false);
            var details=root.AddComponent<WorkshopDeskDetails>();details.shadow=game.Controller.ShadowMaterial;details.Build();
        }
        Material Paint(string name,Color color){var m=new Material(Shader.Find("CozyBoard/Painted")){name=name};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
        Transform Part(string name,Transform parent,Vector3 position,Mesh mesh,Material material,bool castsShadow=true){
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;owned.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(castsShadow){var s=new GameObject(name+" painted shadow");s.transform.SetParent(go.transform,false);s.AddComponent<MeshFilter>().sharedMesh=mesh;s.AddComponent<MeshRenderer>().sharedMaterial=shadow;}
            return go.transform;
        }
        static Mesh Turn(Vector2[] profile){
            const int segments=36;var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int ring=0;ring<profile.Length;ring++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(new Vector3(Mathf.Cos(a)*profile[ring].x,profile[ring].y,Mathf.Sin(a)*profile[ring].x));uv.Add(new Vector2((float)i/segments,(float)ring/(profile.Length-1)));}
            for(int ring=0;ring<profile.Length-1;ring++)for(int i=0;i<segments;i++){int a=ring*(segments+1)+i,b=a+segments+1;triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            var mesh=new Mesh{name="Hand thrown pottery"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void Build(){
            var ceramic=Paint("Warm cream stoneware",new Color(.85f,.78f,.62f));var tea=Paint("Quiet amber tea",new Color(.26f,.13f,.065f));
            var cork=Paint("Soft terracotta coaster",new Color(.62f,.38f,.26f));var sage=Paint("Sage linen sketchbook",new Color(.39f,.52f,.43f));
            var pages=Paint("Ivory sketch paper",new Color(.88f,.82f,.68f));var graphite=Paint("Pencil graphite",new Color(.24f,.28f,.25f));var ochre=Paint("Honey pencil lacquer",new Color(.74f,.52f,.26f));
            var coaster=Part("A little tea break",transform,new Vector3(6.95f,.025f,3.65f),Turn(new[]{new Vector2(0,-.02f),new Vector2(.70f,-.02f),new Vector2(.72f,0),new Vector2(.69f,.025f),new Vector2(0,.025f)}),cork);
            var cup=Part("Hand thrown tea cup",coaster,Vector3.up*.04f,Turn(new[]{new Vector2(0,0),new Vector2(.39f,0),new Vector2(.46f,.07f),new Vector2(.51f,.53f),new Vector2(.50f,.57f),new Vector2(.45f,.57f),new Vector2(.40f,.10f),new Vector2(0,.10f)}),ceramic);
            Part("Amber tea surface",cup,Vector3.zero,Turn(new[]{new Vector2(.43f,.46f),new Vector2(0,.46f)}),tea,false);
            // A small curved handle, sharing the pot's soft painted material.
            var handle=new GameObject("Stoneware handle");handle.transform.SetParent(cup,false);handle.transform.localPosition=new Vector3(.43f,.29f,0);
            var line=handle.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=25;line.widthMultiplier=.09f;line.numCapVertices=5;line.numCornerVertices=3;line.sharedMaterial=ceramic;
            for(int i=0;i<25;i++){float a=(-105+i*210f/24)*Mathf.Deg2Rad;line.SetPosition(i,new Vector3(Mathf.Cos(a)*.28f,Mathf.Sin(a)*.21f,0));}
            var book=Part("Small linen sketchbook",transform,new Vector3(7.35f,.055f,1.25f),WorkshopPropMesh.RoundedBox(new Vector3(1.43f,.10f,1.92f),.09f),sage);book.localRotation=Quaternion.Euler(0,-12,0);
            Part("Paper page block",book,Vector3.up*.085f,WorkshopPropMesh.RoundedBox(new Vector3(1.32f,.075f,1.82f),.06f),pages);
            Part("Linen notebook cover",book,Vector3.up*.145f,WorkshopPropMesh.RoundedBox(new Vector3(1.43f,.04f,1.92f),.09f),sage);
            Part("Sketchbook binding",book,new Vector3(-.58f,.173f,0),WorkshopPropMesh.RoundedBox(new Vector3(.06f,.012f,1.76f),.015f),ochre,false);
            var pencil=Part("A honey coloured pencil",transform,new Vector3(8.34f,.08f,1.42f),WorkshopPropMesh.RoundedBox(new Vector3(.095f,.095f,1.65f),.035f),ochre);pencil.localRotation=Quaternion.Euler(0,17,0);
            Part("Pencil point",pencil,new Vector3(0,0,.88f),WorkshopPropMesh.RoundedBox(new Vector3(.06f,.06f,.14f),.025f),graphite);
        }
        void OnDestroy(){foreach(var asset in owned)if(asset){if(Application.isPlaying)Destroy(asset);else DestroyImmediate(asset);}}
    }
}
