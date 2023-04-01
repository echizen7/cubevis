using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Utility
{
    public static List<Color> colorList = DataLoader.clusterColorSetter();
    public static Color cluster2Color(int cluster)
    {
        if (cluster > colorList.Count - 1)
        {
            cluster = colorList.Count - 1;
        }
        return colorList[cluster];
    }
}
public class DataProcessor0
{
 
    // 以下static成员变量用作全局变量，便于其他script使用，从而实现是视图间联动
    public static string graphFile = "karate_layout";// Name of the input graph file, no extension
    public static GraphData graph = DataLoader.CreateFromJson(graphFile);
    public static int n = graph.nodes.Count; //number of nodes
    public static Dictionary<string, int> idxMap; // map nodeid to its index
    public static Dictionary<string, HashSet<string>> nbrMap;

    public int[,] matrix2;
    public int[,,] matrix3;
    public List<int> order; //重排序结果
    public int[,,] cube;

    public DataProcessor0(){//默认初始化函数
        //初始化matrix2和matrix3
        setMatrix(graph);
    }

    //计算成员变量matrix2和matrix3
    private void setMatrix(GraphData graph){
        matrix2  = new int[n, n];//int型默认为0
        matrix3  = new int[n, n, n];//int型默认为0

        idxMap = new Dictionary<string, int>();
        nbrMap = new Dictionary<string, HashSet<string>>();
        for (int i = 0; i < n; i++){
            idxMap.Add(graph.nodes[i].id, i);
        }

        for (int i = 0; i < graph.links.Count; i++){
            string src = graph.links[i].source, tgt = graph.links[i].target;
            if(nbrMap.ContainsKey(src)){
                nbrMap[src].Add(tgt);
            } else{
                nbrMap.Add(src, new HashSet<string>(){tgt});
            }
            if(nbrMap.ContainsKey(tgt)){
                nbrMap[tgt].Add(src);
            } else{
                nbrMap.Add(tgt, new HashSet<string>(){src});
            }
        }

        for (int i = 0; i < graph.links.Count; i++){
            string src = graph.links[i].source, tgt = graph.links[i].target;
            HashSet<string> srcNbrSet = nbrMap[src], tgtNbrSet = nbrMap[tgt];
            List<string> srcNbrList = new List<string>(srcNbrSet), tgtNbrList = new List<string>(tgtNbrSet);
            List<string> triangles = srcNbrList.Intersect(tgtNbrList).ToList();
            int srcIdx = idxMap[src], tgtIdx = idxMap[tgt];
            //初始化matrix2
            matrix2[srcIdx, tgtIdx] = 1;
            matrix2[tgtIdx, srcIdx] = 1;

            for(int j = 0; j<triangles.Count; j++){
                int triIdx = idxMap[triangles[j]];
                matrix3[srcIdx, tgtIdx, triIdx]=1;
                matrix3[srcIdx, triIdx, tgtIdx]=1;
                matrix3[tgtIdx, srcIdx, triIdx]=1;
                matrix3[tgtIdx, triIdx, srcIdx]=1;
                matrix3[triIdx, srcIdx, tgtIdx]=1;
                matrix3[triIdx, srcIdx, triIdx]=1;
            }
        }
        return;
    }

    //基于对matrix3重排序得到cube坐标
    public int[,,] getCube(float d_min){//d_min: minimum density
        float[,] similarityMatrix = get3dSimilarityMatrix();
        List<SimilarityElement> similarityList = get3dSimilarityList(similarityMatrix);
        List<List<int>> denseClusters = new List<List<int>>();
        Dictionary<int, int> similarityNodeMap = new Dictionary<int, int>();
        for(int i = 0; i<n; i++){
            similarityNodeMap[i]=i;
        }
        int[,] subG = new int[n, n]; //subG是matrix2的深拷贝，用于分割dense subgraph，防止matrix2被改变
        for(int i = 0; i<n; i++){
            for(int j = 0; j<n; j++){
                subG[i,j] = matrix2[i,j];
            }
        }
        setDenseClusters(subG, similarityMatrix, similarityList, similarityNodeMap);
        denseClusters.Sort((x,y)=>{return -x.Count.CompareTo(y.Count);});//降序

        //展开denseClusters，得到重排序结果
        order = new List<int>();
        order.Add(-1);//实际下标从1开始，下标0留出来放邻接矩阵,占位用
        for(int i = 0; i < denseClusters.Count; i++){
            order = order.Concat(denseClusters[i]).ToList<int>();
        }
        //基于重排序得到cube
        cube  = new int[n+1, n+1, n+1];//int型默认为0
        for(int i = 0; i<n+1; i++){//下标0留出来放邻接矩阵
            for(int j = 0; j<n+1; j++){
                for(int k = 0; k<n+1; k++){
                    //不画对称的格点
                    if(k>j&&j>i&&i>0){
                        cube[i, j, k] = matrix3[order[i], order[j], order[k]];
                    } else{
                        //坐标面为二维邻接矩阵的投影
                        if(i==0 && k>j && j>0){
                            cube[i, j, k] = matrix2[order[j], order[k]];
                        } else if(j==0 && k>i && i>0){
                            cube[i, j, k] = matrix2[order[i], order[k]];
                        } else if(k==0 && j>i && i>0){
                            cube[i, j, k] = matrix2[order[i], order[j]];
                        }
                    }

                }
            }
        }
        return cube;

        //以下为若干内部函数定义
        float[,] get3dSimilarityMatrix() {
            float[,] similarityMatrix = new float[n, n];//int型默认为0
            float[] norm_matrix = new float[n];
            for(int i = 0; i < n; i++){
                //计算每个矩阵的模，用于计算cos相似度
                float norm = 0;
                for (int j = 0; j < n; j++) {
                    for (int k = 0; k < n; k++) {
                        norm += (float) Math.Pow(matrix3[i, j, k], 2f);
                    }
                }
                norm_matrix[i] =  (float) Math.Sqrt(norm);
            }
            for (int i = 0; i < n; i++) {
                for (int j = i + 1; j < n; j++) {
                    //计算cos相似度
                    float cos_value = 0;
                    //0向量的特殊处理
                    if (norm_matrix[i] == 0) {
                        cos_value = norm_matrix[j] == 0 ? 1 : 0;
                    } else if (norm_matrix[j] == 0) {
                        cos_value = 0;
                    } else {
                        //正常计算cos相似度
                        float product = 0;
                        for (int ii = 0; ii < n; ii++) {
                            for (int jj = 0; jj < n; jj++) {
                                product += matrix3[i, ii, jj] * matrix3[j, ii, jj];
                            }
                        }
                        cos_value = product / (norm_matrix[i] * norm_matrix[j]);
                    }
                    similarityMatrix[i, j] = cos_value;
                    similarityMatrix[j, i] = cos_value;
                }
            }
            return similarityMatrix;
        }

        List<SimilarityElement> get3dSimilarityList(float[,] similarityMatrix) {
            List<SimilarityElement> similarityList = new List<SimilarityElement>();
            for (int i = 0; i < n; i++) {
                for (int j = i + 1; j < n; j++) {
                    SimilarityElement ele = new SimilarityElement(){row=i, col=j, similarity=similarityMatrix[i,j]};
                    similarityList.Add(ele);
                }
            }
            similarityList.Sort((x,y)=>{return x.similarity.CompareTo(y.similarity);});//升序
            return similarityList;
        }

        void setDenseClusters(int[,] G2, float[,] similarityMatrix, List<SimilarityElement> similarityList, Dictionary<int, int> similarityNodeMap) {
            int m = 0;
            List<List<int>> connectedComponents = getConnectedComponents(similarityMatrix);
            while (m<similarityList.Count && connectedComponents.Count<2) {
                similarityMatrix[similarityList[m].row, similarityList[m].col] = 0;
                similarityMatrix[similarityList[m].col, similarityList[m].row] = 0;
                m += 1;
                connectedComponents = getConnectedComponents(similarityMatrix);
            }
            for(int i = 0; i<connectedComponents.Count; i++){//connectedComponents长度一定为2
                List<int> connectedComponent = connectedComponents[i];
                int _n = connectedComponent.Count;
                int[,] subG = new int[_n, _n];
                float[,] subSimilarityMatrix = new float[_n, _n];

                Dictionary<int, int> newSimilarityNodeMap = new Dictionary<int, int>();
                for(int j = 0; j < connectedComponent.Count; j++){
                    newSimilarityNodeMap[j] = similarityNodeMap[connectedComponent[j]];
                }
                for(int j = 0; j<_n; j++){
                    for(int k = j+1; k<_n; k++){
                        subSimilarityMatrix[j, k] = similarityMatrix[connectedComponent[j], connectedComponent[k]];
                        subSimilarityMatrix[k, j] = subSimilarityMatrix[j, k];
                        subG[j, k] = G2[connectedComponent[j], connectedComponent[k]];
                        subG[k, j] = subG[j, k];
                    }
                }
                List<int> newConnectedComponent = new List<int>();
                for(int j = 0; j < connectedComponent.Count; j++){
                    newConnectedComponent.Add(newSimilarityNodeMap[j]);
                }
                if(_n==1 || getDensity(subG, newConnectedComponent)>=d_min){
                    denseClusters.Add(newConnectedComponent);
                } else {
                    Dictionary<int, int> reverseNodeMap = new Dictionary<int, int>();
                    for(int j = 0; j < connectedComponent.Count; j++){
                        reverseNodeMap[connectedComponent[j]] = j;
                    }
                    List<SimilarityElement> subSimilarityList = new List<SimilarityElement>(); 
                    for(int j = 0; j<similarityList.Count; j++){
                        int row = similarityList[j].row, col = similarityList[j].col;
                        if(connectedComponent.Exists(d => d==row)&&connectedComponent.Exists(d => d==col)){
                            SimilarityElement ele = new SimilarityElement(){row=reverseNodeMap[similarityList[j].row], col=reverseNodeMap[similarityList[j].col], similarity=similarityList[j].similarity};
                            subSimilarityList.Add(ele);
                        }
                    }
                    setDenseClusters(subG, subSimilarityMatrix, subSimilarityList, newSimilarityNodeMap);
                }
            }
        }

        List<List<int>> getConnectedComponents(float[,] similarityMatrix) {
            List<List<int>> connectedComponents = new List<List<int>>();
            List<int> connectedComponent = new List<int>();
            // Mark all the vertices as not visited
            int _n = similarityMatrix.GetLength(0);
            bool[] visited = new bool[_n];//bool型默认值为false
            for (int i = 0; i < _n; i++) {
                if (!visited[i]) {
                    // print all reachable vertices from i
                    DFSUtil(i, visited);
                    // 合并数组
                    connectedComponents.Add(new List<int>(connectedComponent));//深拷贝，否则数组值会被下一行代码清空
                    connectedComponent.Clear();
                }
            }
            return connectedComponents;

            void DFSUtil(int i, bool[] visited) {
                // Mark the current node as visited and print it
                visited[i] = true;
                connectedComponent.Add(i);
                // Recur for all the vertices adjacent to this vertex
                for (int j = 0; j < _n; j++) {
                    if((similarityMatrix[i,j]>0) && (!visited[j])){
                        DFSUtil(j, visited);
                    }
                }
            }
        }

        float getDensity(int[,] subG, List<int> connectedComponent){//matrix2 is an adjacency matrix
            int _n = subG.GetLength(0);
            float t = 0, w = 0;
            //计算w: the number of wedges (wedge is a two-hop path)
            for(int i = 0; i < _n; i++){
                float line_w = 0;
                for(int j = 0; j < _n; j++){
                    line_w += (float) subG[i,j];
                }
                w += (line_w*0.5f*(line_w-1));
            }
            if(w==0f){
                return 0f;
            }
            //计算t: the number of triangles
            for(int i = 0; i<_n; i++){
                for(int j = i+1; j<_n; j++){
                    for(int k = j+1; k<_n; k++){
                        t+=matrix3[connectedComponent[i], connectedComponent[j], connectedComponent[k]];
                    }
                }
            }
            return (3f*t)/w;
        }
    }
}

public class DataProcessor1
{

    // 以下static成员变量用作全局变量，便于其他script使用，从而实现是视图间联动
    public static string graphFile = "football_layout";// Name of the input graph file, no extension
    public static GraphData graph = DataLoader.CreateFromJson(graphFile);
    public static int n = graph.nodes.Count; //number of nodes
    public static Dictionary<string, int> idxMap; // map nodeid to its index
    public static Dictionary<string, HashSet<string>> nbrMap;

    public int[,] matrix2;
    public int[,,] matrix3;
    public List<int> order; //重排序结果
    public int[,,] cube;

    public DataProcessor1()
    {//默认初始化函数
        //初始化matrix2和matrix3
        setMatrix(graph);
    }

    //计算成员变量matrix2和matrix3
    private void setMatrix(GraphData graph)
    {
        matrix2 = new int[n, n];//int型默认为0
        matrix3 = new int[n, n, n];//int型默认为0

        idxMap = new Dictionary<string, int>();
        nbrMap = new Dictionary<string, HashSet<string>>();
        for (int i = 0; i < n; i++)
        {
            idxMap.Add(graph.nodes[i].id, i);
        }

        for (int i = 0; i < graph.links.Count; i++)
        {
            string src = graph.links[i].source, tgt = graph.links[i].target;
            if (nbrMap.ContainsKey(src))
            {
                nbrMap[src].Add(tgt);
            }
            else
            {
                nbrMap.Add(src, new HashSet<string>() { tgt });
            }
            if (nbrMap.ContainsKey(tgt))
            {
                nbrMap[tgt].Add(src);
            }
            else
            {
                nbrMap.Add(tgt, new HashSet<string>() { src });
            }
        }

        for (int i = 0; i < graph.links.Count; i++)
        {
            string src = graph.links[i].source, tgt = graph.links[i].target;
            HashSet<string> srcNbrSet = nbrMap[src], tgtNbrSet = nbrMap[tgt];
            List<string> srcNbrList = new List<string>(srcNbrSet), tgtNbrList = new List<string>(tgtNbrSet);
            List<string> triangles = srcNbrList.Intersect(tgtNbrList).ToList();
            int srcIdx = idxMap[src], tgtIdx = idxMap[tgt];
            //初始化matrix2
            matrix2[srcIdx, tgtIdx] = 1;
            matrix2[tgtIdx, srcIdx] = 1;

            for (int j = 0; j < triangles.Count; j++)
            {
                int triIdx = idxMap[triangles[j]];
                matrix3[srcIdx, tgtIdx, triIdx] = 1;
                matrix3[srcIdx, triIdx, tgtIdx] = 1;
                matrix3[tgtIdx, srcIdx, triIdx] = 1;
                matrix3[tgtIdx, triIdx, srcIdx] = 1;
                matrix3[triIdx, srcIdx, tgtIdx] = 1;
                matrix3[triIdx, srcIdx, triIdx] = 1;
            }
        }
        return;
    }

    //基于对matrix3重排序得到cube坐标
    public int[,,] getCube(float d_min)
    {//d_min: minimum density
        float[,] similarityMatrix = get3dSimilarityMatrix();
        List<SimilarityElement> similarityList = get3dSimilarityList(similarityMatrix);
        List<List<int>> denseClusters = new List<List<int>>();
        Dictionary<int, int> similarityNodeMap = new Dictionary<int, int>();
        for (int i = 0; i < n; i++)
        {
            similarityNodeMap[i] = i;
        }
        int[,] subG = new int[n, n]; //subG是matrix2的深拷贝，用于分割dense subgraph，防止matrix2被改变
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                subG[i, j] = matrix2[i, j];
            }
        }
        setDenseClusters(subG, similarityMatrix, similarityList, similarityNodeMap);
        denseClusters.Sort((x, y) => { return -x.Count.CompareTo(y.Count); });//降序

        //展开denseClusters，得到重排序结果
        order = new List<int>();
        order.Add(-1);//实际下标从1开始，下标0留出来放邻接矩阵,占位用
        for (int i = 0; i < denseClusters.Count; i++)
        {
            order = order.Concat(denseClusters[i]).ToList<int>();
        }
        //基于重排序得到cube
        cube = new int[n + 1, n + 1, n + 1];//int型默认为0
        for (int i = 0; i < n + 1; i++)
        {//下标0留出来放邻接矩阵
            for (int j = 0; j < n + 1; j++)
            {
                for (int k = 0; k < n + 1; k++)
                {
                    //不画对称的格点
                    if (k > j && j > i && i > 0)
                    {
                        cube[i, j, k] = matrix3[order[i], order[j], order[k]];
                    }
                    else
                    {
                        //坐标面为二维邻接矩阵的投影
                        if (i == 0 && k > j && j > 0)
                        {
                            cube[i, j, k] = matrix2[order[j], order[k]];
                        }
                        else if (j == 0 && k > i && i > 0)
                        {
                            cube[i, j, k] = matrix2[order[i], order[k]];
                        }
                        else if (k == 0 && j > i && i > 0)
                        {
                            cube[i, j, k] = matrix2[order[i], order[j]];
                        }
                    }

                }
            }
        }
        return cube;

        //以下为若干内部函数定义
        float[,] get3dSimilarityMatrix()
        {
            float[,] similarityMatrix = new float[n, n];//int型默认为0
            float[] norm_matrix = new float[n];
            for (int i = 0; i < n; i++)
            {
                //计算每个矩阵的模，用于计算cos相似度
                float norm = 0;
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        norm += (float)Math.Pow(matrix3[i, j, k], 2f);
                    }
                }
                norm_matrix[i] = (float)Math.Sqrt(norm);
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    //计算cos相似度
                    float cos_value = 0;
                    //0向量的特殊处理
                    if (norm_matrix[i] == 0)
                    {
                        cos_value = norm_matrix[j] == 0 ? 1 : 0;
                    }
                    else if (norm_matrix[j] == 0)
                    {
                        cos_value = 0;
                    }
                    else
                    {
                        //正常计算cos相似度
                        float product = 0;
                        for (int ii = 0; ii < n; ii++)
                        {
                            for (int jj = 0; jj < n; jj++)
                            {
                                product += matrix3[i, ii, jj] * matrix3[j, ii, jj];
                            }
                        }
                        cos_value = product / (norm_matrix[i] * norm_matrix[j]);
                    }
                    similarityMatrix[i, j] = cos_value;
                    similarityMatrix[j, i] = cos_value;
                }
            }
            return similarityMatrix;
        }

        List<SimilarityElement> get3dSimilarityList(float[,] similarityMatrix)
        {
            List<SimilarityElement> similarityList = new List<SimilarityElement>();
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    SimilarityElement ele = new SimilarityElement() { row = i, col = j, similarity = similarityMatrix[i, j] };
                    similarityList.Add(ele);
                }
            }
            similarityList.Sort((x, y) => { return x.similarity.CompareTo(y.similarity); });//升序
            return similarityList;
        }

        void setDenseClusters(int[,] G2, float[,] similarityMatrix, List<SimilarityElement> similarityList, Dictionary<int, int> similarityNodeMap)
        {
            int m = 0;
            List<List<int>> connectedComponents = getConnectedComponents(similarityMatrix);
            while (m < similarityList.Count && connectedComponents.Count < 2)
            {
                similarityMatrix[similarityList[m].row, similarityList[m].col] = 0;
                similarityMatrix[similarityList[m].col, similarityList[m].row] = 0;
                m += 1;
                connectedComponents = getConnectedComponents(similarityMatrix);
            }
            for (int i = 0; i < connectedComponents.Count; i++)
            {//connectedComponents长度一定为2
                List<int> connectedComponent = connectedComponents[i];
                int _n = connectedComponent.Count;
                int[,] subG = new int[_n, _n];
                float[,] subSimilarityMatrix = new float[_n, _n];

                Dictionary<int, int> newSimilarityNodeMap = new Dictionary<int, int>();
                for (int j = 0; j < connectedComponent.Count; j++)
                {
                    newSimilarityNodeMap[j] = similarityNodeMap[connectedComponent[j]];
                }
                for (int j = 0; j < _n; j++)
                {
                    for (int k = j + 1; k < _n; k++)
                    {
                        subSimilarityMatrix[j, k] = similarityMatrix[connectedComponent[j], connectedComponent[k]];
                        subSimilarityMatrix[k, j] = subSimilarityMatrix[j, k];
                        subG[j, k] = G2[connectedComponent[j], connectedComponent[k]];
                        subG[k, j] = subG[j, k];
                    }
                }
                List<int> newConnectedComponent = new List<int>();
                for (int j = 0; j < connectedComponent.Count; j++)
                {
                    newConnectedComponent.Add(newSimilarityNodeMap[j]);
                }
                if (_n == 1 || getDensity(subG, newConnectedComponent) >= d_min)
                {
                    denseClusters.Add(newConnectedComponent);
                }
                else
                {
                    Dictionary<int, int> reverseNodeMap = new Dictionary<int, int>();
                    for (int j = 0; j < connectedComponent.Count; j++)
                    {
                        reverseNodeMap[connectedComponent[j]] = j;
                    }
                    List<SimilarityElement> subSimilarityList = new List<SimilarityElement>();
                    for (int j = 0; j < similarityList.Count; j++)
                    {
                        int row = similarityList[j].row, col = similarityList[j].col;
                        if (connectedComponent.Exists(d => d == row) && connectedComponent.Exists(d => d == col))
                        {
                            SimilarityElement ele = new SimilarityElement() { row = reverseNodeMap[similarityList[j].row], col = reverseNodeMap[similarityList[j].col], similarity = similarityList[j].similarity };
                            subSimilarityList.Add(ele);
                        }
                    }
                    setDenseClusters(subG, subSimilarityMatrix, subSimilarityList, newSimilarityNodeMap);
                }
            }
        }

        List<List<int>> getConnectedComponents(float[,] similarityMatrix)
        {
            List<List<int>> connectedComponents = new List<List<int>>();
            List<int> connectedComponent = new List<int>();
            // Mark all the vertices as not visited
            int _n = similarityMatrix.GetLength(0);
            bool[] visited = new bool[_n];//bool型默认值为false
            for (int i = 0; i < _n; i++)
            {
                if (!visited[i])
                {
                    // print all reachable vertices from i
                    DFSUtil(i, visited);
                    // 合并数组
                    connectedComponents.Add(new List<int>(connectedComponent));//深拷贝，否则数组值会被下一行代码清空
                    connectedComponent.Clear();
                }
            }
            return connectedComponents;

            void DFSUtil(int i, bool[] visited)
            {
                // Mark the current node as visited and print it
                visited[i] = true;
                connectedComponent.Add(i);
                // Recur for all the vertices adjacent to this vertex
                for (int j = 0; j < _n; j++)
                {
                    if ((similarityMatrix[i, j] > 0) && (!visited[j]))
                    {
                        DFSUtil(j, visited);
                    }
                }
            }
        }

        float getDensity(int[,] subG, List<int> connectedComponent)
        {//matrix2 is an adjacency matrix
            int _n = subG.GetLength(0);
            float t = 0, w = 0;
            //计算w: the number of wedges (wedge is a two-hop path)
            for (int i = 0; i < _n; i++)
            {
                float line_w = 0;
                for (int j = 0; j < _n; j++)
                {
                    line_w += (float)subG[i, j];
                }
                w += (line_w * 0.5f * (line_w - 1));
            }
            if (w == 0f)
            {
                return 0f;
            }
            //计算t: the number of triangles
            for (int i = 0; i < _n; i++)
            {
                for (int j = i + 1; j < _n; j++)
                {
                    for (int k = j + 1; k < _n; k++)
                    {
                        t += matrix3[connectedComponent[i], connectedComponent[j], connectedComponent[k]];
                    }
                }
            }
            return (3f * t) / w;
        }
    }
}

public class DataProcessor2
{

    // 以下static成员变量用作全局变量，便于其他script使用，从而实现是视图间联动
    public static string graphFile = "email_layout";// Name of the input graph file, no extension
    public static GraphData graph = DataLoader.CreateFromJson(graphFile);
    public static int n = graph.nodes.Count; //number of nodes
    public static Dictionary<string, int> idxMap; // map nodeid to its index
    public static Dictionary<string, HashSet<string>> nbrMap;

    public int[,] matrix2;
    public int[,,] matrix3;
    public List<int> order; //重排序结果
    public int[,,] cube;

    public DataProcessor2()
    {//默认初始化函数
        //初始化matrix2和matrix3
        setMatrix(graph);
    }

    //计算成员变量matrix2和matrix3
    private void setMatrix(GraphData graph)
    {
        matrix2 = new int[n, n];//int型默认为0
        matrix3 = new int[n, n, n];//int型默认为0

        idxMap = new Dictionary<string, int>();
        nbrMap = new Dictionary<string, HashSet<string>>();
        for (int i = 0; i < n; i++)
        {
            idxMap.Add(graph.nodes[i].id, i);
        }

        for (int i = 0; i < graph.links.Count; i++)
        {
            string src = graph.links[i].source, tgt = graph.links[i].target;
            if (nbrMap.ContainsKey(src))
            {
                nbrMap[src].Add(tgt);
            }
            else
            {
                nbrMap.Add(src, new HashSet<string>() { tgt });
            }
            if (nbrMap.ContainsKey(tgt))
            {
                nbrMap[tgt].Add(src);
            }
            else
            {
                nbrMap.Add(tgt, new HashSet<string>() { src });
            }
        }

        for (int i = 0; i < graph.links.Count; i++)
        {
            string src = graph.links[i].source, tgt = graph.links[i].target;
            HashSet<string> srcNbrSet = nbrMap[src], tgtNbrSet = nbrMap[tgt];
            List<string> srcNbrList = new List<string>(srcNbrSet), tgtNbrList = new List<string>(tgtNbrSet);
            List<string> triangles = srcNbrList.Intersect(tgtNbrList).ToList();
            int srcIdx = idxMap[src], tgtIdx = idxMap[tgt];
            //初始化matrix2
            matrix2[srcIdx, tgtIdx] = 1;
            matrix2[tgtIdx, srcIdx] = 1;

            for (int j = 0; j < triangles.Count; j++)
            {
                int triIdx = idxMap[triangles[j]];
                matrix3[srcIdx, tgtIdx, triIdx] = 1;
                matrix3[srcIdx, triIdx, tgtIdx] = 1;
                matrix3[tgtIdx, srcIdx, triIdx] = 1;
                matrix3[tgtIdx, triIdx, srcIdx] = 1;
                matrix3[triIdx, srcIdx, tgtIdx] = 1;
                matrix3[triIdx, srcIdx, triIdx] = 1;
            }
        }
        return;
    }

    //基于对matrix3重排序得到cube坐标
    public int[,,] getCube(float d_min)
    {//d_min: minimum density
        float[,] similarityMatrix = get3dSimilarityMatrix();
        List<SimilarityElement> similarityList = get3dSimilarityList(similarityMatrix);
        List<List<int>> denseClusters = new List<List<int>>();
        Dictionary<int, int> similarityNodeMap = new Dictionary<int, int>();
        for (int i = 0; i < n; i++)
        {
            similarityNodeMap[i] = i;
        }
        int[,] subG = new int[n, n]; //subG是matrix2的深拷贝，用于分割dense subgraph，防止matrix2被改变
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                subG[i, j] = matrix2[i, j];
            }
        }
        setDenseClusters(subG, similarityMatrix, similarityList, similarityNodeMap);
        denseClusters.Sort((x, y) => { return -x.Count.CompareTo(y.Count); });//降序

        //展开denseClusters，得到重排序结果
        order = new List<int>();
        order.Add(-1);//实际下标从1开始，下标0留出来放邻接矩阵,占位用
        for (int i = 0; i < denseClusters.Count; i++)
        {
            order = order.Concat(denseClusters[i]).ToList<int>();
        }
        //基于重排序得到cube
        cube = new int[n + 1, n + 1, n + 1];//int型默认为0
        for (int i = 0; i < n + 1; i++)
        {//下标0留出来放邻接矩阵
            for (int j = 0; j < n + 1; j++)
            {
                for (int k = 0; k < n + 1; k++)
                {
                    //不画对称的格点
                    if (k > j && j > i && i > 0)
                    {
                        cube[i, j, k] = matrix3[order[i], order[j], order[k]];
                    }
                    else
                    {
                        //坐标面为二维邻接矩阵的投影
                        if (i == 0 && k > j && j > 0)
                        {
                            cube[i, j, k] = matrix2[order[j], order[k]];
                        }
                        else if (j == 0 && k > i && i > 0)
                        {
                            cube[i, j, k] = matrix2[order[i], order[k]];
                        }
                        else if (k == 0 && j > i && i > 0)
                        {
                            cube[i, j, k] = matrix2[order[i], order[j]];
                        }
                    }

                }
            }
        }
        return cube;

        //以下为若干内部函数定义
        float[,] get3dSimilarityMatrix()
        {
            float[,] similarityMatrix = new float[n, n];//int型默认为0
            float[] norm_matrix = new float[n];
            for (int i = 0; i < n; i++)
            {
                //计算每个矩阵的模，用于计算cos相似度
                float norm = 0;
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        norm += (float)Math.Pow(matrix3[i, j, k], 2f);
                    }
                }
                norm_matrix[i] = (float)Math.Sqrt(norm);
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    //计算cos相似度
                    float cos_value = 0;
                    //0向量的特殊处理
                    if (norm_matrix[i] == 0)
                    {
                        cos_value = norm_matrix[j] == 0 ? 1 : 0;
                    }
                    else if (norm_matrix[j] == 0)
                    {
                        cos_value = 0;
                    }
                    else
                    {
                        //正常计算cos相似度
                        float product = 0;
                        for (int ii = 0; ii < n; ii++)
                        {
                            for (int jj = 0; jj < n; jj++)
                            {
                                product += matrix3[i, ii, jj] * matrix3[j, ii, jj];
                            }
                        }
                        cos_value = product / (norm_matrix[i] * norm_matrix[j]);
                    }
                    similarityMatrix[i, j] = cos_value;
                    similarityMatrix[j, i] = cos_value;
                }
            }
            return similarityMatrix;
        }

        List<SimilarityElement> get3dSimilarityList(float[,] similarityMatrix)
        {
            List<SimilarityElement> similarityList = new List<SimilarityElement>();
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    SimilarityElement ele = new SimilarityElement() { row = i, col = j, similarity = similarityMatrix[i, j] };
                    similarityList.Add(ele);
                }
            }
            similarityList.Sort((x, y) => { return x.similarity.CompareTo(y.similarity); });//升序
            return similarityList;
        }

        void setDenseClusters(int[,] G2, float[,] similarityMatrix, List<SimilarityElement> similarityList, Dictionary<int, int> similarityNodeMap)
        {
            int m = 0;
            List<List<int>> connectedComponents = getConnectedComponents(similarityMatrix);
            while (m < similarityList.Count && connectedComponents.Count < 2)
            {
                similarityMatrix[similarityList[m].row, similarityList[m].col] = 0;
                similarityMatrix[similarityList[m].col, similarityList[m].row] = 0;
                m += 1;
                connectedComponents = getConnectedComponents(similarityMatrix);
            }
            for (int i = 0; i < connectedComponents.Count; i++)
            {//connectedComponents长度一定为2
                List<int> connectedComponent = connectedComponents[i];
                int _n = connectedComponent.Count;
                int[,] subG = new int[_n, _n];
                float[,] subSimilarityMatrix = new float[_n, _n];

                Dictionary<int, int> newSimilarityNodeMap = new Dictionary<int, int>();
                for (int j = 0; j < connectedComponent.Count; j++)
                {
                    newSimilarityNodeMap[j] = similarityNodeMap[connectedComponent[j]];
                }
                for (int j = 0; j < _n; j++)
                {
                    for (int k = j + 1; k < _n; k++)
                    {
                        subSimilarityMatrix[j, k] = similarityMatrix[connectedComponent[j], connectedComponent[k]];
                        subSimilarityMatrix[k, j] = subSimilarityMatrix[j, k];
                        subG[j, k] = G2[connectedComponent[j], connectedComponent[k]];
                        subG[k, j] = subG[j, k];
                    }
                }
                List<int> newConnectedComponent = new List<int>();
                for (int j = 0; j < connectedComponent.Count; j++)
                {
                    newConnectedComponent.Add(newSimilarityNodeMap[j]);
                }
                if (_n == 1 || getDensity(subG, newConnectedComponent) >= d_min)
                {
                    denseClusters.Add(newConnectedComponent);
                }
                else
                {
                    Dictionary<int, int> reverseNodeMap = new Dictionary<int, int>();
                    for (int j = 0; j < connectedComponent.Count; j++)
                    {
                        reverseNodeMap[connectedComponent[j]] = j;
                    }
                    List<SimilarityElement> subSimilarityList = new List<SimilarityElement>();
                    for (int j = 0; j < similarityList.Count; j++)
                    {
                        int row = similarityList[j].row, col = similarityList[j].col;
                        if (connectedComponent.Exists(d => d == row) && connectedComponent.Exists(d => d == col))
                        {
                            SimilarityElement ele = new SimilarityElement() { row = reverseNodeMap[similarityList[j].row], col = reverseNodeMap[similarityList[j].col], similarity = similarityList[j].similarity };
                            subSimilarityList.Add(ele);
                        }
                    }
                    setDenseClusters(subG, subSimilarityMatrix, subSimilarityList, newSimilarityNodeMap);
                }
            }
        }

        List<List<int>> getConnectedComponents(float[,] similarityMatrix)
        {
            List<List<int>> connectedComponents = new List<List<int>>();
            List<int> connectedComponent = new List<int>();
            // Mark all the vertices as not visited
            int _n = similarityMatrix.GetLength(0);
            bool[] visited = new bool[_n];//bool型默认值为false
            for (int i = 0; i < _n; i++)
            {
                if (!visited[i])
                {
                    // print all reachable vertices from i
                    DFSUtil(i, visited);
                    // 合并数组
                    connectedComponents.Add(new List<int>(connectedComponent));//深拷贝，否则数组值会被下一行代码清空
                    connectedComponent.Clear();
                }
            }
            return connectedComponents;

            void DFSUtil(int i, bool[] visited)
            {
                // Mark the current node as visited and print it
                visited[i] = true;
                connectedComponent.Add(i);
                // Recur for all the vertices adjacent to this vertex
                for (int j = 0; j < _n; j++)
                {
                    if ((similarityMatrix[i, j] > 0) && (!visited[j]))
                    {
                        DFSUtil(j, visited);
                    }
                }
            }
        }

        float getDensity(int[,] subG, List<int> connectedComponent)
        {//matrix2 is an adjacency matrix
            int _n = subG.GetLength(0);
            float t = 0, w = 0;
            //计算w: the number of wedges (wedge is a two-hop path)
            for (int i = 0; i < _n; i++)
            {
                float line_w = 0;
                for (int j = 0; j < _n; j++)
                {
                    line_w += (float)subG[i, j];
                }
                w += (line_w * 0.5f * (line_w - 1));
            }
            if (w == 0f)
            {
                return 0f;
            }
            //计算t: the number of triangles
            for (int i = 0; i < _n; i++)
            {
                for (int j = i + 1; j < _n; j++)
                {
                    for (int k = j + 1; k < _n; k++)
                    {
                        t += matrix3[connectedComponent[i], connectedComponent[j], connectedComponent[k]];
                    }
                }
            }
            return (3f * t) / w;
        }
    }
}

class SimilarityElement
{
    public int row;
    public int col;
    public float similarity; //余弦相似度的值，在0~1之间
}

