using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CozyBoard {
    public sealed partial class WorkshopRepairBench {
        readonly MeshRenderer[] instrumentLeds=new MeshRenderer[3];
        readonly TMP_Text[] instrumentLabels=new TMP_Text[3];
        Material ledOff,ledPass,ledFail;
        Transform instrumentDial,instrumentPlug,instrumentSocket;
        MeshFilter instrumentLead,instrumentLeadShadow;
        Vector3 lastLeadEnd;
        Quaternion lastLeadRotation;
        bool leadReady,lastLeadConnected;
        public bool InstrumentConnected=>initialized&&game.IsRepair&&game.Repair!=null&&game.Testing.Active;

        public bool UseTestInstrument(){
            if(!initialized||Moving||Unpacking||DetailOpen||game.Tools.Busy)return false;
            if(!AtRepair){Visit(true);return false;}
            if(!game.IsRepair||game.Repair==null){game.Menu.Toast("Cihaza bağlı klavye yok. Önce bir tamir işi seç.");return false;}
            if(game.Testing.Active){game.Testing.End();return false;}
            game.Testing.Begin();
            if(game.Testing.Active)game.Audio.Play(game.Audio.Snap,.25f);
            return game.Testing.Active;
        }
        static string KeyName(WorkshopItem key)=>key.Label.Replace("Tuş ","");
        public void RefreshInstrument(){
            if(!initialized||!deviceText||!ledOff||!game.Testing)return;
            bool repair=game.IsRepair&&game.Repair!=null;
            instrumentSocket.gameObject.SetActive(repair);
            var keys=repair?game.TestKeys.OrderBy(x=>x.Slot.x).ToArray():System.Array.Empty<WorkshopItem>();
            for(int i=0;i<instrumentLeds.Length;i++){
                var key=i<keys.Length?keys[i]:null;
                bool failed=key&&(game.Testing.HasFailed(key.Id)||game.Repair!=null&&game.Repair.diagnosed&&!game.Repair.replaced&&key.Id==game.Repair.capId);
                instrumentLeds[i].sharedMaterial=key&&game.Testing.HasSignal(key.Id)?ledPass:failed?ledFail:ledOff;
                instrumentLabels[i].text=key?KeyName(key):"—";
            }
            if(!repair)deviceText.text="ÜRÜN YOK";
            else if(!game.Completed)deviceText.text=game.Controller.Lookup[game.Repair.switchId].Fitted?"KAPAK EKSİK":"SWITCH EKSİK";
            else if(game.Testing.Passed)deviceText.text="3 / 3\n<size=65%>TEST TAMAM</size>";
            else if(game.Testing.Active&&game.Testing.LastMeasuredKey&&keys.Contains(game.Testing.LastMeasuredKey))
                deviceText.text=KeyName(game.Testing.LastMeasuredKey)+"\n<size=65%>"+(game.Testing.LastSignal?"SİNYAL VAR":"SİNYAL YOK")+"</size>";
            else if(game.Repair!=null&&game.Repair.diagnosed&&!game.Repair.replaced)
                deviceText.text=KeyName(game.Controller.Lookup[game.Repair.capId])+"\n<size=65%>SİNYAL YOK</size>";
            else deviceText.text=game.Testing.Active?"TUŞA BAS\n<size=65%>"+game.Testing.Count+" / 3</size>":game.Repair!=null&&game.Repair.replaced?"SON KONTROL":"TESTE HAZIR";
            UpdateInstrumentMotion();
        }
        void UpdateInstrumentMotion(){
            if(!instrumentDial||!instrumentPlug)return;
            var target=Quaternion.Euler(0,InstrumentConnected?35:-35,0);
            instrumentDial.localRotation=Application.isPlaying?Quaternion.RotateTowards(instrumentDial.localRotation,target,240*Time.unscaledDeltaTime):target;
            var board=game.Controller.Lookup["Case"].transform;
            var rotation=InstrumentConnected?board.rotation:Quaternion.identity;
            // The socket moves with the case, including rotated or repositioned keyboards.
            var end=InstrumentConnected?bench.InverseTransformPoint(instrumentSocket.position+board.forward*.20f):new Vector3(1.15f,.08f,1.78f);
            if(leadReady&&lastLeadConnected==InstrumentConnected&&(end-lastLeadEnd).sqrMagnitude<.00001f&&Quaternion.Angle(rotation,lastLeadRotation)<.1f)return;
            instrumentPlug.localPosition=end;instrumentPlug.rotation=rotation;
            var previous=instrumentLead.sharedMesh;
            var mesh=WireMesh(.027f,InstrumentLead(InstrumentConnected,end,rotation));owned.Add(mesh);
            instrumentLead.sharedMesh=instrumentLeadShadow.sharedMesh=mesh;
            owned.Remove(previous);if(Application.isPlaying)Destroy(previous);else DestroyImmediate(previous);
            lastLeadConnected=InstrumentConnected;lastLeadEnd=end;lastLeadRotation=rotation;leadReady=true;
        }
        static Vector3[] InstrumentLead(bool connected,Vector3 end,Quaternion rotation){
            var lead=new List<Vector3>();var start=new Vector3(1.97f,.105f,3.10f);
            var bend=connected?new Vector3(end.x+.65f,.08f,end.z+.70f):new Vector3(.75f,.055f,2.55f);
            var control=connected?new Vector3(end.x+1,.065f,3.15f):new Vector3(.85f,.055f,3.10f);
            for(int i=0;i<=28;i++){
                float t=i/28f,u=1-t;
                lead.Add(u*u*u*start+3*u*u*t*new Vector3(1.95f,.055f,2.60f)+3*u*t*t*control+t*t*t*bend);
            }
            var back=end+rotation*Vector3.forward*.13f;
            for(int i=1;i<=36;i++){
                float t=i/36f;var point=Vector3.Lerp(bend,back,t);
                point.x+=Mathf.Sin(t*Mathf.PI*4)*.12f*Mathf.Sin(t*Mathf.PI);lead.Add(point);
            }
            return lead.ToArray();
        }
    }
}
