using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace game.Assets
{
    public class AssetSpriteDataWriter 
    {
        public void Save(string path,AssetTarget[] targets)
        {
            FileStream fs = new FileStream(path, FileMode.CreateNew);
            Save(fs, targets);
            
        }

        void Save(Stream stream,AssetTarget[] targets)
        {
            StreamWriter sw = new StreamWriter(stream);

            foreach(var item in targets)
            {
                sw.WriteLine(item.bundleShortName);
                sw.WriteLine(item.dependencies.Count);
                foreach(var child in item.dependencies)
                {
                    sw.WriteLine(child.bundleShortName);
                }
                sw.WriteLine("<--------->");
            }
            sw.Close();
        }
    }
}

