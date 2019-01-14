namespace game.Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    //资源清单
    public class ResourcesManifestData
    {
        public class AssetBundleData
        {
            public string assetBundleName;//资源名称
            public List<string> assets = new List<string>();//资源列表
            public long size;//ab包大小
            public bool isNative;//是否原始资源
        }

        public uint version;
        public Dictionary<string, AssetBundleData> assetbundles = new Dictionary<string, AssetBundleData>();
    }

    public class ResourcesManifest
    {
        public ResourcesManifestData data;
        public Dictionary<string, List<string>> assetTable;//资源查询表

        public ResourcesManifest()
        {
            data = new ResourcesManifestData();
        }

        public bool Load(string fileName)
        {
            var result = JsonExtension.ReadFormFile<ResourcesManifestData>(ref data, fileName);
            if (result)
                Build();
            return result;
        }

        public bool Save(string fileName)
        {
            return JsonExtension.WriteToFile(data, fileName);
        }

        /// <summary>
        /// 建立资源查询表
        /// </summary>
        private void Build()
        {
            assetTable = new Dictionary<string, List<string>>();
            if(data.assetbundles != null)
            {
                var itor = data.assetbundles.Values.GetEnumerator();
                while(itor.MoveNext())
                {
                    List<string> list = itor.Current.assets;
                    foreach(var item in list)
                    {
                        if(!assetTable.ContainsKey(item))
                        {
                            assetTable.Add(item, new List<string>());
                        }
                        assetTable[item].Add(itor.Current.assetBundleName);
                    }
                }
                itor.Dispose();
            }
        }
        /// <summary>
        /// 获取一个AB数据
        /// </summary>
        public ResourcesManifestData.AssetBundleData Find(string assetbundleName)
        {
            if (data == null) return null;
            if (data.assetbundles == null) return null;
            if (data.assetbundles.Count == 0) return null;
            if (!data.assetbundles.ContainsKey(assetbundleName))
                return null;
            return data.assetbundles[assetbundleName];
        }

        public string[] GetAllAssetBundleName(string asset)
        {
            if (assetTable == null) return null;
            if (!assetTable.ContainsKey(asset)) return null;
            return assetTable[asset].ToArray();
        }
    }
}

