using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace game.Assets
{
    public static class AssetsUtil
    {
        public static string path = "Assets.Art.Res.Atlas.{0}";
        public static Dictionary<string, List<string>> atlas = new Dictionary<string, List<string>>();

        public static string GetSpriteName(string spriteName)
        {
            foreach (var pair in atlas)
            {
                foreach (var value in pair.Value)
                {
                    if (value.Contains(spriteName.ToLower()))
                        return pair.Key;
                }
            }
            return string.Empty;
        }

        public static Dictionary<string, string> prefabs = new Dictionary<string, string>();
        public static string GetAssetPath(string name)
        {
            var value = string.Empty;
            if (prefabs.TryGetValue(name, out value))
                return value;
            Debug.LogError(string.Format("[AssetPath]{0} no find", name));
            return value;
        }

        public static Dictionary<string,string> configs = new Dictionary<string, string>();
        public static string GetConfigPath(string name)
        {
            var keys = configs.Keys;
            foreach(var key in keys)
            {
                if (key.Contains(name))
                    return configs[key];
            }
            return string.Empty;
        }
        public static string GetConfigName(string name)
        {
            var keys = configs.Keys;
            foreach (var key in keys)
            {
                if (key.Contains(name))
                    return key;
            }
            return string.Empty;
        }
    }

    public abstract class AssetDataReader
    {
        public virtual void Read(Stream fs) { }
    }

    public class AssetSpriteDataReader : AssetDataReader
    {
        public override void Read(Stream fs)
        {
            StreamReader sr = new StreamReader(fs);
            while (true)
            {
                var atlas = sr.ReadLine();
                if (string.IsNullOrEmpty(atlas))
                    break;
                var count = int.Parse(sr.ReadLine());
                var temp = new List<string>();
                for(int i = 0; i < count; i++)
                {
                    temp.Add(sr.ReadLine());
                }
                AssetsUtil.atlas.Add(atlas, temp);
                sr.ReadLine();//skip
            }
            sr.Close();
        }
    }
}
