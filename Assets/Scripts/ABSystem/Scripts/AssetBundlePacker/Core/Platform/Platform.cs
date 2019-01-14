namespace game.Assets
{
    using UnityEngine;

    public static class Platform 
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        public static string STREAMING_ASSETS_PATH = Application.streamingAssetsPath;
        public static string PERSISTENT_DATA_PATH = Application.dataPath + "/PersistentAssets";
#elif UNITY_IPHONE ||  UNITY_ANDROID
        public static string STREAMING_ASSETS_PATH = Application.streamingAssetsPath;
        public static string PERSISTENT_DATA_PATH = Application.persistentDataPath;
#endif
    }
}

