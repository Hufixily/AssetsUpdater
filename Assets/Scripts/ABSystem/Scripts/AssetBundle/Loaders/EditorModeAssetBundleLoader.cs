#if UNITY_EDITOR

#if AB_MODE
using System.Collections;

namespace game.Assets
{
    /// <summary>
    /// 编辑器模式并启用AB_MODE下用的加载器
    /// 与iOS的相同，直接加载StreamAssets里的AB
    /// </summary>
    public class EditorModeAssetBundleLoader : IOSAssetBundleLoader
    {
        protected override IEnumerator LoadFromPackage()
        {
            _assetBundleSourceFile = PathResolver.GetBundleSourceFile(bundleName, false);
            return base.LoadFromPackage();
        }
    }
}
#else
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace game.Assets
{
    /// <summary>
    /// 编辑器模式下用的加载器
    /// </summary>
    public class EditorModeAssetBundleLoader : AssetBundleLoader
    {
        class ABInfo : AssetBundleInfo
        {
            public override Object mainObject
            {
                get
                {
                    string newPath = PathResolver.GetEditorModePath(bundleName);
                    Object mainObject = AssetDatabase.LoadMainAssetAtPath(newPath);
                    return mainObject;
                }
            }
        }

        public override void Start()
        {
            bundleManager.StartCoroutine(this.LoadResource());
        }

        private void OnBundleUnload(AssetBundleInfo abi)
        {
            this.bundleInfo = null;
            this.state = LoadState.State_None;
        }

        IEnumerator LoadResource()
        {
            yield return new WaitForEndOfFrame();

            string newPath = PathResolver.GetEditorModePath(bundleName);
            Object mainObject = AssetDatabase.LoadMainAssetAtPath(newPath);
            if (mainObject)
            {
                if (bundleInfo == null)
                {
                    state = LoadState.State_Complete;
                    bundleInfo = bundleManager.CreateBundleInfo(this, new ABInfo());
                    bundleInfo.isReady = true;
                    bundleInfo.onUnloaded = OnBundleUnload;
                }

                Complete();
            }
            else
            {
                state = LoadState.State_Error;
                Error();
            }
        }
// 
//         IEnumerator LoadBundleRes()
//         {
//             yield return new WaitForEndOfFrame();
//             var newPath = string.Format("{0}/{1}", PathResolver.BundleCacheDir, bundleName);
//             if (File.Exists(newPath))
//                 yield return LoadFromCachedFile();
//         }
// 
//          protected virtual IEnumerator LoadFromCachedFile()
//         {
//             if (state != LoadState.State_Error)
//             {
//                 //兼容低版本API
// #if UNITY_4 || UNITY_4_6 || UNITY_5_1 || UNITY_5_2
//                 _bundle = AssetBundle.CreateFromFile(_assetBundleCachedFile);
//                 yield return null;
// #else
//                 AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(_assetBundleCachedFile);
//                 yield return req;
//                 _bundle = req.assetBundle;
// #endif
// 
//                 this.Complete();
//             }
//         }
    }
}
#endif

#endif