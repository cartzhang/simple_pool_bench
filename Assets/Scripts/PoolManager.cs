using UnityEngine;
using System.Collections.Generic;
using SLQJ_POOL;
using UnityEngine.Internal;

/// <summary>
/// 扩展GameObject函数
/// 使用扩展函数方面代码调用，可以Unity的任意代码中，调用对象池来产生和回池对象，
/// 并且不需要调用命名空间等繁琐东西。
/// 代码例子： obj = gameObject.InstantiateFromPool(prefab);
///  gameObject.DestroyToPool(poolObjs[poolObjs.Count - 1], 0.2f);
///  具体可以参考HowToUse脚本和TestEffect。
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
        private Transform myTransform;
        private static PoolManager instance;

        private static List<GameObject> prefabList = new List<GameObject>();
        //存放预制体对应的id，ObjcetPool
        public static Dictionary<int, ObjectPool> poolManagerDic = new Dictionary<int, ObjectPool>();
        private static Dictionary<Transform, ObjectPool> transformDic = new Dictionary<Transform, ObjectPool>();

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
                GameObject.Destroy(handleTransform.gameObject,delayTime);
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
    }
}
