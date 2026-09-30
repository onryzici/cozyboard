using System.Linq;
using TMPro;
using UnityEngine;
namespace CozyBoard {
 public sealed partial class WorkshopShop {
  GameObject mailView,mailNotification;
  TMP_Text mailSender,mailSubject,mailBody,mailReward,mailCounter,mailNoticeText;
  UnityEngine.UI.RawImage mailPortrait;
  UnityEngine.UI.Button previousMail,nextMail;
  int mailIndex=-1;
  public bool MailOpen=>IsOpen&&mailView&&mailView.activeSelf;
  public int UnreadMail=>Game.DeliveryMail.Count(x=>x.pending);
  void BuildMailUI(RectTransform root){
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
   mailBody=WorkshopUI.Text("Customer letter",mail,font,"",29,new Vector2(0,1),new Vector2(265,-165),new Vector2(950,240));mailBody.enableAutoSizing=true;mailBody.fontSizeMin=23;mailBody.fontSizeMax=29;
   mailReward=WorkshopUI.Text("Delivery summary",mail,font,"",23,new Vector2(0,1),new Vector2(265,-430),new Vector2(950,90));
   mailCounter=WorkshopUI.Text("Message count",mail,font,"",22,new Vector2(0,0),new Vector2(0,30),new Vector2(330,36));
   previousMail=WorkshopUI.Button("Older mail",mail,font,"← Önceki",new Vector2(0,0),new Vector2(350,22),new Vector2(180,48),()=>ReadMail(mailIndex-1));
   nextMail=WorkshopUI.Button("Newer mail",mail,font,"Sonraki →",new Vector2(0,0),new Vector2(545,22),new Vector2(180,48),()=>ReadMail(mailIndex+1));
   WorkshopUI.Button("Return to workshop",mail,font,"Atölyeye dön",new Vector2(1,0),new Vector2(0,22),new Vector2(240,48),Close);
   var notification=WorkshopUI.Button("New customer mail",root,font,"",new Vector2(.5f,1),new Vector2(0,-24),new Vector2(490,60),OpenMail);mailNotification=notification.gameObject;
   notification.GetComponent<UnityEngine.UI.Image>().color=WorkshopUI.Paper;mailNoticeText=notification.GetComponentInChildren<TMP_Text>();mailNoticeText.color=WorkshopUI.Ink;mailNoticeText.fontSize=23;
   mailView.SetActive(false);mailNotification.SetActive(false);
  }
  public void OpenMail(){if(!IsOpen)Open();if(IsOpen)SelectMailbox(true);}
  void SelectMailbox(bool value){
   if(!mailView)return;
   var screen=panel.transform.Find("Laptop catalogue");foreach(Transform child in screen)if(child.name.StartsWith("Category ")||child.name.StartsWith("Product ")||child.name=="Shop message"||child.name=="Workshop economy")child.gameObject.SetActive(!value);
   mailView.SetActive(value);if(value)ReadMail(Game.DeliveryMail.Count-1);
  }
  public void ReadMail(int index){
   if(Game.DeliveryMail.Count==0){mailIndex=-1;mailSender.text="Gelen kutun henüz boş";mailSubject.text="İlk klavyenin hikâyesi burada başlayacak.";mailBody.text="Bir sipariş teslim ettiğinde müşterinin maili buraya gelir.";mailReward.text="";mailPortrait.transform.parent.gameObject.SetActive(false);mailCounter.text="0 mail";previousMail.interactable=nextMail.interactable=false;return;}
   mailIndex=Mathf.Clamp(index,0,Game.DeliveryMail.Count-1);var receipt=Game.DeliveryMail[mailIndex];var customer=WorkshopOrders.For(receipt.order);
   mailPortrait.transform.parent.gameObject.SetActive(true);mailPortrait.uvRect=new Rect(((receipt.order-1)%3)/3f,0,1f/3f,1);
   mailSender.text=customer.Name+"  →  Cozy Board Atölyesi";mailSubject.text="Klavyem geldi!";
   mailBody.text=receipt.Reaction+"\n\nSevgiler,\n"+customer.Name;
   mailReward.text=$"Sipariş {receipt.order:00} · {WorkshopOrders.SwitchName(receipt.switchId)}\n{receipt.matches} / 2 istek karşılandı · Hesabına {receipt.reward} Tık eklendi.";
   mailCounter.text=$"{mailIndex+1} / {Game.DeliveryMail.Count} mail";previousMail.interactable=mailIndex>0;nextMail.interactable=mailIndex<Game.DeliveryMail.Count-1;
   if(receipt.pending){receipt.pending=false;Game.SaveProgress();}UpdateMailNotification();
  }
  void UpdateMailNotification(){
   if(!mailNotification)return;bool visible=UnreadMail>0&&!Game.Experience.MainVisible&&!Game.Experience.Packing&&!Game.Experience.Inspecting&&!Game.Painter.Editing&&!IsOpen;
   mailNotification.SetActive(visible);if(visible){var latest=Game.DeliveryMail.Last(x=>x.pending);mailNoticeText.text=$"Yeni mail · {WorkshopOrders.For(latest.order).Name}   ·   Oku →";mailNotification.transform.SetAsLastSibling();}
  }
 }
}
