using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BEERLabs.ProjectEsky.Tracking{
    public class EskyAnchor : MonoBehaviour
    {
        public int AnchorID;
        [HideInInspector]
        public Dictionary<string,EskyAnchorContent> myContent = new Dictionary<string, EskyAnchorContent>();
        public Transform contentOrigin;
        public static Dictionary<int,EskyAnchor> instances = new Dictionary<int, EskyAnchor>();
        public GameObject meshRoot;
        public bool loadMapMesh = false;
        public Material meshMaterial;

        public int HookedTrackerID;
        private void Awake() {
            if(!instances.ContainsKey(AnchorID)){
                instances.Add(AnchorID,this);
            }else{
                Debug.LogWarning("WARNING: Multiple Anchor IDs in the scene: " + AnchorID);
            }
        }
        private void Start(){
            if(EskyTracker.instances.ContainsKey(HookedTrackerID)){
                EskyTracker.instances[HookedTrackerID].MeshParent = meshRoot;
                EskyTracker.instances[HookedTrackerID].SubscribeAnchor(this);
            }
        }
        public (Dictionary<string,EskyAnchorContentInfo>, byte[] meshData) GetEskyMapInfo(){
            Dictionary<string,EskyAnchorContentInfo> retMapInfo = new Dictionary<string, EskyAnchorContentInfo>();
            byte[] b = new byte[]{};
            foreach(KeyValuePair<string,EskyAnchorContent> contVals in myContent){
                EskyAnchorContentInfo eaci = new EskyAnchorContentInfo();
                eaci.localPosition = contVals.Value.transform.localPosition;
                eaci.localRotation = contVals.Value.transform.localRotation;
                retMapInfo.Add(contVals.Key,eaci);
            }
            MeshFilter[] meshFiltersInChild = meshRoot.GetComponentsInChildren<MeshFilter>();
            Debug.Log("Serializing this count of meshes: " + meshFiltersInChild.Length);
            b = BEERLabs.ProjectEsky.Utilities.EskyMeshSerializer.Serialize(meshFiltersInChild,meshRoot.transform);
            Debug.Log("Serialized meshes to binary size: " +b.Length);
            return (retMapInfo,b);
        }
        public void SetEskyMapInfo(EskyMap information){
            foreach(KeyValuePair<string, EskyAnchorContentInfo> content in information.contentLocations){
                if(myContent.ContainsKey(content.Key)){
                    myContent[content.Key].transform.localPosition = content.Value.localPosition;
                    myContent[content.Key].transform.localRotation = content.Value.localRotation;                    
                }else{
                    Debug.LogWarning("Careful, we don't have this content id in the scene: " + content.Key);
                }
            }
            if(loadMapMesh){
                List<Mesh> meshes = new List<Mesh>();
                try
                {
                    meshes = (List<Mesh>)BEERLabs.ProjectEsky.Utilities.EskyMeshSerializer.Deserialize(information.meshDataArray);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
//                Debug.Log("Deserialized")
                Material matm = new Material(meshMaterial);
                for (int j = 0; j < meshes.Count; j++)
                {
                    GameObject gg = new GameObject();
                    gg.name = "Esky Loaded Mesh: " + j;
                    gg.AddComponent<MeshFilter>();
                    gg.GetComponent<MeshFilter>().mesh = meshes[j];
                    gg.layer = LayerMask.NameToLayer("WorldReconstruction");
                    gg.AddComponent<MeshRenderer>();
                    gg.AddComponent<MeshCollider>();
                    gg.transform.parent = meshRoot.transform;
                    gg.GetComponent<MeshRenderer>().sharedMaterial = matm;
                    gg.GetComponent<MeshRenderer>().enabled = true;
                    gg.GetComponent<MeshRenderer>().receiveShadows = true;
                    gg.GetComponent<MeshCollider>().sharedMesh = null;
                    gg.GetComponent<MeshCollider>().sharedMesh = meshes[j];
                }
            }
            BEERLabs.ProjectEsky.Utilities.EskyMeshSerializer.Deserialize(information.meshDataArray);
            
        }
        public static void Subscribe(int ID, EskyAnchorContent contenttosubscribe){
            if(instances[ID] != null){
                if(!instances[ID].myContent.ContainsKey(contenttosubscribe.ContentID)){
                    instances[ID].myContent.Add(contenttosubscribe.ContentID,contenttosubscribe);
                    contenttosubscribe.transform.parent = instances[ID].transform;                   
                }else{
                    Debug.LogError("ID " + contenttosubscribe.ContentID + " already exists, gameobject " + contenttosubscribe.gameObject.name + " will not be subscribed");
                }
            } 
        }
        public void RelocalizationCallback(){
            foreach(KeyValuePair<string,EskyAnchorContent> kvpeac in myContent){
                kvpeac.Value.OnLocalizedCallback();
            }
        }
    }
}