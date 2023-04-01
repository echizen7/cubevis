using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class GlobalParameter
{
    public static Dictionary<string, int>[] TaskList = new Dictionary<string, int>[] { new Dictionary<string, int>() { { "stage", 0 }, { "dataset", 0 }, { "condition", 0 }, { "task", 0 }, { "cluster0", 0}, { "cluster1", 1 } },
                                                                                    new Dictionary<string, int>() { { "stage", 0 }, { "dataset", 0 }, { "condition", 0 }, { "task", 1 }, { "node0", 2 } },
                                                                                    new Dictionary<string, int>() { { "stage", 0 }, { "dataset", 0 }, { "condition", 0 }, { "task", 2 }, { "node1", 0 },{ "node2", 32 } },

                                                                                    new Dictionary<string, int>() { { "stage", 0 }, { "dataset", 0 }, { "condition", 1 }, { "task", 0 }, { "cluster0", 0}, { "cluster1", 1 }},
                                                                                    new Dictionary<string, int>() { { "stage", 0 }, { "dataset", 0 }, { "condition", 1 }, { "task", 1 }, { "node0", 30 } },
                                                                                    new Dictionary<string, int>() { { "stage", 0 }, { "dataset", 0 }, { "condition", 1 }, { "task", 2 }, { "node1", 13 },{ "node2", 31 } },

                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 1 }, { "condition", 0 }, { "task", 0 },{ "cluster0", 0}, { "cluster1", 1 }},
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 1 }, { "condition", 0 }, { "task", 1 }, { "node0", 11 } },
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 1 }, { "condition", 0 }, { "task", 2 }, { "node1", 28 },{ "node2", 87 } },

                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 1 }, { "condition", 1 }, { "task", 0 },{ "cluster0", 5}, { "cluster1", 1 }},
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 1 }, { "condition", 1 }, { "task", 1 }, { "node0", 9 } },
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 1 }, { "condition", 1 }, { "task", 2 }, { "node1", 40 },{ "node2", 110 } },


                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 2 }, { "condition", 0 }, { "task", 0 }, { "cluster0", 1}, { "cluster1", 2 } },
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 2 }, { "condition", 0 }, { "task", 1 }, { "node0", 32 } },
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 2 }, { "condition", 0 }, { "task", 2 }, { "node1", 65 },{ "node2", 118 } },

                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 2 }, { "condition", 1 }, { "task", 0 }, { "cluster0", 1}, { "cluster1", 3 } },
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 2 }, { "condition", 1 }, { "task", 1 }, { "node0", 17 } },
                                                                                    new Dictionary<string, int>() { { "stage", 1 }, { "dataset", 2 }, { "condition", 1 }, { "task", 2 }, { "node1", 53 },{ "node2", 98 } },
    };
    public static Dictionary<string, string>[] OptionList = new Dictionary<string, string>[] { new Dictionary<string,string>(){ { "OptionA", "Cluster: Red" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Red" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionA"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Node: 0" }, { "OptionB", "Node: 32" }, {"answer", "OptionA"} },

                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Red" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Red" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Node: 13" }, { "OptionB", "Node: 31" }, {"answer", "OptionA"} },

                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Red" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Pink" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionA"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Node: 28" }, { "OptionB", "Node: 87" }, {"answer", "OptionB"} },

                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Green" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionA"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Orange" }, { "OptionB", "Cluster: Pink" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Node: 40" }, { "OptionB", "Node: 110" }, {"answer", "OptionB"} },

                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Yellow" }, { "OptionB", "Cluster: Blue" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Pink" }, { "OptionB", "Cluster: Purple" }, {"answer", "OptionA"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Node: 65" }, { "OptionB", "Node: 118" }, {"answer", "OptionA"} },

                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Blue" }, { "OptionB", "Cluster: Purple" }, {"answer", "OptionB"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Cluster: Green" }, { "OptionB", "Cluster: Pink" }, {"answer", "OptionA"} },
                                                                                               new Dictionary<string,string>(){ { "OptionA", "Node: 53" }, { "OptionB", "Node: 98" }, {"answer", "OptionB"} },
    };

    public static Color NodeColor = Color.white;
    public static Color EdgeColor = new Color(57 / 255f, 121 / 255f, 205 / 255f, 1.0f);
    public static Color CellColor = new Color(255 / 255f, 250 / 255f, 250 / 255f, 0.6f);
    public static Color OtherCellColor = new Color(130 / 255f, 130 / 255f, 130 / 255f, 0.8f);
    public static Color CellProjectionColor = new Color(180 / 255f, 180 / 255f, 180 / 255f, 0.7f);
    public static Color PlaneColor = new Color(155 / 255f, 155 / 255f, 155 / 255f, 0.1f);
    public static Color SliceColor = new Color(200 / 255f, 200 / 255f, 200 / 255f, 0.5f);
    public static Color labelColor = Color.white;
    public static Color OptionColor = Color.white;

    public static Color NodeHighlightColor = Color.red;
    public static Color EdgeHighlightColor = Color.cyan;
    public static Color CellHighlightColor = Color.yellow;
    public static Color CellProjectionHighlightColor = Color.yellow;
    public static Color labelHightlightColor = Color.yellow;
    public static Color OptionHighlightedColor = Color.yellow;
    public static Color OptionSelectedColor = Color.green;

    public static float NodeEnlargeScale = 1.25f;

    public static int[,] sequences = new int[6, 3]
    {
        {0,1,2 },//s1
        {1,0,2 },//s2
        {1,2,0 },//s3
        {2,1,0 },//s4
        {2,0,1 },//s5
        {0,2,1 },//s6
    };
    public static int realTaskByUserGroup(int TaskIndex, int UserIndex)
    {
        int groupIndex = UserIndex % 6;
        int conditionIndex = TaskIndex / 3;
        int realTaskIndex = TaskIndex % 3;
        int TaskSequenceIndex = (conditionIndex + groupIndex) % 6;
        return conditionIndex * 3 + sequences[TaskSequenceIndex, realTaskIndex];
    }
    public static void RecordUserResponse(int userIndex, int taskIndex, bool result, float timeConsumed)
    {
        string filePath = "C:/Users/Rusheng Pan/Documents/cubevis/Assets/User Response/user-" + userIndex + ".txt";
        // Open the file for appending
        StreamWriter writer = new StreamWriter(filePath, true);

        // Write some text to the file
        writer.WriteLine((taskIndex-5) + ": " + result + ", "+ timeConsumed.ToString("0.00"));

        // Close the file
        writer.Close();
    }
    public static string NodePrefabId(string id) { return "Node: " + id; }
    public static string EdgePrefabId(string source, string target) { return "Edge: " + source + "->" + target; }
    public static string CellPrefabId(float x, float y, float z) { return "Cell: (" + x + ", " + y + ", " + z + ")"; }
    public static string LabelXId(string label_text) { return "x_" + label_text; }
    public static string LabelYId(string label_text) { return "y_" + label_text; }
    public static string LabelZId(string label_text) { return "z_" + label_text; }
    public static int CellXIndex(string cellPrefabId) { return int.Parse(cellPrefabId.Split(", ")[0].Split(')')[0]); }
    public static int CellYIndex(string cellPrefabId) { return int.Parse(cellPrefabId.Split(", ")[1].Split(')')[0]); }
    public static int CellZIndex(string cellPrefabId) { return int.Parse(cellPrefabId.Split(", ")[2].Split(')')[0]); }


}
