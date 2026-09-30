using UnityEngine;
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
        Material[] materials;
        readonly KeycapSurface surface;
        public readonly RenderTexture Target;
        float yaw=32,pitch=48,zoom=1;
        readonly Vector4[] wetPoints=new Vector4[12];
        readonly float[] wetTimes=new float[12];
        int wetIndex;
        float lastDab;
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
            Target=new RenderTexture(1860,990,24,RenderTextureFormat.ARGB32){name="Live 3D keycap",antiAliasing=4};Target.Create();camera.targetTexture=Target;
            var pointer=new GameObject("Surface brush footprint");pointer.layer=31;pointer.transform.SetParent(root.transform,false);ring=pointer.AddComponent<LineRenderer>();ring.useWorldSpace=true;ring.loop=true;ring.positionCount=64;ring.shadowCastingMode=ShadowCastingMode.Off;
            ringMaterial=new Material(shader);ringMaterial.SetColor("_BaseColor",Color.white);ring.sharedMaterial=ringMaterial;ring.enabled=false;UpdateCamera();
        }
        public void UpdateMaterials(Material[] source){
            for(int i=0;i<materials.Length;i++){materials[i].SetColor("_BaseColor",source[i].HasProperty("_BaseColor")?source[i].GetColor("_BaseColor"):Color.white);materials[i].SetTexture("_BaseMap",source[i].GetTexture("_BaseMap"));}
            renderer.sharedMaterials=materials;
        }
        public void Orbit(Vector2 delta){yaw-=delta.x*.32f;pitch=Mathf.Clamp(pitch+delta.y*.32f,-75,85);UpdateCamera();}
        public void Zoom(float delta){zoom=Mathf.Clamp(zoom*Mathf.Exp(-delta*.0015f),.55f,2.2f);UpdateCamera();}
        public void ResetView(){yaw=32;pitch=48;zoom=1;UpdateCamera();}
        void UpdateCamera(){float size=Mathf.Max(surface.Bounds.size.z*.8f,surface.Bounds.size.x*.43f);camera.orthographicSize=size*zoom;camera.transform.position=origin+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-4);camera.transform.LookAt(origin);}
        public bool Hit(Vector2 viewport,out RaycastHit hit)=>collider.Raycast(camera.ViewportPointToRay(viewport),out hit,30);
        public Vector3 Local(Vector3 point)=>model.transform.InverseTransformPoint(point);
        public Vector3 ViewDirection=>-camera.transform.forward;
        public void Cursor(RaycastHit hit,float radius,Color color){
            ring.enabled=true;ring.widthMultiplier=surface.Bounds.size.z*.004f;ringMaterial.SetColor("_BaseColor",Color.Lerp(color,Color.white,.35f));
            var normal=hit.normal;var right=Vector3.Cross(normal,Mathf.Abs(normal.y)>.95f?Vector3.forward:Vector3.up).normalized;var up=Vector3.Cross(normal,right);
            for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;var p=hit.point+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*radius;
                // Project each segment onto the actual bevel instead of drawing a screen-space circle.
                if(collider.Raycast(new Ray(p+normal*radius*2,-normal),out var contact,radius*4))p=contact.point+contact.normal*.001f;else p+=normal*.001f;
                ring.SetPosition(i,p);
            }
        }
        public void HideCursor()=>ring.enabled=false;
        public void Dab(Vector3 point,float radius){if(Time.time-lastDab<.025f)return;lastDab=Time.time;var local=Local(point);wetPoints[wetIndex]=new Vector4(local.x,local.y,local.z,radius*1.1f);wetTimes[wetIndex]=Time.time;wetIndex=(wetIndex+1)%wetPoints.Length;}
        public void Render(){foreach(var m in materials){m.SetVectorArray("_WetPoints",wetPoints);m.SetFloatArray("_WetTimes",wetTimes);}camera.Render();}
        static void Release(Object value){if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);}
        public void Dispose(){Release(root);Release(collision);Release(ringMaterial);foreach(var m in materials)Release(m);Target.Release();Release(Target);surface.ReleaseSamples();}
    }
}
