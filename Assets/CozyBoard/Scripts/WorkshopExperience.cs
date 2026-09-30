using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CozyBoard {
    [DefaultExecutionOrder(-40)]
    public sealed class WorkshopExperience : MonoBehaviour {
        public Texture2D GuideArt,ToolIcons,PackingPaper,MenuArt;
        public TMP_FontAsset MenuTitleFont;
        public WorkshopGameMode Game;
        public bool MainVisible=>main&&main.activeSelf;
        public bool Inspecting{get;private set;}
        public bool Packing=>packStep>0;
        public bool DeliveryReady{get;private set;}
        public bool Blocking=>(Game&&Game.Shop&&Game.Shop.IsOpen)||(Game&&Game.StoryUI&&Game.StoryUI.Open)||MainVisible||Inspecting||Packing||confirmation&&confirmation.activeSelf;
        public int PackStep=>packStep;
        public int TutorialStep=>tutorialStep;
        GameObject main,confirmation,tutorial,packPanel,inspectPanel,homeButton,statusCard;
        TMP_Text packTitle;
        RectTransform hud,root,guideRect,highlight;
        TMP_FontAsset font;
        TMP_Text tooltip,tutorialText,tutorialTitle,packText;
        UnityEngine.UI.Button continueButton,packButton;
        Transform supplies,board;
        WorkshopCarton parcel;
        Vector3 boardPosition,boardScale,cameraTarget;
        Quaternion boardRotation;
        float cameraPitch,cameraYaw,cameraWidth;
        bool initialized,busy,inspected,painted;
        int packStep,tutorialStep=-1;
        float tutorialStarted;Vector2 packingPress;bool packingDragging;
        public void Initialize(WorkshopGameMode game){
            if(initialized)return;initialized=true;Game=game;font=Game.Objective.font;hud=Game.Menu.OrderPanel.transform.parent as RectTransform;
            root=WorkshopUI.Rect("CozyExperience",hud.parent,Vector2.zero,Vector2.zero,Vector2.zero);root.anchorMax=Vector2.one;root.offsetMax=Vector2.zero;
            tooltip=WorkshopUI.Text("Tool hint",root,font,"",18,new Vector2(.5f,0),new Vector2(0,22),new Vector2(680,36));tooltip.alignment=TextAlignmentOptions.Center;tooltip.color=WorkshopUI.Paper;tooltip.gameObject.SetActive(false);
            BuildMain();BuildStatus();BuildOrder();BuildTutorial();BuildPacking();BuildInspection();ReplaceSupplies();RefreshSupplyBoxes();
            Game.Menu.OrderPanel.AddComponent<WorkshopPanelFade>();Game.Menu.SettingsPanel.AddComponent<WorkshopPanelFade>();var toolbar=new[]{Game.Menu.PointerButton,Game.Menu.MoveButton,Game.Menu.RotateButton,Game.Menu.PaintButton};for(int i=0;i<toolbar.Length;i++){((RectTransform)toolbar[i].transform).sizeDelta=new Vector2(86,86);toolbar[i].gameObject.AddComponent<WorkshopToolbarSelection>().Setup(Game,i);}((RectTransform)Game.Menu.UndoButton.transform).sizeDelta=new Vector2(86,86);Game.Menu.PaintButton.GetComponent<UnityEngine.UI.RawImage>().uvRect=new Rect(.096f,.097f,.805f,.805f);
            var deskButtons=new[]{Game.Menu.PointerButton,Game.Menu.MoveButton,Game.Menu.RotateButton,Game.Menu.UndoButton,Game.Menu.PaintButton};for(int i=0;i<deskButtons.Length;i++)Position((RectTransform)deskButtons[i].transform,new Vector2(.5f,0),new Vector2((i-2)*100,20),new Vector2(86,86));
            var home=WorkshopUI.Button("Main menu",hud,font,"☰",Vector2.one,new Vector2(-325,-25),new Vector2(86,86),ShowMain);home.GetComponentInChildren<TMP_Text>().text="";home.GetComponent<UnityEngine.UI.Image>().enabled=false;home.GetComponent<UnityEngine.UI.Shadow>().enabled=false;var homeMark=WorkshopUI.Art("Painted atelier home",home.transform,Resources.Load<Texture2D>("UI/WorkshopHome"),Vector2.one*.5f,Vector2.zero,new Vector2(80,80));homeMark.uvRect=new Rect(.045f,.045f,.91f,.91f);homeMark.raycastTarget=true;home.targetGraphic=homeMark;var homeTip=home.gameObject.AddComponent<WorkshopTooltip>();homeTip.Experience=this;homeTip.Message="Ana menü";homeButton=home.gameObject;
            Game.Menu.RotateButton.onClick.RemoveAllListeners();Game.Menu.RotateButton.onClick.AddListener(BeginInspection);
            var tip=Game.Menu.RotateButton.gameObject.AddComponent<WorkshopTooltip>();tip.Experience=this;tip.Message="Klavyeyi kaldır ve 360° incele";
            if(Game.Testing)Game.Testing.Initialize(root,font);Game.Menu.RefreshOrder();
            if(Game.Tools)Game.Tools.Initialize(Game,root,font);if(Game.Shop)Game.Shop.Initialize(Game,root,font);Game.StoryUI=gameObject.GetComponent<WorkshopStoryUI>()??gameObject.AddComponent<WorkshopStoryUI>();Game.StoryUI.Initialize(Game,root,font);StylePaint(Game.Painter);StyleToolbar();WorkshopDeskDetails.Create(Game);Game.Refresh();ShowMain();
        }
        void BuildStatus(){
            var desk=GameObject.Find("FlatWorkspace2D");if(desk)desk.transform.localScale=new Vector3(32,1,24);
            var paper=WorkshopUI.Panel("Workshop progress paper",root,new Vector2(.5f,0),new Vector2(0,145),new Vector2(740,92),WorkshopUI.Paper);
            WorkshopAtelierStyle.Paper(paper,new Color(.94f,.88f,.73f,.78f),16).raycastTarget=false;statusCard=paper.gameObject;
            var layout=paper.gameObject.AddComponent<WorkshopStatusLayout>();layout.Game=Game;
            Game.Objective.transform.SetParent(paper.transform,false);Game.Objective.fontSize=20;Game.Objective.enableAutoSizing=true;Game.Objective.fontSizeMin=17;Game.Objective.fontSizeMax=20;
            Game.Progress.transform.SetParent(paper.transform,false);Game.Progress.fontSize=17;Game.Progress.color=new Color(.30f,.36f,.31f);
            Game.CompletionLabel.transform.SetParent(paper.transform,false);Game.CompletionLabel.fontSize=14;Game.CompletionLabel.enableAutoSizing=true;Game.CompletionLabel.fontSizeMin=12;Game.CompletionLabel.fontSizeMax=14;
            foreach(var text in new[]{Game.Objective,Game.Progress,Game.CompletionLabel}){text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;}
            var test=WorkshopUI.Button("Open keyboard test",paper.transform,font,"Tuşları kontrol et",new Vector2(1,.5f),new Vector2(-18,0),new Vector2(218,52),()=>Game.Testing.Begin());
            test.targetGraphic=WorkshopAtelierStyle.Paper(test.GetComponent<UnityEngine.UI.Image>(),WorkshopUI.Sage,12);
            test.GetComponent<UnityEngine.UI.Shadow>().enabled=false;
            var label=test.GetComponentInChildren<TMP_Text>();label.fontSize=18;label.rectTransform.sizeDelta=new Vector2(154,38);label.rectTransform.anchoredPosition=new Vector2(18,0);
            WorkshopAtelierStyle.Art("Keyboard check icon",test.transform,27,new Vector2(0,.5f),new Vector2(8,0),new Vector2(38,38));
        }
        void StyleToolbar(){
            var buttons=new[]{Game.Menu.SaveButton,Game.Menu.OrderButton,Game.Menu.SettingsButton,Game.Menu.ConfirmButton,Game.Menu.PointerButton,Game.Menu.MoveButton,Game.Menu.RotateButton,Game.Menu.UndoButton,Game.Menu.PaintButton};
            int[] indices={0,1,2,3,4,5,6,7,8};
            string[] tips={"Atölyeyi kaydet","Sipariş kartı","Ses ayarları","Siparişi tamamla","Parça yerleştir","Masada taşı","Klavyeyi incele","Son yerleştirmeyi geri al","Tuşları boya"};
            for(int i=0;i<buttons.Length;i++)WorkshopAtelierStyle.Icon(buttons[i],indices[i],tips[i],this);
            WorkshopAtelierStyle.Icon(homeButton.GetComponent<UnityEngine.UI.Button>(),9,"Ana menü",this);
            WorkshopAtelierStyle.Icon(Game.Menu.CloseOrder,26,"Sipariş kartını kapat",this);
            WorkshopAtelierStyle.Icon(Game.Menu.CloseSettings,26,"Ses ayarlarını kapat",this);
            var credit=WorkshopUI.Text("Music credit",Game.Menu.SettingsPanel.transform,font,"A Warm Fireplace · MouthlessGames\nCC BY 3.0 · OpenGameArt",13,new Vector2(.5f,0),new Vector2(0,15),new Vector2(440,40));credit.alignment=TextAlignmentOptions.Center;credit.color=WorkshopUI.Ink;
        }

        void BuildMain(){
            var background=WorkshopUI.Panel("MainMenu",root,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.08f,.12f,.10f));var r=background.rectTransform;r.anchorMax=Vector2.one;r.offsetMax=Vector2.zero;main=background.gameObject;
            var art=WorkshopUI.Art("Sunlit atelier",r,MenuArt,Vector2.one*.5f,Vector2.zero,new Vector2(1920,1080));var fit=art.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectRatio=MenuArt?(float)MenuArt.width/MenuArt.height:16f/9;fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            var shade=WorkshopUI.Rect("Readable left vignette",r,Vector2.zero,Vector2.zero,Vector2.zero);shade.anchorMax=Vector2.one;shade.offsetMax=Vector2.zero;shade.gameObject.AddComponent<WorkshopMenuShade>().raycastTarget=false;
            var card=WorkshopUI.Rect("Menu content",r,new Vector2(0,.5f),new Vector2(120,0),new Vector2(570,860));card.gameObject.AddComponent<WorkshopMenuEntrance>();
            var eyebrow=WorkshopUI.Text("Eyebrow",card,font,"KÜÇÜK BİR KLAVYE ATÖLYESİ",20,new Vector2(0,1),new Vector2(2,-58),new Vector2(530,35));eyebrow.color=new Color(.76f,.78f,.62f);eyebrow.characterSpacing=3;
            var title=WorkshopUI.Text("Cozy Board",card,MenuTitleFont?MenuTitleFont:font,"cozy\nboard",108,new Vector2(0,1),new Vector2(-4,-112),new Vector2(550,245));title.color=new Color(.98f,.94f,.80f);title.fontStyle=FontStyles.Normal;title.lineSpacing=-22;title.characterSpacing=-4;
            var note=WorkshopUI.Text("Menu note",card,font,"Bir tuş, bir renk,\nbiraz da kendinden.",28,new Vector2(0,1),new Vector2(3,-374),new Vector2(480,84));note.color=new Color(.86f,.87f,.75f);note.lineSpacing=5;
            continueButton=MenuButton("Continue",card,"Atölyeye dön",-502,Continue,true);
            MenuButton("New game",card,"Yeni bir başlangıç",-590,RequestNewGame,false);
            MenuButton("Tutorial",card,"Atölyeyi öğren",-661,()=>{Continue();StartTutorial();},false);
            MenuButton("Settings",card,"Ayarlar",-732,()=>{Game.Menu.SettingsPanel.SetActive(true);Game.Menu.SettingsPanel.transform.SetParent(root,false);Game.Menu.SettingsPanel.transform.SetAsLastSibling();},false);
            var quit=MenuButton("Quit",r,"Çıkış",0,()=>{Game.SaveProgress();Application.Quit();},false);Position((RectTransform)quit.transform,new Vector2(1,0),new Vector2(-65,32),new Vector2(135,54));quit.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta=new Vector2(95,45);
            var footer=WorkshopUI.Text("Quiet invitation",r,font,"YAVAŞLA.  TASARLA.  KENDİNDEN BİR PARÇA KAT.",17,new Vector2(0,0),new Vector2(125,35),new Vector2(900,28));footer.characterSpacing=2;footer.color=new Color(.84f,.86f,.74f,.75f);
            var confirm=WorkshopUI.Panel("New game confirmation",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,290),WorkshopUI.Paper);confirmation=confirm.gameObject;
            WorkshopUI.Text("Question",confirm.transform,font,"Yeni bir atölye açalım mı?\nMevcut kayıt yeni oyunla değişecek.",25,new Vector2(.5f,1),new Vector2(0,-37),new Vector2(540,110)).alignment=TextAlignmentOptions.Center;
            WorkshopUI.Button("Cancel",confirm.transform,font,"Vazgeç",new Vector2(.5f,0),new Vector2(-140,35),new Vector2(240,58),()=>confirmation.SetActive(false));WorkshopUI.Button("Start",confirm.transform,font,"Yeni oyun",new Vector2(.5f,0),new Vector2(140,35),new Vector2(240,58),NewGame);confirmation.SetActive(false);
        }
        UnityEngine.UI.Button MenuButton(string name,Transform parent,string label,float y,UnityEngine.Events.UnityAction action,bool primary){
            var button=WorkshopUI.Button(name,parent,font,label,new Vector2(0,1),new Vector2(0,y),new Vector2(430,primary?72:58),action);
            var image=button.GetComponent<UnityEngine.UI.Image>();image.color=primary?new Color(.83f,.87f,.71f):new Color(.7f,.8f,.65f,0);var shadow=button.GetComponent<UnityEngine.UI.Shadow>();shadow.enabled=false;
            var text=button.GetComponentInChildren<TMP_Text>();text.fontSize=primary?30:27;text.alignment=TextAlignmentOptions.MidlineLeft;text.color=primary?new Color(.14f,.22f,.17f):new Color(.94f,.93f,.82f);text.rectTransform.sizeDelta=new Vector2(350,52);text.rectTransform.anchoredPosition=new Vector2(-14,0);
            var arrow=WorkshopUI.Text("Arrow",button.transform,font,"→",30,new Vector2(1,.5f),new Vector2(-22,0),new Vector2(38,45));arrow.color=text.color;arrow.alignment=TextAlignmentOptions.Center;
            button.gameObject.AddComponent<WorkshopMenuHover>();return button;
        }
        public void ShowMain(){if(!initialized||(Game.Tools&&Game.Tools.Busy))return;if(Game.Testing)Game.Testing.End();if(Game.Tools)Game.Tools.Deselect();if(Game.Shop)Game.Shop.Close();EndInspection();CancelPacking();Game.Controller.CancelDrag();Game.Painter.CloseEditor();Game.Menu.ClosePanels();if(Game.StoryUI)Game.StoryUI.Hide();main.SetActive(true);main.transform.SetAsLastSibling();hud.gameObject.SetActive(false);Game.Objective.transform.parent.gameObject.SetActive(false);Game.Controller.HintLabel.gameObject.SetActive(false);if(tutorial)tutorial.SetActive(false);if(highlight)highlight.gameObject.SetActive(false);continueButton.interactable=Game.Installed>0||File.Exists(Game.SavePath);}
        public void Continue(){if(!initialized)return;main.SetActive(false);confirmation.SetActive(false);hud.gameObject.SetActive(true);Game.Objective.transform.parent.gameObject.SetActive(true);Game.Controller.HintLabel.gameObject.SetActive(true);if(tutorialStep>=0)ShowTutorialStep();if(Game.Story.enabled&&!Game.Story.openingRead&&Game.StoryUI)Game.StoryUI.OpenNotebook(true);else if(Game.WaitingForOrder&&Game.Shop)Game.Shop.OpenOrders();}
        public void RequestNewGame(){if(File.Exists(Game.SavePath)||Game.Installed>0){confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();}else NewGame();}
        public void NewGame(){confirmation.SetActive(false);tutorialStep=-1;if(tutorial)tutorial.SetActive(false);if(highlight)highlight.gameObject.SetActive(false);Game.StartStory();Game.SaveProgress();Continue();}
        void BuildOrder(){
            var panel=(RectTransform)Game.Menu.OrderPanel.transform;panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(1,.5f);panel.anchoredPosition=new Vector2(-28,0);panel.sizeDelta=new Vector2(510,780);panel.localScale=Vector3.one;
            var art=panel.GetComponent<UnityEngine.UI.RawImage>();if(art){art.texture=Texture2D.whiteTexture;art.color=new Color(.31f,.37f,.39f);}
            var paper=WorkshopUI.Panel("Plain request paper",panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(474,744),WorkshopUI.Paper);paper.raycastTarget=false;paper.transform.SetAsFirstSibling();
            WorkshopUI.Panel("Simple metal clip",panel,new Vector2(.5f,1),new Vector2(-60,-10),new Vector2(175,26),new Color(.57f,.61f,.59f)).raycastTarget=false;
            var photo=WorkshopUI.Panel("Photo paper",panel,Vector2.one,new Vector2(-32,-56),new Vector2(132,156),new Color(.98f,.95f,.85f));photo.rectTransform.localRotation=Quaternion.Euler(0,0,-6);photo.raycastTarget=false;
            var portrait=WorkshopUI.Art("Drawn customer",photo.transform,new Texture2D(1,1),new Vector2(.5f,.5f),new Vector2(0,5),new Vector2(115,126));Destroy(portrait.texture);portrait.texture=GuideArt;portrait.uvRect=new Rect(.12f,.42f,.70f,.58f);
            Position(Game.Menu.OrderName.rectTransform,new Vector2(0,1),new Vector2(36,-70),new Vector2(290,155));Game.Menu.OrderName.fontSize=28;
            Position(Game.Menu.OrderBody.rectTransform,new Vector2(0,1),new Vector2(36,-246),new Vector2(438,390));Game.Menu.OrderBody.fontSize=24;
            Position(Game.Menu.OrderAction.rectTransform,new Vector2(0,0),new Vector2(36,59),new Vector2(310,42));Game.Menu.OrderAction.fontSize=21;
            Position((RectTransform)Game.Menu.ConfirmButton.transform,new Vector2(1,0),new Vector2(-30,35),new Vector2(70,70));
            Position((RectTransform)Game.Menu.CloseOrder.transform,Vector2.one,new Vector2(-12,-12),new Vector2(46,46));
        }
        static void Position(RectTransform r,Vector2 anchor,Vector2 p,Vector2 size){r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=p;r.sizeDelta=size;}
        void ReplaceSupplies(){
            WorkshopCarton.PaperTexture=PackingPaper;WorkshopCarton.LabelFont=font;
            supplies=Game.Controller.transform.Find("SupplyPackages");if(!supplies)return;
            for(int stage=1;stage<=4;stage++){var box=supplies.Find("SupplyBox"+stage);if(!box)continue;if(stage==3)box.localPosition+=Vector3.forward*.8f;foreach(Transform child in box)child.gameObject.SetActive(false);var carton=WorkshopCarton.Create(box,"Open corrugated carton",stage<3?4.25f:2.85f,stage<3?1.68f:3.55f,.68f);carton.transform.localPosition=Vector3.zero;var motion=box.gameObject.AddComponent<WorkshopSupplyMotion>();motion.Initialize(Game);box.gameObject.SetActive(false);}
        }
        public void RefreshSupplyBoxes(){if(!supplies||!Game)return;for(int stage=1;stage<=4;stage++){var box=supplies.Find("SupplyBox"+stage);if(box){bool show=!Game.WaitingForOrder&&stage==Game.CurrentStage&&(!Game.Shop||Game.Shop.HasSupply(stage));var motion=box.GetComponent<WorkshopSupplyMotion>();if(motion)motion.Show(show);else box.gameObject.SetActive(show);}}}
        public bool SupplyArrived(int stage){if(!supplies||stage<1||stage>4)return true;var box=supplies.Find("SupplyBox"+stage);var motion=box?box.GetComponent<WorkshopSupplyMotion>():null;return !motion||motion.Arrived;}
        public void StylePaint(WorkshopKeyPainter painter){
            if(!initialized||!ToolIcons||!painter.PalettePanel)return;
            foreach(var swatch in painter.SwatchButtons)if(swatch)swatch.gameObject.SetActive(false);
            var palette=(RectTransform)painter.PalettePanel.transform;palette.sizeDelta=new Vector2(320,860);palette.anchoredPosition=new Vector2(38,30);
            var buttons=new[]{painter.RoundButton,painter.SquareButton,painter.AirbrushButton,painter.SpongeButton,painter.DryBrushButton,painter.SplatterButton,painter.EraserButton,painter.LineButton,painter.RectangleButton,painter.EllipseButton,painter.RedoButton,painter.ClearButton};
            int[] indices={12,13,14,15,16,17,18,7,19,20,10,11};string[] tips={"Detay fırçası","Yassı fırça","Yumuşak sprey","Sünger","Kuru fırça","Boya sıçrat","Silgi","Geri al · Ctrl / Cmd + Z","Tuşu doldur","Görünümü sıfırla · F","Yinele · Ctrl / Cmd + Y","Bu tuşun boyasını temizle"};
            for(int i=0;i<buttons.Length;i++){var r=(RectTransform)buttons[i].transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(-108+(i%4)*72,-330-(i/4)*76);r.sizeDelta=new Vector2(67,67);WorkshopAtelierStyle.Icon(buttons[i],indices[i],tips[i],this);}
            foreach(string name in new[]{"ToolLabel","ShapeLabel","SizeLabel","HardnessLabel","OpacityLabel","StabilizationLabel","BrushReadout","Shortcut"}){var child=palette.Find(name);if(child)child.gameObject.SetActive(false);}
            var sliders=new[]{painter.BrushSize,painter.Hardness,painter.Opacity};string[] sliderTips={"Fırça boyutu · Tekerlek","Kenar yumuşaklığı","Boya miktarı"};
            for(int i=0;i<3;i++){var r=(RectTransform)sliders[i].transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(24,-646-i*60);r.sizeDelta=new Vector2(164,24);var track=r.Find("Track") as RectTransform;if(track)track.sizeDelta=new Vector2(164,4);if(sliders[i].handleRect.parent is RectTransform handleArea)handleArea.sizeDelta=new Vector2(164,24);sliders[i].handleRect.sizeDelta=new Vector2(13,22);
                var icon=palette.Find("SliderIcon"+i);if(!icon){var button=WorkshopUI.Button("SliderIcon"+i,palette,font,"",new Vector2(.5f,1),new Vector2(-104,-624-i*60),new Vector2(45,45),()=>{});WorkshopAtelierStyle.Icon(button,21+i,sliderTips[i],this);}
            }
            var hint=palette.Find("PaintHint");if(!hint){var t=WorkshopUI.Text("PaintHint",palette,font,"Rengi seç. Yüzeye dokun.",17,new Vector2(.5f,0),new Vector2(0,34),new Vector2(288,35));t.alignment=TextAlignmentOptions.Center;t.color=WorkshopUI.Paper;}
        }
        public void ShowTooltip(string message){if(!tooltip)return;tooltip.text=message;tooltip.gameObject.SetActive(!string.IsNullOrEmpty(message));tooltip.transform.SetAsLastSibling();}
        void SavePose(){board=Game.Controller.Lookup["Case"].transform;boardPosition=board.position;boardRotation=board.rotation;boardScale=board.localScale;cameraPitch=Game.Controller.Pitch;cameraYaw=Game.Controller.Yaw;cameraWidth=Game.Controller.ViewWidth;cameraTarget=Game.Controller.ViewTarget;}
        void RestorePose(){if(board){board.SetPositionAndRotation(boardPosition,boardRotation);board.localScale=boardScale;}Game.Controller.Pitch=cameraPitch;Game.Controller.Yaw=cameraYaw;Game.Controller.ViewWidth=cameraWidth;Game.Controller.ViewTarget=cameraTarget;Game.Controller.UpdateCamera();}
        void BuildInspection(){var p=WorkshopUI.Panel("Inspection controls",root,new Vector2(.5f,0),new Vector2(0,30),new Vector2(900,92),WorkshopUI.Paper);inspectPanel=p.gameObject;WorkshopUI.Text("Orbit instructions",p.transform,font,"Sürükle: 360° döndür   ·   Tekerlek: yakınlaş",21,new Vector2(0,.5f),new Vector2(24,0),new Vector2(570,40));WorkshopUI.Button("Put keyboard down",p.transform,font,"Masaya bırak",new Vector2(1,.5f),new Vector2(-20,0),new Vector2(250,56),EndInspection);inspectPanel.SetActive(false);}
        public void BeginInspection(){if(!initialized||MainVisible||Packing||(Game.Tools&&Game.Tools.Busy))return;if(Game.Testing)Game.Testing.End();Game.Controller.CancelDrag();Game.Menu.ClosePanels();Game.Menu.SelectTool(0);SavePose();Inspecting=true;inspected=true;inspectPanel.SetActive(true);if(tutorial)tutorial.SetActive(false);board.position=new Vector3(0,1.8f,0);Game.Controller.Pitch=63;Game.Controller.ViewWidth=15;Game.Controller.UpdateCamera();ShowTooltip("Sol / sağ sürükle: 360° döndür   ·   Tekerlek: yakınlaş   ·   Esc: masaya bırak");}
        public void EndInspection(){if(!Inspecting)return;Inspecting=false;inspectPanel.SetActive(false);RestorePose();ShowTooltip(null);if(tutorialStep>=0)ShowTutorialStep();}
        void BuildPacking(){
            var p=WorkshopUI.Panel("Packing instructions",root,new Vector2(.5f,0),new Vector2(0,30),new Vector2(1000,164),WorkshopUI.Paper);packPanel=p.gameObject;
            var line=p.gameObject.AddComponent<UnityEngine.UI.Shadow>();line.effectDistance=new Vector2(5,-6);line.effectColor=new Color(.16f,.20f,.17f,.25f);
            packTitle=WorkshopUI.Text("Packing chapter",p.transform,font,"",27,new Vector2(0,1),new Vector2(28,-18),new Vector2(880,40));packTitle.color=WorkshopUI.Sage;
            packText=WorkshopUI.Text("Packing step",p.transform,font,"",23,new Vector2(0,1),new Vector2(28,-67),new Vector2(680,76));
            packButton=WorkshopUI.Button("Next packing step",p.transform,font,"",new Vector2(1,0),new Vector2(-22,22),new Vector2(245,58),AdvancePacking);
            WorkshopUI.Button("Cancel packing",p.transform,font,"×",Vector2.one,new Vector2(-12,-12),new Vector2(36,36),CancelPacking);packPanel.SetActive(false);
        }
        void SetPackingProps(bool visible){foreach(var item in Game.Controller.Items.Where(x=>x.Stage==0&&x.Id!="Case"))item.gameObject.SetActive(visible);var laptop=Game.Controller.transform.Find("Cozy workshop laptop");if(laptop)laptop.gameObject.SetActive(visible);var rest=Game.Controller.transform.Find("Linen tool rest");if(rest)rest.gameObject.SetActive(visible);var details=Game.Controller.transform.Find("Quiet desk details");if(details)details.gameObject.SetActive(visible);}
        public void BeginPacking(){if(!Game.Completed||Packing||MainVisible)return;if(Game.Testing&&!Game.Testing.Passed){Game.Testing.Begin();return;}if(Game.Tools&&Game.Tools.Tightened!=15){Game.Menu.Toast("Önce dört köşe vidasını sabitle.");return;}if(Game.Testing)Game.Testing.End();EndInspection();Game.Menu.SelectTool(0);Game.Menu.ClosePanels();SavePose();SetPackingProps(false);packStep=1;if(tutorial)tutorial.SetActive(false);if(highlight)highlight.gameObject.SetActive(false);DeliveryReady=false;parcel=WorkshopCarton.Create(Game.Controller.transform,"Shipping carton",Game.IsMacro?5f:9f,Game.IsMacro?3.8f:4.3f,1.38f,true);parcel.transform.position=new Vector3(0,0,0);if(supplies)supplies.gameObject.SetActive(false);board.position=new Vector3(-.9f,1.6f,-.5f);Game.Controller.Pitch=65;Game.Controller.ViewWidth=22;Game.Controller.ViewTarget=new Vector3(0,0,1.2f);Game.Controller.UpdateCamera();parcel.Address(Game.CurrentRequest.Name,Game.OrderNumber);packPanel.SetActive(true);packPanel.transform.SetAsLastSibling();RefreshPacking();}
        void RefreshPacking(){if(!packText)return;string[] text={"",Game.IsMacro?"Makro pad'i tut ve küçük kutunun içine sürükle.":"Klavyeyi tut ve açık kutunun içine sürükle.","Koruyucu kâğıdı kutunun bir yanından diğerine sürükle.","Karton kapağa dokun; katlayıp bantlayalım.","Hazır! Kutuyu yukarı doğru sürükleyerek kargoya ver."};string[] labels={"","Kutuya yerleştir","Kâğıdı katla","Kapat ve bantla","Paketi teslim et"};packTitle.text=$"ÖZENLE PAKETLE · {packStep} / 4   /   {Game.CurrentRequest.Name}";packText.text=text[packStep];packButton.GetComponentInChildren<TMP_Text>().text=labels[packStep];packButton.interactable=!busy;}
        public void AdvancePacking(){if(!Packing||busy)return;if(packStep==4){busy=true;root.gameObject.AddComponent<WorkshopSceneCurtain>().Run(root,()=>{DeliveryReady=true;Game.Deliver();DeliveryReady=false;});return;}StartCoroutine(PackAnimation(packStep));}
        IEnumerator PackAnimation(int step){busy=true;RefreshPacking();var start=board.position;var rotation=board.rotation;float time=0;Quaternion lidStart=parcel.Lid.localRotation;
            if(step==2){parcel.Wrap.SetActive(true);parcel.FoldPaper(0);}
            while(time<1){time+=Time.unscaledDeltaTime*1.4f;float t=Mathf.SmoothStep(0,1,time);
                if(step==1){board.position=Vector3.Lerp(start,new Vector3(0,.21f,0),t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.4f;board.rotation=Quaternion.Slerp(rotation,Quaternion.identity,t);}
                if(step==2)parcel.FoldPaper(t);
                if(step==3){parcel.Lid.localRotation=Quaternion.Slerp(lidStart,Quaternion.identity,Mathf.Clamp01(t*1.5f));parcel.Seal(Mathf.Clamp01((t-.65f)/.35f));}
                yield return null;
            }
            if(step==1){board.position=new Vector3(0,.21f,0);board.rotation=Quaternion.identity;}if(step==2)parcel.FoldPaper(1);if(step==3){parcel.Lid.localRotation=Quaternion.identity;parcel.Seal(1);}packStep++;busy=false;Game.Audio.Play(Game.Audio.Place,.35f);RefreshPacking();
        }
        public void CancelPacking(){if(!Packing)return;StopAllCoroutines();busy=false;packStep=0;SetPackingProps(true);RestorePose();if(parcel)Destroy(parcel.gameObject);parcel=null;if(supplies)supplies.gameObject.SetActive(true);if(packPanel)packPanel.SetActive(false);}
        public void Delivered(){if(tutorialStep==6)FinishTutorial();Game.Menu.ClosePanels();if(Game.WaitingForOrder&&Game.Shop)Game.Shop.OpenOrders();}
        void BuildTutorial(){
            var t=WorkshopUI.Rect("Illustrated tutorial",root,Vector2.zero,Vector2.zero,Vector2.zero);t.anchorMax=Vector2.one;t.offsetMax=Vector2.zero;tutorial=t.gameObject;
            guideRect=WorkshopUI.Art("Mina the guide",t,GuideArt,new Vector2(.5f,0),new Vector2(310,132),new Vector2(380,570)).rectTransform;
            var bubble=WorkshopUI.Panel("Tutorial paper",t,new Vector2(.5f,0),new Vector2(80,26),new Vector2(1080,195),WorkshopUI.Paper);
            var border=bubble.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectColor=new Color(.20f,.16f,.13f);border.effectDistance=new Vector2(3,-3);
            tutorialTitle=WorkshopUI.Text("Guide name",bubble.transform,font,"MİNA  /  ATÖLYE NOTU",25,new Vector2(0,1),new Vector2(26,-16),new Vector2(780,26));tutorialTitle.color=WorkshopUI.Sage;
            tutorialText=WorkshopUI.Text("Guide dialogue",bubble.transform,font,"",29,new Vector2(0,1),new Vector2(26,-57),new Vector2(840,126));
            WorkshopUI.Button("Next tutorial",bubble.transform,font,"→",new Vector2(1,.5f),new Vector2(-22,0),new Vector2(90,70),()=>SetTutorialStep(tutorialStep+1));
            WorkshopUI.Button("Skip tutorial",bubble.transform,font,"Atla",Vector2.one,new Vector2(-18,38),new Vector2(100,34),FinishTutorial);
            var frame=WorkshopUI.Panel("Tutorial focus",root,Vector2.zero,Vector2.zero,new Vector2(95,95),new Color(.98f,.88f,.52f,.12f));frame.raycastTarget=false;highlight=frame.rectTransform;var line=frame.gameObject.AddComponent<UnityEngine.UI.Outline>();line.effectColor=new Color(1,.87f,.51f,.9f);line.effectDistance=new Vector2(3,-3);highlight.gameObject.SetActive(false);tutorial.SetActive(false);
        }
        public void StartTutorial(){Continue();SetTutorialStep(0);}
        public void SetTutorialStep(int step){if(step>6){FinishTutorial();return;}tutorialStep=step;tutorialStarted=Time.unscaledTime;ShowTutorialStep();}
        void ShowTutorialStep(){if(tutorialStep<0||MainVisible)return;tutorial.SetActive(true);tutorial.transform.SetAsLastSibling();
            string[] lines={"Merhaba, ben Mina! İlk siparişin geldi. Sağ üstteki zarfı aç; Ece'nin nasıl bir klavye istediğine bakalım.","Önce PCB'yi, sonra plakayı kutudan alıp kasaya bırak. Parçalar doğru yere gelince küçük bir tıkla oturur.","Şimdi switch'ler. Kutudan al ve fareyi basılı tutarak yuvaların üzerinde gezdir. Her yuvaya bir tane!","Tuşları yerleştirelim. Geniş tuşlar kendi yuvalarına uyum sağlar. Takılı tuşlara basıp seslerini de deneyebilirsin.","Biraz renk katalım! Fırça ikonunu seç, sonra bir tuşa tıkla. Sağ sürüklemeyle döndür; kenarlarını da boya.","Nasıl olmuş? Döndürme ikonuyla klavyeyi kaldır. Sürükleyerek altına, yanlarına, her açısına bak. Esc ile masaya bırak.","Önce tuş kontrolünü tamamla; yanıt vermeyen switch’i söküp yeniden tak. Dört vidayı sabitle, sonra sipariş kartından paketlemeyi başlat; klavyeyi kutuya koy, kâğıdı katla, kapağı kapat ve gönder."};tutorialText.text=lines[tutorialStep];tutorialTitle.text=$"MİNA  /  {tutorialStep+1} · 7";
            RectTransform target=tutorialStep is 0 or 6?(RectTransform)Game.Menu.OrderButton.transform:tutorialStep==4?(RectTransform)Game.Menu.PaintButton.transform:tutorialStep==5?(RectTransform)Game.Menu.RotateButton.transform:null;
            highlight.gameObject.SetActive(target);if(target){highlight.anchorMin=target.anchorMin;highlight.anchorMax=target.anchorMax;highlight.pivot=target.pivot;highlight.anchoredPosition=target.anchoredPosition;highlight.sizeDelta=target.sizeDelta+Vector2.one*12;highlight.SetAsLastSibling();}
        }
        public void FinishTutorial(){tutorialStep=-1;if(tutorial)tutorial.SetActive(false);if(highlight)highlight.gameObject.SetActive(false);PlayerPrefs.SetInt("CozyBoard.TutorialComplete",1);PlayerPrefs.Save();}
        void PackingInput(Mouse mouse){
            Vector2 point=mouse.position.ReadValue();var ray=Game.Controller.ViewCamera.ScreenPointToRay(point);var plane=new Plane(Vector3.up,new Vector3(0,.4f,0));if(!plane.Raycast(ray,out float distance))return;var hit=ray.GetPoint(distance);bool box=Mathf.Abs(hit.x)<4.6f&&Mathf.Abs(hit.z)<2.4f;
            if(mouse.leftButton.wasPressedThisFrame&&!(UnityEngine.EventSystems.EventSystem.current&&UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())){packingPress=point;packingDragging=packStep==1?Vector2.Distance(point,Game.Controller.ViewCamera.WorldToScreenPoint(board.position))<400:box;if(packStep==3&&Mathf.Abs(hit.x)<4.6f&&hit.z>1.4f&&hit.z<6.5f){AdvancePacking();packingDragging=false;}}
            if(packingDragging&&mouse.leftButton.isPressed&&packStep==1)board.position=new Vector3(Mathf.Clamp(hit.x,-4,4),1.6f,Mathf.Clamp(hit.z,-3,3));
            if(packingDragging&&mouse.leftButton.wasReleasedThisFrame){packingDragging=false;var delta=point-packingPress;if(packStep==1&&box&&delta.magnitude>25||packStep==2&&box&&Mathf.Abs(delta.x)>100||packStep==4&&delta.y>100)AdvancePacking();}
        }
        void Update(){if(!initialized)return;foreach(var button in new[]{Game.Menu.PointerButton,Game.Menu.MoveButton,Game.Menu.RotateButton,Game.Menu.PaintButton,Game.Menu.UndoButton})button.gameObject.SetActive(!Packing&&(!Game.Testing||!Game.Testing.Active));if(statusCard){statusCard.SetActive(!MainVisible&&!Packing&&!Inspecting&&(!Game.Testing||!Game.Testing.Active)&&!Game.Painter.Editing&&(!Game.Shop||!Game.Shop.IsOpen));statusCard.transform.Find("Open keyboard test").gameObject.SetActive(Game.Completed&&(!Game.Testing||!Game.Testing.Active));}if(Game.Controller.HintLabel)Game.Controller.HintLabel.gameObject.SetActive(false);if(homeButton)homeButton.SetActive(!Game.Painter.Editing&&!Packing&&!Inspecting);var keyboard=Keyboard.current;var mouse=Mouse.current;
            if(Packing&&!busy&&mouse!=null)PackingInput(mouse);
            if(Inspecting&&mouse!=null){if(mouse.leftButton.isPressed||mouse.rightButton.isPressed){var d=mouse.delta.ReadValue();board.rotation=Quaternion.AngleAxis(-d.x*.32f,Game.Controller.ViewCamera.transform.up)*Quaternion.AngleAxis(d.y*.32f,Game.Controller.ViewCamera.transform.right)*board.rotation;}Game.Controller.ViewWidth=Mathf.Clamp(Game.Controller.ViewWidth*Mathf.Exp(-WorkshopAtelierStyle.WheelSteps(mouse.scroll.ReadValue().y)*.12f),10,22);Game.Controller.UpdateCamera();}
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame){if(Game.StoryUI&&Game.StoryUI.Open){Game.StoryUI.CloseNotebook();return;}if(Inspecting)EndInspection();else if(Packing)CancelPacking();else if(!MainVisible&&(!Game.Tools||!Game.Tools.Active)&&(!Game.Shop||!Game.Shop.IsOpen)&&!Game.Menu.OrderPanel.activeSelf&&!Game.Menu.SettingsPanel.activeSelf&&!Game.Painter.Editing&&Game.Menu.ToolMode==0)ShowMain();}
            if(tutorialStep>=0&&!MainVisible&&(!Game.Shop||!Game.Shop.IsOpen)&&Time.unscaledTime-tutorialStarted>1.5f){bool done=tutorialStep switch{0=>Game.Menu.OrderPanel.activeSelf,1=>Game.Installed>=2,2=>Game.CurrentStage>=4,3=>Game.Completed,4=>Game.Painter.PaintIds.Length>0,5=>inspected&&!Inspecting,_=>false};if(done)SetTutorialStep(tutorialStep+1);}
        }
    }
}
