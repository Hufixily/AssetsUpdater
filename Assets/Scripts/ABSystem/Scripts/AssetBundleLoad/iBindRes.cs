using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using game.Assets;
using game;

namespace ABL
{
    public interface IYield<T> : IEnumerator
    {
        AssetBundleLoader assetInfo { get; }
    }

    public interface IYields<T> : IEnumerator
    {
        AssetBundleLoader[] assets { get; }
    }

    public interface iBindRes 
    {
        //直接获取(未加载的图集会返回空)
        T GetAsset<T>(string name) where T: UnityEngine.Object;
        //加载相关(GameObject)
        void LoadAsset(string name, System.Action<UnityEngine.GameObject> onEnd);
        void LoadAssets(IList<string> name, System.Action<List<UnityEngine.GameObject>> onEnd);
        //加载特定资源
         void LoadAsset<T>(string name, System.Action<T> onEnd) where T : UnityEngine.Object;
        //加载精灵
        void LoadSprite(string spriteName, Image image);

        void Reset();
        void Release(string assetPath);
    }

    public class Factory
    {
        public static iBindRes GetOrCreate(GameObject target)
        {
            //测试案例不提供生成
            //ResLoad ra = new ResLoad(GameApp.my.assetBundleManager, target);
            //return ra;
            return null;
        }
    }
}

