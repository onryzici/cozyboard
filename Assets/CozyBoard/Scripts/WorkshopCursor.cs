using UnityEngine;
namespace CozyBoard {
    public sealed class WorkshopCursor : MonoBehaviour {
        public Texture2D Pointer;
        Texture2D smallPointer;
        public void Apply(){
            if(!smallPointer){smallPointer=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Small paper arrow",filterMode=FilterMode.Bilinear};
                var shape=new[]{new Vector2(3,2),new Vector2(3,24),new Vector2(9,18),new Vector2(14,28),new Vector2(18,26),new Vector2(13,17),new Vector2(23,16)};
                var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){bool inside=Inside(new Vector2(x,y),shape);bool edge=false;if(!inside)for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)edge|=Inside(new Vector2(x+i,y+j),shape);pixels[(31-y)*32+x]=inside?new Color(1,.99f,.95f,1):edge?new Color(.2f,.18f,.15f,.55f):Color.clear;}
                smallPointer.SetPixels(pixels);smallPointer.Apply();}
            Cursor.SetCursor(smallPointer,new Vector2(3,2),CursorMode.Auto);
        }
        static bool Inside(Vector2 p,Vector2[] vertices){bool value=false;for(int i=0,j=vertices.Length-1;i<vertices.Length;j=i++){var a=vertices[i];var b=vertices[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)value=!value;}return value;}
        void Start()=>Apply();
        void OnApplicationFocus(bool focused){if(focused)Apply();}
        void OnDestroy(){Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);if(smallPointer)Destroy(smallPointer);}
    }
}
