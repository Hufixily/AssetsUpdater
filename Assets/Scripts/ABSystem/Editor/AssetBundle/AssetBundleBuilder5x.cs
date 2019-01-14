#if UNITY_5 || UNITY_2017_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace game.Assets
{
    public class AssetBundleBuilder5x : ABBuilder
    {
        public AssetBundleBuilder5x(): base()
        {

        }

        public override void Export()
        {
            #region 源代码
            base.Export();

            var platform = PathResolver.GetPlatformName();
            var filePath = Path.Combine(Path.Combine(Environment.CurrentDirectory, PathResolver.BundleSaveDirName), platform).Replace("\\", "/") + "/";
            if (Directory.Exists(filePath))
                Directory.Delete(filePath, true);
            Directory.CreateDirectory(filePath);

            List<AssetBundleBuild> list = new List<AssetBundleBuild>();
            //标记所有 asset bundle name
            var all = AssetBundleUtils.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                if (target.needSelfExport)
                {
                    AssetBundleBuild build = new AssetBundleBuild();
                    build.assetBundleName = target.bundleName;
                    build.assetNames = new string[] { target.assetPath };
                    list.Add(build);
                }
            }

            //开始打包
            BuildPipeline.BuildAssetBundles(
                PathResolver.BundleSavePath, 
                list.ToArray(), 
                BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.DeterministicAssetBundle, 
                EditorUserBuildSettings.activeBuildTarget);

#if UNITY_5_1 || UNITY_5_2
             AssetBundle ab = AssetBundle.CreateFromFile(PathResolver.BundleSavePath + PathResolver.GetPlatformForAssetBundles(Application.platform));
#else
            var ab = AssetBundle.LoadFromFile(PathResolver.BundleSavePath +"/"+  PathResolver.GetPlatformName());
#endif
            var manifest = ab.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
            //hash
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                if (target.needSelfExport)
                {
                    Hash128 hash = manifest.GetAssetBundleHash(target.bundleName);
                    target.bundleCrc = hash.ToString();
                }
            }

            this.SaveDepAll(all);
            this.SaveSpriteAll(all);
            this.SaveAssetAll(all);
            this.ExportResourcesManifestFile(manifest);
            ab.Unload(true);
            this.RemoveUnused(all);


            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.Refresh();

            Debug.Log("[Assets]Build Finish!");
            #endregion
        }

        public override void SaveDepFile()
        {
            base.SaveDepFile();
        }

        void ExportResourcesManifestFile(AssetBundleManifest manifest)
        {
            if (manifest == null)
                return;
            var info = new ResourcesManifest();
            //读取所有AB
            //读取主manifest信息
            var rootDir = PathResolver.BUILD_PATH;
            var manifestName = PathResolver.MAIN_MANIFEST_FILE_NAME;
            var desc = new ResourcesManifestData.AssetBundleData();
            desc.assetBundleName = PathResolver.MAIN_MANIFEST_FILE_NAME;
            desc.size = FileHelper.GetFileSize(rootDir + manifestName);
            info.data.assetbundles.Add(manifestName, desc);

            //读取其他AB信息
            foreach(var name in manifest.GetAllAssetBundles())
            {
                desc = new ResourcesManifestData.AssetBundleData();
                desc.assetBundleName = name;
                desc.size = FileHelper.GetFileSize(rootDir + name);
                desc.isNative = true;
                var ab = AssetBundle.LoadFromFile(rootDir + name);
                foreach (var asset in ab.GetAllAssetNames())
                    desc.assets.Add(asset);
                ab.Unload(false);
                info.data.assetbundles.Add(name, desc);
            }

            //读取旧的ResourcesManifest信息，同步一下其他信息
            var oldManiFest = new ResourcesManifest();
            oldManiFest.Load(PathResolver.EDITOR_RESOURCE_MANIFEST_FILE_PATH);
            if(oldManiFest.data != null && oldManiFest.data.assetbundles.Count > 0)
            {
                foreach(var assetBundle in oldManiFest.data.assetbundles.Values)
                {
                    if (string.IsNullOrEmpty(assetBundle.assetBundleName)) continue;
                    if(info.data.assetbundles.ContainsKey(assetBundle.assetBundleName))
                        info.data.assetbundles[assetBundle.assetBundleName].isNative = assetBundle.isNative;
                }
            }
            var buildConfig = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(AssetBundleBuildPanel.savePath);
            info.data.version = buildConfig.version;
            //保存ResourcesInfo
            info.Save(PathResolver.EDITOR_RESOURCE_MANIFEST_FILE_PATH);
        }
    }
}
#endif