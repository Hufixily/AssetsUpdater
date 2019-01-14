using System;
using System.Collections;
using System.Collections.Generic;
using game.Assets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace ABL
{
    public class ResLoad : iBindRes
    {
        AssetBundleManager m_assetMgr;
       // Dictionary<string, AssetBundleLoader> m_assets = new Dictionary<string, AssetBundleLoader>();

        public GameObject m_Owner { get; private set; }
        public ResLoad(AssetBundleManager assetMgr,GameObject owner)
        {
            m_Owner = owner;
            m_assetMgr = assetMgr;
        }

        void Release()
        {
            //             foreach (var item in m_assets.Values)
            //             {
            //                 item.bundleManager.RemoveBundle(item.bundleName);
            //             }
        }

        void iBindRes.Reset()
        {
            //Release();
        }

         void iBindRes.Release(string name)
         {
//             var assetPath = GetAssetPath(name);
//             AssetBundleLoader loader = Get(assetPath);
//             if (null == loader)
//                 return;
//             loader.bundleManager.RemoveBundle(loader.bundleName);
//             m_assets.Remove(assetPath);
         }

        //如果资源已加载，直接返回已加载的资源，否则返回null
        T iBindRes.GetAsset<T>(string name)
        {
            return this.GetAsset<T>(name);
        }


        void iBindRes.LoadSprite(string spriteName, Image image)
        {
            this.LoadSprite(spriteName, image);
        }

        //资源加载
        void iBindRes.LoadAsset<T>(string name, System.Action<T> onEnd)
        {
            this.LoadAsset<T>(name, onEnd);
        }

        List<UnityEngine.GameObject> m_infos = new List<UnityEngine.GameObject>();//批量加载缓存数组
        System.Action<List<UnityEngine.GameObject>> m_onEnd;//回调缓存
        void iBindRes.LoadAssets(IList<string> names, System.Action<List<UnityEngine.GameObject>> onEnd)
        {
            m_onEnd = onEnd;
            m_assetMgr.onProgress += LoadAssetsCallback;
            foreach (var name in names)
                LoadAsset(name, (obj) => m_infos.Add(obj));
        }

        void iBindRes.LoadAsset(string name, System.Action<UnityEngine.GameObject> onEnd)
        {
            this.LoadAsset(name, onEnd);
        }


        #region 内部方法，不要修改
        void LoadAssetsCallback(AssetBundleLoadProgress handle)
        {
            if (handle.percent == 1 && null != m_onEnd)
                m_onEnd(m_infos);

            m_assetMgr.onProgress -= LoadAssetsCallback;
            m_infos.Clear();
            m_onEnd = null;
        }

        T GetAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            AssetBundleLoader loader = Get(assetPath);
            if (null == loader)
                return null;
            if(null == loader.bundleInfo)
                return null;
            return GameObject.Instantiate(loader.bundleInfo.mainObject) as T;
        }

        AssetBundleLoader Get(string assetPath)
        {
//             AssetBundleLoader loader;
//             if (m_assets.TryGetValue(assetPath, out loader))
//                 return loader;
            return null;
        }

        //加载GameObject
        void LoadAsset(string name, System.Action<UnityEngine.GameObject> onEnd)
        {
            var assetPath = GetAssetPath(name);
            this.m_assetMgr.Load(assetPath, handle =>
            {
#if UNITY_EDITOR
                try
                {
#endif
                    var go = handle.Instantiate();
                    onEnd?.Invoke(go);
#if UNITY_EDITOR
                }
                catch (System.Exception e)
                {
                    Debug.LogError(string.Format("handle load faild :{0}", assetPath));
                }
#endif
            });
        }

        void LoadAsset<T>(string name, System.Action<T> onEnd) where T : UnityEngine.Object
        {
            var assetPath = GetAssetPath(name);

            this.m_assetMgr.Load(assetPath, handle =>
            {
                if (null == handle)
                    return;
                var obj = handle.LoadAsset<T>(m_Owner, name);
                onEnd(obj);
            });
        }

        void LoadSprite(string spriteName, Image image) 
        {
            var atlasName = GetSpriteName(spriteName);
            var path = string.Format(AssetsUtil.path, atlasName);
            this.m_assetMgr.Load(path, handle =>
            {
                if (null == handle)
                    return;
                SpriteAtlas atlas = handle.LoadAsset<SpriteAtlas>(m_Owner, atlasName);
                Sprite sprite = atlas.GetSprite(spriteName);
                image.sprite = sprite;
            });
        }

        string GetSpriteName(string spriteName)
        {
            return AssetsUtil.GetSpriteName(spriteName);
        }

        string GetAssetPath(string name)
        {
            return AssetsUtil.GetAssetPath(name);
        }

        #endregion
    }
}