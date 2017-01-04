/************************************************************************/
/* 测试对象池，一边生成，一边回池。
 * 这里的回池时间测试，并没有什么具体意义。
 * 这里并没有考虑，多线程使用情况。
 * @cartzhang
 */
/************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class TestEffect : MonoBehaviour
{
    [Header("test 预制体")]
    public GameObject prefab;
    public int TestNumber = 100;

    private GameObject obj;
    private SphereCollider sphereCollider;
    private List<GameObject> poolObjs;

    void Start()
    {
        poolObjs = new List<GameObject>();
        sphereCollider = GameObject.Find("Box").GetComponent<Collider>() as SphereCollider;
    }
  
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(CreatePrefabs(TestNumber));
        
            StartCoroutine(ReturnToPool());
        }
        //退出游戏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
    /// <summary>
    /// 对象创建测试
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    IEnumerator CreatePrefabs(int count)
    {
        float startTime = Time.realtimeSinceStartup;
        Debug.Log("Current start time: " + startTime);
        int tmpCount = count;
        while (count > 0)
        {
            yield return null; 
            obj = gameObject.InstantiateFromPool(prefab);
            obj.transform.position = sphereCollider.center + Random.insideUnitSphere * Random.Range(0, sphereCollider.radius);
            if (!poolObjs.Contains(obj))
            {
                poolObjs.Add(obj);
            }
            //
            count--;
        }        
        float EndTime = Time.realtimeSinceStartup;
        Debug.Log(tmpCount + " times to create prefab " + " take times: " + (EndTime - startTime));
        yield break;
    }
    /// <summary>
    /// 对象回池测试
    /// </summary>
    /// <returns></returns>
    IEnumerator ReturnToPool()
    {
        int count = poolObjs.Count;
        int recordnumber = poolObjs.Count;
        yield return new WaitWhile(() => count >= 15);
        float startTime = Time.realtimeSinceStartup;
        Debug.Log("Current return pool start time: " + startTime);
        //
        while (poolObjs.Count - 1 >= 0 && poolObjs[poolObjs.Count - 1])
        {
            yield return null;
            gameObject.DestroyToPool(poolObjs[poolObjs.Count - 1], 0.2f);
            poolObjs.Remove(poolObjs[poolObjs.Count - 1]);
        }
        float EndTime = Time.realtimeSinceStartup;
        Debug.Log(recordnumber + " times to return prefab " + " take times: " + (EndTime - startTime));
        yield break;
    }
}
