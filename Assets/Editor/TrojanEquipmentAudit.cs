#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FinalSpam.EditorTools
{
    public static class TrojanEquipmentAudit
    {
        public static void Audit()
        {
            GameObject prefab=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Enemies/SoldadoTroia.prefab");
            try
            {
                Transform left=Find(prefab.transform,"HAND_L");
                Transform right=Find(prefab.transform,"HAND_R");
                if(left==null||right==null)throw new Exception("Trojan hand bones missing");
                Debug.Log("[TROJAN AUDIT] Left hand "+left.localPosition+" rot "+left.localRotation);
                Debug.Log("[TROJAN AUDIT] Right hand "+right.localPosition+" rot "+right.localRotation);
                LogMount("LEFT",left);
                LogMount("RIGHT",right);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        private static Transform Find(Transform root,string needle)
        {
            foreach(Transform x in root.GetComponentsInChildren<Transform>(true))
                if(x.name==needle)return x;
            return null;
        }
        private static void LogMount(string side,Transform hand)
        {
            for(int i=0;i<hand.childCount;i++)
            {
                Transform child=hand.GetChild(i);
                Debug.Log("[TROJAN AUDIT] "+side+" mounted "+child.name+
                    " pos "+child.localPosition+" rot "+child.localRotation);
                Vector3 min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity);
                Vector3 max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity);
                foreach(MeshFilter f in child.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(f.sharedMesh==null)continue;
                    Bounds b=f.sharedMesh.bounds;
                    for(int x=-1;x<=1;x+=2)
                    for(int y=-1;y<=1;y+=2)
                    for(int z=-1;z<=1;z+=2)
                    {
                        Vector3 pos=hand.InverseTransformPoint(f.transform.TransformPoint(
                            b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                        min=Vector3.Min(min,pos);max=Vector3.Max(max,pos);
                    }
                }
                foreach(SkinnedMeshRenderer f in child.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if(f.sharedMesh==null)continue;
                    Bounds b=f.sharedMesh.bounds;
                    for(int x=-1;x<=1;x+=2)
                    for(int y=-1;y<=1;y+=2)
                    for(int z=-1;z<=1;z+=2)
                    {
                        Vector3 pos=hand.InverseTransformPoint(f.transform.TransformPoint(
                            b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                        min=Vector3.Min(min,pos);max=Vector3.Max(max,pos);
                    }
                }
                Debug.Log("[TROJAN AUDIT] "+side+" "+child.name+" MESH bounds MIN "+
                    min+" MAX "+max+" CENTER "+((min+max)/2f)+" SIZE "+(max-min));
            }
        }
    }
}
#endif
