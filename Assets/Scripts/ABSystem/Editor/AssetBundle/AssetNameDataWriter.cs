using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace game.Assets
{
    public class AssetNameDataWriter 
    {
        public void Save(string path,AssetTarget[] targets)
        {
            FileStream fs = new FileStream(path, FileMode.CreateNew);
            Save(fs, targets);
        }

        void Save(Stream fs,AssetTarget[] targets)
        {
            StreamWriter sw = new StreamWriter(fs);
            foreach (var item in targets)
            {
                sw.WriteLine(item.asset.name);
                sw.WriteLine(item.assetPath);
            }
            sw.Close();
        }
    }
}
