using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Author: inkiu0@gmail.com
/// Date: 2016/08/13
/// Repository: https://github.com/inkiu0/ABRedundancyChecker
/// </summary>
class CheckComponent
{
    /// <summary>
    /// 文件名匹配规则
    /// </summary>
    public string searchPattern = "*.prefab";
    /// <summary>
    /// 需要查找的Component的类型
    /// </summary>
    public Type componentType =  typeof(UnityEngine.UI.ContentSizeFitter);
    /// <summary>
    /// 输出路径
    /// </summary>
    public string outPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    /// <summary>
    /// 文件存放路径，会从这个文件夹下递归查找符合查找规则searchPattern的文件。
    /// </summary>
    public string abPath = "Assets";

    Dictionary<string, string> _ResultMap = new Dictionary<string, string>();
    List<string> _FilesList = new List<string>();

    public CheckComponent()
    {

    }

    [MenuItem("CheckComponent/CheckComponent")]
    public static void Launch()
    {
        CheckComponent checker = new CheckComponent();
        checker.StartCheck();
    }

    #region CheckComponent

    private void StartCheck()
    {
        GetFileListFromFolderPath(_FilesList, abPath);
        byte[] fileBytes = new byte[] { };
        int startIndex = 0;
        Component[] _ResultArr = new Component[] { };

        EditorApplication.update = delegate ()
        {
            string file = _FilesList[startIndex];

            bool isCancel = EditorUtility.DisplayCancelableProgressBar("文件检测中", file, (float)startIndex / (float)_FilesList.Count);
            fileBytes = File.ReadAllBytes(_FilesList[startIndex]);
            GameObject obj = AssetDatabase.LoadAssetAtPath(file, typeof(GameObject)) as GameObject;
            try
            {
                if (obj != null)
                {
                    _ResultArr = obj.GetComponentsInChildren(componentType, true);
                    for (int i = 0; i < _ResultArr.Length; ++i)
                    {
                        _ResultMap.Add(file, _ResultArr[i].gameObject.name);
                    }
                }
            }
            catch(Exception e)
            {

            }

            startIndex++;
            if (isCancel || startIndex >= _FilesList.Count)
            {
                ConvertMapToMarkDown();
                _ResultMap = null;
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

    struct PrefabInfo
    {
        public string PrefabName;
        public string GameObjectName;
    }

    #endregion

    #region ConvertMapToMarkDown

    private void ConvertMapToMarkDown()
    {
        string path = outPath + "/Result_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + ".md";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        using (FileStream fs = File.Create(path))
        {
            AddText(fs, "# Result_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "  \r\n");
            AddText(fs, "Prefab名称 | GameObject名称\r\n");
            AddText(fs, "---|---\r\n");
            foreach(var item in _ResultMap)
            {
                AddText(fs, item.Key + "|" + item.Value + "\r\n");
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