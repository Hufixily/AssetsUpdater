using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace game.Assets
{

    public class AssetNameDataReader : AssetDataReader
    {
        public override void Read(Stream fs)
        {
            StreamReader sr = new StreamReader(fs);
            while (true)
            {
                var name = sr.ReadLine();
                if (string.IsNullOrEmpty(name))
                    break;
                var asset = sr.ReadLine();
#if AB_MODE
                asset = asset.Replace("\\", ".");
#endif
                try
                {
                    if (asset.Contains(".txt"))
                        AssetsUtil.configs.Add(name, asset);
                    else
                        AssetsUtil.prefabs.Add(name, asset);
                }
                catch(System.Exception e)
                {
                    Debug.LogError(string.Format("[Assets]{0} add error", name));
                    Debug.LogError(e.ToString());
                }
            }
            sr.Close();
        }
    }
}
