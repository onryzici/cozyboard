using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
namespace CozyBoard.Editor {
    public static class WorkshopArtRevision {
        const string Root="Assets/CozyBoard";
        public static void Apply(WorkshopController controller) {
            var colors=new Dictionary<string,string> {
                {"cream","D2B99E"},{"case_side","986C58"},{"foam","35443C"},
                {"key_cream","E4DCCD"},{"key_sage","96A58E"},{"key_clay","BD765D"},{"key_teal","507D77"},
                {"pcb","2B5944"},{"pcb_edge","234434"},{"plate","394D43"},
                {"housing","555850"},{"socket","303B35"},{"stem","B8874F"},{"stem_top","DFB978"},
                {"metal","A2ACA8"},{"legend","434B45"},{"legend_light","E8E0D0"}
            };
            foreach(var pair in colors) {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Paint_"+pair.Key+".mat");
                if(!mat)continue;
                ColorUtility.TryParseHtmlString("#"+pair.Value,out var color);
                mat.SetTexture("_BaseMap",Texture2D.whiteTexture);mat.SetColor("_BaseColor",color);EditorUtility.SetDirty(mat);
            }
            foreach(var item in controller.Items) {
                if(item.Kind!="keycap")continue;
                var source=PrefabUtility.GetCorrespondingObjectFromSource(item);
                var original=(source?source:item).GetComponentInChildren<MeshFilter>().sharedMesh;
                var materials=item.Visual.sharedMaterials;
                int body=Array.FindIndex(materials,m=>m.name.StartsWith("Paint_key_"));
                if(body<0)continue;
                var mesh=MakeCap(original,body,item.BoundsSize.x);
                string path=Root+"/Meshes/Soft_"+item.Id+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}
                else {saved=mesh;AssetDatabase.CreateAsset(saved,path);}
                var filter=item.GetComponentInChildren<MeshFilter>();filter.sharedMesh=saved;
                PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            }
            var game=controller.Game;
            var hud=game.Objective.transform.parent;
            // Move the instructions out of the space used by the large loose PCB.
            Position(game.Objective.rectTransform,new Vector2(.5f,0),new Vector2(0,142));game.Objective.fontSize=23;
            Position(game.Progress.rectTransform,new Vector2(.5f,0),new Vector2(0,107));game.Progress.fontSize=16;
            var track=hud.Find("ProgressTrack") as RectTransform;
            Position(track,new Vector2(.5f,0),new Vector2(0,91));track.sizeDelta=new Vector2(220,4);
            game.ProgressFill.rectTransform.sizeDelta=new Vector2(220,4);
            game.StockLabel.gameObject.SetActive(false);
            game.CompletionLabel.rectTransform.anchoredPosition=new Vector2(0,170);game.CompletionLabel.fontSize=17;
            var quality=hud.Find("Quality");if(quality)quality.gameObject.SetActive(false);
            controller.HintLabel.fontSize=16;controller.HintLabel.color=new Color(.33f,.24f,.22f);
            foreach(var button in new[]{game.TestButton,game.NewOrderButton}) {
                button.GetComponent<UnityEngine.UI.Image>().color=new Color(.80f,.82f,.73f,.88f);
                button.GetComponentInChildren<TMP_Text>().fontSize=16;
            }
        }
        static void Position(RectTransform r,Vector2 anchor,Vector2 p){r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=p;}
        static Mesh MakeCap(Mesh original,int body,float width) {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var triangles=new List<int>();const int count=32;
            float depth=.356f,height=.195f;
            var rings=new[]{new Vector4(.93f,.93f,.016f,.037f),new Vector4(1,1,.043f,.044f),new Vector4(.94f,.94f,height-.028f,.052f),new Vector4(.88f,.88f,height,.053f),new Vector4(.57f,.57f,height-.023f,.046f),new Vector4(.18f,.18f,height-.030f,.015f)};
            foreach(var ring in rings) {
                float w=(width-.009f)*ring.x,h=depth*ring.y,r=Mathf.Min(ring.w,h*.25f);
                for(int corner=0;corner<4;corner++)for(int j=0;j<8;j++) {
                    float a=(corner*90+j*90f/7)*Mathf.Deg2Rad;
                    float x=(corner==0||corner==3?1:-1)*(w/2-r)+Mathf.Cos(a)*r;
                    float z=(corner<2?1:-1)*(h/2-r)+Mathf.Sin(a)*r;
                    vertices.Add(new Vector3(x,ring.z,z));uv.Add(new Vector2(x,z));
                }
            }
            for(int k=0;k<rings.Length-1;k++)for(int j=0;j<count;j++) {
                int a=k*count+j,b=k*count+(j+1)%count,c=a+count,d=b+count;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            int center=vertices.Count;vertices.Add(new Vector3(0,height-.031f,0));uv.Add(Vector2.zero);
            for(int j=0;j<count;j++)triangles.AddRange(new[]{center,(rings.Length-1)*count+(j+1)%count,(rings.Length-1)*count+j});
            var mesh=new Mesh{name="Soft sculpted keycap"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();normals.AddRange(mesh.normals);
            var subs=new List<int>[original.subMeshCount];subs[body]=triangles;
            var oldV=original.vertices;var oldN=original.normals;var oldUV=original.uv;
            for(int sub=0;sub<subs.Length;sub++) {
                if(sub==body)continue;subs[sub]=new List<int>();
                foreach(int idx in original.GetTriangles(sub)) {
                    var v=oldV[idx];v.y=height-.012f;
                    subs[sub].Add(vertices.Count);vertices.Add(v);normals.Add(oldN[idx]);uv.Add(oldUV[idx]);
                }
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=subs.Length;
            for(int sub=0;sub<subs.Length;sub++)mesh.SetTriangles(subs[sub],sub);mesh.RecalculateBounds();return mesh;
        }
    }
}
