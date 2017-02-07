using UnityEngine;
using System.Collections.Generic;

public class TestTimeClean : MonoBehaviour
{
    [Header("test 预制体")]
    public GameObject prefab;
    private List<GameObject> objList;
    private GameObject obj;
    // Use this for initialization
    void Start ()
    {
        objList = new List<GameObject>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKey(KeyCode.M))
        {
            obj = gameObject.InstantiateFromPool(prefab);
            objList.Add(obj);
        }

        if (Input.GetKey(KeyCode.R))
        {
            if (objList.Count > 0)
            {
                gameObject.DestroyToPool(objList[0]);
                objList.RemoveAt(0);
            }
        }
    }
}

