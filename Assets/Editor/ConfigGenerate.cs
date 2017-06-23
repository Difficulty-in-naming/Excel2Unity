using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Config
{
    public class ConfigGenerate : AssetPostprocessor
    {
        private const int mStartLine = 3;
        private static readonly string mNameSpace = "QiYuan.Config";
        private static readonly string mAssembly = ",Assembly-CSharp";
        private static string mLoadPath;
        private static string mScriptSavePath;
        private static string mCodeTemplatePath;
        public static string AssetSavePath = "Assets/Resources/ConfigAssets/";

        internal static Dictionary<string, string> TypeConverter = new Dictionary<string, string>
        {
            {"int", "System.Int32"},
            {"string", "System.String"},
            {"bool", "System.Boolean"},
            {"float", "System.Single"},
            {"long", "System.Int64"},
            {"int[]", "System.Int32[]"},
            {"long[]", "System.Int64[]"},
            {"bool[]", "System.Boolean[]"},
            {"string[]", "System.String[]"},
            {"float[]", "System.Float[]"}
        };

        internal static Dictionary<string, Func<string[], object>> ValueConverter = new Dictionary<string, Func<string[], object>>
        {
            {"System.Int32", str => str.Length > 0 ? Convert.ToInt32(str[0]) : 0},
            {"System.String", str => str.Length > 0 ? str[0] : ""},
            {"System.Boolean", str => str.Length > 0 && Convert.ToBoolean(str[0])},
            {"System.Single", str => str.Length > 0 ? Convert.ToSingle(str[0]) : 0},
            {"System.Int64", str => str.Length > 0 ? Convert.ToInt64(str[0]) : 0},
            {"System.Int32[]", str => str.Length > 0 ? Array.ConvertAll(str, int.Parse) : new int[0]},
            {"System.Int64[]", str => str.Length > 0 ? Array.ConvertAll(str, long.Parse) : new long[0]},
            {"System.Boolean[]", str => str.Length > 0 ? Array.ConvertAll(str, bool.Parse) : new bool[0]},
            {"System.String[]", str => str.Length > 0 ? str : new string[0]},
            {"System.Float[]", str => str.Length > 0 ? Array.ConvertAll(str, float.Parse) : new float[0]}
        };


        private static readonly string mWaitLoadCsvEvent = "WaitLoadExcel";
        private static readonly string mGenerateWrong = "GenerateExcelWrong";

        public static string LoadPath
        {
            get { return mLoadPath ?? (mLoadPath = Application.dataPath + "/Excel/"); }
        }

        public static string ScriptSavePath
        {
            get { return mScriptSavePath ?? (mScriptSavePath = Application.dataPath + "/Script/Config/"); }
        }

        public static string CodeTemplatePath
        {
            get { return mCodeTemplatePath ?? (mCodeTemplatePath = Application.dataPath + "/Editor/ConfigTemplate.txt"); }
        }

        [MenuItem("Tools/Config Generate/生成所有的配置表", priority = 1)]
        public static void GenerateConfig()
        {
            EditorPrefs.SetString(mGenerateWrong, "");
            var dir = new DirectoryInfo(LoadPath);
            foreach (var node in dir.GetFiles("*.xlsx"))
            {
                if (node.Name.StartsWith("~$"))
                    continue;
                LoadCsv(node.Name.Remove(node.Name.Length - 5));
            }
        }

        [MenuItem("Tools/Config Generate/生成上次出错的配置表", priority = 0)]
        public static void GenerateWrongConfig()
        {
            var wrong = EditorPrefs.GetString(mGenerateWrong, "").Split(new []{","},StringSplitOptions.RemoveEmptyEntries);
            foreach (var node in wrong)
            {
                LoadCsv(node);
            }
            Debug.Log("生成结束！！");
        }

        /// <summary>
        /// 当Excel文件受到修改则进行重新加载
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<string> fileName = new List<string>();
            foreach (var node in importedAssets)
            {
                if(node.EndsWith(".xlsx"))
                    fileName.Add(node.Remove(node.Length - 5).Substring(node.LastIndexOf('/') + 1));
            }
            for (int i = 0; i < fileName.Count; i++)
            {
                LoadCsv(fileName[i]);
            }
        }

        private static void LoadCsv(string fileName)
        {
            try
            {
                string csvContent;
                var csvFile = LoadPath + fileName + ".xlsx";
                try
                {
                    csvContent = File.ReadAllText(csvFile, Encoding.Default);
                }
                catch
                {
                    Debug.LogError(fileName + "进程被锁定,请关闭文件后重新加载");
                    return;
                }
                if (string.IsNullOrEmpty(csvContent))
                {
                    Debug.LogError(fileName + "文件为空");
                    return;
                }
                EditorUtility.DisplayProgressBar("Excel2Unity", "正在加载Excel表 : " + fileName + ".xlsx", 0);
                var varName = new List<string>(); //获得所有变量的名称
                var varType = new List<string>(); //获得所有变量的类型

                using (var stream = new FileStream(csvFile, FileMode.OpenOrCreate))
                {
                    var workbook = new XSSFWorkbook(stream);
                    var sheet = workbook[0]; //只读取Excel表中的第一个表
                    //从1开始,第0行是策划用来写备注的地方第1行为程序使用的变量名,第2行为变量类型
                    var i = 1;
                    for (; i < mStartLine; i++)
                    {
                        var r = sheet.GetRow(i);
                        if (i == 1)
                            r.Cells.ForEach(t1 => varName.Add(t1.ToString()));
                        else if (i == 2)
                            r.Cells.ForEach(t1 => varType.Add(t1.ToString()));
                        EditorUtility.DisplayProgressBar("Excel2Unity", "正在加载表头 : " + fileName + ".xlsx", (float) i / sheet.LastRowNum);
                    }

                    var customClass = AnalysisCustomClass(varType, varName);

                    //搜索脚本如果脚本返回空置则等待编译结束再次调用此函数
                    var scriptType = SearchAndCreateScript(fileName, varType, varName, customClass, false);
                    if (scriptType == null)
                    {
                        EditorPrefs.SetString(mWaitLoadCsvEvent, EditorPrefs.GetString(mWaitLoadCsvEvent, "") + fileName + ",");
                        return;
                    }
                    //检查变量是否存在如果不存在则重新编译脚本
                    if (VariableModify(varName, varType, scriptType))
                    {
                        //强制生成新的脚本
                        SearchAndCreateScript(fileName, varType, varName, customClass, true);
                        return;
                    }
                    //检查自定义类型变量是否存在如果不存在则重新编译脚本
                    for (var j = 0; j < customClass.Count; j++)
                    {
                        var cls = customClass[j];
                        var t = Type.GetType(mNameSpace + "." + cls.ClassName + mAssembly);
                        if (VariableModify(cls.Name, cls.Type, t))
                        {
                            //强制生成新的脚本
                            SearchAndCreateScript(fileName, varType, varName, customClass, true);
                            return;
                        }
                    }

                    var scriptable = GetAssetFile(fileName);
                    var baseType = scriptable.GetType().BaseType;
                    var fieldInfo = baseType.GetField("mConfig", BindingFlags.NonPublic | BindingFlags.Instance);
                    var list = (IList) fieldInfo.GetValue(scriptable);
                    list.Clear();
                    var propertyType = list.GetType().GetGenericArguments()[0];
                    //把值写入脚本当中
                    for (; i <= sheet.LastRowNum; i++)
                    {
                        var instance = Activator.CreateInstance(propertyType);
                        var reflection = instance.GetType();
                        var row = sheet.GetRow(i);
                        for (var n = 0; n < row.Cells.Count; n++)
                        {
                            var cell = row.Cells[n];
                            if (varName.Count > cell.ColumnIndex)
                            {
                                var f = reflection.GetProperty(varName[cell.ColumnIndex]);
                                if (ValueConverter.ContainsKey(f.PropertyType.FullName)) //常规类型可以使用这种方法直接转换
                                {
                                    var attr = SplitData(cell.ToString());
                                    f.SetValue(instance, ValueConverter[f.PropertyType.FullName](attr), null);
                                }
                                else
                                {
                                    //自定义类型序列化
                                    var nestedInstance = Activator.CreateInstance(f.PropertyType);
                                    var ct = nestedInstance.GetType();
                                    var attr = SplitSpData(cell.ToString());
                                    foreach (var node in attr)
                                    {
                                        var nestedProperty = ct.GetProperty(node.Key);
                                        nestedProperty.SetValue(nestedInstance, ValueConverter[nestedProperty.PropertyType.FullName](node.Value), null);
                                    }
                                    f.SetValue(instance, nestedInstance, null);
                                }
                            }
                        }
                        list.Add(instance);
                        var process = (float) i / sheet.LastRowNum;
                        EditorUtility.DisplayProgressBar("Excel2Unity", "正在加载写入数据 : " + fileName + ".xlsx" + "   (" + Mathf.CeilToInt(process * 100) + "%)",process);
                    }
                    Debug.Log("数据库  " + fileName + "  写入成功");
                }
            }
            catch (Exception e)
            {
                EditorPrefs.SetString(mGenerateWrong, EditorPrefs.GetString(mGenerateWrong) + fileName + ",");
                Debug.LogError(fileName + "加载失败 原因:" + e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static bool VariableModify(List<string> varName, List<string> varType, Type scriptType)
        {
            for (var i = 0; i < varName.Count; i++)
            {
                var property = scriptType.GetProperty(varName[i]);
                if (property == null)
                {
                    Debug.LogError("检查到新的变量,请等待重新生成脚本");
                    //名字不存在强制调用创建脚本
                    return true;
                }

                //检查类型是否存在
                {
                    //如果能找到这个类型说明是基础类型
                    if (TypeConverter.ContainsKey(varType[i]))
                    {
                        if (TypeConverter[varType[i]] != property.PropertyType.FullName)
                        {
                            Debug.LogError("检查到变量类型发生变化,请等待重新生成脚本");
                            //类型不同强制调用创建脚本
                            return true;
                        }
                    }
                    else //找不到则从自定义类型当中搜索
                    {
                        if (mNameSpace + "." + varType[i] != property.PropertyType.FullName) //找不到指定类型,这个是新加入的自定义类型
                        {
                            Debug.LogError("检查到变量类型发生变化,请等待重新生成脚本");
                            //类型不同强制调用创建脚本
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///     拆分格子数据
        /// </summary>
        private static string[] SplitData(string data)
        {
            data = Regex.Replace(data, "\"\"", "\"");
            var count = 0;
            var stringData = data;
            if (data.StartsWith("\""))
                stringData = stringData.Remove(stringData.Length - 1, 1).Remove(0, 1);
            var sb = new StringBuilder();
            var dataList = new List<string>();
            for (var i = 0; i < stringData.Length; i++)
            {
                if (stringData[i] == '\"')
                {
                    count++;
                }
                else if (count % 2 == 0 && stringData[i] == ',')
                {
                    count = 0;
                    dataList.Add(sb.ToString());
                    sb = new StringBuilder();
                    continue;
                }
                sb.Append(stringData[i]);
                if (i == stringData.Length - 1)
                    dataList.Add(sb.ToString());
            }
            return dataList.ToArray();
        }

        /// <summary>
        ///     拆分自定义类型的格子数据(返回字典,key为变量名字,Value为变量的数值)
        /// </summary>
        private static Dictionary<string, string[]> SplitSpData(string data)
        {
            data = Regex.Replace(data, "\"\"", "\"");
            var count = 0;
            var stringData = data;
            if (data.StartsWith("\""))
                stringData = stringData.Remove(stringData.Length - 1, 1).Remove(0, 1);
            var sb = new StringBuilder();
            var dataList = new Dictionary<string, string[]>();
            var isGroup = false;
            var variableName = "";
            for (var i = 0; i < stringData.Length; i++)
            {
                //检查包围盒,包围盒内所有关键字都会失效,直到结束
                if (stringData[i] == '@')
                {
                    if (stringData.Length > i + 1 && stringData[i + 1] == '[')
                    {
                        isGroup = true;
                        i += 1;
                        continue;
                    }
                }
                else if (stringData[i] == ']')
                {
                    if (stringData.Length > i + 1 && stringData[i + 1] == '@')
                    {
                        dataList.Add(variableName, SplitData(sb.ToString().TrimStart(' ').Replace("\r", "")));
                        isGroup = false;
                        i += 1;
                        variableName = null;
                        sb = new StringBuilder();
                        continue;
                    }
                }
                if (!isGroup)
                {
                    if (stringData[i] == '=')
                    {
                        variableName = sb.ToString().TrimEnd(' ').Replace("\r", "");
                        sb = new StringBuilder();
                        continue;
                    }
                    if (stringData[i] == '\"')
                    {
                        count++;
                    }
                    else if (count % 2 == 0 && stringData[i] == ',')
                    {
                        if (!string.IsNullOrEmpty(variableName))
                        {
                            count = 0;
                            dataList.Add(variableName, new[] {sb.ToString()});
                        }
                        sb = new StringBuilder();
                        continue;
                    }
                }
                sb.Append(stringData[i]);
                if (i == stringData.Length - 1)
                    if (variableName != null) dataList.Add(variableName, new[] {sb.ToString()});
            }
            return dataList;
        }

        /// <summary>
        ///     创建UnityAsset
        /// </summary>
        private static ScriptableObject GetAssetFile(string fileName)
        {
            var filePath = AssetSavePath + fileName + ".asset";
            var scriptType = Type.GetType(mNameSpace + "." + fileName + mAssembly);
            var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
            if (obj == null)
            {
                var data = ScriptableObject.CreateInstance(scriptType);
                AssetDatabase.CreateAsset(data, filePath);
                obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(filePath);
            }
            return obj;
        }

        /// <summary>
        ///     创建脚本,如果脚本已经存在则返回类型
        ///     focus表示是否强制创建脚本
        /// </summary>
        private static Type SearchAndCreateScript(string fileName, List<string> varType, List<string> varName, List<CustomClass> customClass, bool focus)
        {
            Type t = null;
            if (focus == false)
                t = Type.GetType(mNameSpace + "." + fileName + "Property" + mAssembly);
            if (t == null)
            {
                Debug.LogError("脚本不存在,等待Unity编译结束,重新写入");
                var str = new Generate(CodeTemplatePath, (info, g) =>
                {
                    if (info.PlaceHolder == "CustomClass")
                        for (var i = 0; i < customClass.Count; i++)
                        {
                            var custom = customClass[i];
                            g.SetReplace("Class", custom.ClassName);
                            g.SetReplace("Attribute", custom.Attribute);
                            g.BeginGroup("NestedField");
                            for (var j = 0; j < custom.Name.Count; j++)
                            {
                                g.SetReplace("Name", custom.Name[j]);
                                g.SetReplace("Type", custom.Type[j]);
                                g.Apply();
                            }
                            g.EndGroup();
                            g.BeginGroup("NestedProperty");
                            for (var j = 0; j < custom.Name.Count; j++)
                            {
                                g.SetReplace("Name", custom.Name[j]);
                                g.SetReplace("Type", custom.Type[j]);
                                g.Apply();
                            }
                            g.EndGroup();
                            g.Apply();
                        }
                    if (info.PlaceHolder == "CoreClass")
                    {
                        g.SetReplace("Class", fileName);
                        g.SetReplace("Attribute", "");
                        g.BeginGroup("Field");
                        for (var i = 0; i < varName.Count; i++)
                        {
                            g.SetReplace("Name", varName[i]);
                            g.SetReplace("Type", varType[i]);
                            g.Apply();
                        }
                        g.EndGroup();
                        g.BeginGroup("Property");
                        for (var i = 0; i < varName.Count; i++)
                        {
                            g.SetReplace("Name", varName[i]);
                            g.SetReplace("Type", varType[i]);
                            g.Apply();
                        }
                        g.EndGroup();
                        g.Apply();
                    }
                    if (info.PlaceHolder == "ScriptableObject")
                    {
                        g.SetReplace("Class", fileName);
                        g.Apply();
                    }
                }).StartWrite();
                str = Generate.FormatScript(str);
                File.WriteAllText(ScriptSavePath + fileName + ".cs", str);
                AssetDatabase.Refresh();
            }
            return t;
        }

        /// <summary>
        ///     拆分CSV中的自定义格式
        /// </summary>
        private static List<CustomClass> AnalysisCustomClass(List<string> varType, List<string> varName)
        {
            var customClass = new List<CustomClass>();
            for (var index = varType.Count - 1; index >= 0; index--)
            {
                var node = varType[index];
                var split = node.Split(';');
                varName[index] = varName[index].Replace("\r", "");
                varType[index] = varType[index].Replace("\r", "");
                if (split.Length > 1) //如果带有分号表示这是一个自定义类
                {
                    var custom = new CustomClass();
                    foreach (var c in split)
                    {
                        var subIndex = c.IndexOf('[');
                        custom.Name.Add(c.Substring(0, subIndex));
                        custom.Type.Add(c.Substring(subIndex + 1, c.LastIndexOf(']') - subIndex - 1));
                    }
                    varType[index] = custom.ClassName = varName[index];
                    customClass.Add(custom);
                }
            }
            return customClass;
        }

        /// <summary>
        ///     Unity编译结束后立即调用此函数将数据填充到Asset文件中
        /// </summary>
        [DidReloadScripts]
        private static void AfterLoadCsv()
        {
            EditorPrefs.SetString(mGenerateWrong, "");
            var csvFiles = EditorPrefs.GetString(mWaitLoadCsvEvent).Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < csvFiles.Length; i++)
                LoadCsv(csvFiles[i]);
            EditorPrefs.SetString(mWaitLoadCsvEvent, "");
        }

        internal class CustomClass
        {
            public string Attribute;
            public string ClassName;
            public List<string> Name = new List<string>();
            public List<string> Type = new List<string>();
        }
    }
}