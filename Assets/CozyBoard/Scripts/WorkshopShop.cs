using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace CozyBoard {
 [DefaultExecutionOrder(-35)]
 public sealed partial class WorkshopShop:MonoBehaviour {
  [Serializable] public class ShopData {
   public int mouseKits,mouseColor;public int laptopLayoutVersion;public int credits=320;public bool laptopPlaced;public Vector3 laptopPosition;
   public int[] stock={1,0,0,1,0,0,1,0,0};
   public int[] selected={0,3,6};
   public bool[] used=new bool[3];
   public int[] looseParts=new int[9];
  }
  public WorkshopGameMode Game;
  public Texture2D CoinArt;
  public ShopData Data=new();
  public bool IsOpen=>panel&&panel.activeSelf;
  public bool Ready{get;private set;}
  public bool IsMoving{get;private set;}Vector3 laptopStart,laptopOffset;bool laptopBlocked;
  readonly string[] names={"Ada PCB","Gece PCB","Mercan PCB","Bulut · Lineer","Yaprak · Taktil","Çıtır · Clicky","Krem PBT","Adaçayı PBT","Lavanta PBT"};
  readonly string[] details={"60% · Hot-swap\nKlasik yeşil devre","60% · Hot-swap\nGece mavisi devre","60% · Hot-swap\nMercan renkli devre","61 switch · 45 g\nYumuşak, tok ve sakin","61 switch · 55 g\nTok ses · Belirgin basış","61 switch · 50 g\nParlak, net bir tık","61 tuş · Mat yüzey\nSıcak krem tonları","61 tuş · Mat yüzey\nYumuşak yeşil tonları","61 tuş · Mat yüzey\nPastel mor tonları"};
  readonly int[] prices={40,55,55,60,75,85,45,55,55};
  readonly Color[] colors={new(.25f,.45f,.34f),new(.20f,.30f,.47f),new(.68f,.35f,.27f),new(.78f,.69f,.50f),new(.43f,.59f,.39f),new(.36f,.57f,.72f),new(.91f,.84f,.66f),new(.59f,.69f,.51f),new(.68f,.59f,.76f)};
  GameObject panel,laptop;Collider laptopHit;TMP_Text balance,notice,wallet;TMP_Text[] stockLabels=new TMP_Text[3];UnityEngine.UI.Button[] buy=new UnityEngine.UI.Button[3],use=new UnityEngine.UI.Button[3];int category;bool hovered;TMP_FontAsset font;
  readonly List<UnityEngine.Object> owned=new();
  int Group(int stage)=>stage<3?0:stage==3?1:2;
  int RequiredParts(int group)=>Game.IsRepair?(group==1?1:0):Game.IsMacro&&group>0?6:61;
  public bool HasSupply(int stage){if(Game.IsMouse)return Game.MouseProduct.State.kitReserved||Data.mouseKits>0;if(stage<1||stage>4)return true;int c=Group(stage),id=Data.selected[c];return Data.used[c]||Data.stock[id]>0||c>0&&Data.looseParts[id]>=RequiredParts(c);}
  public void Consume(int stage){if(Game.IsMouse){if(!Game.MouseProduct.State.kitReserved){Data.mouseKits=Mathf.Max(0,Data.mouseKits-1);Game.MouseProduct.State.kitReserved=true;}return;}int c=Group(stage);if(Data.used[c])return;int id=Data.selected[c];if(c>0&&(Game.IsMacro||Game.IsRepair||Data.stock[id]==0)){int needed=RequiredParts(c);if(Data.looseParts[id]<needed&&Data.stock[id]>0){Data.stock[id]--;Data.looseParts[id]+=61;}Data.looseParts[id]-=needed;}else if(Data.stock[id]>0)Data.stock[id]--;Data.used[c]=true;}
  public void NextOrder(){Data.used=new bool[3];}
  public void ResetShop(){Data=new ShopData();ApplyVariants();}
  public void Restore(ShopData data){Data=data??new ShopData();Data.mouseKits=Mathf.Max(0,Data.mouseKits);Data.mouseColor=Mathf.Clamp(Data.mouseColor,0,2);if(Data.looseParts==null||Data.looseParts.Length!=9)Data.looseParts=new int[9];if(Data.stock==null||Data.stock.Length!=9||Data.selected==null||Data.selected.Length!=3||Data.used==null||Data.used.Length!=3)Data=new ShopData();for(int c=0;c<3;c++)Data.selected[c]=Mathf.Clamp(Data.selected[c],c*3,c*3+2);ApplyVariants();if(laptop)RestoreLaptopPosition();}
  public void Reward(int amount){Data.credits+=amount;}
  public void Initialize(WorkshopGameMode game,RectTransform ui,TMP_FontAsset font){if(Ready)return;Game=game;Ready=true;this.font=font;BuildLaptop(font);BuildOpenLaptop();BuildUI(ui,font);BuildMouseKitUI();BuildMailUI(ui);BuildOrdersUI();BuildWallet(game.Menu.OrderPanel.transform.parent,font);ApplyVariants();}
  Material Mat(string name,Color color){var m=new Material(Shader.Find("CozyBoard/Painted")){name=name};m.SetColor("_BaseColor",color);owned.Add(m);return m;}
  Transform Part(string name,Transform parent,Vector3 pos,Vector3 size,Material material){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;}
  Transform Shell(string name,Vector3 position,Vector3 size,Material material,float radius=.16f){var go=new GameObject(name);go.transform.SetParent(laptop.transform,false);go.transform.localPosition=position;var mesh=WorkshopPropMesh.RoundedBox(size,radius);owned.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;var shadow=new GameObject(name+" shadow");shadow.transform.SetParent(go.transform,false);shadow.AddComponent<MeshFilter>().sharedMesh=mesh;shadow.AddComponent<MeshRenderer>().sharedMaterial=Game.Controller.ShadowMaterial;return go.transform;}
  void BuildLaptop(TMP_FontAsset font){
   laptop=new GameObject("Cozy workshop laptop");laptop.transform.SetParent(Game.Controller.transform,false);laptop.transform.localPosition=LaptopHome;laptop.transform.localRotation=Quaternion.Euler(0,-9,0);laptop.transform.localScale=new Vector3(1.32f,1.15f,1.32f);
   var metal=Mat("Satin grey aluminium",new Color(.58f,.60f,.61f));var seam=Mat("Recessed graphite seam",new Color(.17f,.23f,.21f));var edge=Mat("Rolled aluminium highlight",new Color(.77f,.79f,.80f));var brass=Mat("Embossed cream brass logo",new Color(.83f,.72f,.46f));
   Shell("Rounded lower unibody",new Vector3(0,.11f,0),new Vector3(3.38f,.17f,2.28f),metal,.21f);
   Shell("Continuous lid seam",new Vector3(0,.202f,0),new Vector3(3.34f,.022f,2.25f),seam,.20f);
   Shell("Rolled lid edge",new Vector3(0,.232f,0),new Vector3(3.39f,.044f,2.29f),edge,.22f);
   Shell("Closed sculpted lid",new Vector3(0,.276f,0),new Vector3(3.34f,.075f,2.24f),metal,.21f);
   Shell("Front opening recess",new Vector3(0,.207f,-1.128f),new Vector3(.66f,.036f,.035f),seam,.016f);
   Shell("Rear hinge",new Vector3(0,.18f,1.126f),new Vector3(2.45f,.085f,.08f),seam,.035f);
   for(int i=0;i<2;i++)Shell("USB C port",new Vector3(-1.686f,.107f,.45f-i*.4f),new Vector3(.018f,.044f,.19f),seam,.018f);
   Shell("Charging light",new Vector3(-1.695f,.115f,-.18f),new Vector3(.02f,.018f,.035f),brass,.008f);
   var logo=Shell("Embossed keycap maker mark",new Vector3(0,.322f,.06f),new Vector3(.47f,.024f,.43f),brass,.085f);logo.localRotation=Quaternion.Euler(0,-12,0);
   Shell("Logo inset key",new Vector3(0,.337f,.06f),new Vector3(.30f,.011f,.27f),metal,.055f);
   Shell("Logo dot",new Vector3(0,.346f,.06f),new Vector3(.06f,.012f,.06f),brass,.028f);
   var collider=laptop.AddComponent<BoxCollider>();collider.center=new Vector3(0,.18f,0);collider.size=new Vector3(3.42f,.36f,2.32f);laptopHit=collider;RestoreLaptopPosition();
  }
  void BuildUI(RectTransform root,TMP_FontAsset font){
   var overlay=WorkshopUI.Panel("Laptop shop",root,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.10f,.16f,.14f,.82f));overlay.rectTransform.anchorMax=Vector2.one;overlay.rectTransform.offsetMax=Vector2.zero;panel=overlay.gameObject;
   var screen=WorkshopUI.Panel("Laptop catalogue",overlay.transform,Vector2.one*.5f,Vector2.zero,new Vector2(1390,870),WorkshopUI.Paper);var border=screen.gameObject.AddComponent<UnityEngine.UI.Outline>();border.effectDistance=new Vector2(6,-6);border.effectColor=WorkshopUI.Sage;
   WorkshopUI.Text("Store title",screen.transform,font,"ATÖLYE PAZARI",48,new Vector2(0,1),new Vector2(45,-28),new Vector2(750,65));
   WorkshopUI.Text("Store subtitle",screen.transform,font,"Yeni parçalar, yeni sesler. Kendi klavyeni kur.",26,new Vector2(0,1),new Vector2(47,-104),new Vector2(900,45));
   balance=WorkshopUI.Text("Credits",screen.transform,font,"",28,Vector2.one,new Vector2(-110,-44),new Vector2(350,50));balance.alignment=TextAlignmentOptions.Right;WorkshopUI.Art("Tık coin",screen.transform,CoinArt,Vector2.one,new Vector2(-440,-35),new Vector2(55,55));
   WorkshopUI.Button("Close laptop",screen.transform,font,"×",Vector2.one,new Vector2(-22,-22),new Vector2(60,60),Close);
   string[] tabs={"PCB + Plaka","Switch setleri","Tuş setleri","Mouse kiti"};for(int i=0;i<4;i++){int c=i;WorkshopUI.Button("Category "+i,screen.transform,font,tabs[i],new Vector2(0,1),new Vector2(45+i*326,-180),new Vector2(305,57),()=>{category=c;Refresh();});}
   for(int i=0;i<3;i++){
    int slot=i;var card=WorkshopUI.Panel("Product "+i,screen.transform,new Vector2(0,1),new Vector2(45+i*435,-260),new Vector2(410,430),new Color(.86f,.82f,.69f));
    var art=WorkshopUI.Panel("Product swatch",card.transform,new Vector2(.5f,1),new Vector2(0,-24),new Vector2(340,88),colors[i]);
    WorkshopUI.Text("Product name",card.transform,font,"",30,new Vector2(0,1),new Vector2(22,-126),new Vector2(365,48));
    WorkshopUI.Text("Product details",card.transform,font,"",25,new Vector2(0,1),new Vector2(22,-180),new Vector2(365,88));
    stockLabels[i]=WorkshopUI.Text("Stock",card.transform,font,"",22,new Vector2(0,1),new Vector2(22,-272),new Vector2(365,30));
    buy[i]=WorkshopUI.Button("Buy",card.transform,font,"",new Vector2(0,0),new Vector2(18,24),new Vector2(178,60),()=>Purchase(category*3+slot));var priceLabel=buy[i].GetComponentInChildren<TMP_Text>();priceLabel.fontSize=21;priceLabel.rectTransform.sizeDelta=new Vector2(130,48);priceLabel.rectTransform.anchoredPosition=new Vector2(18,0);WorkshopAtelierStyle.Art("Price coin",buy[i].transform,28,new Vector2(0,.5f),new Vector2(5,0),new Vector2(30,30));
    use[i]=WorkshopUI.Button("Use",card.transform,font,"",new Vector2(1,0),new Vector2(-18,24),new Vector2(178,60),()=>Select(category*3+slot));
    WorkshopUI.Button("Return unopened kit",card.transform,font,"Kullanılmamış seti iade et",new Vector2(.5f,1),new Vector2(0,-308),new Vector2(350,32),()=>Refund(category*3+slot));
    DrawProduct(art.rectTransform,i,font);
   }
   notice=WorkshopUI.Text("Shop message",screen.transform,font,"",26,new Vector2(0,0),new Vector2(45,74),new Vector2(1290,64));
   WorkshopUI.Text("Workshop economy",screen.transform,font,"Setler stokta kalır. Teslimat: 240 Tık + 0–60 Tık teşekkür. PCB setine plaka dahildir.",22,new Vector2(0,0),new Vector2(45,24),new Vector2(1290,40));panel.SetActive(false);
  }
  void DrawProduct(RectTransform root,int id,TMP_FontAsset font){
   foreach(Transform child in root){child.gameObject.SetActive(false);Destroy(child.gameObject);}
   if(id<3){var board=WorkshopUI.Panel("PCB illustration",root,Vector2.one*.5f,new Vector2(-35,0),new Vector2(210,62),colors[id]);board.raycastTarget=false;for(int row=0;row<3;row++)for(int col=0;col<9;col++){var pad=WorkshopUI.Panel("Solder pad",board.transform,Vector2.one*.5f,new Vector2(-88+col*22,-20+row*20),new Vector2(6,6),new Color(.84f,.72f,.4f));pad.raycastTarget=false;}}
   else if(id<6){var body=WorkshopUI.Panel("Switch housing",root,Vector2.one*.5f,new Vector2(-75,0),new Vector2(65,60),WorkshopUI.Ink);body.rectTransform.localRotation=Quaternion.Euler(0,0,-12);WorkshopUI.Panel("Colored stem",body.transform,Vector2.one*.5f,Vector2.zero,new Vector2(30,31),colors[id]).raycastTarget=false;WorkshopUI.Panel("Stem cross x",body.transform,Vector2.one*.5f,Vector2.zero,new Vector2(22,7),WorkshopUI.Paper).raycastTarget=false;WorkshopUI.Panel("Stem cross y",body.transform,Vector2.one*.5f,Vector2.zero,new Vector2(7,22),WorkshopUI.Paper).raycastTarget=false;WorkshopUI.Button("Listen to switch",root,font,"Sesi dinle",Vector2.one*.5f,new Vector2(68,0),new Vector2(145,44),()=>{int previous=Game.Audio.SwitchVoice;Game.Audio.SwitchVoice=id-3;Game.Audio.Key(.65f);Game.Audio.SwitchVoice=previous;});}
   else{for(int i=0;i<3;i++){var key=WorkshopUI.Panel("Keycap illustration",root,Vector2.one*.5f,new Vector2(-83+i*82,0),new Vector2(68,62),colors[id]);key.rectTransform.localRotation=Quaternion.Euler(0,0,i*5-5);var shadow=key.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectDistance=new Vector2(3,-4);WorkshopUI.Text("Legend",key.transform,font,new[]{"A","S","D"}[i],30,Vector2.one*.5f,Vector2.zero,new Vector2(50,46)).alignment=TextAlignmentOptions.Center;}}
  }
  void BuildWallet(Transform root,TMP_FontAsset font){
   var p=WorkshopUI.Panel("Tık wallet",root,new Vector2(0,1),new Vector2(30,-294),new Vector2(208,76),new Color(.25f,.35f,.30f));
   WorkshopAtelierStyle.Paper(p,new Color(.25f,.35f,.30f),18).raycastTarget=false;
   WorkshopAtelierStyle.Art("Wallet coin",p.transform,28,new Vector2(0,.5f),new Vector2(10,0),new Vector2(52,52));
   var label=WorkshopUI.Text("Wallet label",p.transform,font,"ATÖLYE KASASI",12,new Vector2(0,1),new Vector2(77,-10),new Vector2(123,20));label.color=new Color(.78f,.81f,.65f);label.characterSpacing=1;
   wallet=WorkshopUI.Text("Wallet amount",p.transform,font,"",29,new Vector2(0,0),new Vector2(77,10),new Vector2(123,40));wallet.color=WorkshopUI.Paper;wallet.alignment=TextAlignmentOptions.Left;
  }
  public void Open(){if(!Ready||Game.ScreenChangeBlocked||Game.Experience.MainVisible||(Game.StoryUI&&Game.StoryUI.Open))return;EndLaptopMove(true);if(Game.MouseProduct)Game.MouseProduct.CloseUnderbody();if(Game.Tools)Game.Tools.CloseKeyboardUnderbody();if(Game.Testing)Game.Testing.End();Game.Experience.EndInspection();if(Game.Tools)Game.Tools.Deselect();Game.Controller.CancelDrag();Game.Painter.CloseEditor();Game.Menu.SelectTool(0);Game.Menu.ClosePanels();panel.SetActive(true);panel.transform.SetAsLastSibling();category=Game.IsMouse?3:Game.IsRepair?1:Game.CurrentStage<3?0:Game.CurrentStage==3?1:2;Refresh();if(Game.WaitingForOrder)SelectOrders();else SelectMailbox(UnreadMail>0);}
  public void Close(){if(panel)panel.SetActive(false);}
  bool Locked(int group)=>Data.used[group]||!(Game.IsRepair&&group==1)&&Game.ProductItems.Any(x=>x.Fitted&&x.Stage>0&&Group(x.Stage)==group);
  int PurchasePrice(int id)=>Game.IsRepair&&id>=3&&id<6?(id==3?8:id==4?10:12):prices[id];
  public bool Purchase(int id){if(id<0||id>=9||Data.credits<PurchasePrice(id)){if(notice)notice.text="Bu set için yeterli Tık’ın yok.";return false;}Data.credits-=PurchasePrice(id);if(Game.IsRepair&&id>=3&&id<6)Data.looseParts[id]++;else Data.stock[id]++;if(!Locked(id/3))Data.selected[id/3]=id;ApplyVariants();Game.PresentStock();Game.Refresh();Game.SaveProgress();Refresh();if(notice)notice.text=names[id]+" stoklarına geldi. Montaja hazırsın.";Game.Audio.Play(Game.Audio.Place,.3f);return true;}
  public bool Refund(int id){if(id<0||id>=9||Data.stock[id]<1)return false;Data.stock[id]--;Data.credits+=prices[id];Game.PresentStock();Game.Refresh();Game.SaveProgress();Refresh();if(notice)notice.text=names[id]+" iade edildi. +"+prices[id]+" Tık";return true;}
  public bool Select(int id){if(id<0||id>=9||Locked(id/3)||Data.stock[id]==0&&Data.looseParts[id]<RequiredParts(id/3))return false;Data.selected[id/3]=id;ApplyVariants();Game.PresentStock();Game.Refresh();Game.SaveProgress();Refresh();return true;}
  void Refresh(){if(!panel)return;balance.text=Data.credits+" Tık";var root=panel.transform.Find("Laptop catalogue");if(RefreshMouseKit(root))return;for(int i=0;i<3;i++){int id=category*3+i;var card=root.Find("Product "+i);var product=card.Find("Product swatch").GetComponent<UnityEngine.UI.Image>();product.color=new Color(.91f,.87f,.75f);DrawProduct(product.rectTransform,id,font);card.Find("Product name").GetComponent<TMP_Text>().text=names[id];var description=card.Find("Product details").GetComponent<TMP_Text>();description.fontSize=category==1?20:25;description.text=(Game.IsRepair&&category==1?"Tek yedek switch · "+PurchasePrice(id)+" Tık":Game.IsMacro&&category==0?"6 tuşlu makro pad\nPCB + plaka kiti":details[id])+(category==1&&!Game.IsRepair?"\n"+Game.CurrentFit(id):"");card.Find("Return unopened kit").GetComponent<UnityEngine.UI.Button>().interactable=Data.stock[id]>0;stockLabels[i].text=Data.stock[id]+" set"+(id>=3?" · "+Data.looseParts[id]+" parça":"")+(Data.selected[category]==id?"  ·  Seçili":"");buy[i].GetComponentInChildren<TMP_Text>().text=PurchasePrice(id)+" Tık · Al";buy[i].interactable=Data.credits>=PurchasePrice(id);use[i].GetComponentInChildren<TMP_Text>().text=Data.selected[category]==id?"Seçili":"Kullan";use[i].interactable=!Locked(category)&&(Data.stock[id]>0||Data.looseParts[id]>=RequiredParts(category))&&Data.selected[category]!=id;}notice.text=Game.IsRepair&&category==1?"Tamir için tek bir yedek switch al. Diğer parçalar müşterinin klavyesinde kalır.":Locked(category)?"Bu siparişte bu parçalar takıldı. Aldığın setleri sonraki klavyede kullanabilirsin.":category==1?Game.CurrentRequest.Name+" için: "+Game.CurrentRequest.Sound+", "+Game.CurrentRequest.Feel+".":"Bir set satın al veya stoktaki setini seç. Renk ve desen seçimi serbest.";}
  public void ApplyVariants(){if(!Game||Game.Controller.Items==null)return;if(Game.MouseProduct)Game.MouseProduct.RefreshPalette();var block=new MaterialPropertyBlock();foreach(var item in Game.Controller.Items){if(item.Id.StartsWith("Mouse_")||item.Stage is not (1 or 3 or 4)||Game.IsRepair&&(item.Stage!=3||item.Id!=Game.Repair?.switchId))continue;int group=Group(item.Stage);int id=Data.selected[group];if(group==2&&Game.Painter&&Game.Painter.PaintIds.Contains(item.Id))continue;item.Visual.GetPropertyBlock(block,0);block.SetColor("_BaseColor",colors[id]);item.Visual.SetPropertyBlock(block,0);block.Clear();}Game.Audio.SwitchVoice=Data.selected[1]-3;}
  void Update(){UpdateDeskLaptop();if(!Ready)return;UpdateMailNotification();if(wallet){wallet.text=Data.credits+" Tık";wallet.transform.parent.gameObject.SetActive(!Game.Painter.Editing&&!Game.Experience.Packing);}if(Game.Menu.EscapeHandledThisFrame)return;var k=Keyboard.current;if(k!=null&&k.lKey.wasPressedThisFrame&&!Game.Painter.Editing&&(!Game.Testing||!Game.Testing.Active)){if(IsOpen)Close();else Open();}
  }
  static readonly Vector3 LaptopHome=new(-8.8f,.025f,-3.2f);
  public bool ClearLaptopPosition(Vector3 position){
   var footprint=new Rect(position.x-2.46f,position.z-1.84f,4.92f,3.68f);
   if(footprint.xMin< -12f||footprint.xMax>10.6f||footprint.yMin< -6.15f||footprint.yMax>6.1f)return false;
   // Reserve every stage's carton and open lid, including while cartons travel in.
   var occupied=new[]{new Rect(-8.95f,-1.28f,3.2f,5.95f),new Rect(5.75f,-2.08f,3.2f,5.95f),new Rect(-4.65f,2.85f,9.3f,3.4f),new Rect(5.6f,-5.1f,3.65f,2.8f)};
   foreach(var zone in occupied)if(footprint.Overlaps(zone))return false;
   var board=Game.Controller.Lookup["Case"].Visual.bounds;return !footprint.Overlaps(new Rect(board.min.x-.12f,board.min.z-.12f,board.size.x+.24f,board.size.z+.24f));
  }
  void RestoreLaptopPosition(){var saved=Data.laptopPlaced&&Data.laptopLayoutVersion>=1?Data.laptopPosition:LaptopHome;Data.laptopLayoutVersion=1;laptop.transform.position=Game.WaitingForOrder?new Vector3(0,.025f,.15f):ClearLaptopPosition(saved)?new Vector3(saved.x,.025f,saved.z):LaptopHome;if(Data.laptopPlaced&&!Game.WaitingForOrder)Data.laptopPosition=laptop.transform.position;}
  public bool BeginLaptopMove(Vector2 point){if(!Ready||Game.WaitingForOrder||IsOpen||Game.ScreenChangeBlocked||!laptopHit.Raycast(Game.Controller.ViewCamera.ScreenPointToRay(point),out _,100))return false;Game.Controller.CancelDrag();Game.Tools.Deselect();laptopStart=laptop.transform.position;laptopOffset=laptopStart-Game.Controller.MousePlane(point,laptopStart.y);laptopBlocked=false;IsMoving=true;return true;}
  public void MoveLaptop(Vector2 point){if(!IsMoving)return;var p=Game.Controller.MousePlane(point,laptopStart.y)+laptopOffset;p=new Vector3(Mathf.Clamp(p.x,-9.4f,8),laptopStart.y,Mathf.Clamp(p.z,-4.25f,4.2f));laptopBlocked=!ClearLaptopPosition(p);if(!laptopBlocked)laptop.transform.position=p;}
  public void EndLaptopMove(bool cancel){if(!IsMoving)return;if(cancel)laptop.transform.position=laptopStart;else{Data.laptopLayoutVersion=1;Data.laptopPlaced=true;Data.laptopPosition=laptop.transform.position;if(laptopBlocked)Game.Menu.Toast("Burada kutu veya alet var. Laptop boş alanda kaldı.");}IsMoving=false;}
  public bool HandlePointer(){
   var m=Mouse.current;if(m==null||!Ready||IsOpen||Game.Painter.Editing)return false;
   if(Game.Controller.Dragged){if(hovered){hovered=false;Game.Experience.ShowTooltip(null);}return false;}
   if(IsMoving){MoveLaptop(m.position.ReadValue());if(m.leftButton.wasReleasedThisFrame)EndLaptopMove(false);return true;}
   bool over=!(EventSystem.current&&EventSystem.current.IsPointerOverGameObject())&&laptopHit.Raycast(Game.Controller.ViewCamera.ScreenPointToRay(m.position.ReadValue()),out _,100);
   if(over!=hovered){hovered=over;Game.Experience.ShowTooltip(over?(Game.Menu.ToolMode==1?"Laptop · Sürükleyerek taşı · Esc: geri koy":"Laptop · Mağazayı aç · Taşımak için taşıma aracını seç"):null);}
   if(!over)return false;if(m.leftButton.wasPressedThisFrame){if(Game.Menu.ToolMode==1)BeginLaptopMove(m.position.ReadValue());else Open();}return true;
  }
  void OnApplicationFocus(bool focused){if(!focused)EndLaptopMove(true);}
  void OnDestroy(){foreach(var x in owned)Destroy(x);}
 }
}
