


## 一、simpe_pool对象池的问题

在上篇对象池simple_pool中提到了它现在的问题。

一个是数据控制，也就是在单个父亲节点下，只会一直增加来满足当前游戏对对象池内的对象数量的需要，没有考虑到减少。也就是说，若在A阶段，游戏场景中同时需要大量的某个池内O1对象，这时候就出现大量的AO1对象在内存中，但是过了A阶段，不需要A这么多O1对象时候，对象池内没有做操作和优化。

另一个问题，就是多线程的问题。这个暂时不这里讨论。有需要可以自己先对linkedlist加锁。

本片针对第一个问题，来做了处理。

顺便给出上篇simple_pool博客地址：

http://blog.csdn.net/cartzhang/article/details/54096845

https://github.com/cartzhang/simple_pool_bench


## 二、对象池内数量优化思路

本着在后台处理，尽量少影响对象池的使用的原则，决定使用在添加一个线程来实现，对需要清理的池内对象进行记录和判断，然后在主线程中进行删除（destroy）操作.

因为Unity不允许在其他自己建立的线程中调用Destroy函数，所以还是要在Update中进行处理。

![image](https://github.com/cartzhang/simple_pool_bench/blob/master/img/pool_bench_optimize.png)

这个是自己画的流程图，基本就是这个样子。当然里面具体参数，可以根据自己需要调整。

## 三、实现代码

1.首先，建立垃圾清理标记和垃圾对象列表。


 	`
	// garbage clean.
	private static bool clearGarbageFlag = false;
	private static List<Transform> GarbageList = new List<Transform>(100);

	`


然后标记为真，开始清理。这里进行了加锁，避免一边删除，一边添加，这样造成野的对象。垃圾表被清空了，对象也不在对象池内列表中。

2.主线程的清理工作



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


3.优化线程启动

线程在第一次调用池的时候开始启动。然后定时检测数量，每轮会检测checkTimesForEach个对象池内的未使用的情况。对象池个数超出的部分，等待5个中的一个多次检测没有异常后，在加入到检测中，这个主要是为了防止多个对象池内，一次性加入垃圾列表中太多对象，需要一次性删掉的太多，造成主线程卡顿的情况。

当然这也不是最理想的，为了防止卡顿，每次检测循环只检测到一个满足垃圾清理条件，需要处理就会停止检测跳出循环，然后进行垃圾处理。这个也是为了轻量级的删减措施。

一旦成功设置标志，就重新计算和检测。并且在设置后，优化线程等待1秒时间，来让主线程做工作，这个时间应该是很充裕的。


主要代码

```

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

```


按照目前设置参数，开始检测的时间间隔为20秒，若20*2=40秒后，没有需要处理的垃圾，就把检测时间间隔翻倍为40秒检测一次；若在过40*4=160秒，没有触发标志，检测时间进一步延长，逐次翻倍增加，但是最大值为90秒。
也就是说，最大的检测间隔为90秒。

若中间被打断，全部归为正常20秒检测一次。


触发加入垃圾列表的条件：

设置触发标志是一个池，在3次检测中都有超过一半的对象没有被使用，并且整体未使用数量超过10个。


## 四、更新工程分享地址：

github地址：https://github.com/cartzhang/simple_pool_bench

可以下载Assets文件，然后用unity测试。

这个测试demo为 PoolTimeOptimizeOjbectsDemo.unity。

优化流程图下载地址：

https://github.com/cartzhang/simple_pool_bench/blob/master/img/pool_bench_optimize.png

若有问题，请提交问题或代码，非常感谢！！！


## 五、附件

poolManager.cs 全部代码：

    
    
    using UnityEngine;
    using System.Collections.Generic;
    using SLQJ_POOL;
    using UnityEngine.Internal;
    using System.Threading;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
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
    public static GameObject InstantiateFromPool(this GameObject gameobject, Object original, Vector3 position, Quaternion rotation)
    {
    return PoolManager.PullObjcetFromPool(original as GameObject, position, rotation).gameObject;
    }
    /// <summary>
    /// 调用对象池，产生对象
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="original"></param>
    /// <returns></returns>
    public static GameObject InstantiateFromPool(this GameObject gameobject, Object original)
    {
    return PoolManager.PullObjcetFromPool(original as GameObject).gameObject;
    }
    /// <summary>
    /// 对象返回对象池
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="obj"></param>
    public static void DestroyToPool(this GameObject gameobject, Object obj)
    {
    PoolManager.PushObjectPool((obj as GameObject).transform);
    }
    /// <summary>
    /// 带延时的返回对象池
    /// </summary>
    /// <param name="gameobject"></param>
    /// <param name="obj"></param>
    /// <param name="t"></param>
    public static void DestroyToPool(this GameObject gameobject, Object obj, [DefaultValue("0.0F")] float t)
    {
    PoolManager.PushObjectPool((obj as GameObject).transform, t);
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
    
    




