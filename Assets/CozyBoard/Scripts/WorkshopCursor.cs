using UnityEngine;

namespace CozyBoard {
    public sealed class WorkshopCursor : MonoBehaviour {
        public Texture2D Pointer;
        public void Apply(){if(!Pointer)return;Cursor.SetCursor(Pointer,FindHotspot(Pointer),CursorMode.Auto);}
        void Start()=>Apply();
        void OnApplicationFocus(bool focused){if(focused)Apply();}
        static Vector2 FindHotspot(Texture2D texture){
            var pixels=texture.GetPixels32();int bestY=-1,bestX=texture.width;
            for(int y=texture.height-1;y>=0;y--){for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>32){if(bestY<0)bestY=y;if(y>=bestY-1)bestX=Mathf.Min(bestX,x);}}
            return bestY<0?Vector2.zero:new Vector2(bestX,texture.height-1-bestY);
        }
    }
}
