using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectEsky.Tracking;
namespace ProjectEsky.Networking
{
    public class EskyClient : EskyNetworkEntity
    {
        // Start is called before the first frame update
        public static EskyClient myClient;
        public bool isAR;
        public UnityEngine.Events.UnityEvent ClientARFlagTriggerEventsLocal;
        public UnityEngine.Events.UnityEvent ClientARFlagTriggerEventsRemote;
        public UnityEngine.Events.UnityEvent ClientARFlagTriggerEventsAll;
        public bool hasSetAR = false;
        [SerializeField]
        public FollowType followType;
        public Vector3 TranslationOffset;
        public Vector3 RotationOffset;
        public void OnStartClient()
        {
            if (isLocalPlayer)
            {
                myClient = this;
            }
            if (isAR)
            {
                if (isLocalPlayer)
                {
                    if (ClientARFlagTriggerEventsLocal != null)
                    {
                        ClientARFlagTriggerEventsLocal.Invoke();
                    }
                }
                else
                {
                    if (isAR)
                    {
                        if (ClientARFlagTriggerEventsRemote != null)
                        {
                            ClientARFlagTriggerEventsRemote.Invoke();
                        }
                    }
                }
                if (isAR)
                {
                    if (ClientARFlagTriggerEventsAll != null)
                    {
                        ClientARFlagTriggerEventsAll.Invoke();
                    }
                }
            }
        }
        public void TriggerClientARObjects()
        {
            if (!hasSetAR)
            {
                hasSetAR = true;
                CmdTriggerServerIsClientFlag();
            }
        }
        public void CmdTriggerServerIsClientFlag()
        {
            isAR = true;
            RpcTriggerClientObjects();
            if (ClientARFlagTriggerEventsRemote != null)
            {
                ClientARFlagTriggerEventsRemote.Invoke();
            }
            if (ClientARFlagTriggerEventsAll != null)
            {
                ClientARFlagTriggerEventsAll.Invoke();
            }
        }
        public void RpcTriggerClientObjects()
        {
            if (isLocalPlayer)
            {
                if (ClientARFlagTriggerEventsLocal != null)
                {
                    ClientARFlagTriggerEventsLocal.Invoke();
                }
            }
            else
            {
                if (ClientARFlagTriggerEventsRemote != null)
                {
                    ClientARFlagTriggerEventsRemote.Invoke();
                }
            }
            if (ClientARFlagTriggerEventsAll != null)
            {
                ClientARFlagTriggerEventsAll.Invoke();
            }
        }
        public void UpdateTrackingLocally()
        {
            if (EskyHMDOrigin.instance != null)
            {
                switch (followType)
                {
                    case FollowType.SixDOF:
                        transform.position = EskyHMDOrigin.instance.transform.position + (EskyHMDOrigin.instance.transform.rotation * TranslationOffset); 
                        transform.rotation = EskyHMDOrigin.instance.transform.rotation * Quaternion.Euler(RotationOffset);
                        break;
                    case FollowType.ThreeDOF_Translation:
                        transform.position = EskyHMDOrigin.instance.transform.position;// + EskyHMDOrigin.instance.transform.localToWorldMatrix.MultiplyPoint(TranslationOffset);;
                        transform.Translate(TranslationOffset, Space.Self);
                        break;
                    case FollowType.ThreeDOF_Rotation:
                        transform.rotation = EskyHMDOrigin.instance.transform.rotation * Quaternion.Euler(RotationOffset);
                        break;
                    case FollowType.FourDOF_Y_Rotation:
                        transform.position = EskyHMDOrigin.instance.transform.position + (EskyHMDOrigin.instance.transform.rotation * TranslationOffset);
                        transform.rotation = Quaternion.Euler(new Vector3(0, EskyHMDOrigin.instance.transform.eulerAngles.y, 0)) * Quaternion.Euler(RotationOffset);
                        break;
                    case FollowType.Floor_FourDOF_Y_ROTATION:
                        transform.position = EskyHMDOrigin.instance.transform.position + (EskyHMDOrigin.instance.transform.rotation * TranslationOffset);
                        transform.rotation = Quaternion.Euler(new Vector3(0, EskyHMDOrigin.instance.transform.eulerAngles.y, 0)) * Quaternion.Euler(RotationOffset);
                        break;
                }
            }
        }
    }
}