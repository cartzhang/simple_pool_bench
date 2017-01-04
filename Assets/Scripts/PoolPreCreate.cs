
using UnityEngine;
using System.Collections;
using SLQJ_POOL;

[System.Serializable]
public struct PooleObject
{
    public GameObject obj;
    public int count;
} 

public class PoolPreCreate : MonoBehaviour
{
    public PooleObject[] poolObj;

    private int Len;
	// Use this for initialization
	void Awake ()
    {
        Len = poolObj.Length;
        for (int i = 0; i < Len; i++)
        {
            if (null == poolObj[i].obj || poolObj[i].count <= 0)
            {
                continue;
            }
            PoolManager.InitPrefab(poolObj[i].obj, poolObj[i].count);
        }
    }
}
