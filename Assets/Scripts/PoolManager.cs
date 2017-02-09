using UnityEngine;
using System.Collections.Generic;
using SLQJ_POOL;
using UnityEngine.Internal;
using System.Threading;
using System.Linq;

/// <summary>
/// 扩展GameObject函数
/// 使用扩展函数方面代码调用，可以Unity的任意代码中，调用对象池来产生和回池对象，
/// 并且不需要调用命名空间等繁琐东西。
/// 代码例子： obj = gameObject.InstantiateFromPool(prefab);
///  gameObject.DestroyToPool(poolObjs[poolObjs.Count - 1], 0.2f);
///  具体可以参考HowToUse脚本和TestEffect。
///  @cartzhang
/// </summary>
public static class GameObjectExten
{
    /// <summary>
    /// 调用对象池，产生一对象，并带有位置和旋转等参考
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="original"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public static GameObject InstantiateFromPool(this GameObject gameobject, GameObject original, Vector3 position, Quaternion rotation)
    {
        return PoolManager.PullObjcetFromPool(original, position, rotation).gameObject;
    }
    /// <summary>
    /// 调用对象池，产生对象
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="original"></param>
    /// <returns></returns>
    public static GameObject InstantiateFromPool(this GameObject gameobject, GameObject original)
    {
        return PoolManager.PullObjcetFromPool(original).gameObject;
    }
    /// <summary>
    /// 对象返回对象池
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="obj"></param>
    public static void DestroyToPool(this GameObject gameobject, GameObject obj)
    {
        PoolManager.PushObjectPool(obj.transform);
    }
    /// <summary>
    /// 带延时的返回对象池
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="obj"></param>
    /// <param name="t"></param>
    public static void DestroyToPool(this GameObject gameobject, GameObject obj, [DefaultValue("0.0F")] float t)
    {
        PoolManager.PushObjectPool(obj.transform, t);
    }
}

namespace SLQJ_POOL
{
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;
        private static bool bStartThreadOnce = false;
        private static bool isStartThread = false;
        private static Thread tOptimizationThread;

        private static List<GameObject> prefabList = new List<GameObject>();
        //存放预制体对应的id，ObjcetPool
        public static Dictionary<int, ObjectPool> poolManagerDic = new Dictionary<int, ObjectPool>();
        private static Dictionary<Transform, ObjectPool> transformDic = new Dictionary<Transform, ObjectPool>();
        // garbage clean.
        private static bool clearGarbageFlag = false;
        private static List<Transform> GarbageList = new List<Transform>(100);
        private void Update()
        {
            CleanGarbageList();
        }

        private void CleanGarbageList()
        {
            if (clearGarbageFlag)
            {
                clearGarbageFlag = false;
                lock (GarbageList)
                {
                    Debug.Assert(GarbageList.Count > 0);
                    Debug.Log("now destroy " + GarbageList.Count + " from pool" + GarbageList[0].name);
                    for (int i = 0; i < GarbageList.Count; i++)
                    {
                        Destroy(GarbageList[i].gameObject);
                    }
                    GarbageList.Clear();
                }
            }
        }

        //初始化某个预制体对应的对象池
        public static void InitPrefab(GameObject prefab, int initNum = 4)
        {
            GetObjectPool(prefab, initNum);
        }
        //外界调用的接口
        public static Transform PullObjcetFromPool(GameObject prefab)
        {
            return _PullObjcetFromPool(prefab);
        }
        public static Transform PullObjcetFromPool(GameObject prefab, Vector3 pos, Quaternion quaternion)
        {
            return _PullObjcetFromPool(prefab, pos, quaternion);
        }
        private static Transform _PullObjcetFromPool(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.Log("prefab is null!");
                return null;
            }
            ObjectPool objPool = GetObjectPool(prefab);
            StartThreadOnce();
            return objPool.PullObjcetFromPool();
        }

        private static Transform _PullObjcetFromPool(GameObject prefab, Vector3 pos, Quaternion quaternion)
        {
            if (prefab == null)
            {
                Debug.Log("prefab is null!");
                return null;
            }
            ObjectPool objPool = GetObjectPool(prefab, pos, quaternion);
            StartThreadOnce();
            return objPool.PullObjcetFromPool(pos, quaternion);
        }

        private static ObjectPool GetObjectPool(GameObject prefab, int initNum = 4)
        {
            ObjectPool objPool = null;
            //判断集合中是否有预制体对应的对象池
            int leng = prefabList.Count;
            int prefabID = prefab.GetInstanceID();
            for (int i = 0; i < leng; i++)
            {
                if (prefabID == prefabList[i].GetInstanceID())
                {
                    objPool = poolManagerDic[prefabID];
                    break;
                }
            }
            //没有找到对象池的话创建一个对象池
            if (objPool == null)
            {
                objPool = CreatObjcetPool(prefab, initNum);
            }
            return objPool;
        }

        private static ObjectPool GetObjectPool(GameObject prefab, Vector3 pos, Quaternion qua, int initNum = 4)
        {
            ObjectPool objPool = null;
            int leng = prefabList.Count;
            int prefabID = prefab.GetInstanceID();
            for (int i = 0; i < leng; i++)
            {
                if (prefabID == prefabList[i].GetInstanceID())
                {
                    objPool = poolManagerDic[prefabID];
                }
            }
            if (objPool == null)
            {
                objPool = CreatObjcetPool(prefab, pos, qua, initNum);
            }
            return objPool;
        }

        private static ObjectPool CreatObjcetPool(GameObject prefab, Vector3 pos, Quaternion qua, int initNum)
        {
            prefabList.Add(prefab);
            GameObject go = new GameObject();
            go.name = prefab.name + "Pool";
            ObjectPool objPool = go.AddComponent<ObjectPool>();
            objPool.InitObjectPool(prefab, pos, qua, transformDic, initNum);
            poolManagerDic.Add(prefab.GetInstanceID(), objPool);
            return objPool;
        }

        private static ObjectPool CreatObjcetPool(GameObject prefab, int initNum)
        {
            prefabList.Add(prefab);
            GameObject go = new GameObject();
            go.name = prefab.name + "Pool";
            ObjectPool objPool = go.AddComponent<ObjectPool>();
            objPool.InitObjectPool(prefab, transformDic, initNum);
            poolManagerDic.Add(prefab.GetInstanceID(), objPool);
            return objPool;
        }

        public static void PushObjectPool(Transform handleTransform)
        {
            ObjectPool objPool = GetPoolByTransform(handleTransform);
            if (objPool)
            {
                objPool.PushObjectToPool(handleTransform);
            }
            else
            {
                GameObject.Destroy(handleTransform.gameObject);
            }
        }
        public static void PushObjectPool(Transform handleTransform, float delayTime)
        {
            ObjectPool objPool = GetPoolByTransform(handleTransform);
            if (objPool)
            {
                objPool.PushObjectToPool(handleTransform, delayTime);
            }
            else
            {
                GameObject.Destroy(handleTransform.gameObject, delayTime);
            }
        }
        //立即回池的接口
        public static void PushObjectPool(Transform handleTransform, GameObject prefab)
        {
            ObjectPool objPool = GetObjectPool(prefab);
            objPool.PushObjectToPool(handleTransform.transform);
        }
        //延迟回池的接口
        public static void PushObjectPool(Transform handleTransform, GameObject prefab, float delayTime)
        {
            ObjectPool objPool = GetObjectPool(prefab);
            objPool.PushObjectToPool(handleTransform, delayTime);
        }

        private static ObjectPool GetPoolByTransform(Transform handleTransform)
        {
            if (transformDic.ContainsKey(handleTransform))
            {
                return transformDic[handleTransform];
            }
            Debug.LogError(handleTransform.name + " no find it's ObjectPool");
            return null;
        }
        // add code to clean pool from time to time.
        private static void StartThreadOnce()
        {
            // start thread to clean pool from time to time.
            if (!bStartThreadOnce)
            {
                bStartThreadOnce = true;
                ThreadPool.QueueUserWorkItem(AutoToCheckOptimization);
            }
        }

        private static void AutoToCheckOptimization(object obj)
        {
            Thread.Sleep(10);
            isStartThread = true;
            tOptimizationThread = new Thread(OptimizationPool);
            tOptimizationThread.Start();
        }

        private static void OptimizationPool()
        {
            // check cpu time to start
            Thread.Sleep(100);
            // 检测间隔时间20秒
            float intervalTimeTodetect = 20f;
            // after how many times counts to reset count.
            // 循环检测多少次，后记录清零。每次只处理需要处理的前5个池。
            const int checkTimesForEach = 5;
            // 临时池管理对象
            Dictionary<int, ObjectPool> poolManagerTempDic = new Dictionary<int, ObjectPool>();            
            System.DateTime timeCount = System.DateTime.Now;
            // 间隔时间内执行一次
            bool eachMinuteGetDicOnce = false;
            // 每个池未使用对象超过一半的标记，记录次数
            Dictionary<int, int> CurrentPoolUnuseCount = new Dictionary<int, int>();
            // 检测刷新次数，也是一次计数的最大时间。
            int icountLoopTime = 0;
            Debug.Log("Thread start");
            // 休眠时间
            int sleepTime = 10;
            while (isStartThread)
            {
                Thread.Sleep(sleepTime);
                if (!eachMinuteGetDicOnce)
                {
                    eachMinuteGetDicOnce = true;
                    poolManagerTempDic = poolManagerDic.ToDictionary(entry => entry.Key,entry => entry.Value);
                    // loop check 3 time to reset.
                    if (icountLoopTime % checkTimesForEach == 0)
                    {
                        CurrentPoolUnuseCount.Clear();
                        icountLoopTime = icountLoopTime > 10000000 ? 0 : icountLoopTime;
                    }

                    // mark unuse nuber for all.
                    foreach(var element in poolManagerTempDic)
                    {
                        ObjectPool opool = element.Value;
                        int unusinglinkCount = opool.UnusingLinkedList.Count;
                        // half of all is useless and more than 10.
                        if (unusinglinkCount * 2 > unusinglinkCount + opool.UsingLinkedList.Count && unusinglinkCount > 10)
                        {   
                            MarkCountForUnusingLink(ref CurrentPoolUnuseCount, element.Key);
                            // satisfy the condition,add unusing link gameobject to garbagelist.
                            int currentMark = 0;
                            CurrentPoolUnuseCount.TryGetValue(element.Key,out currentMark);
                            // be marked three times, add to garbage list.
                            if (currentMark >= 3)
                            {
                                AddObjectsToGarbageList(ref opool.UnusingLinkedList);
                                // count tick to reset.
                                CurrentPoolUnuseCount[element.Key] = 0;
                                clearGarbageFlag = true;
                                // each time only gathing one pool to process.
                                break;
                            }
                        }
                    }
                }
                // leave time for mainthread to delete gameobjects.
                if (clearGarbageFlag)
                {
                    icountLoopTime = 0;
                    intervalTimeTodetect = 20f;
                    Thread.Sleep(1000);
                    timeCount = System.DateTime.Now;
                }

                // interval 20 seconds to start check once;
                if ((System.DateTime.Now - timeCount).TotalSeconds > intervalTimeTodetect)
                {
                    timeCount = System.DateTime.Now;
                    eachMinuteGetDicOnce = false;
                    poolManagerTempDic.Clear();
                    icountLoopTime++;
                    Debug.Log("Loop count is " + icountLoopTime);
                }
                // long time nothing happen, expand the detective interval time (max <= 90s).
                if (icountLoopTime >= 4 )
                {   
                    intervalTimeTodetect = intervalTimeTodetect * 2 >= 90f ? 90f : intervalTimeTodetect * 2;
                    icountLoopTime = 0;
                    Debug.Log("interval time is " + intervalTimeTodetect);
                }
            }
            return;
        }
        
        private static void MarkCountForUnusingLink(ref Dictionary<int, int> poolUnuseCount,int prefabGuid)
        {
            Debug.Assert(null != poolManagerDic);
            int currentMark = 0;
            if (poolUnuseCount.ContainsKey(prefabGuid))
            {
                poolUnuseCount.TryGetValue(prefabGuid, out currentMark);
            }
            currentMark++;
            if (poolUnuseCount.ContainsKey(prefabGuid))
            {
                poolUnuseCount[prefabGuid] = currentMark;
            }
            else
            {
                poolUnuseCount.Add(prefabGuid, currentMark);
            }
        }
        
        /// <summary>
        /// add last gameobject to garbagelist,as when unsing unusinglink is from first place to get.
        /// </summary>
        /// <param name="list"></param>
        private static void AddObjectsToGarbageList(ref LinkedList<Transform> list)
        {   
            Debug.Assert(list.Count > 0);            
            int FlagDestroyNumber = list.Count>>1;
            for (int i = 0; i < FlagDestroyNumber; i++)
            {
                GarbageList.Add(list.Last.Value);
                list.RemoveLast();
            }
        }

        public void Dispose()
        {
            isStartThread = false;
        }
    }
}

