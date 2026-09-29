using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CozyBoard {
    public sealed class WorkshopKeyPainter : MonoBehaviour {
        enum BrushShape { Detail, Flat, Airbrush, Sponge, DryBrush, Splatter, Eraser, Line, Rectangle, Ellipse }
        sealed class CanvasState {
            public WorkshopItem Item;
            public Material[] OriginalMaterials;
            public Material Material;
            public Texture2D Texture;
            public Color32[] Pixels,BasePixels;
            public int Body,Width,Height;
            public bool Modified,LegendVisible=true;
        }
        sealed class Snapshot { public string Id; public Color32[] Pixels; public bool Modified,LegendVisible; }

        public WorkshopGameMode Game;
        public GameObject PalettePanel;
        public Button[] SwatchButtons;
        public Button RoundButton,SquareButton,AirbrushButton,SpongeButton,DryBrushButton,SplatterButton,EraserButton,LineButton,RectangleButton,EllipseButton,RedoButton,ClearButton;
        public Slider BrushSize,Hardness,Opacity,Stabilization;
        public Image CurrentColor,BrushCursor;
        public TMP_Text BrushReadout;
        public RawImage ColorField,HueField;
        public RectTransform ColorCursor,HueCursor;
        public GameObject EditorPanel;
        public RawImage EditorSurface;
        public TMP_Text EditorTitle,EditorLegend,EditorHint;
        public Button DoneButton,LegendButton;
        public TMP_Text LegendButtonLabel;

        public static readonly Color[] Palette = {
            Hex("F3E8D0"), Hex("D7B675"), Hex("CA7156"), Hex("A7B696"),
            Hex("61958B"), Hex("7096AD"), Hex("705A6B"), Hex("333D3A")
        };

        readonly Dictionary<string,CanvasState> canvases=new();
        readonly Stack<List<Snapshot>> undo=new();
        readonly Stack<List<Snapshot>> redo=new();
        readonly List<Snapshot> stroke=new();
        readonly HashSet<string> touched=new();
        Color selected=Palette[4];
        float selectedHue=.46f;
        BrushShape brush=BrushShape.Detail;
        float radius=.025f;
        float hardness=.72f;
        float opacity=1;
        float stabilization=.18f;
        bool strokeOpen;
        string previousId;
        Vector2 previousPixel;
        Vector2 stabilizedUV,shapeStartUV;
        Color32[] shapeStartPixels;
        bool stabilizerReady;
        WorkshopItem editing;
        Texture2D colorFieldTexture,hueTexture;

        public bool Active=>Game&&Game.Menu&&Game.Menu.ToolMode==3;
        public string[] PaintIds=>canvases.Values.Where(c=>c.Modified||!c.LegendVisible).Select(c=>c.Item.Id).OrderBy(id=>id).ToArray();
        public string[] PaintTextures=>PaintIds.Select(id=>Convert.ToBase64String(canvases[id].Texture.EncodeToPNG())).ToArray();
        public bool[] LegendVisibility=>PaintIds.Select(id=>canvases[id].LegendVisible).ToArray();

        public void Bind(WorkshopGameMode game) {
            Game=game;
            for(int i=0;i<SwatchButtons.Length;i++){int index=i;SwatchButtons[i].onClick.AddListener(()=>Select(Palette[index]));}
            RoundButton.onClick.AddListener(()=>SetBrush(BrushShape.Detail));
            SquareButton.onClick.AddListener(()=>SetBrush(BrushShape.Flat));
            AirbrushButton.onClick.AddListener(()=>SetBrush(BrushShape.Airbrush));
            SpongeButton.onClick.AddListener(()=>SetBrush(BrushShape.Sponge));
            DryBrushButton.onClick.AddListener(()=>SetBrush(BrushShape.DryBrush));
            SplatterButton.onClick.AddListener(()=>SetBrush(BrushShape.Splatter));
            EraserButton.onClick.AddListener(()=>SetBrush(BrushShape.Eraser));
            LineButton.onClick.AddListener(()=>SetBrush(BrushShape.Line));
            RectangleButton.onClick.AddListener(()=>SetBrush(BrushShape.Rectangle));
            EllipseButton.onClick.AddListener(()=>SetBrush(BrushShape.Ellipse));
            RedoButton.onClick.AddListener(Redo);
            ClearButton.onClick.AddListener(ClearCurrent);
            LegendButton.onClick.AddListener(ToggleLegend);
            DoneButton.onClick.AddListener(CloseEditor);
            BrushSize.minValue=.004f;BrushSize.maxValue=.16f;BrushSize.SetValueWithoutNotify(radius);BrushSize.onValueChanged.AddListener(SetRadius);
            Hardness.minValue=.05f;Hardness.maxValue=1;Hardness.SetValueWithoutNotify(hardness);Hardness.onValueChanged.AddListener(value=>hardness=value);
            Opacity.minValue=.08f;Opacity.maxValue=1;Opacity.SetValueWithoutNotify(opacity);Opacity.onValueChanged.AddListener(value=>opacity=value);
            Stabilization.minValue=0;Stabilization.maxValue=.92f;Stabilization.SetValueWithoutNotify(stabilization);Stabilization.onValueChanged.AddListener(value=>stabilization=value);
            BuildColorPicker();Select(selected);SetBrush(BrushShape.Detail);SetVisible(false);
        }

        public void SetVisible(bool value){
            if(PalettePanel)PalettePanel.SetActive(value);
            if(!value)CloseEditor();else{if(PalettePanel)PalettePanel.SetActive(false);if(EditorHint)EditorHint.text="Klavyeden boyamak istediğin tuşa tıkla";Cursor.visible=true;}
            if(Game){if(Game.Objective)Game.Objective.gameObject.SetActive(!value);if(Game.Progress)Game.Progress.gameObject.SetActive(!value);if(Game.CompletionLabel)Game.CompletionLabel.gameObject.SetActive(!value&&Game.Completed);}
            if(!value){EndStroke();ShowCursor(false);}
        }
        public void Select(Color color){selected=color;Color.RGBToHSV(color,out var h,out var s,out _);if(s>.01f)selectedHue=h;if(brush==BrushShape.Eraser)SetBrush(BrushShape.Detail);if(CurrentColor)CurrentColor.color=color;RefreshColorPicker();RefreshSelections();}
        void SetBrush(BrushShape value){brush=value;RefreshSelections();RefreshReadout();}
        void SetRadius(float value){radius=Mathf.Clamp(value,.004f,.16f);if(BrushSize)BrushSize.SetValueWithoutNotify(radius);RefreshReadout();}
        void RefreshReadout(){if(BrushReadout)BrushReadout.text=$"{BrushName(brush)}  ·  {Mathf.RoundToInt(radius*1000)}";}
        static string BrushName(BrushShape value)=>value switch { BrushShape.Detail=>"DETAY",BrushShape.Flat=>"YASSI",BrushShape.Airbrush=>"AIRBRUSH",BrushShape.Sponge=>"SÜNGER",BrushShape.DryBrush=>"KURU FIRÇA",BrushShape.Splatter=>"SIÇRATMA",BrushShape.Eraser=>"SİLGİ",BrushShape.Line=>"ÇİZGİ",BrushShape.Rectangle=>"DİKDÖRTGEN",_=>"ELİPS" };

        void RefreshSelections(){
            for(int i=0;i<SwatchButtons.Length;i++)SetOutline(SwatchButtons[i],i<Palette.Length&&brush!=BrushShape.Eraser&&Close(Palette[i],selected));
            SetOutline(RoundButton,brush==BrushShape.Detail);SetOutline(SquareButton,brush==BrushShape.Flat);SetOutline(AirbrushButton,brush==BrushShape.Airbrush);SetOutline(SpongeButton,brush==BrushShape.Sponge);SetOutline(DryBrushButton,brush==BrushShape.DryBrush);SetOutline(SplatterButton,brush==BrushShape.Splatter);SetOutline(EraserButton,brush==BrushShape.Eraser);SetOutline(LineButton,brush==BrushShape.Line);SetOutline(RectangleButton,brush==BrushShape.Rectangle);SetOutline(EllipseButton,brush==BrushShape.Ellipse);
        }
        static void SetOutline(Button button,bool selected){if(!button)return;var outline=button.GetComponent<Outline>();if(outline)outline.effectDistance=selected?new Vector2(4,-4):new Vector2(1,-1);}

        void Update(){
            if(!Active||!Game||!Game.SessionActive||Game.Menu.InputBlocked){ShowCursor(false);return;}
            var mouse=Mouse.current;var keyboard=Keyboard.current;if(mouse==null)return;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(editing){CloseEditor();return;}Game.Menu.SelectTool(0);return;}
            if(editing&&keyboard!=null&&(keyboard.leftCtrlKey.isPressed||keyboard.rightCtrlKey.isPressed)){if(keyboard.zKey.wasPressedThisFrame){Undo();return;}if(keyboard.yKey.wasPressedThisFrame){Redo();return;}}
            float wheel=mouse.scroll.ReadValue().y;if(Mathf.Abs(wheel)>.01f)SetRadius(radius*Mathf.Exp(wheel*.0018f));
            if(editing){UpdateEditor(mouse,keyboard);return;}
            bool overUI=EventSystem.current&&EventSystem.current.IsPointerOverGameObject();
            WorkshopItem item=null;
            if(!overUI&&Physics.Raycast(Game.Controller.ViewCamera.ScreenPointToRay(mouse.position.ReadValue()),out var hit,100,1<<8)){
                item=hit.collider.GetComponent<WorkshopItem>();if(!(item&&item.Fitted&&item.Kind=="keycap"))item=null;
            }
            ShowCursor(false);
            if(!overUI&&mouse.leftButton.wasPressedThisFrame&&item)Edit(item);
        }

        void UpdateEditor(Mouse mouse,Keyboard keyboard){
            if(HandleColorPicker(mouse)){EndStroke();ShowCursor(false);Cursor.visible=true;return;}
            var rect=EditorSurface.rectTransform;var uiCamera=EditorSurface.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:EditorSurface.canvas.worldCamera;
            bool inside=RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,mouse.position.ReadValue(),uiCamera,out var local)&&rect.rect.Contains(local);
            Vector2 uv=inside?new Vector2(Mathf.InverseLerp(rect.rect.xMin,rect.rect.xMax,local.x),Mathf.InverseLerp(rect.rect.yMin,rect.rect.yMax,local.y)):default;
            Cursor.visible=!inside;ShowCursor(inside);if(inside&&BrushCursor){var parent=BrushCursor.rectTransform.parent as RectTransform;RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,mouse.position.ReadValue(),uiCamera,out var cursorWorld);BrushCursor.rectTransform.position=cursorWorld;float diameter=Mathf.Max(12,radius*2*rect.rect.height);float spread=brush==BrushShape.Splatter?1.35f:1;float cursorHeight=brush==BrushShape.Flat||brush==BrushShape.DryBrush?diameter*.44f:diameter*spread;BrushCursor.rectTransform.sizeDelta=new Vector2(diameter*spread,cursorHeight);BrushCursor.color=brush==BrushShape.Eraser?new Color(.95f,.38f,.33f,.95f):new Color(selected.r,selected.g,selected.b,brush==BrushShape.Airbrush?.62f:.95f);}
            if(mouse.leftButton.wasPressedThisFrame&&inside){if(keyboard!=null&&keyboard.altKey.isPressed){Select(Sample(editing,uv));Game.Menu.Toast("Renk tuştan alındı.");}else{BeginStroke();if(IsShape(brush))BeginShape(editing,uv);}}
            if(strokeOpen&&mouse.leftButton.isPressed&&inside){if(IsShape(brush))PreviewShape(editing,uv);else{if(!stabilizerReady){stabilizedUV=uv;stabilizerReady=true;}else stabilizedUV=Vector2.Lerp(stabilizedUV,uv,Mathf.Lerp(.72f,.14f,stabilization));Stamp(editing,stabilizedUV);}}
            if(mouse.leftButton.wasReleasedThisFrame)EndStroke();
        }

        public void Edit(WorkshopItem item){
            if(!item||!item.Fitted||item.Kind!="keycap")return;EndStroke();editing=item;var canvas=Canvas(item);EditorSurface.texture=canvas.Texture;
            float aspect=Mathf.Clamp((float)canvas.Width/canvas.Height,1,4.2f),height=aspect>2.2f?250:430,width=Mathf.Min(1080,height*aspect);
            ((RectTransform)EditorSurface.rectTransform.parent).sizeDelta=new Vector2(width,height);if(EditorSurface.material)EditorSurface.material.SetFloat("_Aspect",aspect);EditorTitle.text="TUŞ ATÖLYESİ  ·  "+DisplayName(item);EditorLegend.text=DisplayName(item);EditorLegend.fontSize=aspect>2.2f?54:42;RefreshLegend(canvas);EditorHint.text="Tuşun üzerinde çiz  ·  Tekerlek: fırça boyutu  ·  Alt: renk al";EditorPanel.SetActive(true);if(PalettePanel)PalettePanel.SetActive(true);Game.Menu.Toast("Seçilen tuş büyütüldü. Şimdi ayrıntılı çizebilirsin.");
        }
        public void CloseEditor(){EndStroke();editing=null;if(EditorPanel)EditorPanel.SetActive(false);if(PalettePanel)PalettePanel.SetActive(false);ShowCursor(false);Cursor.visible=true;if(Active&&EditorHint)EditorHint.text="Klavyeden boyamak istediğin tuşa tıkla";}
        static string DisplayName(WorkshopItem item){var value=string.IsNullOrWhiteSpace(item.Label)?item.Id:item.Label;if(value.StartsWith("Tuş ",StringComparison.OrdinalIgnoreCase))value=value.Substring(4);return value.Replace("Space","BOŞLUK");}

        void ShowCursor(bool value){if(BrushCursor)BrushCursor.gameObject.SetActive(value&&Active);}

        void BuildColorPicker(){
            hueTexture=new Texture2D(256,1,TextureFormat.RGBA32,false){name="Paint hue spectrum",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var huePixels=new Color[256];for(int x=0;x<256;x++)huePixels[x]=Color.HSVToRGB(x/255f,1,1);hueTexture.SetPixels(huePixels);hueTexture.Apply(false);if(HueField)HueField.texture=hueTexture;
            colorFieldTexture=new Texture2D(128,128,TextureFormat.RGBA32,false){name="Paint color field",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};if(ColorField)ColorField.texture=colorFieldTexture;RefreshColorPicker();
        }
        void RefreshColorPicker(){
            if(!colorFieldTexture)return;var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=Color.HSVToRGB(selectedHue,x/127f,y/127f);colorFieldTexture.SetPixels(pixels);colorFieldTexture.Apply(false);
            Color.RGBToHSV(selected,out _,out var saturation,out var value);if(ColorCursor){var r=ColorField.rectTransform.rect;ColorCursor.anchoredPosition=new Vector2((saturation-.5f)*r.width,(value-.5f)*r.height);}if(HueCursor)HueCursor.anchoredPosition=new Vector2((selectedHue-.5f)*HueField.rectTransform.rect.width,0);
        }
        bool HandleColorPicker(Mouse mouse){
            if(!mouse.leftButton.isPressed)return false;var camera=ColorField.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ColorField.canvas.worldCamera;
            if(ScreenUV(ColorField.rectTransform,mouse.position.ReadValue(),camera,out var sv)){selected=Color.HSVToRGB(selectedHue,sv.x,sv.y);if(CurrentColor)CurrentColor.color=selected;RefreshColorPicker();RefreshSelections();return true;}
            if(ScreenUV(HueField.rectTransform,mouse.position.ReadValue(),camera,out var hue)){selectedHue=hue.x;Color.RGBToHSV(selected,out _,out var s,out var v);selected=Color.HSVToRGB(selectedHue,s,v);if(CurrentColor)CurrentColor.color=selected;RefreshColorPicker();RefreshSelections();return true;}return false;
        }
        static bool ScreenUV(RectTransform rect,Vector2 screen,Camera camera,out Vector2 uv){uv=default;if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screen,camera,out var local)||!rect.rect.Contains(local))return false;uv=new Vector2(Mathf.InverseLerp(rect.rect.xMin,rect.rect.xMax,local.x),Mathf.InverseLerp(rect.rect.yMin,rect.rect.yMax,local.y));return true;}
        void BeginStroke(){if(strokeOpen)return;strokeOpen=true;stroke.Clear();touched.Clear();previousId=null;stabilizerReady=false;shapeStartPixels=null;}
        void EndStroke(){if(!strokeOpen)return;strokeOpen=false;if(stroke.Count>0){undo.Push(new List<Snapshot>(stroke));redo.Clear();}stroke.Clear();touched.Clear();previousId=null;stabilizerReady=false;shapeStartPixels=null;}

        static bool IsShape(BrushShape value)=>value==BrushShape.Line||value==BrushShape.Rectangle||value==BrushShape.Ellipse;
        void BeginShape(WorkshopItem item,Vector2 uv){var canvas=Canvas(item);Touch(canvas);shapeStartUV=uv;shapeStartPixels=(Color32[])canvas.Pixels.Clone();PreviewShape(item,uv);}
        void PreviewShape(WorkshopItem item,Vector2 uv){
            var canvas=Canvas(item);if(shapeStartPixels==null)return;canvas.Pixels=(Color32[])shapeStartPixels.Clone();float size=radius*canvas.Height;
            Vector2 a=new Vector2(shapeStartUV.x*(canvas.Width-1),shapeStartUV.y*(canvas.Height-1)),b=new Vector2(uv.x*(canvas.Width-1),uv.y*(canvas.Height-1));
            if(brush==BrushShape.Line)RasterLine(canvas,a,b,size);
            else if(brush==BrushShape.Rectangle){Vector2 c=new Vector2(a.x,b.y);Vector2 d=new Vector2(b.x,a.y);RasterLine(canvas,a,c,size);RasterLine(canvas,c,b,size);RasterLine(canvas,b,d,size);RasterLine(canvas,d,a,size);}
            else {Vector2 center=(a+b)*.5f;Vector2 radii=new Vector2(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y))*.5f;int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.PI*(radii.x+radii.y)/Mathf.Max(2,size*.24f)),24,220);Vector2 last=center+new Vector2(radii.x,0);for(int i=1;i<=steps;i++){float angle=i*Mathf.PI*2/steps;var next=center+new Vector2(Mathf.Cos(angle)*radii.x,Mathf.Sin(angle)*radii.y);RasterLine(canvas,last,next,size);last=next;}}
            canvas.Modified=true;canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);
        }
        void RasterLine(CanvasState canvas,Vector2 from,Vector2 to,float size){int count=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(from,to)/Mathf.Max(1,size*.24f)),1,256);for(int i=0;i<=count;i++)StampPixels(canvas,Vector2.Lerp(from,to,(float)i/count),size);}

        void Stamp(WorkshopItem item,Vector2 uv){
            var canvas=Canvas(item);Touch(canvas);var pixel=new Vector2(uv.x*(canvas.Width-1),uv.y*(canvas.Height-1));
            float brushPixels=radius*canvas.Height;
            int count=previousId==item.Id?Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(previousPixel,pixel)/Mathf.Max(1,brushPixels*.28f)),1,96):1;
            for(int i=1;i<=count;i++)StampPixels(canvas,previousId==item.Id?Vector2.Lerp(previousPixel,pixel,(float)i/count):pixel,brushPixels);
            previousId=item.Id;previousPixel=pixel;canvas.Modified=true;canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);
        }

        void StampPixels(CanvasState canvas,Vector2 center,float size){
            float reach=brush==BrushShape.Splatter?size*1.35f:size;
            int minX=Mathf.Max(0,Mathf.FloorToInt(center.x-reach)),maxX=Mathf.Min(canvas.Width-1,Mathf.CeilToInt(center.x+reach));
            int minY=Mathf.Max(0,Mathf.FloorToInt(center.y-reach)),maxY=Mathf.Min(canvas.Height-1,Mathf.CeilToInt(center.y+reach));
            float edge=Mathf.Max(1,size*Mathf.Lerp(.48f,.04f,hardness));
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++){
                float dx=Mathf.Abs(x-center.x),dy=Mathf.Abs(y-center.y),radial=Mathf.Sqrt(dx*dx+dy*dy),distance=radial;
                if(brush==BrushShape.Flat||brush==BrushShape.DryBrush)distance=Mathf.Max(dx,dy/Mathf.Lerp(.28f,.48f,hardness));
                float noise=Hash(x,y,Mathf.FloorToInt(center.x+center.y));
                float strength;
                if(brush==BrushShape.Airbrush){if(radial>size)continue;strength=Mathf.Pow(1-radial/Mathf.Max(1,size),2.2f)*.24f;}
                else if(brush==BrushShape.Sponge){if(radial>size||noise<.28f)continue;float pores=Hash(x/3,y/3,17);strength=(.28f+.72f*pores)*(1-Mathf.SmoothStep(.72f,1,radial/size));}
                else if(brush==BrushShape.DryBrush){if(distance>size||noise<Mathf.Lerp(.30f,.58f,1-opacity))continue;float bristle=.45f+.55f*Mathf.Abs(Mathf.Sin((y+center.x)*.72f));strength=bristle*(1-Mathf.SmoothStep(.76f,1,distance/size));}
                else if(brush==BrushShape.Splatter){float dot=Hash(x/2,y/2,31),spread=radial/Mathf.Max(1,reach);if(dot<Mathf.Lerp(.89f,.72f,1-spread)||spread>1)continue;strength=Mathf.Lerp(.45f,1,Hash(x,y,53));}
                else {if(distance>size)continue;strength=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(0,size-edge),size,distance));}
                int index=y*canvas.Width+x;Color target=brush==BrushShape.Eraser?canvas.BasePixels[index]:selected;canvas.Pixels[index]=Color.Lerp(canvas.Pixels[index],target,strength*opacity);
            }
        }
        static float Hash(int x,int y,int seed){unchecked{uint n=(uint)(x*374761393+y*668265263+seed*1442695041);n=(n^(n>>13))*1274126177u;return (n^(n>>16))/4294967295f;}}

        void Touch(CanvasState canvas){if(touched.Add(canvas.Item.Id))stroke.Add(new Snapshot{Id=canvas.Item.Id,Pixels=(Color32[])canvas.Pixels.Clone(),Modified=canvas.Modified,LegendVisible=canvas.LegendVisible});}
        public void Undo(){EndStroke();if(undo.Count==0){Game.Menu.Toast("Geri alınacak boya yok.");return;}redo.Push(SwapSnapshots(undo.Pop()));Game.Menu.Toast("Son boya işlemi geri alındı.");}
        public void Redo(){EndStroke();if(redo.Count==0){Game.Menu.Toast("Yinelenecek boya yok.");return;}undo.Push(SwapSnapshots(redo.Pop()));Game.Menu.Toast("Boya işlemi yeniden uygulandı.");}
        List<Snapshot> SwapSnapshots(List<Snapshot> source){var reverse=new List<Snapshot>();foreach(var snapshot in source){if(!Game.Controller.Lookup.TryGetValue(snapshot.Id,out var item))continue;var canvas=Canvas(item);reverse.Add(new Snapshot{Id=snapshot.Id,Pixels=(Color32[])canvas.Pixels.Clone(),Modified=canvas.Modified,LegendVisible=canvas.LegendVisible});canvas.Pixels=(Color32[])snapshot.Pixels.Clone();canvas.Modified=snapshot.Modified;canvas.LegendVisible=snapshot.LegendVisible;ApplyMaterials(canvas);if(editing==item)RefreshLegend(canvas);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);}return reverse;}
        public void ClearCurrent(){if(!editing)return;BeginStroke();var canvas=Canvas(editing);Touch(canvas);canvas.Pixels=(Color32[])canvas.BasePixels.Clone();canvas.Modified=false;canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);EndStroke();Game.Menu.Toast("Seçili tuş temizlendi.");}
        public void ToggleLegend(){if(!editing)return;BeginStroke();var canvas=Canvas(editing);Touch(canvas);canvas.LegendVisible=!canvas.LegendVisible;ApplyMaterials(canvas);RefreshLegend(canvas);EndStroke();Game.Menu.Toast(canvas.LegendVisible?"Tuş harfi gösteriliyor.":"Tuş harfi gizlendi.");}
        void RefreshLegend(CanvasState canvas){if(EditorLegend)EditorLegend.gameObject.SetActive(canvas.LegendVisible);if(LegendButtonLabel)LegendButtonLabel.text=canvas.LegendVisible?"HARF  AÇIK":"HARF  KAPALI";}

        public void Fill(WorkshopItem item,Color color){var canvas=Canvas(item);for(int i=0;i<canvas.Pixels.Length;i++)canvas.Pixels[i]=color;canvas.Modified=true;canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);}
        public void DrawLine(WorkshopItem item,Vector2 from,Vector2 to,Color color,bool square=false){
            if(!item||!item.Fitted||item.Kind!="keycap")return;
            var oldColor=selected;var oldBrush=brush;selected=color;brush=square?BrushShape.Flat:BrushShape.Detail;
            BeginStroke();previousId=null;
            var canvas=Canvas(item);float distance=Vector2.Distance(from,to)*canvas.Height;int steps=Mathf.Clamp(Mathf.CeilToInt(distance/Mathf.Max(1,radius*canvas.Height*.25f)),1,128);
            for(int i=0;i<=steps;i++)Stamp(item,Vector2.Lerp(from,to,(float)i/steps));
            EndStroke();selected=oldColor;brush=oldBrush;RefreshSelections();RefreshReadout();
        }
        public bool HasPaint(string id)=>canvases.TryGetValue(id,out var canvas)&&canvas.Modified;
        public bool LegendIsVisible(string id)=>!canvases.TryGetValue(id,out var canvas)||canvas.LegendVisible;
        public Color Sample(WorkshopItem item,Vector2 uv){var canvas=Canvas(item);int x=Mathf.Clamp(Mathf.RoundToInt(uv.x*(canvas.Width-1)),0,canvas.Width-1),y=Mathf.Clamp(Mathf.RoundToInt(uv.y*(canvas.Height-1)),0,canvas.Height-1);return canvas.Pixels[y*canvas.Width+x];}

        CanvasState Canvas(WorkshopItem item){
            if(canvases.TryGetValue(item.Id,out var canvas))return canvas;
            int body=BodyMaterial(item);var originals=item.Visual.sharedMaterials;var source=originals[body];
            int height=512,width=Mathf.Clamp(Mathf.RoundToInt(height*Mathf.Max(1,item.BoundsSize.x/Mathf.Max(.01f,item.BoundsSize.z))),512,2048);
            Color baseColor=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Paint "+item.Id,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[width*height];for(int i=0;i<pixels.Length;i++)pixels[i]=baseColor;texture.SetPixels32(pixels);texture.Apply(false);
            var material=new Material(source){name=source.name+" · "+item.Id};material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",texture);
            canvas=new CanvasState{Item=item,OriginalMaterials=originals,Material=material,Texture=texture,Pixels=pixels,BasePixels=(Color32[])pixels.Clone(),Body=body,Width=width,Height=height};canvases.Add(item.Id,canvas);ApplyMaterials(canvas);return canvas;
        }
        void ApplyMaterials(CanvasState canvas){var assigned=(Material[])canvas.OriginalMaterials.Clone();assigned[canvas.Body]=canvas.Material;if(!canvas.LegendVisible)for(int i=0;i<assigned.Length;i++)if(assigned[i]&&assigned[i].name.StartsWith("Paint_legend",StringComparison.OrdinalIgnoreCase))assigned[i]=canvas.Material;canvas.Item.Visual.SetPropertyBlock(null,canvas.Body);canvas.Item.Visual.sharedMaterials=assigned;}

        public void ResetPaint(){EndStroke();foreach(var canvas in canvases.Values)Dispose(canvas);canvases.Clear();undo.Clear();redo.Clear();}
        void Dispose(CanvasState canvas){if(canvas.Item&&canvas.Item.Visual)canvas.Item.Visual.sharedMaterials=canvas.OriginalMaterials;if(Application.isPlaying){Destroy(canvas.Material);Destroy(canvas.Texture);}else{DestroyImmediate(canvas.Material);DestroyImmediate(canvas.Texture);}}

        public void Restore(string[] ids,string[] textures,string[] legacyColors=null,bool[] legendVisibility=null){
            ResetPaint();if(ids==null)return;
            for(int i=0;i<ids.Length;i++)if(Game.Controller.Lookup.TryGetValue(ids[i],out var item)){
                if(textures!=null&&i<textures.Length&&!string.IsNullOrEmpty(textures[i])){try{var canvas=Canvas(item);var source=new Texture2D(2,2);source.LoadImage(Convert.FromBase64String(textures[i]));var restored=new Color32[canvas.Width*canvas.Height];for(int y=0;y<canvas.Height;y++)for(int x=0;x<canvas.Width;x++)restored[y*canvas.Width+x]=source.GetPixelBilinear((x+.5f)/canvas.Width,(y+.5f)/canvas.Height);canvas.Pixels=restored;canvas.Modified=true;canvas.LegendVisible=legendVisibility==null||i>=legendVisibility.Length||legendVisibility[i];ApplyMaterials(canvas);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);if(Application.isPlaying)Destroy(source);else DestroyImmediate(source);}catch(Exception e){Debug.LogWarning("Paint restore failed for "+ids[i]+": "+e.Message);}}
                else if(legacyColors!=null&&i<legacyColors.Length&&ColorUtility.TryParseHtmlString(legacyColors[i],out var color))Fill(item,color);
            }
        }

        static Vector2 LocalUV(WorkshopItem item,Vector3 world){var local=item.Visual.transform.InverseTransformPoint(world);var bounds=item.Visual.GetComponent<MeshFilter>().sharedMesh.bounds;return new Vector2(Mathf.InverseLerp(bounds.min.x,bounds.max.x,local.x),Mathf.InverseLerp(bounds.min.z,bounds.max.z,local.z));}
        static int BodyMaterial(WorkshopItem item){var materials=item.Visual.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&materials[i].name.StartsWith("Paint_key_",StringComparison.OrdinalIgnoreCase))return i;return 0;}
        static bool Close(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<.02f;
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        void OnDestroy(){foreach(var canvas in canvases.Values)Dispose(canvas);canvases.Clear();if(colorFieldTexture)Destroy(colorFieldTexture);if(hueTexture)Destroy(hueTexture);Cursor.visible=true;}
    }
}
