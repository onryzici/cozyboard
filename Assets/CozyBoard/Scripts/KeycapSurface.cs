using System.Collections.Generic;
using UnityEngine;

namespace CozyBoard {
    // Six independent planar islands. Each triangle chooses one projection, so bevels
    // never share paint with the opposite wall. Painting is evaluated in model space.
    public sealed class KeycapSurface {
        public const int Tile = 512, Width = Tile * 3, Height = Tile * 2;
        public readonly Mesh Mesh;
        public readonly Bounds Bounds;
        public readonly int Body;
        public static int Face(Vector3 n) {
            var a=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
            if(a.y>=a.x&&a.y>=a.z)return n.y>=0?0:5;
            if(a.x>=a.z)return n.x>=0?1:2;
            return n.z>=0?3:4;
        }
        public Vector2 UV(Vector3 p,int face) {
            var b=Bounds; float x=Mathf.InverseLerp(b.min.x,b.max.x,p.x),y=Mathf.InverseLerp(b.min.y,b.max.y,p.y),z=Mathf.InverseLerp(b.min.z,b.max.z,p.z);
            Vector2 q=face==0||face==5?new Vector2(x,z):face==1||face==2?new Vector2(z,y):new Vector2(x,y);
            // A four-pixel gutter prevents bilinear samples from touching other islands.
            return new Vector2(((face%3)*Tile+4+q.x*(Tile-8))/Width,((face/3)*Tile+4+q.y*(Tile-8))/Height);
        }
        public KeycapSurface(Mesh source,int body) {
            Body=body;Bounds=source.bounds;
            var v=source.vertices;var n=source.normals;var oldUV=source.uv;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>[source.subMeshCount];
            for(int sub=0;sub<indices.Length;sub++) {
                indices[sub]=new List<int>();var t=source.GetTriangles(sub);
                for(int i=0;i<t.Length;i+=3) {
                    int face=Face(Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]));
                    for(int j=0;j<3;j++){int k=t[i+j];indices[sub].Add(vertices.Count);vertices.Add(v[k]);normals.Add(n[k]);uv.Add(sub==body?UV(v[k],face):oldUV[k]);}
                }
            }
            Mesh=new Mesh{name=source.name+" paint surface",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};Mesh.SetVertices(vertices);Mesh.SetNormals(normals);Mesh.SetUVs(0,uv);Mesh.subMeshCount=indices.Length;
            for(int s=0;s<indices.Length;s++)Mesh.SetTriangles(indices[s],s);Mesh.RecalculateBounds();
        }
        public Mesh CollisionMesh(){var mesh=new Mesh{name="Keycap paint collider",indexFormat=Mesh.indexFormat};mesh.vertices=Mesh.vertices;mesh.uv=Mesh.uv;mesh.triangles=Mesh.GetTriangles(Body);mesh.RecalculateBounds();return mesh;}

        public struct Texel { public int Index; public Vector3 Position,Normal; }
        readonly Dictionary<Vector3Int,List<Texel>> cells=new();
        readonly List<(int target,int source)> gutters=new();
        float cellSize;
        public bool SamplesReady {get;private set;}
        Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/cellSize),Mathf.FloorToInt(p.y/cellSize),Mathf.FloorToInt(p.z/cellSize));
        public void ReleaseSamples(){cells.Clear();gutters.Clear();SamplesReady=false;}
        public void PadEdges(Color32[] pixels){foreach(var pair in gutters)pixels[pair.target]=pixels[pair.source];}
        public void CacheSamples(){if(SamplesReady)return;var preparation=CacheSamplesIncremental();while(preparation.MoveNext()){}}
        public System.Collections.IEnumerator CacheSamplesIncremental(){
            if(SamplesReady)yield break;ReleaseSamples();
            var budget=System.Diagnostics.Stopwatch.StartNew();
            cellSize=Mathf.Max(Bounds.size.z,Bounds.size.y)/24;
            var v=Mesh.vertices;var uv=Mesh.uv;var triangles=Mesh.GetTriangles(Body);var used=new bool[Width*Height];
            for(int i=0;i<triangles.Length;i+=3){
                int a=triangles[i],b=triangles[i+1],c=triangles[i+2];Vector2 u=Vector2.Scale(uv[a],new Vector2(Width,Height)),w=Vector2.Scale(uv[b],new Vector2(Width,Height)),q=Vector2.Scale(uv[c],new Vector2(Width,Height));
                float den=(w.y-q.y)*(u.x-q.x)+(q.x-w.x)*(u.y-q.y);if(Mathf.Abs(den)<.0001f)continue;
                Vector3 normal=Vector3.Cross(v[b]-v[a],v[c]-v[a]).normalized;
                for(int y=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(u.y,w.y,q.y)));y<=Mathf.Min(Height-1,Mathf.CeilToInt(Mathf.Max(u.y,w.y,q.y)));y++){
                for(int x=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(u.x,w.x,q.x)));x<=Mathf.Min(Width-1,Mathf.CeilToInt(Mathf.Max(u.x,w.x,q.x)));x++){
                    float s=((w.y-q.y)*(x+.5f-q.x)+(q.x-w.x)*(y+.5f-q.y))/den,t=((q.y-u.y)*(x+.5f-q.x)+(u.x-q.x)*(y+.5f-q.y))/den;
                    if(s<-.002f||t<-.002f||s+t>1.002f)continue;int index=y*Width+x;if(used[index])continue;used[index]=true;
                    var p=s*v[a]+t*v[b]+(1-s-t)*v[c];var cell=Cell(p);if(!cells.TryGetValue(cell,out var list))cells[cell]=list=new List<Texel>();list.Add(new Texel{Index=index,Position=p,Normal=normal});
                }
                if(budget.Elapsed.TotalMilliseconds>=6){yield return null;budget.Restart();}
                }
            }
            // Extend edge texels into the unused UV gutter so bilinear filtering cannot reveal base-colour seams.
            var nearest=new int[Width*Height];var distances=new byte[Width*Height];
            for(int index=0;index<used.Length;index++){
                if(index%2048==0&&budget.Elapsed.TotalMilliseconds>=6){yield return null;budget.Restart();}
                if(!used[index])continue;int x=index%Width,y=index/Width;
                if(x>0&&x<Width-1&&y>0&&y<Height-1&&used[index-1]&&used[index+1]&&used[index-Width]&&used[index+Width])continue;
                for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){
                    int nx=x+dx,ny=y+dy;if(nx<0||nx>=Width||ny<0||ny>=Height||nx/Tile!=x/Tile||ny/Tile!=y/Tile)continue;
                    int target=ny*Width+nx;if(used[target])continue;int d=dx*dx+dy*dy;
                    if(nearest[target]==0||d<distances[target]){nearest[target]=index+1;distances[target]=(byte)d;}
                }
            }
            for(int i=0;i<nearest.Length;i++){if(nearest[i]>0)gutters.Add((i,nearest[i]-1));if(i%8192==0&&budget.Elapsed.TotalMilliseconds>=6){yield return null;budget.Restart();}}
            SamplesReady=true;
        }
        public void Visit(Vector3 center,float radius,System.Action<Texel> action){
            CacheSamples();var lo=Cell(center-Vector3.one*radius);var hi=Cell(center+Vector3.one*radius);float squared=radius*radius;
            for(int z=lo.z;z<=hi.z;z++)for(int y=lo.y;y<=hi.y;y++)for(int x=lo.x;x<=hi.x;x++)if(cells.TryGetValue(new Vector3Int(x,y,z),out var list))foreach(var texel in list)if((texel.Position-center).sqrMagnitude<=squared)action(texel);
        }
    }
}
