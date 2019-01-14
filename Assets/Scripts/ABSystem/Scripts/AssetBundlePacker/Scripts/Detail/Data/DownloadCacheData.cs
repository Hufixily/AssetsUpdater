
namespace game.Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    public class DownloadCacheData
    {
        public class AssetBundle
        {
            public string assetbundleName;
            public string hash;
        }
        public Dictionary<string, AssetBundle> assetBundles = new Dictionary<string, AssetBundle>();
    }

    /// <summary>
    /// 下载缓存器，用于断点续传
    /// </summary>
    public class DownloadCache
    {
        public DownloadCacheData data = new DownloadCacheData();
        
        public bool Load(string path)
        {
            return JsonExtension.ReadFormFile<DownloadCacheData>(ref data, path);
        }

        public bool Save(string path)
        {
            return JsonExtension.WriteToFile(data, path);
        }

        /// <summary>
        /// 判断缓存器是否为空
        /// </summary>
        public bool IsEmpty()
        {
            return data == null || data.assetBundles.Count <= 0;
        }

        /// <summary>
        /// 判断资源是否存在
        /// </summary>
        public bool IsExist(string assetbundleName)
        {
            return data.assetBundles.ContainsKey(assetbundleName);
        }

        public string GetHash(string assetbundleName)
        {
            DownloadCacheData.AssetBundle elem;
            if (data.assetBundles.TryGetValue(assetbundleName, out elem))
                return elem.hash;
            return string.Empty;
        }
    }
}

