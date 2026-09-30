using System.Collections.Generic;
using UnityEngine;
namespace CozyBoard {
 public static class WorkshopPropMesh {
  public static Mesh RoundedBox(Vector3 size,float radius=.16f){
   var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();float bevel=Mathf.Min(.035f,size.y*.28f);radius=Mathf.Min(radius,Mathf.Min(size.x,size.z)*.45f);const int n=36;
   for(int layer=0;layer<4;layer++){float inset=layer==0||layer==3?bevel:0;float y=layer==0?-size.y/2:layer==1?-size.y/2+bevel:layer==2?size.y/2-bevel:size.y/2;float r=Mathf.Max(.001f,radius-inset);
    for(int corner=0;corner<4;corner++)for(int j=0;j<=8;j++){float a=(corner*90+j*90f/8)*Mathf.Deg2Rad;float cx=(corner==0||corner==3?1:-1)*(size.x/2-radius),cz=(corner<2?1:-1)*(size.z/2-radius);var p=new Vector3(cx+Mathf.Cos(a)*r,y,cz+Mathf.Sin(a)*r);v.Add(p);uv.Add(new Vector2(p.x/size.x+.5f,p.z/size.z+.5f));}
   }
   for(int k=0;k<3;k++)for(int j=0;j<n;j++){int a=k*n+j,b=k*n+(j+1)%n;t.AddRange(new[]{a,a+n,b,b,a+n,b+n});}
   int bottom=v.Count;v.Add(new Vector3(0,-size.y/2,0));uv.Add(Vector2.one*.5f);int top=v.Count;v.Add(new Vector3(0,size.y/2,0));uv.Add(Vector2.one*.5f);for(int j=0;j<n;j++){int next=(j+1)%n;t.AddRange(new[]{bottom,j,next,top,3*n+next,3*n+j});}
   var mesh=new Mesh{name="Soft rolled prop shell"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
 }
}
