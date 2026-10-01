using System.Collections.Generic;
using UnityEngine;
namespace CozyBoard {
    public sealed class WorkshopCarton : MonoBehaviour {
        public static Texture2D PaperTexture;
        public static TMPro.TMP_FontAsset LabelFont;
        public Transform Lid;
        public GameObject Tape,Wrap;
        Transform leftPaper,rightPaper;float packDepth;TMPro.TextMeshPro address;
        readonly List<Object> owned=new();
        Material kraft,edge,paper,shadow;
        Material Material(string label,Color color){var m=new Material(Shader.Find("CozyBoard/Painted")){name=label};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
        public static WorkshopCarton Create(Transform parent,string name,float width,float depth,float height,bool packing=false,float contentsTop=0){
            var go=new GameObject(name);go.transform.SetParent(parent,false);var carton=go.AddComponent<WorkshopCarton>();carton.Build(width,depth,packing?Mathf.Max(height,contentsTop+.25f):height,packing,contentsTop);return carton;
        }
        // Measure in the upright packing pose, regardless of the current inspection rotation.
        public static float PackedTop(Transform board,float baseHeight){
            float top=0;
            foreach(var filter in board.GetComponentsInChildren<MeshFilter>()){
                var renderer=filter.GetComponent<MeshRenderer>();
                if(!renderer||!renderer.enabled||!filter.sharedMesh)continue;
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++){
                    var corner=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z);
                    top=Mathf.Max(top,board.InverseTransformPoint(filter.transform.TransformPoint(corner)).y);
                }
            }
            return baseHeight+top*board.lossyScale.y;
        }
        Transform Panel(string name,Transform parent,Vector3 position,Vector3 size,Material material){
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;Destroy(go.GetComponent<Collider>());go.GetComponent<MeshRenderer>().sharedMaterial=material;if(shadow){var shade=new GameObject("Soft carton shadow");shade.transform.SetParent(go.transform,false);shade.AddComponent<MeshFilter>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;shade.AddComponent<MeshRenderer>().sharedMaterial=shadow;}return go.transform;
        }
        void Build(float w,float d,float h,bool packing,float contentsTop){
            shadow=new Material(Shader.Find("CozyBoard/PaintedSilhouette"));shadow.SetFloat("_Opacity",.20f);owned.Add(shadow);
            kraft=Material("Uncoated ochre cardboard",new Color(.71f,.57f,.39f));edge=Material("Cut cardboard edge",new Color(.45f,.28f,.16f));paper=Material("Crumpled packing paper",new Color(.96f,.88f,.72f));
            if(PaperTexture)paper.SetTexture("_BaseMap",PaperTexture);paper.SetFloat("_Shading",.25f);
            Panel("Bottom",transform,new Vector3(0,.035f,0),new Vector3(w,.055f,d),kraft);
            Panel("Left",transform,new Vector3(-w/2,h/2,0),new Vector3(.045f,h,d),kraft);Panel("Right",transform,new Vector3(w/2,h/2,0),new Vector3(.045f,h,d),kraft);
            Panel("Front",transform,new Vector3(0,h/2,-d/2),new Vector3(w,.0f+h,.045f),kraft);Panel("Back",transform,new Vector3(0,h/2,d/2),new Vector3(w,h,.045f),kraft);
            Panel("Front cut edge",transform,new Vector3(0,h,-d/2),new Vector3(w,.012f,.053f),edge);
            Panel("Left cut edge",transform,new Vector3(-w/2,h,0),new Vector3(.053f,.012f,d),edge);Panel("Right cut edge",transform,new Vector3(w/2,h,0),new Vector3(.053f,.012f,d),edge);
            Lid=new GameObject("Folded lid hinge").transform;Lid.SetParent(transform,false);Lid.localPosition=new Vector3(0,h,d/2);Lid.localRotation=Quaternion.Euler(115,0,0);
            Panel("Full cardboard lid",Lid,new Vector3(0,0,-d/2),new Vector3(w+.07f,.035f,d+.04f),kraft);
            if(LabelFont){var stamp=new GameObject("Printed carton stamp");stamp.transform.SetParent(Lid,false);stamp.transform.localPosition=new Vector3(0,.024f,-d*.48f);stamp.transform.localRotation=Quaternion.Euler(90,0,0);var text=stamp.AddComponent<TMPro.TextMeshPro>();text.font=LabelFont;text.text="COZY BOARD\nATÖLYE";text.fontSize=2.1f;text.color=new Color(.36f,.25f,.13f,.7f);text.alignment=TMPro.TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(w*.8f,d*.55f);}
            Panel("Lid cut edge",Lid,new Vector3(0,.023f,-d),new Vector3(w,.012f,.035f),edge);
            Panel("Tucked front lip",Lid,new Vector3(0,-.11f,-d),new Vector3(w-.1f,.22f,.028f),kraft);
            Panel("Fold crease",Lid,new Vector3(0,.024f,-.11f),new Vector3(w-.08f,.007f,.012f),edge);
            // Thin tucked flaps make the lid read as folded card rather than furniture.
            Panel("Left lid flap",Lid,new Vector3(-w/2,-.075f,-d/2),new Vector3(.025f,.15f,d-.15f),kraft);
            Panel("Right lid flap",Lid,new Vector3(w/2,-.075f,-d/2),new Vector3(.025f,.15f,d-.15f),kraft);
            for(int side=-1;side<=1;side+=2){var flap=Panel("Angled side fold",transform,new Vector3(side*(w/2-.08f),h*.72f,0),new Vector3(.025f,h*.60f,d-.18f),kraft);flap.localRotation=Quaternion.Euler(0,0,side*18);}
            Tissue("Wrinkled liner",w-.14f,d-.14f,.10f,0);
            var left=Panel("Left folded corner",transform,new Vector3(-w/2+.12f,h*.6f,d*.38f),new Vector3(.24f,h*.65f,.035f),kraft);left.localRotation=Quaternion.Euler(0,35,0);
            var right=Panel("Right folded corner",transform,new Vector3(w/2-.12f,h*.6f,d*.38f),new Vector3(.24f,h*.65f,.035f),kraft);right.localRotation=Quaternion.Euler(0,-35,0);
            for(int side=-1;side<=1;side+=2){for(int k=0;k<16;k++){var tick=Panel("Corrugated cut",Lid,new Vector3(side*w/2,.027f,-d*(k+.5f)/16),new Vector3(.044f,.004f,.018f),edge);tick.localRotation=Quaternion.Euler(0,24,0);}}
            if(packing){
                Wrap=new GameObject("Folding tissue wings");Wrap.transform.SetParent(transform,false);
                leftPaper=new GameObject("Left paper hinge").transform;leftPaper.SetParent(Wrap.transform,false);leftPaper.localPosition=new Vector3(-w*.46f,Mathf.Max(1.04f,contentsTop+.06f),0);
                rightPaper=new GameObject("Right paper hinge").transform;rightPaper.SetParent(Wrap.transform,false);rightPaper.localPosition=new Vector3(w*.46f,Mathf.Max(1.04f,contentsTop+.06f)+.025f,0);
                var lp=Tissue("Left tissue sheet",w*.49f,d-.30f,0,1);lp.SetParent(leftPaper,false);lp.localPosition=new Vector3(w*.245f,0,0);
                var rp=Tissue("Right tissue sheet",w*.49f,d-.30f,0,2);rp.SetParent(rightPaper,false);rp.localPosition=new Vector3(-w*.245f,0,0);FoldPaper(0);Wrap.SetActive(false);packDepth=d;
                var tape=Material("Paper sealing tape",new Color(.91f,.78f,.50f));Tape=Panel("Sealed kraft tape",transform,new Vector3(0,h+.027f,0),new Vector3(.65f,.015f,d+.10f),tape).gameObject;Tape.SetActive(false);
                var labelMat=Material("Ivory parcel label",new Color(.95f,.91f,.79f));
                Panel("Address label",Lid,new Vector3(w*.24f,.035f,-d*.54f),new Vector3(2.4f,.012f,1.45f),labelMat);
                var note=new GameObject("Customer parcel address");note.transform.SetParent(Lid,false);note.transform.localPosition=new Vector3(w*.24f,.046f,-d*.54f);note.transform.localRotation=Quaternion.Euler(90,0,0);address=note.AddComponent<TMPro.TextMeshPro>();address.font=LabelFont;address.fontSize=1.65f;address.color=new Color(.23f,.34f,.29f);address.alignment=TMPro.TextAlignmentOptions.Center;address.rectTransform.sizeDelta=new Vector2(2.2f,1.2f);
            }
        }
        public void Address(string customer,int order){if(address)address.text=$"{customer} için\nSevgiyle hazırlandı\n#{order:000}";}
        public void FoldPaper(float progress){if(!leftPaper)return;leftPaper.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(100,0,Mathf.SmoothStep(0,1,Mathf.Clamp01(progress*1.7f)))) ;rightPaper.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-100,0,Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.35f)/.65f))));}
        public void Seal(float progress){if(!Tape)return;Tape.SetActive(progress>0);Tape.transform.localScale=new Vector3(.65f,.015f,(packDepth+.1f)*Mathf.Max(.001f,progress));var p=Tape.transform.localPosition;p.z=-(packDepth+.1f)*(1-progress)*.5f;Tape.transform.localPosition=p;}
        Transform Tissue(string name,float w,float d,float y,int seed){
            int nx=36,nz=24;var vertices=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[vertices.Length];var triangles=new int[nx*nz*6];int cursor=0;
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){
                float px=(x/(float)nx-.5f)*w,pz=(z/(float)nz-.5f)*d;
                float fold=Mathf.Sin(px*15+pz*9+seed)*Mathf.Sin(pz*12-px*5)*.025f+Mathf.PerlinNoise(px*5+30,pz*5+30)*.065f;
                float edgeRise=Mathf.Pow(Mathf.Max(Mathf.Abs(px)/(w*.5f),Mathf.Abs(pz)/(d*.5f)),8)*.085f;
                int i=z*(nx+1)+x;vertices[i]=new Vector3(px,y+fold+edgeRise,pz);uv[i]=new Vector2(x/(float)nx*w/5f,z/(float)nz*d/5f);
                if(x<nx&&z<nz){int a=i,b=i+1,c=i+nx+1;triangles[cursor++]=a;triangles[cursor++]=c;triangles[cursor++]=b;triangles[cursor++]=b;triangles[cursor++]=c;triangles[cursor++]=c+1;}
            }
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;var twoSided=new int[triangles.Length*2];System.Array.Copy(triangles,twoSided,triangles.Length);for(int i=0;i<triangles.Length;i+=3){twoSided[triangles.Length+i]=triangles[i];twoSided[triangles.Length+i+1]=triangles[i+2];twoSided[triangles.Length+i+2]=triangles[i+1];}mesh.triangles=triangles;mesh.RecalculateNormals();mesh.triangles=twoSided;mesh.RecalculateBounds();owned.Add(mesh);
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=paper;return go.transform;
        }
        void OnDestroy(){foreach(var value in owned){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}}
    }
}
