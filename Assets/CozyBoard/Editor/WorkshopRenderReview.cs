using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace CozyBoard.Editor {
    public static class WorkshopRenderReview {
        public static void ReviewAndBuild() { Render(); WorkshopGameBuild.BuildMac(); }
        public static void Render() {
            EditorSceneManager.OpenScene("Assets/CozyBoard/Scenes/Workbench.unity");
            var controller=Object.FindFirstObjectByType<WorkshopController>();controller.Initialize();
            var camera=controller.ViewCamera;
            var target=new RenderTexture(3840,2160,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4};target.Create();
            camera.targetTexture=target;camera.aspect=3840f/2160;controller.UpdateCamera();
            controller.SendMessage("Start");controller.SendMessage("LateUpdate");
            var canvas=Object.FindFirstObjectByType<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();
            Directory.CreateDirectory("Verification");Save(target,"Verification/unity-game-4k.png");
            controller.Game.Menu.ShowOrder();Canvas.ForceUpdateCanvases();camera.Render();Save(target,"Verification/unity-order-4k.png");controller.Game.Menu.ClosePanels();
            foreach(var item in controller.Items)if(item.Stage>0&&item.Stage<4)controller.Attach(item);
            controller.Game.Refresh();controller.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();camera.Render();Save(target,"Verification/unity-switches-4k.png");
            controller.Game.SessionActive=false;controller.Assemble();controller.Game.SessionActive=true;controller.Game.Refresh();controller.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();camera.Render();
            Save(target,"Verification/unity-completed-4k.png");
            var caps=controller.Items.Where(item=>item.Kind=="keycap").OrderBy(item=>item.Id).ToArray();
            for(int i=0;i<caps.Length;i++)if(i%3==0){
                var color=WorkshopKeyPainter.Palette[(i/3)%WorkshopKeyPainter.Palette.Length];
                controller.Game.Painter.DrawLine(caps[i],new Vector2(.18f,.2f),new Vector2(.82f,.8f),color,i%2==0);
                if(i%6==0)controller.Game.Painter.DrawLine(caps[i],new Vector2(.82f,.2f),new Vector2(.18f,.8f),color);
            }
            controller.Game.Menu.SelectTool(3);controller.Game.Painter.Edit(caps[0]);controller.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();camera.Render();Save(target,"Verification/unity-paint-4k.png");
            controller.Game.Menu.SelectTool(0);
            camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);
            Debug.Log("COZY_4K_RENDER_READY 3840x2160");
        }
        static void Save(RenderTexture target,string path) {
            var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(3840,2160,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,3840,2160),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);RenderTexture.active=previous;
        }
    }
}
