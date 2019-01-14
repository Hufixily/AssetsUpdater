using game.Assets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ABLaunch : MonoBehaviour
{

    /// <summary>
    /// 资源管理器
    /// </summary>
    public AssetBundleManager m_Mgr;

    void Start ()
    {
        StartCoroutine(OnLaunch());
    }
    

    IEnumerator OnLaunch()
    {
        yield return m_Mgr.Launch();
    }

    public void PreLoadPrefab()
    {
        Load("TestPrefab"); 
    }

    void Load(string name)
    {
        var path = AssetsUtil.GetAssetPath(name);//"TestPrefab");
        m_Mgr.Load(path, (handle) =>
        {
            var m_testPrefab = handle.Instantiate();
            //m_testPrefab.transform.parent = panel.transform;
            m_testPrefab.transform.localPosition = Vector3.zero;
            m_testPrefab.transform.localScale = Vector3.one;
        });
    }
}
