using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
public class Generate
{
    internal Stack<PlaceHolder> CurrentPlace = new Stack<PlaceHolder>();
    internal Stack<string> Group = new Stack<string>();
    private string[] mTemplateFragment;
    private Action<Info,Generate> mWriterCallBack;
    StringBuilder sb = new StringBuilder();
    public class Info
    {
        public string PlaceHolder;
    }

    internal class PlaceHolder
    {
        public string Name;//占位符名称
        public string Content;//原内容
        public string PreContent;//等待推入StringBuilder的内容
        public string AllContent;//用于给上级进行整体替换的文本
        public StringBuilder StringBuilder = new StringBuilder();//最后压入SB中,提交给上一层,如果没有上一层则写入到文件当中
        private List<PlaceHolder> mMatches;
        public List<PlaceHolder> Matches
        {
            get { return mMatches ?? (mMatches = AnalysisPlaceHolder(Content)); }
        }
        public PlaceHolder FilterMatches(string name)
        {
            var place = Matches.Find(t1 => t1.Name == name);
            return new PlaceHolder {AllContent = place.AllContent, Content = place.Content, Name = place.Name};
        }
    }

    public Generate(string path, Action<Info,Generate> callback)
    {
        mTemplateFragment = File.ReadAllLines(path);
        mWriterCallBack = callback;
    }

    public string StartWrite()
    {
        for (var index = 0; index < mTemplateFragment.Length; )
        {
            CurrentPlace.Clear();
            var match = Regex.Match(mTemplateFragment[index], @"\[@(.+?)]");
            if (!match.Success)
                sb.AppendLine(mTemplateFragment[index++]);
            else
            {
                index = AnalysisPlaceHolder(index);
                mWriterCallBack(Replace(CurrentPlace.Peek()),this);
                var place = CurrentPlace.Pop();
                sb.AppendLine(place.StringBuilder.ToString());
            }
        }
        return sb.ToString();
    }

    private Info Replace(PlaceHolder holder)
    {
        return new Info{PlaceHolder =  holder.Name};
    }

    private static List<PlaceHolder> AnalysisPlaceHolder(string s)
    {
        var matches = Regex.Matches(s, @"\[@(.+?)]");
        int stringLength = s.Length;
        bool startCounting = false;
        int counting = 0;//计算当counting变为0时表示占位符标记已经结束
        int length = matches.Count;
        var place = new List<PlaceHolder>();
        for (int i = 0; i < length; i++)
        {
            startCounting = false;
            StringBuilder placeHolder = new StringBuilder();
            StringBuilder totalString = new StringBuilder();
            var match = matches[i];
            for (int j = match.Index + match.Length; j < stringLength; j++)
            {
                totalString.Append(s[j]);
                if (s[j] == '{')//直到找到第一个花括号开始才是真正的替换目标
                {
                    counting++;
                    startCounting = true;
                }
                if (!startCounting)
                    continue;
                if (s[j] == '}')
                {
                    counting--;
                    if (counting == 0)
                    {
                        //最后去掉第一个花括号和最后一个花括号(这里的花括号是用作标记替代符的范围现在不需要了)
                        var str = placeHolder.ToString();
                        str = str.Substring(str.IndexOf('{') + 1);
                        str = Regex.Replace(str, "^(\r\n)*", "");
                        str = Regex.Replace(str, "(\r\n*.)$", "");
                        place.Add(new PlaceHolder { Name = match.Value.Remove(0, 2).TrimEnd(']'), Content = str,AllContent = match.Value + totalString});
                        break;
                    }
                }
                placeHolder.Append(s[j]);
            }
        }
        return place;
    }

    /// <summary>
    /// 解析占位符
    /// </summary>
    private int AnalysisPlaceHolder(int index)
    {
        int recoder = index;
        int length = mTemplateFragment.Length;
        StringBuilder sb = new StringBuilder();
        bool startCounting = false;
        int counting = 0;//计算当counting变为0时表示占位符标记已经结束
        for (int i = recoder; i < length; i++)
        {
            string temp = mTemplateFragment[i];
            sb.AppendLine(temp);
            counting += Regex.Matches(temp, @"{", RegexOptions.IgnorePatternWhitespace).Count;
            if (counting > 0)
                startCounting = true;
            if (!startCounting)
                continue;
            counting -= Regex.Matches(temp, @"}", RegexOptions.IgnorePatternWhitespace).Count;
            if (counting == 0)
            {
                recoder = i;
                break;
            }
        }
        var match = Regex.Match(sb.ToString(), @"\[@(.+?)]");
        int stringLength = sb.Length;
        startCounting = false;
        StringBuilder placeHolder = new StringBuilder();
        for (int j = 0; j < stringLength; j++)
        {
            if (sb[j] == '{')//直到找到第一个花括号开始才是真正的替换目标
            {
                counting++;
                startCounting = true;
            }
            if (!startCounting)
                continue;
            if (sb[j] == '}')
            {
                counting--;
                if (counting == 0)
                {
                    //最后去掉第一个花括号和最后一个花括号(这里的花括号是用作标记替代符的范围现在不需要了)
                    var str = placeHolder.ToString();
                    str = str.Substring(str.IndexOf('{') + 1);
                    str = Regex.Replace(str, "^(\r\n)*", "");
                    str = Regex.Replace(str, "(\r\n*.)$", "");
                    CurrentPlace.Push(new PlaceHolder{Name = match.Value.Remove(0, 2).TrimEnd(']'), Content = str});
                    break;
                }
            }
            placeHolder.Append(sb[j]);
        }
        return recoder + 1;
    }

    public Generate SetReplace(string key,string value)
    {
        var place = CurrentPlace.Peek();
        if (place.PreContent == null)
            place.PreContent = place.Content;
        place.PreContent = place.PreContent.Replace("{@" + key + "}", value);
        return this;
    }

    public Generate Apply()
    {
        var place = CurrentPlace.Peek();
        //Apply的时候清除所有的替换符,此时替换符已经无法再被重写
        for (int i = 0; i < place.Matches.Count; i++)
            place.PreContent = place.PreContent.Replace(place.Matches[i].AllContent, "");
        //写入字符串
        place.StringBuilder.AppendLine(place.PreContent);
        place.PreContent = null;
        return this;
    }

    public Generate BeginGroup(string groupName)
    {
        CurrentPlace.Push(CurrentPlace.Peek().FilterMatches(groupName));
        return this;
    }

    public Generate EndGroup()
    {
        var place = CurrentPlace.Pop();
        var upperPlace = CurrentPlace.Peek();
        upperPlace.PreContent = upperPlace.PreContent.Replace(place.AllContent, place.StringBuilder.ToString());
        return this;
    }

    public static string FormatScript(string str)
    {
        int indent = 0;
        var split = str.Split(new[]{"\r\n"},StringSplitOptions.None);
        bool removeEmpty = false;
        for (int i = 0; i < split.Length; i++)
            split[i] = Regex.Replace(split[i], "(^[\\r\\n\\t ]*)|([\\r\\n\\t ]*$)","");
        StringBuilder sb = new StringBuilder();
        foreach (var node in split)
        {
            if (removeEmpty && string.IsNullOrEmpty(node))
                continue;
            removeEmpty = false;
            var leftMatches = Regex.Matches(node, "{");
            var rightMatches = Regex.Matches(node, "}");
            //延迟设置
            if (node != "{")
            {
                indent += leftMatches.Count - rightMatches.Count;
                removeEmpty = true;
            }
            var t = "";
            for (int i = 0; i < indent; i++)
                t += "\t";
            var s = t + node;
            sb.AppendLine(s);
            if (node == "{")
                indent += leftMatches.Count - rightMatches.Count;
        }
        return sb.ToString();
    }

    private static int Space(int indent, StringBuilder sb, int i, int index)
    {
        for (int j = 0; j < indent; j++)
            sb.Insert(i + 1 + index++, '\t');
        return index;
    }
}
