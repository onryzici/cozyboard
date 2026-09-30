using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CozyBoard {
    public sealed partial class WorkshopKeyPainter : MonoBehaviour {
        enum BrushShape { Detail, Flat, Airbrush, Sponge, DryBrush, Splatter, Eraser, Line, Rectangle, Ellipse }
        sealed class CanvasState {
            public WorkshopItem Item;
            public Mesh OriginalMesh;
            public KeycapSurface Surface;
            public Material[] OriginalMaterials;
            public Material Material;
            public Texture2D Texture;
            public Color32[] Pixels,BasePixels;
            public int Body,Width,Height;
            public bool Modified,LegendVisible=true;
            public readonly List<WorkshopTapeStrip> Tape=new();
        }
        sealed class Snapshot { public string Id; public Color32[] Pixels; public bool Modified,LegendVisible; public WorkshopTapeStrip[] Tape; }

        public WorkshopGameMode Game;
        public Shader StudioShader;
        KeycapStudio studio;
        Coroutine samplesPreparation;
        KeycapSurface cachedSurface;
        const string PaintHelp="Sol: boya · Sağ / orta: döndür · Tekerlek: yakınlaş · Shift + tekerlek: boyut · Alt: renk al · F: sıfırla";
        Vector2 lastScreenStroke;
        bool surfaceStrokeReady;
        public bool Editing=>editing;
        public bool PreparingSurface=>samplesPreparation!=null;
        public bool StudioWarmed {get;private set;}
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
        float selectedValue=1;
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
        public string[] PaintIds=>canvases.Values.Where(c=>c.Modified||!c.LegendVisible||c.Tape.Count>0).Select(c=>c.Item.Id).OrderBy(id=>id).ToArray();
        public string[] PaintTextures=>PaintIds.Select(id=>Convert.ToBase64String(canvases[id].Texture.EncodeToPNG())).ToArray();
        public string[] TapeMasks=>PaintIds.Select(id=>JsonUtility.ToJson(new WorkshopTapeSave{Strips=canvases[id].Tape.ToArray()})).ToArray();
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
            ConfigureStudioUI();BuildColorPicker();Select(selected);SetBrush(BrushShape.Detail);SetVisible(false);
            if(Application.isPlaying)StartCoroutine(WarmStudio());
        }

        System.Collections.IEnumerator WarmStudio(){
            yield return null;
            if(studio!=null){StudioWarmed=true;yield break;}
            var item=Game.Controller.Items.FirstOrDefault(x=>x.Kind=="keycap");if(!item)yield break;
            // Compile the first studio draw while entering the atelier, before a key is selected.
            var surface=new KeycapSurface(item.Visual.GetComponent<MeshFilter>().sharedMesh,BodyMaterial(item));
            using(var preview=new KeycapStudio(surface,item.Visual.sharedMaterials,StudioShader?StudioShader:Shader.Find("CozyBoard/KeycapStudio")))preview.Render();
            Destroy(surface.Mesh);StudioWarmed=true;
        }

        public void SetVisible(bool value){
            if(PalettePanel)PalettePanel.SetActive(value);
            if(!value)CloseEditor();else{if(PalettePanel)PalettePanel.SetActive(false);if(EditorHint)EditorHint.text="Klavyeden boyamak istediğin tuşa tıkla";Cursor.visible=true;}
            if(Game){if(Game.Objective)Game.Objective.gameObject.SetActive(!value);if(Game.Progress)Game.Progress.gameObject.SetActive(!value);if(Game.CompletionLabel)Game.CompletionLabel.gameObject.SetActive(!value&&Game.Completed);}
            if(!value){EndStroke();ShowCursor(false);}
        }
        public void Select(Color color){selected=color;Color.RGBToHSV(color,out var h,out var s,out selectedValue);if(s>.01f)selectedHue=h;if(brush==BrushShape.Eraser)SetBrush(BrushShape.Detail);if(CurrentColor)CurrentColor.color=color;RefreshColorPicker();RefreshSelections();}
        void SetBrush(BrushShape value){tapeMode=false;brush=value;RefreshTapeUI();RefreshSelections();RefreshReadout();}
        void SetRadius(float value){radius=Mathf.Clamp(value,.004f,.16f);if(BrushSize)BrushSize.SetValueWithoutNotify(radius);RefreshReadout();}
        void RefreshReadout(){if(BrushReadout)BrushReadout.text=$"{BrushName(brush)}  ·  {Mathf.RoundToInt(radius*1000)}";}
        static string BrushName(BrushShape value)=>value switch { BrushShape.Detail=>"DETAY",BrushShape.Flat=>"YASSI",BrushShape.Airbrush=>"AIRBRUSH",BrushShape.Sponge=>"SÜNGER",BrushShape.DryBrush=>"KURU FIRÇA",BrushShape.Splatter=>"SIÇRATMA",BrushShape.Eraser=>"SİLGİ",BrushShape.Line=>"ÇİZGİ",BrushShape.Rectangle=>"DİKDÖRTGEN",_=>"ELİPS" };

        void RefreshSelections(){
            for(int i=0;i<SwatchButtons.Length;i++)SetOutline(SwatchButtons[i],i<Palette.Length&&brush!=BrushShape.Eraser&&Close(Palette[i],selected));
            SetOutline(RoundButton,brush==BrushShape.Detail);SetOutline(SquareButton,brush==BrushShape.Flat);SetOutline(AirbrushButton,brush==BrushShape.Airbrush);SetOutline(SpongeButton,brush==BrushShape.Sponge);SetOutline(DryBrushButton,brush==BrushShape.DryBrush);SetOutline(SplatterButton,brush==BrushShape.Splatter);SetOutline(EraserButton,brush==BrushShape.Eraser);SetOutline(LineButton,brush==BrushShape.Line);SetOutline(RectangleButton,brush==BrushShape.Rectangle);SetOutline(EllipseButton,brush==BrushShape.Ellipse);
        }
        static void SetOutline(Button button,bool selected){if(!button||!button.targetGraphic)return;var graphic=button.targetGraphic;var outline=graphic.GetComponent<Outline>()??graphic.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.98f,.88f,.61f,.8f);outline.effectDistance=selected?new Vector2(2,-2):Vector2.zero;graphic.color=selected?Color.white:new Color(.86f,.86f,.86f);}

        void Update(){
            if(!Active||!Game||!Game.SessionActive||Game.Menu.InputBlocked){ShowCursor(false);return;}
            var mouse=Mouse.current;var keyboard=Keyboard.current;if(mouse==null)return;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(editing){CloseEditor();return;}Game.Menu.SelectTool(0);return;}
            if(editing&&keyboard!=null&&(keyboard.leftCtrlKey.isPressed||keyboard.rightCtrlKey.isPressed||keyboard.leftMetaKey.isPressed||keyboard.rightMetaKey.isPressed)){if(keyboard.zKey.wasPressedThisFrame){Undo();return;}if(keyboard.yKey.wasPressedThisFrame){Redo();return;}}
            float wheel=mouse.scroll.ReadValue().y;if(Mathf.Abs(wheel)>.01f){if(editing&&(keyboard==null||!keyboard.shiftKey.isPressed))studio?.Zoom(wheel);else SetRadius(radius*Mathf.Exp(WorkshopAtelierStyle.WheelSteps(wheel)*.14f));}
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
            if(studio==null)return;
            if(peeling){studio.Render();return;}
            var rect=EditorSurface.rectTransform;var uiCamera=EditorSurface.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:EditorSurface.canvas.worldCamera;
            var screen=mouse.position.ReadValue();bool inside=ScreenUV(rect,screen,uiCamera,out var viewport);
            ShowCursor(false);Cursor.visible=true;
            if(HandleColorPicker(mouse)){EndStroke();studio.HideCursor();studio.Render();return;}
            if(keyboard!=null&&keyboard.fKey.wasPressedThisFrame)studio.ResetView();
            if(!Canvas(editing).Surface.SamplesReady){if(inside&&(mouse.rightButton.isPressed||mouse.middleButton.isPressed))studio.Orbit(mouse.delta.ReadValue());studio.Render();return;}
            if(HandleTape(mouse,inside,viewport)){studio.Render();return;}
            if(inside&&(mouse.rightButton.isPressed||mouse.middleButton.isPressed)){EndStroke();surfaceStrokeReady=false;studio.Orbit(mouse.delta.ReadValue());studio.Render();return;}
            bool hit=inside&&studio.Hit(viewport,out _);
            if(hit){
                studio.Hit(viewport,out var contact);var canvas=Canvas(editing);float size=radius*canvas.Surface.Bounds.size.z*2;
                studio.Cursor(contact,size,selected);Cursor.visible=false;
                if(mouse.leftButton.wasPressedThisFrame){
                    if(keyboard!=null&&keyboard.altKey.isPressed){Select(Sample(editing,contact.textureCoord));studio.Render();return;}
                    BeginStroke();surfaceStrokeReady=false;
                }
                if(strokeOpen&&mouse.leftButton.isPressed){
                    Touch(canvas);int steps=surfaceStrokeReady?Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(lastScreenStroke,screen)/3),1,128):1;
                    for(int i=1;i<=steps;i++){
                        Vector2 point=surfaceStrokeReady?Vector2.Lerp(lastScreenStroke,screen,(float)i/steps):screen;
                        if(ScreenUV(rect,point,uiCamera,out var uv)&&studio.Hit(uv,out var sample))PaintSurface(canvas,studio.Local(sample.point),sample.normal,size);
                    }
                    studio.Dab(contact.point,size);lastScreenStroke=screen;surfaceStrokeReady=true;canvas.Modified=true;canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();
                }
            }else {surfaceStrokeReady=false;studio.HideCursor();}
            if(mouse.leftButton.wasReleasedThisFrame)EndStroke();
            studio.Render();
        }

        void PaintSurface(CanvasState canvas,Vector3 center,Vector3 normal,float size){
            var tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)>.95f?Vector3.forward:Vector3.up).normalized;
            var vertical=Vector3.Cross(normal,tangent);float reach=brush==BrushShape.Splatter?size*1.35f:size;
            canvas.Surface.Visit(center,reach,texel=>{
                if(Vector3.Dot(texel.Normal,normal)<-.05f||(studio!=null&&Vector3.Dot(texel.Normal,studio.ViewDirection)<=.02f))return;
                if(Masked(canvas,texel.Position))return;
                Vector3 offset=texel.Position-center;float distance=offset.magnitude/size;
                if(brush==BrushShape.Flat||brush==BrushShape.DryBrush)distance=Mathf.Max(Mathf.Abs(Vector3.Dot(offset,tangent)),Mathf.Abs(Vector3.Dot(offset,vertical))/.4f)/size;
                int x=texel.Index%canvas.Width,y=texel.Index/canvas.Width;float grain=Hash(x,y,19);
                distance*=1+(Mathf.PerlinNoise(x*.11f,y*.11f)-.5f)*.085f;
                float strength=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(hardness*.8f,1,distance));
                if(brush==BrushShape.Airbrush)strength=Mathf.Pow(Mathf.Max(0,1-distance),2)*.22f;
                else if(brush==BrushShape.Sponge)strength*=grain<.28f?0:.35f+.65f*grain;
                else if(brush==BrushShape.DryBrush)strength*=grain<.4f?0:.4f+.6f*Mathf.Abs(Mathf.Sin(y*.72f));
                else if(brush==BrushShape.Splatter)strength=distance<1.35f&&Hash(x/3,y/3,31)>.91f?.8f:0;
                if(strength<=0)return;
                Color target=brush==BrushShape.Eraser?canvas.BasePixels[texel.Index]:selected;
                if(brush!=BrushShape.Eraser){float pigment=.97f+.03f*Mathf.PerlinNoise(x*.17f,y*.17f);target=new Color(target.r*pigment,target.g*pigment,target.b*pigment,1);}
                canvas.Pixels[texel.Index]=Color.Lerp(canvas.Pixels[texel.Index],target,strength*opacity);
            });
        }

        public void PaintAtSurface(WorkshopItem item,Vector3 localPoint,Vector3 localNormal,Color color,float brushRadius){
            if(!item||!item.Fitted||item.Kind!="keycap")return;
            var canvas=Canvas(item);var previous=selected;selected=color;BeginStroke();Touch(canvas);
            PaintSurface(canvas,localPoint,localNormal,brushRadius);canvas.Modified=true;canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();EndStroke();selected=previous;
        }
        public Vector2 SurfaceUV(WorkshopItem item,Vector3 localPoint,Vector3 normal)=>Canvas(item).Surface.UV(localPoint,KeycapSurface.Face(normal));

        public void Edit(WorkshopItem item){
            if(!item||!item.Fitted||item.Kind!="keycap")return;CloseEditor();editing=item;var canvas=Canvas(item);
            if(cachedSurface!=canvas.Surface){cachedSurface?.ReleaseSamples();cachedSurface=canvas.Surface;}
            ConfigureStudioUI();
            studio=new KeycapStudio(canvas.Surface,item.Visual.sharedMaterials,StudioShader?StudioShader:Shader.Find("CozyBoard/KeycapStudio"));
            EditorSurface.texture=studio.Target;EditorTitle.text="BOYA ATÖLYESİ  /  "+DisplayName(item);
            BuildTapeUI();SyncTape();RefreshTapeUI();RefreshLegend(canvas);EditorHint.text=PaintHelp;
            EditorPanel.SetActive(true);if(PalettePanel)PalettePanel.SetActive(true);studio.Render();
            if(!canvas.Surface.SamplesReady){if(Application.isPlaying)samplesPreparation=StartCoroutine(PrepareSurface(canvas));else canvas.Surface.CacheSamples();}
        }

        System.Collections.IEnumerator PrepareSurface(CanvasState canvas){
            EditorHint.text="Birazdan boyayabilirsin…";yield return null;
            var preparation=canvas.Surface.CacheSamplesIncremental();while(preparation.MoveNext())yield return preparation.Current;
            samplesPreparation=null;if(editing==canvas.Item)EditorHint.text=PaintHelp;
        }

        void ConfigureStudioUI(){
            if(!EditorSurface)return;
            EditorSurface.material=null;
            var surface=EditorSurface.rectTransform.parent as RectTransform;surface.sizeDelta=new Vector2(1240,660);
            var mask=surface.GetComponent<UnityEngine.UI.Mask>();if(mask)mask.enabled=false;
            var image=surface.GetComponent<UnityEngine.UI.Image>();if(image)image.enabled=false;
            var shadow=surface.GetComponent<UnityEngine.UI.Shadow>();if(shadow)shadow.enabled=false;
            var mount=surface.parent as RectTransform;mount.sizeDelta=new Vector2(1240,660);mount.anchoredPosition=new Vector2(0,-4);
            var card=mount.parent as RectTransform;card.sizeDelta=new Vector2(1360,860);card.anchoredPosition=new Vector2(205,30);
            var cardImage=card.GetComponent<UnityEngine.UI.Image>();if(cardImage)cardImage.color=new Color(.13f,.17f,.18f,1);
            EditorTitle.color=new Color(.94f,.9f,.79f);EditorTitle.fontSize=24;
            EditorHint.color=new Color(.76f,.82f,.79f);EditorHint.fontSize=14;EditorHint.rectTransform.sizeDelta=new Vector2(1260,38);
            if(EditorLegend)EditorLegend.gameObject.SetActive(false);
            foreach(var button in new[]{LineButton,RectangleButton,EllipseButton})if(button)button.gameObject.SetActive(false);
            var label=PalettePanel.transform.Find("ShapeLabel");if(label)label.GetComponent<TMP_Text>().text="3D YÜZEY BOYAMA";
            var paletteRect=(RectTransform)PalettePanel.transform;paletteRect.anchorMin=paletteRect.anchorMax=paletteRect.pivot=new Vector2(0,.5f);paletteRect.anchoredPosition=new Vector2(38,0);paletteRect.sizeDelta=new Vector2(356,956);
            foreach(string name in new[]{"ColorCard","BrushCard","ControlCard"}){var background=PalettePanel.transform.Find(name);if(background)background.gameObject.SetActive(false);}
            Place("Title",0,28,310,34);Place("CurrentColor",0,53,286,4);Place("ColorField",0,155,190,190);Place("HueField",0,268,280,18);
            for(int i=0;i<SwatchButtons.Length;i++)Place("Swatch"+i,-105+(i%4)*70,310+(i/4)*40,52,30);
            Place("ToolLabel",0,386,290,22);Place("RoundBrush",-100,417,92,36);Place("SquareBrush",0,417,92,36);Place("Airbrush",100,417,92,36);
            Place("Sponge",-100,457,92,36);Place("DryBrush",0,457,92,36);Place("Splatter",100,457,92,36);Place("Eraser",0,497,290,34);
            Place("ShapeLabel",0,534,290,20);Place("Line",-100,565,92,34);Place("Rectangle",0,565,92,34);Place("Ellipse",100,565,92,34);
            Place("SizeLabel",0,607,290,18);Place("BrushSize",0,631,270,24);Place("BrushReadout",0,659,270,22);
            Place("HardnessLabel",0,690,270,18);Place("Hardness",0,714,270,22);Place("OpacityLabel",0,754,270,18);Place("Opacity",0,778,270,22);
            Place("StabilizationLabel",0,814,290,18);Place("RedoPaint",-76,855,136,36);Place("ClearPaint",76,855,136,36);Place("Shortcut",0,900,320,20);
            var title=PalettePanel.transform.Find("Title");if(title)title.GetComponent<TMP_Text>().text="RENK & FIRÇA";
            var shortcut=PalettePanel.transform.Find("Shortcut");if(shortcut)shortcut.GetComponent<TMP_Text>().text="Ctrl / Cmd + Z  geri al    ·    Ctrl / Cmd + Y  yinele";
            // Reuse the former shape row for real surface operations.
            SetupStudioButton(LineButton,"GERİ AL",Undo);SetupStudioButton(RectangleButton,"DOLDUR",FillCurrent);SetupStudioButton(EllipseButton,"ÖNİZLE",()=>studio?.ResetView());
            if(Stabilization){Stabilization.gameObject.SetActive(false);var l=PalettePanel.transform.Find("StabilizationLabel");if(l)l.GetComponent<TMP_Text>().text="ÜSTÜNÜ VE KENARLARINI BOYA";}
            if(Game&&Game.Experience)Game.Experience.StylePaint(this);
        }
        void Place(string name,float x,float y,float width,float height){var r=PalettePanel.transform.Find(name) as RectTransform;if(!r)return;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        void SetupStudioButton(Button button,string label,UnityEngine.Events.UnityAction action){if(!button)return;button.gameObject.SetActive(true);button.GetComponentInChildren<TMP_Text>(true).text=label;button.onClick.RemoveAllListeners();button.onClick.AddListener(action);}
        void FillCurrent(){if(!editing||!Canvas(editing).Surface.SamplesReady)return;BeginStroke();Touch(Canvas(editing));Fill(editing,selected);EndStroke();}

        public void CloseEditor(){EndStroke();if(samplesPreparation!=null){StopCoroutine(samplesPreparation);samplesPreparation=null;cachedSurface?.ReleaseSamples();}tapeDragging=false;peeling=false;studio?.Dispose();studio=null;editing=null;if(EditorPanel)EditorPanel.SetActive(false);if(PalettePanel)PalettePanel.SetActive(false);ShowCursor(false);Cursor.visible=true;if(Active&&EditorHint)EditorHint.text="Klavyeden boyamak istediğin tuşa tıkla";}
        static string DisplayName(WorkshopItem item){var value=string.IsNullOrWhiteSpace(item.Label)?item.Id:item.Label;if(value.StartsWith("Tuş ",StringComparison.OrdinalIgnoreCase))value=value.Substring(4);return value.Replace("Space","BOŞLUK");}

        void ShowCursor(bool value){if(BrushCursor)BrushCursor.gameObject.SetActive(value&&Active);}

        void BuildColorPicker(){
            hueTexture=new Texture2D(256,1,TextureFormat.RGBA32,false){name="Paint hue spectrum",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var huePixels=new Color[256];for(int x=0;x<256;x++)huePixels[x]=Color.HSVToRGB(x/255f,1,1);hueTexture.SetPixels(huePixels);hueTexture.Apply(false);if(HueField)HueField.texture=hueTexture;
            colorFieldTexture=new Texture2D(128,128,TextureFormat.RGBA32,false){name="Paint color field",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};if(ColorField)ColorField.texture=colorFieldTexture;RefreshColorPicker();
        }
        void RefreshColorPicker(){
            if(!colorFieldTexture)return;var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++){var p=new Vector2(x/127f-.5f,y/127f-.5f)*2;float saturation=p.magnitude;var c=Color.HSVToRGB(Mathf.Repeat(Mathf.Atan2(p.y,p.x)/(2*Mathf.PI),1),Mathf.Min(1,saturation),selectedValue);c.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.97f,1,saturation));pixels[y*128+x]=c;}
            colorFieldTexture.SetPixels(pixels);colorFieldTexture.Apply(false);
            var values=new Color[256];for(int i=0;i<256;i++)values[i]=Color.HSVToRGB(selectedHue,1,i/255f);hueTexture.SetPixels(values);hueTexture.Apply(false);
            Color.RGBToHSV(selected,out _,out var s,out var value);if(ColorCursor){var r=ColorField.rectTransform.rect;float angle=selectedHue*Mathf.PI*2;ColorCursor.anchoredPosition=new Vector2(Mathf.Cos(angle)*s*r.width*.5f,Mathf.Sin(angle)*s*r.height*.5f);}if(HueCursor)HueCursor.anchoredPosition=new Vector2((value-.5f)*HueField.rectTransform.rect.width,0);
        }
        bool HandleColorPicker(Mouse mouse){
            if(!mouse.leftButton.isPressed)return false;var camera=ColorField.canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ColorField.canvas.worldCamera;
            if(ScreenUV(ColorField.rectTransform,mouse.position.ReadValue(),camera,out var uv)){var p=(uv-Vector2.one*.5f)*2;if(p.magnitude>1)return false;selectedHue=Mathf.Repeat(Mathf.Atan2(p.y,p.x)/(2*Mathf.PI),1);selected=Color.HSVToRGB(selectedHue,p.magnitude,selectedValue);if(CurrentColor)CurrentColor.color=selected;RefreshColorPicker();RefreshSelections();return true;}
            if(ScreenUV(HueField.rectTransform,mouse.position.ReadValue(),camera,out var value)){Color.RGBToHSV(selected,out _,out var saturation,out _);selectedValue=value.x;selected=Color.HSVToRGB(selectedHue,saturation,selectedValue);if(CurrentColor)CurrentColor.color=selected;RefreshColorPicker();RefreshSelections();return true;}return false;
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
            canvas.Modified=true;canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();
        }
        void RasterLine(CanvasState canvas,Vector2 from,Vector2 to,float size){int count=Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(from,to)/Mathf.Max(1,size*.24f)),1,256);for(int i=0;i<=count;i++)StampPixels(canvas,Vector2.Lerp(from,to,(float)i/count),size);}

        void Stamp(WorkshopItem item,Vector2 uv){
            var canvas=Canvas(item);Touch(canvas);var pixel=new Vector2(uv.x*(canvas.Width-1),uv.y*(canvas.Height-1));
            float brushPixels=radius*canvas.Height;
            int count=previousId==item.Id?Mathf.Clamp(Mathf.CeilToInt(Vector2.Distance(previousPixel,pixel)/Mathf.Max(1,brushPixels*.28f)),1,96):1;
            for(int i=1;i<=count;i++)StampPixels(canvas,previousId==item.Id?Vector2.Lerp(previousPixel,pixel,(float)i/count):pixel,brushPixels);
            previousId=item.Id;previousPixel=pixel;canvas.Modified=true;canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();
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

        void Touch(CanvasState canvas){if(touched.Add(canvas.Item.Id))stroke.Add(new Snapshot{Id=canvas.Item.Id,Pixels=(Color32[])canvas.Pixels.Clone(),Modified=canvas.Modified,LegendVisible=canvas.LegendVisible,Tape=canvas.Tape.ToArray()});}
        public void Undo(){EndStroke();if(undo.Count==0){Game.Menu.Toast("Geri alınacak boya yok.");return;}redo.Push(SwapSnapshots(undo.Pop()));Game.Menu.Toast("Son boya işlemi geri alındı.");}
        public void Redo(){EndStroke();if(redo.Count==0){Game.Menu.Toast("Yinelenecek boya yok.");return;}undo.Push(SwapSnapshots(redo.Pop()));Game.Menu.Toast("Boya işlemi yeniden uygulandı.");}
        List<Snapshot> SwapSnapshots(List<Snapshot> source){var reverse=new List<Snapshot>();foreach(var snapshot in source){if(!Game.Controller.Lookup.TryGetValue(snapshot.Id,out var item))continue;var canvas=Canvas(item);reverse.Add(new Snapshot{Id=snapshot.Id,Pixels=(Color32[])canvas.Pixels.Clone(),Modified=canvas.Modified,LegendVisible=canvas.LegendVisible,Tape=canvas.Tape.ToArray()});canvas.Pixels=(Color32[])snapshot.Pixels.Clone();canvas.Modified=snapshot.Modified;canvas.LegendVisible=snapshot.LegendVisible;if(editing==item)studio?.ClearWetness();canvas.Tape.Clear();if(snapshot.Tape!=null)canvas.Tape.AddRange(snapshot.Tape);ApplyMaterials(canvas);if(editing==item){RefreshLegend(canvas);SyncTape();RefreshTapeUI();}canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();}return reverse;}
        public void ClearCurrent(){if(!editing)return;BeginStroke();var canvas=Canvas(editing);Touch(canvas);canvas.Pixels=(Color32[])canvas.BasePixels.Clone();studio?.ClearWetness();canvas.Modified=false;canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();EndStroke();Game.Menu.Toast("Seçili tuş temizlendi.");}
        public void ToggleLegend(){if(!editing)return;BeginStroke();var canvas=Canvas(editing);Touch(canvas);canvas.LegendVisible=!canvas.LegendVisible;ApplyMaterials(canvas);RefreshLegend(canvas);EndStroke();Game.Menu.Toast(canvas.LegendVisible?"Tuş harfi gösteriliyor.":"Tuş harfi gizlendi.");}
        void RefreshLegend(CanvasState canvas){if(EditorLegend)EditorLegend.gameObject.SetActive(false);studio?.UpdateMaterials(canvas.Item.Visual.sharedMaterials);if(LegendButtonLabel)LegendButtonLabel.text=canvas.LegendVisible?"HARF  AÇIK":"HARF  KAPALI";}

        public void Fill(WorkshopItem item,Color color){var canvas=Canvas(item);if(canvas.Tape.Count>0){canvas.Surface.Visit(canvas.Surface.Bounds.center,canvas.Surface.Bounds.size.magnitude,t=>{if(!Masked(canvas,t.Position))canvas.Pixels[t.Index]=color;});}else for(int i=0;i<canvas.Pixels.Length;i++)canvas.Pixels[i]=color;canvas.Modified=true;canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();}
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
            int height=KeycapSurface.Height,width=KeycapSurface.Width;
            var originalMesh=item.Visual.GetComponent<MeshFilter>().sharedMesh;var surface=new KeycapSurface(originalMesh,body);item.Visual.GetComponent<MeshFilter>().sharedMesh=surface.Mesh;
            Color baseColor=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white;var variant=new MaterialPropertyBlock();item.Visual.GetPropertyBlock(variant,body);var variantColor=variant.GetColor("_BaseColor");if(variantColor.a>0)baseColor=variantColor;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Paint "+item.Id,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            var pixels=new Color32[width*height];Array.Fill(pixels,(Color32)baseColor);texture.SetPixels32(pixels);texture.Apply(false);
            var material=new Material(source){name=source.name+" · "+item.Id};material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",texture);
            canvas=new CanvasState{Item=item,OriginalMesh=originalMesh,Surface=surface,OriginalMaterials=originals,Material=material,Texture=texture,Pixels=pixels,BasePixels=(Color32[])pixels.Clone(),Body=body,Width=width,Height=height};canvases.Add(item.Id,canvas);ApplyMaterials(canvas);return canvas;
        }
        void ApplyMaterials(CanvasState canvas){var assigned=(Material[])canvas.OriginalMaterials.Clone();assigned[canvas.Body]=canvas.Material;if(!canvas.LegendVisible)for(int i=0;i<assigned.Length;i++)if(assigned[i]&&assigned[i].name.StartsWith("Paint_legend",StringComparison.OrdinalIgnoreCase))assigned[i]=canvas.Material;canvas.Item.Visual.SetPropertyBlock(null,canvas.Body);canvas.Item.Visual.sharedMaterials=assigned;
            for(int sub=0;sub<assigned.Length;sub++)if(sub!=canvas.Body)canvas.Surface.Mesh.SetTriangles(canvas.LegendVisible?canvas.OriginalMesh.GetTriangles(sub).Select((_,i)=>LegendIndex(canvas,sub,i)).ToArray():Array.Empty<int>(),sub);
        }
        static int LegendIndex(CanvasState canvas,int sub,int index){int start=0;for(int i=0;i<sub;i++)start+=canvas.OriginalMesh.GetTriangles(i).Length;return start+index;}

        public void ResetPaint(){CloseEditor();foreach(var canvas in canvases.Values)Dispose(canvas);canvases.Clear();cachedSurface=null;undo.Clear();redo.Clear();}
        void Dispose(CanvasState canvas){if(canvas.Item&&canvas.Item.Visual){canvas.Item.Visual.sharedMaterials=canvas.OriginalMaterials;canvas.Item.Visual.GetComponent<MeshFilter>().sharedMesh=canvas.OriginalMesh;}if(Application.isPlaying)Destroy(canvas.Surface.Mesh);else DestroyImmediate(canvas.Surface.Mesh);if(Application.isPlaying){Destroy(canvas.Material);Destroy(canvas.Texture);}else{DestroyImmediate(canvas.Material);DestroyImmediate(canvas.Texture);}}

        public void Restore(string[] ids,string[] textures,string[] legacyColors=null,bool[] legendVisibility=null,string[] tapeMasks=null){
            ResetPaint();if(ids==null)return;
            for(int i=0;i<ids.Length;i++)if(Game.Controller.Lookup.TryGetValue(ids[i],out var item)){
                if(textures!=null&&i<textures.Length&&!string.IsNullOrEmpty(textures[i])){try{var canvas=Canvas(item);var source=new Texture2D(2,2);source.LoadImage(Convert.FromBase64String(textures[i]));var restored=new Color32[canvas.Width*canvas.Height];for(int y=0;y<canvas.Height;y++)for(int x=0;x<canvas.Width;x++)restored[y*canvas.Width+x]=(source.width==KeycapSurface.Width&&source.height==KeycapSurface.Height?source.GetPixel(x,y):(x<KeycapSurface.Tile&&y<KeycapSurface.Tile?source.GetPixelBilinear(Mathf.Clamp01((x-4f)/(KeycapSurface.Tile-8)),Mathf.Clamp01((y-4f)/(KeycapSurface.Tile-8))):(Color)canvas.BasePixels[y*canvas.Width+x]));canvas.Pixels=restored;canvas.Modified=true;if(tapeMasks!=null&&i<tapeMasks.Length&&!string.IsNullOrEmpty(tapeMasks[i])){var tape=JsonUtility.FromJson<WorkshopTapeSave>(tapeMasks[i]);if(tape?.Strips!=null)canvas.Tape.AddRange(tape.Strips.Take(8));}canvas.LegendVisible=legendVisibility==null||i>=legendVisibility.Length||legendVisibility[i];ApplyMaterials(canvas);canvas.Surface.PadEdges(canvas.Pixels);canvas.Texture.SetPixels32(canvas.Pixels);canvas.Texture.Apply(false);studio?.Invalidate();if(Application.isPlaying)Destroy(source);else DestroyImmediate(source);}catch(Exception e){Debug.LogWarning("Paint restore failed for "+ids[i]+": "+e.Message);}}
                else if(legacyColors!=null&&i<legacyColors.Length&&ColorUtility.TryParseHtmlString(legacyColors[i],out var color))Fill(item,color);
            }
        }

        static Vector2 LocalUV(WorkshopItem item,Vector3 world){var local=item.Visual.transform.InverseTransformPoint(world);var bounds=item.Visual.GetComponent<MeshFilter>().sharedMesh.bounds;return new Vector2(Mathf.InverseLerp(bounds.min.x,bounds.max.x,local.x),Mathf.InverseLerp(bounds.min.z,bounds.max.z,local.z));}
        static int BodyMaterial(WorkshopItem item){var materials=item.Visual.sharedMaterials;for(int i=0;i<materials.Length;i++)if(materials[i]&&materials[i].name.StartsWith("Paint_key_",StringComparison.OrdinalIgnoreCase))return i;return 0;}
        static bool Close(Color a,Color b)=>Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<.02f;
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        void OnDestroy(){studio?.Dispose();studio=null;foreach(var canvas in canvases.Values)Dispose(canvas);canvases.Clear();if(colorFieldTexture)Destroy(colorFieldTexture);if(hueTexture)Destroy(hueTexture);Cursor.visible=true;}
    }
}
