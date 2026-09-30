using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyBoard {
    // Explicit opt-in. SavePath routes this runner to a separate temporary save.
    public sealed class WorkshopStorySmoke : MonoBehaviour {
        readonly List<string> results=new();
        string report;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(Environment.GetCommandLineArgs().Contains("--cozy-story-smoke"))new GameObject("Story verification").AddComponent<WorkshopStorySmoke>();}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);results.Add("PASS "+message);}
        IEnumerator Start(){
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--cozy-story-report");report=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.temporaryCachePath,"story-smoke.txt");Directory.CreateDirectory(Path.GetDirectoryName(report));
            yield return null;yield return null;var run=Run();
            while(true){bool more;object current=null;try{more=run.MoveNext();if(more)current=run.Current;}catch(Exception e){results.Add("FAIL "+e);File.WriteAllLines(report,results);Application.Quit(1);yield break;}if(!more)break;yield return current;}
            File.WriteAllLines(report,results);Application.Quit(0);
        }
        void Fits(Transform root){Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();var rect=text.rectTransform.rect;bool within=text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(x=>x.isVisible).All(x=>x.bottomLeft.x>=rect.xMin-1&&x.topRight.x<=rect.xMax+1&&x.bottomLeft.y>=rect.yMin-1&&x.topRight.y<=rect.yMax+1);Check(!text.isTextOverflowing&&within,"Runtime text fits: "+text.name);}}
        IEnumerator Run(){
            var game=FindAnyObjectByType<WorkshopGameMode>();
            Check(game.SavePath==Path.Combine(Application.temporaryCachePath,"story-smoke-save.json"),"Story runner cannot write the user's workshop save");
            game.Experience.NewGame();yield return null;
            Check(game.StoryUI.Open&&game.WaitingForOrder&&game.Menu.InputBlocked,"Runtime new game opens a blocking introduction");
            Fits(GameObject.Find("Notebook paper").transform);
            GameObject.Find("Leave notebook").GetComponent<Button>().onClick.Invoke();yield return null;
            Check(game.Story.openingRead&&game.Shop.IsOpen,"Opening button opens laptop's first offer");Fits(GameObject.Find("Available orders").transform);
            GameObject.Find("Available orders/Offer 0/Accept offer").GetComponent<Button>().onClick.Invoke();yield return null;
            Check(!game.WaitingForOrder&&game.CurrentRequest.Name=="Ece"&&game.Menu.OrderPanel.activeSelf,"Offer button accepts Ece and shows the order");Fits(game.Menu.OrderPanel.transform);
            string accepted=game.ActiveOrder.instanceId;game.TrySaveQuiet();game.LoadProgress();
            Check(game.ActiveOrder.instanceId==accepted&&!game.StoryUI.Open,"Runtime save/load keeps identity without replaying opening");
            game.Menu.ClosePanels();
            foreach(var item in game.Controller.Items.Where(x=>x.Stage>0).OrderBy(x=>x.Stage))game.Controller.Attach(item);
            yield return new WaitForSeconds(.8f);game.Refresh();game.Testing.LooseSwitch=null;game.Testing.FaultAssigned=true;game.Tools.Tightened=15;
            game.Testing.Begin();foreach(var cap in game.Controller.Items.Where(x=>x.Stage==4))game.Press(cap);yield return null;
            Check(game.Testing.Passed,"Actual key checks complete the story keyboard test");game.Testing.End();
            int balance=game.Shop.Data.credits;game.Deliver();yield return null;
            Check(game.Experience.PackStep==1,"Story delivery enters existing physical packing");
            for(int i=1;i<=3;i++){game.Experience.AdvancePacking();yield return new WaitForSeconds(.95f);Check(game.Experience.PackStep==i+1,"Packing step "+i+" completes");}
            game.Experience.AdvancePacking();yield return new WaitForSeconds(2f);
            Check(!game.Experience.Packing&&game.WaitingForOrder&&game.Story.FirstDelivered,"Shipment records story and returns to offers");
            Check(game.Shop.Data.credits==balance+300&&game.DeliveryMail.Count==1,"Runtime shipment pays only once");
            game.Deliver();Check(game.Shop.Data.credits==balance+300,"Repeated shipment call cannot duplicate payment");
            Check(game.Shop.IsOpen&&GameObject.Find("Available orders/Offer 2"),"Three offers are visible after first shipment");Fits(GameObject.Find("Available orders").transform);
            GameObject.Find("Available orders/Offer 2/Accept offer").GetComponent<Button>().onClick.Invoke();yield return null;
            Check(game.OrderNumber==2&&game.CustomerId=="mina","Runtime player can choose Mina instead of rotated Deniz");
            game.Shop.Open();Check(game.Shop.Purchase(5),"Selected customer's switch set can be purchased");game.Shop.OpenMail();yield return null;
            Check(GameObject.Find("Sender").GetComponent<TMP_Text>().text.StartsWith("Ece")&&GameObject.Find("Customer letter").GetComponent<TMP_Text>().text.Contains("pencere"),"Ece's story mail remains intact during Mina's job");Fits(GameObject.Find("Customer mailbox").transform);
            int paid=game.Shop.Data.credits;game.Shop.ReadMail(0);Check(game.Shop.Data.credits==paid,"Rereading mail does not repay delivery");
            game.Shop.Close();game.StoryUI.OpenNotebook();yield return null;Fits(GameObject.Find("Notebook paper").transform);game.StoryUI.CloseNotebook();
            game.TrySaveQuiet();game.LoadProgress();Check(game.CustomerId=="mina"&&game.Story.FirstDelivered&&!game.LastDelivery.pending,"Runtime reload retains chosen customer, narrative and read mail");
            WorkshopPaintVerify.Run(game,Check);
        }
    }
}
