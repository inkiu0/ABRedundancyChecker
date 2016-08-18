using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Author: inkiu0@gmail.com
/// Date: 2016/07/28
/// Repository: https://github.com/inkiu0/ABRedundancyChecker
/// </summary>
class ABRedundancyChecker
{
    /// <summary>
    /// AB文件名匹配规则
    /// </summary>
    public string searchPattern = "*.ab";
    /// <summary>
    /// 冗余资源类型白名单
    /// </summary>
    public List<Type> assetTypeList = new List<Type> { typeof(Material), typeof(Texture2D), typeof(AnimationClip), typeof(AudioClip), typeof(Sprite), typeof(Shader), typeof(Font), typeof(Mesh) };
    /// <summary>
    /// 输出路径
    /// </summary>
    public string outPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    /// <summary>
    /// AB文件存放路径，会从这个文件夹下递归查找符合查找规则searchPattern的文件。
    /// </summary>
    public string abPath = "Assets/StreamingAssets";

    Dictionary<string, AssetInfo> _AssetMap = new Dictionary<string, AssetInfo>();
    List<string> _FilesList = new List<string>();

    public ABRedundancyChecker()
    {

    }

    [MenuItem("AB冗余检测/AB检测")]
    public static void Launch()
    {
        ABRedundancyChecker checker = new ABRedundancyChecker();
        checker.StartCheck();
    }

    #region CheckAB

    private void StartCheck()
    {
        GetFileListFromFolderPath(_FilesList, abPath);
        byte[] fileBytes = new byte[] { };
        int startIndex = 0;

        EditorApplication.update = delegate ()
        {
            string file = _FilesList[startIndex];

            bool isCancel = EditorUtility.DisplayCancelableProgressBar("AB资源检测中", file, (float)startIndex / (float)_FilesList.Count);

            fileBytes = File.ReadAllBytes(_FilesList[startIndex]);
            try
            {
                AssetBundle ab = AssetBundle.CreateFromMemoryImmediate(fileBytes);
                string[] abFilePathArr = _FilesList[startIndex].Split('/');
                string abFile = File.ReadAllText(_FilesList[startIndex]);
                CheckABInfo(ab, abFilePathArr[abFilePathArr.Length - 1]);
                ab.Unload(true);
            }
            catch (Exception e)
            {
                Debug.Log("<color=red>" + e + "</color>");
            }

            startIndex++;
            if (isCancel || startIndex >= _FilesList.Count)
            {
                ConvertMapToMarkDown();
                _AssetMap = null;
                _FilesList = null;
                Resources.UnloadUnusedAssets();
                GC.Collect();

                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                startIndex = 0;
                Debug.Log("检测结束");
            }
        };
    }

    public void CheckABInfo(AssetBundle ab, string abName)
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        string[] names = ab.GetAllAssetNames();
        string[] dependencies = AssetDatabase.GetDependencies(names);
        string[] allDepen = dependencies.Length > 0 ? dependencies : names;
        Dictionary<string, UnityEngine.Object> assetMap = new Dictionary<string, UnityEngine.Object>();
        for (int i = 0; i < allDepen.Length; ++i)
        {
            UnityEngine.Object obj = ab.LoadAsset(allDepen[i]);
            if (obj != null && assetTypeList.Contains(obj.GetType()))
                TryAddAssetToMap(obj.name, allDepen[i], abName, GetObjectType(obj));
        }
    }

    private void TryAddAssetToMap(string assetName, string assetPath, string abName, string type)
    {
        if (_AssetMap.ContainsKey(assetPath))
        {
            AssetInfo assetInfo = _AssetMap[assetPath];
            if (!assetInfo.referenceABNames.Contains(abName))
            {
                assetInfo.referenceCount += 1;
                assetInfo.referenceABNames += "`" + abName + "` ";
                _AssetMap[assetPath] = assetInfo;
            }
        }
        else
        {
            AddAssetToMap(assetName, assetPath, abName, type);
        }
    }

    private void AddAssetToMap(string assetName, string assetPath, string abName, string type)
    {
        AssetInfo assetInfo = new AssetInfo();
        assetInfo.name = assetName;
        assetInfo.abType = type;
        assetInfo.referenceCount += 1;
        assetInfo.referenceABNames += "`" + abName + "` ";
        _AssetMap.Add(assetPath, assetInfo);
    }

    /// <summary>
    /// 从文件夹中递归读取符合查找规则的文件名
    /// </summary>
    /// <param name="files"></param>
    /// <param name="folder"></param>
    private List<string> GetFileListFromFolderPath(List<string> files, string folder)
    {
        folder = AppendSlash(folder);
        if (files == null)
            files = new List<string>();
        System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(folder);
        foreach (var file in dir.GetFiles(searchPattern))
        {
            files.Add(folder + file.Name);
        }
        foreach (var sub in dir.GetDirectories())
        {
            files = GetFileListFromFolderPath(files, folder + sub.Name);
        }
        return files;
    }

    private string GetObjectType(UnityEngine.Object obj)
    {
        string longType = obj.GetType().ToString();
        string[] longTypeArr = longType.Split('.');
        return longTypeArr[longTypeArr.Length - 1];
    }

    /// <summary>
    /// 在路径后面加上斜杠
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>路径</returns>
    private string AppendSlash(string path)
    {
        if (path == null || path == "")
            return "";
        int idx = path.LastIndexOf('/');
        if (idx == -1)
            return path + "/";
        if (idx == path.Length - 1)
            return path;
        return path + "/";
    }

    struct AssetInfo
    {
        public string name;
        public string abType;
        public int referenceCount;
        public string referenceABNames;
    }

    #endregion

    #region ConvertMapToMarkDown

    private void ConvertMapToMarkDown()
    {
        string path = outPath + "/ABRedundency" + DateTime.Now.ToString("yyMMddHHmm") + ".md";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        using (FileStream fs = File.Create(path))
        {
            AddText(fs, "# ABRedundency_" + DateTime.Now.ToString("yyMMddHHmm") + "  \r\n");
            AddText(fs, "资源名称 | 资源类型 | AB文件数量 | AB文件名\r\n");
            AddText(fs, "---|---|---|---\r\n");
            string single = "";
            string repeat = "";
            foreach (AssetInfo assetInfo in _AssetMap.Values)
            {
                if (assetInfo.referenceCount > 1)
                    repeat += assetInfo.name + "|" + assetInfo.abType + "|" + assetInfo.referenceCount + "|" + assetInfo.referenceABNames + "\r\n";
                else
                    single += assetInfo.name + "|" + assetInfo.abType + "|" + assetInfo.referenceCount + "|" + assetInfo.referenceABNames + "\r\n";
            }
        }
    }

    private void AddText(FileStream fs, string value)
    {
        byte[] info = new UTF8Encoding(true).GetBytes(value);
        fs.Write(info, 0, info.Length);
    }

    #endregion

}
