using System;
using UnityEngine;
namespace CozyBoard {
    // Pigment deposition is measured in contact time, not rendered frames.
    public static class WorkshopPaintMedium {
        public const float DrySeconds=7f;
        public static float Wetness(float depositedAt,float now)=>Mathf.Clamp01(1-(now-depositedAt)/DrySeconds);
        public static Color Deposit(Color under,Color pigment,float strength,float seconds,float wet){
            float coverage=1-Mathf.Exp(-Mathf.Max(0,strength)*Mathf.Max(0,seconds)*9);
            // A little of the still-wet undercoat is picked up by the new pigment.
            Color loaded=Color.Lerp(pigment,under,wet*.28f);
            var result=Color.Lerp(under,loaded,coverage);result.a=1;return result;
        }
    }
    [Serializable] public struct WorkshopTapeStrip {
        public Vector3 Start,End,Normal;
        public float Width;
        public bool Covers(Vector3 point){
            var direction=Vector3.ProjectOnPlane(End-Start,Normal).normalized;
            if(direction.sqrMagnitude<.001f)return false;
            var cross=Vector3.Cross(Normal,direction).normalized;var offset=point-Start;
            float along=Vector3.Dot(offset,direction),length=Vector3.ProjectOnPlane(End-Start,Normal).magnitude;
            return along>=-Width*.5f&&along<=length+Width*.5f&&Mathf.Abs(Vector3.Dot(offset,cross))<=Width*.5f;
        }
    }
    [Serializable] public sealed class WorkshopTapeSave {public WorkshopTapeStrip[] Strips;}
}
