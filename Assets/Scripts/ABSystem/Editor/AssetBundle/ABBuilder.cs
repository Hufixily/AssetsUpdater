using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace game.Assets
{
    public class ABBuilder
    {
        protected AssetBundleDataWriter dataWriter = new AssetBundleDataBinaryWriter();
        //protected PathResolver PathResolver;

        public ABBuilder(/*PathResolver resolver*/)
        {
            //this.PathResolver = resolver;
            this.InitDirs();
            //AssetBundleUtils.PathResolver = PathResolver;
        }

        void InitDirs()
        {
            new DirectoryInfo(PathResolver.BundleSavePath).Create();
            new FileInfo(PathResolver.HashCacheSaveFile).Directory.Create();
        }

        public void Begin()
        {
            EditorUtility.DisplayProgressBar("Loading", "Loading...", 0.1f);
            AssetBundleUtils.Init();
        }

        public void End()
        {
            AssetBundleUtils.SaveCache();
            AssetBundleUtils.ClearCache();
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        public virtual void Analyze()
        {
            var all = AssetBundleUtils.GetAll();
            foreach (AssetTarget target in all)
            {
                target.Analyze();
            }
            all = AssetBundleUtils.GetAll();
            foreach (AssetTarget target in all)
            {
                target.Merge();
            }
            all = AssetBundleUtils.GetAll();
            foreach (AssetTarget target in all)
            {
                target.BeforeExport();
            }
        }

        public virtual void Export()
        {
            this.Analyze();
        }

        public virtual void SaveDepFile()
        {
            this.Analyze();
        }

        public void AddRootTargets(DirectoryInfo bundleDir, string[] partterns = null, SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (partterns == null)
                partterns = new string[] { "*.*" };
            for (int i = 0; i < partterns.Length; i++)
            {
                FileInfo[] prefabs = bundleDir.GetFiles(partterns[i], searchOption);
                foreach (FileInfo file in prefabs)
                {
                    if (file.Extension.Contains("meta"))
                        continue;
                    AssetTarget target = AssetBundleUtils.Load(file);
                    target.exportType = AssetBundleExportType.Root;
                }
            }
        }

        protected void SaveDepAll(List<AssetTarget> all)
        {
            string path = Path.Combine(PathResolver.BundleSavePath, PathResolver.DependFileName);

            if (File.Exists(path))
                File.Delete(path);

            List<AssetTarget> exportList = new List<AssetTarget>();
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                if (target.needSelfExport)
                    exportList.Add(target);
            }
            AssetBundleDataWriter writer = dataWriter;
            writer.Save(path, exportList.ToArray());
        }

        protected void SaveSpriteAll(List<AssetTarget> all)
        {
            var path = Path.Combine(PathResolver.BundleSavePath, PathResolver.SpriteFileName);
            if (File.Exists(path))
                File.Delete(path);

            var exportList = new List<AssetTarget>();
            foreach(var item in all)
            {
                if (!item.bundleShortName.Contains(".spriteatlas"))
                    continue;
                exportList.Add(item);
            }

            var write = new AssetSpriteDataWriter();
            write.Save(path, exportList.ToArray());
        }

        protected void SaveAssetAll(List<AssetTarget> all)
        {
            var path = Path.Combine(PathResolver.BundleSavePath, PathResolver.AssetFileName);

            if (File.Exists(path))
                File.Delete(path);

            var exportList = new List<AssetTarget>();
            foreach (var item in all)
            {
                if (item.bundleShortName.Contains(".prefab") || 
                    item.bundleShortName.Contains(".spriteatlas")||
                    item.bundleShortName.Contains(".unity") ||
                    item.bundleShortName.Contains(".ogg")||
                    item.bundleShortName.Contains(".mp3") ||
                    item.bundleShortName.Contains(".txt"))
                    exportList.Add(item);
            }
            var write = new AssetNameDataWriter();
            write.Save(path, exportList.ToArray());
        }

        public void SetDataWriter(AssetBundleDataWriter w)
        {
            this.dataWriter = w;
        }

        /// <summary>
        /// 删除未使用的AB，可能是上次打包出来的，而这一次没生成的
        /// </summary>
        /// <param name="all"></param>
        protected void RemoveUnused(List<AssetTarget> all)
        {
            HashSet<string> usedSet = new HashSet<string>();
            for (int i = 0; i < all.Count; i++)
            {
                AssetTarget target = all[i];
                if (target.needSelfExport)
                    usedSet.Add(target.bundleName);
            }

            DirectoryInfo di = new DirectoryInfo(PathResolver.BundleSavePath);
            FileInfo[] abFiles = di.GetFiles("*.ab");
            for (int i = 0; i < abFiles.Length; i++)
            {
                FileInfo fi = abFiles[i];
                if (usedSet.Add(fi.Name))
                {
                    Debug.Log("Remove unused AB : " + fi.Name);

                    fi.Delete();
                    //for U5X
                    File.Delete(fi.FullName + ".manifest");
                }
            }
        }

        
    }
}
