# ABRedundancyChecker
## 使用方法
### 1. 修改脚本参数
1. 把以下参数改成自己想要的:
```CSharp
/// <summary>
/// AB文件名匹配规则
/// </summary>
public string searchPattern = "*.ab";
/// <summary>
/// 冗余资源类型白名单
/// </summary>
public List<Type> assetTypeList = new List<Type> { typeof(Material), typeof(Texture2D), typeof(AnimationClip),   
typeof(AudioClip), typeof(Sprite), typeof(Shader), typeof(Font), typeof(Mesh) };
/// <summary>
/// 输出路径
/// </summary>
public string outPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
/// <summary>
/// AB文件存放路径，会从这个文件夹下递归查找符合查找规则searchPattern的文件。
/// </summary>
public string abPath = "Assets/StreamingAssets";

[MenuItem("AB冗余检测/AB检测")]
```
### 2. 开始使用
1. 将`ABRedundancyChecker.cs`放在Unity项目的Editor目录下
2. 将所有打包好的AssetBundle文件放在`abPath`目录下
3. 点击菜单栏`AB冗余检测`->`AB检测`
4. 喝一杯茶
  - 250MB的AB文件(1600个文件)检测时间为2分钟
5. 打开输出到目标目录的MarkDown文件  

### 3. 输出的MarkDown形如

| 资源名称 | 资源类型 | AB文件数量 | AB文件名|
|---|---|---|---|
|smoke_01|Texture2D|14|`art_11_1.ab` `art_13_103.ab` `art_13_104.ab` `art_13_107.ab` `art_13_109.ab` `art_13_131.ab` `art_13_132.ab` `art_13_31.ab` `art_13_63.ab` `art_13_77.ab` `art_13_81.ab` `art_13_87.ab` `art_2_128.ab` `art_4_1.ab` |
|wuti_07|Texture2D|12|`art_11_1.ab` `art_12_4.ab` `art_13_102.ab` `art_13_116.ab` `art_13_142.ab` `art_13_17.ab` `art_13_22.ab` `art_13_31.ab` `art_13_59.ab` `art_13_61.ab` `art_13_73.ab` `art_4_1.ab` |
