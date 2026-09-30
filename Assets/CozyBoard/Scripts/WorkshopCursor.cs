using UnityEngine;
namespace CozyBoard {
    public sealed class WorkshopCursor : MonoBehaviour {
        public Texture2D Pointer;
        Texture2D smallPointer;
        Vector2 hotspot;
        public void Apply(){
            if(!Pointer)return;
            if(!smallPointer){
                // Crop transparent padding before resampling the existing painted cursor artwork.
                var pixels=Pointer.GetPixels32();int minX=Pointer.width,minY=Pointer.height,maxX=0,maxY=0;
                for(int y=0;y<Pointer.height;y++)for(int x=0;x<Pointer.width;x++)if(pixels[y*Pointer.width+x].a>32){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
                if(maxX<=minX||maxY<=minY)return;
                int w=maxX-minX+1,h=maxY-minY+1;float ratio=42f/Mathf.Max(w,h);int width=Mathf.CeilToInt(w*ratio)+4,height=Mathf.CeilToInt(h*ratio)+4;
                smallPointer=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Painted workshop brush cursor",filterMode=FilterMode.Bilinear};var result=new Color[width*height];
                for(int y=2;y<height-2;y++)for(int x=2;x<width-2;x++)result[y*width+x]=Pointer.GetPixelBilinear((minX+(x-2)/ratio)/Pointer.width,(minY+(y-2)/ratio)/Pointer.height);
                smallPointer.SetPixels(result);smallPointer.Apply();hotspot=new Vector2(3,3);
            }
            Cursor.SetCursor(smallPointer,hotspot,CursorMode.Auto);
        }
        void Start()=>Apply();
        void OnApplicationFocus(bool focused){if(focused)Apply();}
        void OnDestroy(){Cursor.SetCursor(null,Vector2.zero,CursorMode.Auto);if(smallPointer)Destroy(smallPointer);}
    }
}
