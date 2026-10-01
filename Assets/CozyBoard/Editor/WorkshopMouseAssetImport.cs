using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace CozyBoard.Editor {
    public static class WorkshopMouseAssetImport {
        [Serializable] class Data {public float[] vertices,normals,uv;public Submesh[] submeshes;}
        [Serializable] class Submesh {public int[] indices;}
        public static void Run(){foreach(var name in new[]{"pcb","base","palm","left","right"})Import(name);}
        public static void ImportHands(){foreach(var name in new[]{"grip","pinch","driver-grip","dish"})Import(name,"workshop-","WorkshopHands");}
        static void Import(string name,string prefix="mouse-",string folder="Mouse"){
            var data=JsonUtility.FromJson<Data>(File.ReadAllText("Assets/CozyBoard/SourceData/"+prefix+name+".json"));var mesh=new Mesh{name="Authored workshop "+name,indexFormat=IndexFormat.UInt32};int count=data.vertices.Length/3;var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];
            for(int i=0;i<count;i++){v[i]=new Vector3(data.vertices[i*3],data.vertices[i*3+1],data.vertices[i*3+2]);n[i]=new Vector3(data.normals[i*3],data.normals[i*3+1],data.normals[i*3+2]);uv[i]=new Vector2(data.uv[i*2],data.uv[i*2+1]);}
            mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.subMeshCount=data.submeshes.Length;for(int i=0;i<data.submeshes.Length;i++)mesh.SetTriangles(data.submeshes[i].indices,i);mesh.RecalculateBounds();
            if(!AssetDatabase.IsValidFolder("Assets/Resources/"+folder))AssetDatabase.CreateFolder("Assets/Resources",folder);string path="Assets/Resources/"+folder+"/"+name.ToUpperInvariant()+".asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior){EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);}else AssetDatabase.CreateAsset(mesh,path);AssetDatabase.SaveAssets();Debug.Log("COZY_ASSET_IMPORTED "+folder+"/"+name+" "+count);
        }
    }
}
