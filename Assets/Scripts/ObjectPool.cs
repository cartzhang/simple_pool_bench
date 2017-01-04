using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SLQJ_POOL
{
    public class ObjectPool :MonoBehaviour
    {
        public LinkedList<Transform> UsingLinkedList;
        public LinkedList<Transform> UnusingLinkedList;
        [HideInInspector]
        public GameObject prefab;

        private Dictionary<Transform, ObjectPool> transformDic; 
        public void InitObjectPool(GameObject prefab,Dictionary<Transform,ObjectPool> transformDic,  int preloadNum = 10)
        {
            this.prefab = prefab;
            this.transformDic = transformDic;
            //this.parent = GameObject.FindObjectOfType<PoolManager>().transform;
            UsingLinkedList = new LinkedList<Transform>();
            UnusingLinkedList = new LinkedList<Transform>();
            for (int i = 0; i < preloadNum; i++)
            {
                GameObject go = GameObject.Instantiate(prefab);
                go.SetActive(false);
                go.transform.SetParent(transform);
                UnusingLinkedList.AddFirst(go.transform);
                this.transformDic.Add(go.transform,this);
            }
        }
        public void InitObjectPool(GameObject prefab,Vector3 pos,Quaternion qua,Dictionary<Transform,ObjectPool> transformDic,int preloadNum =10)
        {
            this.prefab = prefab;
            this.transformDic = transformDic;
            //this.parent = GameObject.FindObjectOfType<PoolManager>().transform;
            UsingLinkedList = new LinkedList<Transform>();
            UnusingLinkedList = new LinkedList<Transform>();
            for (int i = 0; i < preloadNum; i++)
            {
                GameObject go = (GameObject)GameObject.Instantiate(prefab,pos,qua);
                go.SetActive(false);
                go.transform.SetParent(transform);
                UnusingLinkedList.AddFirst(go.transform);
                this.transformDic.Add(go.transform, this);
            }
        }
        public Transform PullObjcetFromPool()
        {
            Transform pullTransfom = null;
            if (UnusingLinkedList.Count>0)
            {
                pullTransfom = UnusingLinkedList.First.Value;
                pullTransfom.gameObject.SetActive(true);
                UnusingLinkedList.RemoveFirst();
                UsingLinkedList.AddLast(pullTransfom.transform);
            }
            else
            {
                pullTransfom = GameObject.Instantiate(prefab).transform;
                pullTransfom.SetParent(transform);
                UsingLinkedList.AddLast(pullTransfom);
                transformDic.Add(pullTransfom, this);
            }
            return pullTransfom;
        }

        public Transform PullObjcetFromPool(Vector3 pos,Quaternion q)
        {
            Transform pullTransfom = null;
            if (UnusingLinkedList.Count > 0)
            {
                pullTransfom = UnusingLinkedList.First.Value;
                pullTransfom.rotation = q;
                pullTransfom.position = pos;
                pullTransfom.gameObject.SetActive(true);
                UnusingLinkedList.RemoveFirst();
                UsingLinkedList.AddLast(pullTransfom);
            }
            else
            {
                pullTransfom = (Instantiate(prefab,pos,q) as GameObject).transform;
                pullTransfom.rotation = q;
                pullTransfom.position = pos;
                pullTransfom.SetParent(transform);
                UsingLinkedList.AddLast(pullTransfom);
                transformDic.Add(pullTransfom, this);
            }
            return pullTransfom;
        }

        public void PushObjectToPool(Transform handleTransform)
        {
            if (handleTransform.gameObject.activeSelf)
            {
                handleTransform.gameObject.SetActive(false);
                UnusingLinkedList.AddFirst(handleTransform);
                UsingLinkedList.Remove(handleTransform);
            }
        }

        public void PushObjectToPool(Transform handleTransform, float delayTime)
        {
            StartCoroutine(DelayPushObjectToPool(handleTransform,delayTime));
        }

        IEnumerator DelayPushObjectToPool(Transform handleTransform, float delayTime)
        {
            while (delayTime > 0)
            {
                yield return null;
                // If the instance was deactivated while waiting here, just quit
                if (!handleTransform.gameObject.activeInHierarchy)
                {
                    yield break;
                }
                delayTime -= Time.deltaTime;
            }
            PushObjectToPool(handleTransform);
        }

        public void Dispose()
        {
            UsingLinkedList.Clear();
            UnusingLinkedList.Clear();
            UsingLinkedList = null;
            UnusingLinkedList = null;
            prefab = null;
        }
    }
}

