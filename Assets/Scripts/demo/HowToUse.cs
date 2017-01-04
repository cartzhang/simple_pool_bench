using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// 使用按键G来从对象池里取得对象
/// 使用按键R把对象放回到对象池中。
/// </summary>
public class HowToUse : MonoBehaviour
{
    [Header("use G button to use prefab,and use R button to return to pool")]
    public int a;
    [Header("test 预制体")]
    public GameObject prefab;
    private bool isShowUITips = true;
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
        if (Input.GetKeyDown(KeyCode.G))
        {
            obj = gameObject.InstantiateFromPool(prefab);
            obj.transform.position = sphereCollider.center + Random.insideUnitSphere * Random.Range(0, sphereCollider.radius);
            if (!poolObjs.Contains(obj))
            {
                poolObjs.Add(obj);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (poolObjs.Count - 1 >= 0 && poolObjs[poolObjs.Count - 1])
            {
                gameObject.DestroyToPool(poolObjs[poolObjs.Count - 1], 0.2f);
                poolObjs.Remove(poolObjs[poolObjs.Count - 1]);
            }
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            isShowUITips = !isShowUITips;
        }
    }
    /// <summary>
    /// 显示提示信息
    /// </summary>
    public void OnGUI()
    {
        if (isShowUITips)
        {
            string content = " 使用F1 按键来切换UI提示的隐藏和显示;\n 使用按键G来从对象池里取得对象;  使用按键R把对象放回到对象池中";
            GUI.TextArea(new Rect(10, 10, 350, 60), content);
            content = "使用按键H来从对象池里取得对象,达到一定数量开始，启用一边回池，一边产生对象的测试";
            GUI.TextArea(new Rect(10, 70, 350, 40), content);
        }
    }
}
