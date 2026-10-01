using System.Linq;
using TMPro;
using UnityEngine;
namespace CozyBoard {
 public sealed partial class WorkshopShop {
  GameObject mailView,mailNotification,laptopMailBadge;
  RectTransform mailRoot;TMP_Text laptopMailCount;
  TMP_Text mailSender,mailSubject,mailBody,mailReward,mailCounter,mailNoticeText;
  UnityEngine.UI.RawImage mailPortrait;
  UnityEngine.UI.Button previousMail,nextMail;
  int mailIndex=-1;
  public bool MailOpen=>IsOpen&&mailView&&mailView.activeSelf;
  public int UnreadMail=>Game.DeliveryMail.Count(x=>x.pending);
  void BuildMailUI(RectTransform root){
   mailRoot=root;
   var screen=panel.transform.Find("Laptop catalogue");
   screen.Find("Store title").GetComponent<TMP_Text>().text="ATÖLYE LAPTOPU";
   screen.Find("Store subtitle").gameObject.SetActive(false);
   WorkshopUI.Button("Market tab",screen,font,"Parça pazarı",new Vector2(0,1),new Vector2(45,-112),new Vector2(220,48),()=>SelectMailbox(false));
   WorkshopUI.Button("Mailbox tab",screen,font,"Gelen kutusu",new Vector2(0,1),new Vector2(279,-112),new Vector2(240,48),OpenMail);
   var mail=WorkshopUI.Rect("Customer mailbox",screen,new Vector2(0,1),new Vector2(45,-186),new Vector2(1300,650));mailView=mail.gameObject;
   var portraitPaper=WorkshopUI.Panel("Portrait paper",mail,new Vector2(0,1),new Vector2(0,-18),new Vector2(218,258),new Color(.98f,.95f,.86f));
   mailPortrait=WorkshopUI.Art("Sender portrait",portraitPaper.transform,Resources.Load<Texture2D>("Customers/Portraits"),new Vector2(.5f,1),new Vector2(0,-10),new Vector2(198,236));
   mailSender=WorkshopUI.Text("Sender",mail,font,"",28,new Vector2(0,1),new Vector2(265,-18),new Vector2(960,44));
   mailSubject=WorkshopUI.Text("Subject",mail,font,"",38,new Vector2(0,1),new Vector2(265,-78),new Vector2(980,64));
   mailBody=WorkshopUI.Text("Customer letter",mail,font,"",29,new Vector2(0,1),new Vector2(265,-165),new Vector2(950,240));mailBody.textWrappingMode=TextWrappingModes.Normal;mailBody.enableAutoSizing=true;mailBody.fontSizeMin=23;mailBody.fontSizeMax=29;
   mailReward=WorkshopUI.Text("Delivery summary",mail,font,"",23,new Vector2(0,1),new Vector2(265,-430),new Vector2(950,90));
   mailCounter=WorkshopUI.Text("Message count",mail,font,"",22,new Vector2(0,0),new Vector2(0,30),new Vector2(330,36));
   previousMail=WorkshopUI.Button("Older mail",mail,font,"← Önceki",new Vector2(0,0),new Vector2(350,22),new Vector2(180,48),()=>ReadMail(mailIndex-1));
   nextMail=WorkshopUI.Button("Newer mail",mail,font,"Sonraki →",new Vector2(0,0),new Vector2(545,22),new Vector2(180,48),()=>ReadMail(mailIndex+1));
   WorkshopUI.Button("Return to workshop",mail,font,"Atölyeye dön",new Vector2(1,0),new Vector2(0,22),new Vector2(240,48),Close);
   var notification=WorkshopUI.Button("New customer mail",root,font,"",new Vector2(.5f,1),new Vector2(0,-24),new Vector2(490,60),OpenMail);mailNotification=notification.gameObject;
   notification.GetComponent<UnityEngine.UI.Image>().color=WorkshopUI.Paper;mailNoticeText=notification.GetComponentInChildren<TMP_Text>();mailNoticeText.color=WorkshopUI.Ink;mailNoticeText.fontSize=23;
   var source=new {texture=WorkshopAtelierStyle.Icons,uvRect=WorkshopAtelierStyle.IconUV(1)};
   var envelope=WorkshopUI.Art("Notification envelope",notification.transform,source.texture,new Vector2(0,.5f),new Vector2(12,0),new Vector2(44,44));envelope.uvRect=source.uvRect;mailNoticeText.rectTransform.anchoredPosition=new Vector2(25,0);mailNoticeText.rectTransform.sizeDelta=new Vector2(415,48);
   var badge=WorkshopUI.Button("Laptop unread mail",root,font,"",Vector2.one*.5f,Vector2.zero,new Vector2(60,60),OpenMail);laptopMailBadge=badge.gameObject;badge.GetComponent<UnityEngine.UI.Image>().enabled=false;badge.GetComponent<UnityEngine.UI.Shadow>().enabled=false;
   var badgeArt=WorkshopUI.Art("Unread envelope",badge.transform,source.texture,Vector2.one*.5f,Vector2.zero,new Vector2(60,60));badgeArt.uvRect=source.uvRect;badgeArt.raycastTarget=true;badge.targetGraphic=badgeArt;
   var count=WorkshopUI.Panel("Unread counter",badge.transform,Vector2.one,new Vector2(5,5),new Vector2(25,25),new Color(.65f,.28f,.20f));count.raycastTarget=false;laptopMailCount=WorkshopUI.Text("Count",count.transform,font,"",18,Vector2.one*.5f,Vector2.zero,new Vector2(25,25));laptopMailCount.alignment=TextAlignmentOptions.Center;laptopMailCount.color=WorkshopUI.Paper;
   mailView.SetActive(false);mailNotification.SetActive(false);laptopMailBadge.SetActive(false);
  }
  public void OpenMail(){if(!IsOpen)Open();if(IsOpen)SelectMailbox(true);}
  void SelectMailbox(bool value,bool readLatest=true){
   if(!mailView)return;if(ordersView)ordersView.SetActive(false);
   var screen=panel.transform.Find("Laptop catalogue");foreach(Transform child in screen)if(child.name.StartsWith("Category ")||child.name.StartsWith("Product ")||child.name=="Shop message"||child.name=="Workshop economy")child.gameObject.SetActive(!value);
   if(!value)Refresh();
   mailView.SetActive(value);if(value&&readLatest)ReadMail(Game.DeliveryMail.Count-1);
  }
  public void ReadMail(int index){
   if(Game.DeliveryMail.Count==0){mailIndex=-1;mailSender.text="Gelen kutun henüz boş";mailSubject.text="İlk klavyenin hikâyesi burada başlayacak.";mailBody.text="Bir sipariş teslim ettiğinde müşterinin maili buraya gelir.";mailReward.text="";mailPortrait.transform.parent.gameObject.SetActive(false);mailCounter.text="0 mail";previousMail.interactable=nextMail.interactable=false;return;}
   mailIndex=Mathf.Clamp(index,0,Game.DeliveryMail.Count-1);var receipt=Game.DeliveryMail[mailIndex];var customer=WorkshopOrders.ForCustomer(receipt.CustomerId);
   mailPortrait.transform.parent.gameObject.SetActive(true);mailPortrait.uvRect=new Rect(WorkshopStory.PortraitIndex(receipt.CustomerId)/3f,0,1f/3f,1);
   mailSender.text=customer.Name+"  →  Cozy Board Atölyesi";mailSubject.text=receipt.Subject;
   mailBody.text=receipt.Letter+"\n\nSevgiler,\n"+customer.Name;
   mailReward.text=receipt.kind=="repair"?$"Sipariş {receipt.order:00} · Switch değiştirildi · 3 tuş kontrol edildi\nHesabına {receipt.reward} Tık eklendi.":$"Sipariş {receipt.order:00} · {(receipt.kind=="macro"?"6 tuşlu makro pad · ":"")}{WorkshopOrders.SwitchName(receipt.switchId)}\n{receipt.matches} / 2 istek karşılandı · Hesabına {receipt.reward} Tık eklendi.";
   mailCounter.text=$"{mailIndex+1} / {Game.DeliveryMail.Count} mail";previousMail.interactable=mailIndex>0;nextMail.interactable=mailIndex<Game.DeliveryMail.Count-1;
   if(receipt.pending){receipt.pending=false;if(!Game.TrySaveQuiet())receipt.pending=true;}UpdateMailNotification();
  }
  void UpdateMailNotification(){
   if(!mailNotification)return;bool visible=UnreadMail>0&&!Game.Experience.MainVisible&&!Game.Experience.Packing&&!Game.Experience.Inspecting&&!Game.Painter.Editing&&!IsOpen;
   mailNotification.SetActive(visible);if(laptopMailBadge){laptopMailBadge.SetActive(visible);if(visible){var screen=Game.Controller.ViewCamera.WorldToScreenPoint(laptop.transform.TransformPoint(new Vector3(1.05f,.45f,.6f)));var canvas=mailRoot.GetComponentInParent<Canvas>();var uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;if(RectTransformUtility.ScreenPointToWorldPointInRectangle(mailRoot,screen,uiCamera,out var point))laptopMailBadge.transform.position=point;laptopMailCount.text=UnreadMail.ToString();laptopMailBadge.transform.SetAsLastSibling();}}if(visible){var latest=Game.DeliveryMail.Last(x=>x.pending);mailNoticeText.text=$"Yeni mail · {WorkshopOrders.ForCustomer(latest.CustomerId).Name}   ·   Oku →";mailNotification.transform.SetAsLastSibling();}
  }
 }
}
