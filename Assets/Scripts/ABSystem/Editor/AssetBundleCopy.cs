namespace game.Assets
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;

    public partial class AssetBundleBuildPanel
    {
        /// <summary>
        /// 将当前平台模式的资源打进包里，如果外部路径的资源不存在，则需要手动执行以下步骤1
        /// editor in 2018.8.21
        /// </summary>
        [MenuItem("Tool/ABSystem/2_Copy AB to Initial")]
        static void CoypAssetBundles()
        {
            //Clear Streaming assets foldes
            var platform = PathResolver.GetPlatformName();
            var filePath = Path.Combine(Application.streamingAssetsPath, PathResolver.BundleSaveDirName);
            if (Directory.Exists(filePath))
                Directory.Delete(filePath, true);
            Directory.CreateDirectory(filePath);
            //coyp asset bundle
            try
            {
                var dest = Path.Combine(Path.Combine(Application.streamingAssetsPath, PathResolver.BundleSaveDirName), platform).Replace("\\", "/") + "/";
                var res = Path.Combine(Path.Combine(Environment.CurrentDirectory, PathResolver.BundleSaveDirName), platform).Replace("\\", "/") + "/";
                FileUtil.CopyFileOrDirectory(res, dest);
            }
            catch(Exception e)
            {
                Debug.LogError("[Assets]No AssetBundle output folder,try to build first");
            }
            AssetDatabase.Refresh();

            //注意包内的资源会自动把isNative标示自动设为true
            var resPath = PathResolver.INITIAL_PATH + "/" + PathResolver.RESOURCES_MANIFEST_FILE_NAME;
            var resCfg = PathResolver.LoadResourceManifestByPath(resPath);
            if(resCfg != null)
            {
                foreach (var item in resCfg.data.assetbundles.Values)
                    item.isNative = true;
                resCfg.Save(resPath);
            }
            
            Debug.Log("[Assets]AssetBundle Copy Finish");
        }
    }
}

