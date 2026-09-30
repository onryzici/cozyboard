using System;
using UnityEngine;
namespace CozyBoard {
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
