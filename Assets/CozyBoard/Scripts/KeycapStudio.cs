using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace CozyBoard {
    public sealed class KeycapStudio : System.IDisposable {
        readonly GameObject root,model;
        readonly Camera camera;
        readonly MeshRenderer renderer;
        readonly MeshCollider collider;
        readonly Mesh collision;
        readonly LineRenderer ring;
        readonly Material ringMaterial;
        readonly GameObject backdrop;readonly Material matMaterial;
        Material[] materials;
        readonly KeycapSurface surface;
        public readonly RenderTexture Target;
        float yaw=32,pitch=48,zoom=1;
        readonly Vector4[] wetPoints=new Vector4[12];
        readonly float[] wetTimes=new float[12];
        int wetIndex;
        float lastDab;
        bool dirty=true;double lastRender;Vector3 cursorPoint;float cursorSize;Color cursorColor;bool cursorCached;
        readonly Vector3 origin=new Vector3(0,2000,0);
        public KeycapStudio(KeycapSurface surface,Material[] source,Shader shader) {
            this.surface=surface;
            root=new GameObject("3D Keycap Studio"){hideFlags=HideFlags.DontSave};root.transform.position=origin;
            model=new GameObject("Paintable keycap");model.transform.SetParent(root.transform,false);model.layer=31;model.transform.localPosition=-surface.Bounds.center;
            model.AddComponent<MeshFilter>().sharedMesh=surface.Mesh;renderer=model.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            collision=surface.CollisionMesh();collider=model.AddComponent<MeshCollider>();collider.sharedMesh=collision;
            materials=new Material[source.Length];for(int i=0;i<source.Length;i++)materials[i]=new Material(shader);
            UpdateMaterials(source);
            var cameraObject=new GameObject("Studio camera");cameraObject.transform.SetParent(root.transform,false);camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.115f,.145f,.15f);camera.nearClipPlane=.01f;camera.farClipPlane=30;camera.allowHDR=false;camera.enabled=false;
            backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad);backdrop.name="Close-up cutting mat";backdrop.layer=31;backdrop.transform.SetParent(root.transform,false);Release(backdrop.GetComponent<Collider>());
            matMaterial=new Material(Shader.Find("CozyBoard/WorkMat"));matMaterial.SetColor("_BaseColor",new Color(.37f,.51f,.48f));matMaterial.SetFloat("_Grid",.12f);matMaterial.SetFloat("_Border",0);matMaterial.SetFloat("_Shadow",1);backdrop.GetComponent<MeshRenderer>().sharedMaterial=matMaterial;
            Target=new RenderTexture(1860,990,24,RenderTextureFormat.ARGB32){name="Live 3D keycap",antiAliasing=4};Target.Create();camera.targetTexture=Target;
            var pointer=new GameObject("Surface brush footprint");pointer.layer=31;pointer.transform.SetParent(root.transform,false);ring=pointer.AddComponent<LineRenderer>();ring.useWorldSpace=true;ring.loop=true;ring.positionCount=64;ring.shadowCastingMode=ShadowCastingMode.Off;
            ringMaterial=new Material(shader);ringMaterial.SetColor("_BaseColor",Color.white);ring.sharedMaterial=ringMaterial;ring.enabled=false;UpdateCamera();
        }
        public void UpdateMaterials(Material[] source){
            for(int i=0;i<materials.Length;i++){materials[i].SetColor("_BaseColor",source[i].HasProperty("_BaseColor")?source[i].GetColor("_BaseColor"):Color.white);materials[i].SetTexture("_BaseMap",source[i].GetTexture("_BaseMap"));}
            renderer.sharedMaterials=materials;dirty=true;
        }
        public void Orbit(Vector2 delta){yaw-=delta.x*.32f;pitch=Mathf.Clamp(pitch+delta.y*.32f,-75,85);UpdateCamera();}
        public void Zoom(float delta){zoom=Mathf.Clamp(zoom*Mathf.Exp(-WorkshopAtelierStyle.WheelSteps(delta)*.16f),.55f,2.2f);UpdateCamera();}
        public void ResetView(){yaw=32;pitch=48;zoom=1;UpdateCamera();}
        void UpdateCamera(){dirty=true;float size=Mathf.Max(surface.Bounds.size.z*.8f,surface.Bounds.size.x*.43f);camera.orthographicSize=size*zoom;camera.transform.position=origin+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-4);camera.transform.LookAt(origin);if(backdrop){float width=camera.orthographicSize*camera.aspect*2.2f,height=camera.orthographicSize*2.2f;backdrop.transform.SetPositionAndRotation(origin+camera.transform.forward*(surface.Bounds.size.magnitude+.1f),camera.transform.rotation);backdrop.transform.localScale=new Vector3(width,height,1);matMaterial.SetVector("_Size",new Vector4(width,height,0,0));UpdateShadow();}}
        void UpdateShadow(){
            // Project the cap's bounds in the studio camera basis so the shadow follows orbit and zoom.
            var points=new List<Vector2>();var ext=surface.Bounds.extents;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var v=new Vector3(ext.x*x,ext.y*y,ext.z*z);points.Add(new Vector2(Vector3.Dot(v,camera.transform.right)+.065f,Vector3.Dot(v,camera.transform.up)-.075f));}
            points=points.OrderBy(p=>p.x).ThenBy(p=>p.y).ToList();var hull=new List<Vector2>();
            float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
            foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;for(int i=points.Count-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}hull.RemoveAt(hull.Count-1);
            var projected=new Vector4[16];for(int i=0;i<hull.Count;i++)projected[i]=new Vector4(hull[i].x,hull[i].y,0,0);matMaterial.SetVectorArray("_ShadowPoints",projected);matMaterial.SetInt("_ShadowCount",hull.Count);
        }
        public bool Hit(Vector2 viewport,out RaycastHit hit)=>collider.Raycast(camera.ViewportPointToRay(viewport),out hit,30);
        public Vector3 Local(Vector3 point)=>model.transform.InverseTransformPoint(point);
        public Vector3 ViewDirection=>-camera.transform.forward;
        public void Cursor(RaycastHit hit,float radius,Color color){
            bool changed=!ring.enabled||!cursorCached||(cursorPoint-hit.point).sqrMagnitude>.000001f||!Mathf.Approximately(cursorSize,radius)||cursorColor!=color;
            ring.enabled=true;if(!changed)return;cursorCached=true;cursorPoint=hit.point;cursorSize=radius;cursorColor=color;dirty=true;ring.widthMultiplier=surface.Bounds.size.z*.004f;ringMaterial.SetColor("_BaseColor",Color.Lerp(color,Color.white,.35f));
            var normal=hit.normal;var right=Vector3.Cross(normal,Mathf.Abs(normal.y)>.95f?Vector3.forward:Vector3.up).normalized;var up=Vector3.Cross(normal,right);
            for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;var p=hit.point+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*radius;
                // Project each segment onto the actual bevel instead of drawing a screen-space circle.
                if(collider.Raycast(new Ray(p+normal*radius*2,-normal),out var contact,radius*4))p=contact.point+contact.normal*.001f;else p+=normal*.001f;
                ring.SetPosition(i,p);
            }
        }
        public void SetTape(WorkshopTapeStrip[] strips){
            dirty=true;
            var starts=new Vector4[8];var axes=new Vector4[8];var normals=new Vector4[8];int count=Mathf.Min(8,strips.Length);
            for(int i=0;i<count;i++){var t=strips[i];starts[i]=new Vector4(t.Start.x,t.Start.y,t.Start.z,t.Width);var direction=Vector3.ProjectOnPlane(t.End-t.Start,t.Normal);axes[i]=new Vector4(direction.normalized.x,direction.normalized.y,direction.normalized.z,direction.magnitude);normals[i]=new Vector4(t.Normal.x,t.Normal.y,t.Normal.z,0);}
            foreach(var m in materials){m.SetInt("_TapeCount",count);m.SetVectorArray("_TapeStarts",starts);m.SetVectorArray("_TapeAxes",axes);m.SetVectorArray("_TapeNormals",normals);}
        }
        public void Invalidate()=>dirty=true;
        public void ClearWetness(){System.Array.Clear(wetPoints,0,wetPoints.Length);dirty=true;}
        public void HideCursor(){if(ring.enabled){ring.enabled=false;dirty=true;}}
        public void Dab(Vector3 point,float radius){if(Time.time-lastDab<.025f)return;lastDab=Time.time;var local=Local(point);wetPoints[wetIndex]=new Vector4(local.x,local.y,local.z,radius*1.1f);wetTimes[wetIndex]=Time.time;wetIndex=(wetIndex+1)%wetPoints.Length;dirty=true;}
        public void Render(){bool wet=false;for(int i=0;i<wetPoints.Length;i++)if(wetPoints[i].w>0&&Time.time-wetTimes[i]<2.5f){wet=true;break;}
            double now=Time.realtimeSinceStartupAsDouble;if(!dirty&&(!wet||now-lastRender<1f/30))return;
            foreach(var m in materials){m.SetVectorArray("_WetPoints",wetPoints);m.SetFloatArray("_WetTimes",wetTimes);}camera.Render();dirty=false;lastRender=now;}
        static void Release(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
        public void Dispose(){Release(root);Release(matMaterial);Release(collision);Release(ringMaterial);foreach(var m in materials)Release(m);Target.Release();Release(Target);}
    }
}
