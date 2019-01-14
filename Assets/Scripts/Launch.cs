namespace game
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Assets;

    public class Launch : MonoBehaviour
    {
        /// <summary>
        /// 下载地址
        /// </summary>
        public string url = "http://127.0.0.1/";
        public Panel panel;
        /// <summary>
        /// 资源管理器
        /// </summary>
        public AssetBundleManager m_Mgr;
        /// <summary>
        /// 资源更新器
        /// </summary>
        public AssetsUpdater m_Updater;

        GameObject m_testPrefab;

        private void Start()
        {
            StartCoroutine(OnStart());
        }

        IEnumerator OnStart()
        {
            //初始化管理器
            yield return m_Mgr.Launch();
            panel.loadCallback = this.LoadPrefab;
            panel.updateCallback = this.LaunchUpdater;
        }

        void LoadPrefab()
        {
            Load("TestPrefab");
        }

        void LaunchUpdater()
        {
            StartCoroutine(Updater());
        }

        IEnumerator Updater()
        {
            var isUpdating = false;
            m_Updater.StartUpdate(new List<string>() { url });
            m_Updater.OnDone += (updater) => { isUpdating = true; };
            yield return new WaitUntil(() => isUpdating);
            Destroy(m_testPrefab);
            yield return m_Mgr.Relaunch();
            panel.preLoadCallback = this.PreLoadPrefab;
        }

        void PreLoadPrefab()
        {
            Load("TestPrefab");
        }

        void Load(string name)
        {
            var path = AssetsUtil.GetAssetPath(name);//"TestPrefab");
            m_Mgr.Load(path, (handle) =>
            {
                m_testPrefab = handle.Instantiate();
                m_testPrefab.transform.parent = panel.transform;
                m_testPrefab.transform.localPosition = Vector3.zero;
                m_testPrefab.transform.localScale = Vector3.one;
            });
        }
    }
}

