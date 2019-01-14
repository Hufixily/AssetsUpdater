namespace game.Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.IO;
    using System.Text;

    public static class JsonExtension 
    {
        public static bool ReadFormFile<T>(ref T data,string fileName)
        {
            try
            {
                if(!string.IsNullOrEmpty(fileName))
                {
                    string str = null;
                    if (File.Exists(fileName))
                        str = File.ReadAllText(fileName);
                    data = LitJson.JsonMapper.ToObject<T>(str);
                    return true;
                }
            }
            catch(System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
            return false;
        }

        public static bool ReadFromString<T>(ref T data, string str) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(str))
                    return false;
                data = LitJson.JsonMapper.ToObject<T>(str);
                return true;
            }
            catch(System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
            return false;
        }

        public static bool WriteToFile(object json,string fileName)
        {
            try
            {
                string text;
                if (WriteToString(json, out text))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                    File.WriteAllText(fileName, text, Encoding.UTF8);
                    return true;
                }
                return false;
            }
            catch(System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
            return false;
        }

        public static bool WriteToString(object json,out string str)
        {
            str = null;

            try
            {
                str = LitJson.JsonMapper.ToJson(json);
                return true;
            }
            catch(System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
            return false;
        }
    }
}

