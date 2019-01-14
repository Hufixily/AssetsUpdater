using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using game.Assets;
namespace ABL
{
    public class ResLoadElement
    {
        class Data
        {
            public AssetBundleInfo handle;
            public int count = 1;

            public Data(AssetBundleInfo handle) { handle = handle; }

            public void Release()
            {
            }
        }
    }
}
