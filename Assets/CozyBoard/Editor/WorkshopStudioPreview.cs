using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CozyBoard.Editor {
    [InitializeOnLoad]
    static class WorkshopStudioPreview {
        static WorkshopStudioPreview(){EditorApplication.update+=Tick;}
        [MenuItem("Cozy Board/Preview 3D paint studio")]
        public static void Launch(){EditorSceneManager.OpenScene("Assets/CozyBoard/Scenes/Workbench.unity");SessionState.SetBool("Cozy.StudioPreview",true);EditorApplication.isPlaying=true;}
        static void Tick(){if(!SessionState.GetBool("Cozy.StudioPreview",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            var c=Object.FindFirstObjectByType<WorkshopController>();if(!c||!c.Game||!c.Game.SessionActive)return;
            SessionState.SetBool("Cozy.StudioPreview",false);
            foreach(var item in c.Items.Where(p=>p.Stage>0).OrderBy(p=>p.Stage))if(!item.Fitted)c.Attach(item);
            c.Game.Menu.SelectTool(3);c.Game.Painter.Edit(c.Items.First(p=>p.Kind=="keycap"&&p.Label.Contains("Esc")));
        }
    }
}
