using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DataLoader
{
    public static GraphData CreateFromJson(string file){
        TextAsset data = Resources.Load(file) as TextAsset; //Loads the TextAsset named in the file argument of the function
        return JsonUtility.FromJson<GraphData>(data.text);
    }

    public static List<Color> clusterColorSetter()
    {
        //初始化Cluster颜色数组
        //string[] colorStrings = new string[] { "rgb(251,128,114)", "rgb(128,177,211)", "rgb(255,255,179)", "rgb(190,186,218)", "rgb(253,180,98)", "rgb(179,222,105)", "rgb(210,180,140)", "rgb(252,205,229)" };
        string[] colorStrings = new string[] { "rgb(251,128,114)", "rgb(128,177,211)", "rgb(255,255,179)", "rgb(179,222,105)", "rgb(253,180,98)",  "rgb(190,186,218)", "rgb(210,180,140)", "rgb(252,205,229)" };
        List<Color> colorList = new List<Color>();
        for (int i = 0; i < colorStrings.Length; i++)
        {
            string colorString = colorStrings[i];
            colorString = colorString.Substring(4, colorString.Length - 5);//E.g.，从"rgb(141,211,199)"提取出"141,211,199"
            string[] rgb = colorString.Split(",");
            int r = int.Parse(rgb[0]),
                g = int.Parse(rgb[1]),
                b = int.Parse(rgb[2]);
            colorList.Add(new Color(r / 255f, g / 255f, b / 255f, 0.8f));
        }
        return colorList;
    }
}


[System.Serializable]
public class GraphData
{
    public List<NodeData> nodes;
    public List<LinkData> links;
}

[System.Serializable]
public class NodeData
{
    public string id;
    public int cluster;
    public float[] pos;
}

[System.Serializable]
public class LinkData
{
    public string source;
    public string target;
    public int value;
}
