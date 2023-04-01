using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using OculusSampleFramework;
using TMPro;
//using Oculus.Interaction.Input;

public class Cell
{
    public GameObject prefab;
    public Vector3 initialPosition;//原始的cube中坐标，不随着展开交互而更新
    public int x;//index
    public int y;
    public int z;
}

public class DataPlotter : MonoBehaviour
{
    public TMP_Text log;

    public float plotScale;

    // The prefab for the data points to be instantiated
    public GameObject CellPrefab;
    public GameObject ProjectionPrefab;
    public GameObject LabelPrefab;

    // The prefab for edge that used to highlight cells
    public GameObject crossHairX;
    public GameObject crossHairY;
    public GameObject crossHairZ;

    //选中的cell的z坐标
    private int currentIndex;

    //用来高亮triang cells所在平面
    public GameObject slice;
    Quaternion startRotation;
    Quaternion endRotation;
    private float currentSliceZ;
    private float lastSliceZ;


    // Object which will contain instantiated prefabs in hiearchy
    private GameObject CellHolder;
    private GameObject Label;
    private GameObject LabelX;
    private GameObject LabelY;
    private GameObject LabelZ;


    //节点数量+1，多一个邻接矩阵的维度
    private int n;

    public List<NodeData> nodes; 
    public List<LinkData> links;
    public Dictionary<string, HashSet<string>> nbrMap;
    public List<int> order;

    public int[,,] cube;
    private Vector3 cellPrefabScale;

    public List<Cell> CellList = new List<Cell>(); //所有的Cell，存的位置是原始的cube坐标，不随着展开而更新
    public Dictionary<string, List<GameObject>> CellPrefabMap = new Dictionary<string, List<GameObject>>(); // key: the nodeId that is contained by cell; value: the cell prefab
    public Dictionary<string, Color> CellClusterColorMap = new Dictionary<string, Color>();
    public List<Vector3> lastCellPositions = new List<Vector3>();
    public List<Vector3> currentCellPositions = new List<Vector3>();

    private float radius;

    public GameObject CubeObject;
    public GameObject PivotHolder;
    public GameObject XAxis;
    public GameObject YAxis;
    public GameObject ZAxis;
    public GameObject RotateIcon;
    public GameObject XRotate;
    public GameObject YRotate;
    public GameObject ZRotate;
    
    public GameObject XYPlane;
    public GameObject YZPlane;
    public GameObject XZPlane;


    //Controller-Button
    private OVRInput.Controller L_controller;
    private OVRInput.Controller R_controller;
    private bool lastStatusOfButtonA = false;
    private bool lastStatusOfButtonB = false;
    private bool lastStatusOfButtonX = false;
    private bool lastStatusOfButtonY = false;
    private bool lastStatusOfButtonStick = false;

    //Controller-Raycast
    public GameObject PhysicalControllerPointer_R;
    private Vector3 m_rayOrigin;
    private Vector3 m_rayDirection;
    private float m_rayDistance = Mathf.Infinity;
    private int m_rayMask = 1 << 6;
    private RaycastHit hit;
    private GameObject lastHitObject;
    private GameObject currentHitObject;
    private Ray ray;

    //Transition of expanding the cube
    private bool shouldUpdateCube = false;
    private bool shouldUpdateSlice = false;
    private bool isCubeRotating = false;
    private Quaternion initialCubeRotation;
    private float startTime;
    private float duration = 1f;
    private float expandWidth;
    private float interval;
    private bool optionSelected = false;

    //Node-link相关
    public class Node
    {
        public string id;
        public int cluster;
        public GameObject prefab;
    }

    public class Edge
    {
        public string id;
        public GameObject prefab;
    }
    public List<Node> RenderNodes;
    public List<Edge> RenderEdges;
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    private Dictionary<string, Node> nodesMap; //key is nodeid
    // Object which will contain instantiated prefabs in hiearchy
    public GameObject NodeLink;
    private GameObject NodeHolder;
    private GameObject EdgeHolder;
    private float width = 10f;
    private float height = 10f;

    private Vector2 center = new Vector2(5f, -6f);
    private Vector3 NL_ofs;
    private Vector3 nodePrefabScale;

    //GraphHolder
    public GameObject CellHolder_0;
    public GameObject Label_0;
    public GameObject NodeHolder_0;
    public GameObject EdgeHolder_0;
    public GameObject CellHolder_1;
    public GameObject Label_1;
    public GameObject NodeHolder_1;
    public GameObject EdgeHolder_1;
    public GameObject CellHolder_2;
    public GameObject Label_2;
    public GameObject NodeHolder_2;
    public GameObject EdgeHolder_2;

    //User Study Task相关
    private int TaskIndex;
    public int TaskforControl;//用来++，但不是实际的taskindex，仅表示当前是第几个Task Index
    private int datasetIndex;
    private int stage;
    private int condition = -1;
    private int lastCondition = -1;
    private int taskType;

    public int graphIndex;//用于根据数据集生成静态图，仅外部使用
    public TMP_Text TaskType;
    public TMP_Text TaskDescription;
    public TMP_Text OptionA;
    public TMP_Text OptionB;
    public TMP_Text Next;
    public TMP_Text Progress;
    public TMP_Text Stage;
    public TMP_Text Condition;
    public TMP_Text TimeConsumed;
    public TMP_Text Correct;
    public TMP_Text Wrong;


    private GameObject HoverNodeLabel;
    private Vector3 HoverNodeLabelOfs;

    private GameObject TaskNodeLabel0;
    private GameObject TaskNodeLabel1;

    public int userIndex;
    private string response;
    private float taskStart;//单位：s
    private float taskEnd;//单位：s



    //task by task
    private int cluster0 = -1;
    private int cluster1 = -1;
    private int node0 = -1;
    private int node1 = -1;
    private int node2 = -1;



    // Start is called before the first frame update
    void Start()
    {
        init();
    }

    // Update is called once per frame
    void Update()
    {
        updateTimer();
        OnClick();
        OnRaycast();
        OnStick();
        //Cube过渡动画函数
        if (!isCubeRotating && shouldUpdateCube)
        {
            float rate = (Time.time - startTime) / duration;
            bool hasUpdateSlice = false;
            if (rate < 1)
            {
                //Cell展开
                for (int i = 0; i < currentCellPositions.Count; i++)
                {
                    Vector3 lastPosition = lastCellPositions[i], currentPosition = currentCellPositions[i];
                    CellList[i].prefab.transform.position = Vector3.Lerp(lastPosition, currentPosition, rate);
                    if (shouldUpdateSlice && !hasUpdateSlice && CellList[i].z == currentIndex)//Slice跟着旋转
                    {
                        slice.GetComponent<Renderer>().transform.position = Vector3.Lerp(new Vector3(0.5f * plotScale, 0.5f * plotScale, lastPosition.z), new Vector3(0.5f * plotScale, 0.5f * plotScale, currentSliceZ), rate);
                        slice.GetComponent<Renderer>().transform.rotation = Quaternion.Lerp(startRotation, endRotation, rate);
                        hasUpdateSlice = true;
                    }
                }
            }
            else//过渡动画结束
            {
                //补上最后一帧，确保完全到达指定位置
                hasUpdateSlice = false;
                for (int i = 0; i < currentCellPositions.Count; i++)
                {
                    Vector3 lastPosition = lastCellPositions[i], currentPosition = currentCellPositions[i];
                    CellList[i].prefab.transform.position = currentPosition;
                    if (shouldUpdateSlice && !hasUpdateSlice && CellList[i].z == currentIndex)//Slice跟着旋转
                    {
                        slice.GetComponent<Renderer>().transform.position = new Vector3(0.5f * plotScale, 0.5f * plotScale, currentSliceZ);
                        slice.GetComponent<Renderer>().transform.rotation = endRotation;
                        hasUpdateSlice = true;
                    }
                }

                //最后一帧需要展示的内容
                if (slice.activeSelf)
                {
                    showTrianglesContainSameNode();
                }

                //停止动画
                shouldUpdateCube = false;
                lastCellPositions = currentCellPositions.ToList();//这里需要深拷贝

            }
        }
    }

    void updateTimer()
    {
        this.TimeConsumed.text = ""+ Mathf.Round(Time.time-this.taskStart);//四舍五入取整
    }
    void OnClick()
    {
        //存储上一个frame的结果，和当前进行比较，如果变了才触发事件响应

        //True: pressed, False: released
        bool CurrentStatusOfA = OVRInput.Get(OVRInput.Button.One, R_controller),
            CurrentStatusOfB = OVRInput.Get(OVRInput.Button.Two, R_controller),
            CurrentStatusOfX = OVRInput.Get(OVRInput.Button.One, L_controller),
            CurrentStatusOfY = OVRInput.Get(OVRInput.Button.Two, L_controller);

        //The Button A on the Right Oculus Touch controller is currently being pressed
        if (!lastStatusOfButtonA && CurrentStatusOfA)
        {
            if (currentHitObject != null)
            {
                log.text = "currentHitType: " + currentHitObject.tag;
                if(currentHitObject.tag == "Node")
                {
                    HighlightEgonetwork(currentHitObject);
                    if (this.condition == 1)
                    {
                        HighlightCubeTriangles(currentHitObject);
                    }
                } else if(currentHitObject.tag == "Cell")
                {
                    //展开Cube动画开始
                    int zIndex = GlobalParameter.CellZIndex(currentHitObject.name);
                    currentCellPositions = expandCubeAtNodeIndex(zIndex);
                    currentIndex = zIndex;
                    slice.SetActive(true);
                    shouldUpdateSlice = true;
                    shouldUpdateCube = true;
                    startTime = Time.time;
                    foreach(GameObject tmp in GameObject.FindGameObjectsWithTag("Temp"))
                    {
                        Destroy(tmp);
                    }
                } else if (currentHitObject.tag == "Option")
                {
                    if (this.response != null && this.response != currentHitObject.name)
                    {
                        GameObject.Find(this.response).GetComponent<TMP_Text>().color = GlobalParameter.OptionColor;
                    }
                    currentHitObject.GetComponent<TMP_Text>().color = GlobalParameter.OptionSelectedColor;
                    this.response = currentHitObject.name;
                    optionSelected = true;
                    if (this.stage == 0)//Train模式，需要显示答案正误
                    {
                        if (this.response == GlobalParameter.OptionList[this.TaskIndex]["answer"])
                        {
                            this.Correct.alpha = 1;
                            this.Wrong.alpha = 0;
                        }
                        else
                        {
                            this.Correct.alpha = 0;
                            this.Wrong.alpha = 1;
                        }
                    }
                } else if(optionSelected && currentHitObject.tag == "Next")
                {
                    currentHitObject.GetComponent<TMP_Text>().color = GlobalParameter.OptionSelectedColor;
                    this.taskEnd = Time.time;
                    if (this.stage == 1)//Test模式，需要记录Response
                    {
                        GlobalParameter.RecordUserResponse(this.userIndex, this.TaskIndex, this.response == GlobalParameter.OptionList[this.TaskIndex]["answer"], this.taskEnd - this.taskStart);
                    }
                    this.TaskforControl += 1;
                    //将cube复位
                    for (int i = 0; i < currentCellPositions.Count; i++)
                    {
                        CellList[i].prefab.transform.position = CellList[i].initialPosition;
                    }
                    //删除遗留的Temp对象
                    foreach (GameObject tmp in GameObject.FindGameObjectsWithTag("Temp"))
                    {
                        Destroy(tmp);
                    }
                    //按照新的task重新初始化场景
                    if (this.TaskforControl <= 18)
                    {
                        init();
                    }
                }

            }
            else
            {
                log.text = "currentHitObject: null";
                //收起Cube回到初始位置
                //Cell
                currentCellPositions = new List<Vector3>();
                for(int i = 0; i < CellList.Count; i++)
                {
                    currentCellPositions.Add(CellList[i].initialPosition);
                }

                slice.SetActive(false);
                shouldUpdateSlice = false;
                shouldUpdateCube = true;
                startTime = Time.time;
                foreach (GameObject tmp in GameObject.FindGameObjectsWithTag("Temp"))
                {
                    Destroy(tmp);
                }
            }

        }
        else if(lastStatusOfButtonA && !CurrentStatusOfA)
        {
            // The A button on the Left Oculus Touch controller is currently being released
        }

        //The Button B on the Right Oculus Touch controller is currently being pressed
        if (!lastStatusOfButtonB && CurrentStatusOfB)
        {
            //Cancel all the selection and highlights
            int i = 0;
            foreach (var nodeData in this.nodes)
            {
                string id = nodeData.id;
                if (this.condition == 1) {
                    //恢复cube中所有坐标轴label
                    GameObject.Find(GlobalParameter.LabelXId(id)).GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
                    GameObject.Find(GlobalParameter.LabelYId(id)).GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
                    GameObject.Find(GlobalParameter.LabelZId(id)).GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
                }
                //恢复node的scale和颜色
                Renderer nodeRenderer = GameObject.Find(GlobalParameter.NodePrefabId(nodeData.id)).GetComponent<Renderer>();
                nodeRenderer.transform.localScale = this.nodePrefabScale;
                if (i == this.node0)
                {
                    nodeRenderer.material.color = GlobalParameter.NodeColor;
                }
                else
                {
                    nodeRenderer.material.color = Utility.cluster2Color(nodeData.cluster);
                }
                i += 1;
            }
            foreach (var edgeData in this.links)
            {
                string source = edgeData.source, target = edgeData.target;
                //恢复edge至原来的color
                GameObject edge = GameObject.Find(GlobalParameter.EdgePrefabId(source, target));
                edge.GetComponent<Renderer>().material.color = GlobalParameter.EdgeColor;
            }
            if (this.condition == 1)
            {
                foreach (Cell cell in CellList)
                {
                    //恢复cell至原来的color
                    if (cell.prefab.tag == "Cell")
                    {
                        cell.prefab.GetComponent<Renderer>().material.color = CellClusterColorMap[cell.prefab.name];
                    }
                    //else
                    //{
                    //    cell.prefab.GetComponent<Renderer>().material.color = GlobalParameter.CellProjectionColor;
                    //}
                }
                foreach (GameObject cellProj in GameObject.FindGameObjectsWithTag("CellProj"))
                {
                    cellProj.GetComponent<Renderer>().material.color = GlobalParameter.CellProjectionColor;
                }
            }
        }
        lastStatusOfButtonA = CurrentStatusOfA;
        lastStatusOfButtonB = CurrentStatusOfB;
    }

    void OnRaycast()
    {
        // Get the world location and rotation of the controller
        m_rayOrigin = PhysicalControllerPointer_R.transform.position;
        m_rayDirection = PhysicalControllerPointer_R.transform.rotation * Vector3.forward;

        ray = new Ray(m_rayOrigin, m_rayDirection);

        // Cast a ray from the controller in the direction of its forward vector
        if (Physics.Raycast(ray, out hit, m_rayDistance, m_rayMask))//hit
        {
            // A collision occurred, do something with the hit object
            currentHitObject = hit.collider.gameObject;

            if (lastHitObject==null || currentHitObject.name != lastHitObject.name)//防止在hover同一个object时重复进入
            {

                if (currentHitObject.tag == "Node") //hover时高亮Node
                {
                    Color highlightColor = GlobalParameter.NodeHighlightColor;
                    Color initialColor = GlobalParameter.NodeColor;
                    if (lastHitObject == null)//第一次进入，直接高亮即可
                    {
                        //放大高亮当前hit中的node
                        currentHitObject.GetComponent<Renderer>().transform.localScale = this.nodePrefabScale * GlobalParameter.NodeEnlargeScale;
                        this.HoverNodeLabel.SetActive(true);
                        this.HoverNodeLabel.GetComponent<TMP_Text>().text = currentHitObject.name.Substring(6);
                        this.HoverNodeLabel.transform.position = this.HoverNodeLabelOfs + currentHitObject.transform.position;
                    }
                    else if (lastHitObject.name != currentHitObject.name)//非第一次进入，需要判断node与上一次不同才触发高亮
                    {
                        //放大高亮当前hit中的node
                        currentHitObject.GetComponent<Renderer>().transform.localScale = this.nodePrefabScale * GlobalParameter.NodeEnlargeScale;
                        this.HoverNodeLabel.SetActive(true);
                        this.HoverNodeLabel.GetComponent<TMP_Text>().text = currentHitObject.name.Substring(6);
                        this.HoverNodeLabel.transform.position = this.HoverNodeLabelOfs + currentHitObject.transform.position;
                        //取消高亮上一个，实现类似hover效果
                        if (lastHitObject.tag == "Node")
                        {
                            lastHitObject.GetComponent<Renderer>().transform.localScale = this.nodePrefabScale;
                        }

                    }
                }
                else if (currentHitObject.tag == "Cell")
                {
                    //高亮xyz垂线
                    Vector3 pos = currentHitObject.transform.localPosition;
                    crossHairX.SetActive(true);
                    crossHairY.SetActive(true);
                    crossHairZ.SetActive(true);

                    crossHairX.GetComponent<Renderer>().transform.localScale = new Vector3(0.05f, pos.x / 2, 0.05f);
                    crossHairX.GetComponent<Renderer>().transform.localPosition = new Vector3(pos.x / 2, pos.y, pos.z);
                    crossHairY.GetComponent<Renderer>().transform.localScale = new Vector3(0.05f, pos.y / 2, 0.05f);
                    crossHairY.GetComponent<Renderer>().transform.localPosition = new Vector3(pos.x, pos.y / 2, pos.z);
                    crossHairZ.GetComponent<Renderer>().transform.localScale = new Vector3(0.05f, pos.z / 2, 0.05f);
                    crossHairZ.GetComponent<Renderer>().transform.localPosition = new Vector3(pos.x, pos.y, pos.z / 2);

                    if (lastHitObject == null || lastHitObject.name != currentHitObject.name)
                    {
                        //高亮label
                        toggleLabelColorOfCell(currentHitObject.transform.name, GlobalParameter.labelHightlightColor);
                        if (lastHitObject != null && lastHitObject.tag == "Cell")
                        {
                            //取消高亮label
                            toggleLabelColorOfCell(lastHitObject.transform.name, GlobalParameter.labelColor);
                        }
                    }
                }
                else if (currentHitObject.tag == "Option")
                {
                    if(this.response!=null)
                    {
                        if(this.response != currentHitObject.name)
                        {
                            currentHitObject.GetComponent<TMP_Text>().color = GlobalParameter.OptionHighlightedColor;
                        }
                    }
                    else
                    {
                        this.OptionA.color = GlobalParameter.OptionColor;
                        this.OptionB.color = GlobalParameter.OptionColor;
                        currentHitObject.GetComponent<TMP_Text>().color = GlobalParameter.OptionHighlightedColor;
                    }
                }
                else if (optionSelected && currentHitObject.tag == "Next")
                {
                    currentHitObject.GetComponent<TMP_Text>().color = GlobalParameter.OptionHighlightedColor;
                }
                lastHitObject = currentHitObject;
            }
        }
        else
        {
            currentHitObject = null;
            if (lastHitObject != null)//not hit
            {
                if(lastHitObject.tag == "Node")
                {
                    //取消高亮上一个node
                    lastHitObject.GetComponent<Renderer>().transform.localScale = this.nodePrefabScale;
                    this.HoverNodeLabel.SetActive(false);

                } else if(lastHitObject.tag == "Cell")
                {
                    //清除高亮的xyz垂线
                    crossHairX.SetActive(false);
                    crossHairY.SetActive(false);
                    crossHairZ.SetActive(false);

                    //取消高亮label
                    toggleLabelColorOfCell(lastHitObject.transform.name, GlobalParameter.labelColor);
                }
                else if (lastHitObject.tag == "Option" || lastHitObject.tag == "Next")
                {
                    if (this.response == null || lastHitObject.name != this.response)
                    {
                        lastHitObject.GetComponent<TMP_Text>().color = GlobalParameter.OptionColor;
                    }
                }
            }
            lastHitObject = currentHitObject;
        }
    }

    void OnStick()
    {
        bool currentStatusOfButtonStick = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, R_controller);
        if (!lastStatusOfButtonStick && currentStatusOfButtonStick){//按键按下，切换rotating状态
            isCubeRotating = !isCubeRotating;
            if (!isCubeRotating) //转回原位，为了后续的展开交互
            {
                RotateIcon.SetActive(false);
                CubeObject.transform.rotation = initialCubeRotation;
            }
            else
            {
                RotateIcon.SetActive(true);
            }
        }

        if (isCubeRotating) {
            RotateCube();
        }

        lastStatusOfButtonStick = currentStatusOfButtonStick;
    }

    void RotateCube()
    {
        Vector2 stickAxis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, R_controller);
        log.text = "" + stickAxis;
        // 前：x=1; 后：x=-1; 左：y=-1，右：y=1
        // Create a rotation vector based on the input values
        Vector3 rotation = new Vector3(0, -stickAxis.x, stickAxis.y);
        // Apply the rotation to the game object
        CubeObject.transform.Rotate(rotation * 30f * Time.deltaTime, Space.Self);
    }

    void HighlightEgonetwork(GameObject nodeObject)
    {
        //高亮改为放大
        nodeObject.GetComponent<Renderer>().transform.localScale = this.nodePrefabScale * GlobalParameter.NodeEnlargeScale;
        string nodeId = nodeObject.name.Split(": ")[1];
        if (condition == 1)
        {
            foreach (var nodeData in this.nodes)
            {
                string id = nodeData.id;
                //高亮之前先恢复cube中所有坐标轴label
                GameObject.Find(GlobalParameter.LabelXId(id)).GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
                GameObject.Find(GlobalParameter.LabelYId(id)).GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
                GameObject.Find(GlobalParameter.LabelZId(id)).GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
            }
        }
        //先恢复节点颜色
        int i = 0;
        foreach (var nodeData in this.nodes)
        {
            string id = nodeData.id;
            GameObject nodePrefab = GameObject.Find(GlobalParameter.NodePrefabId(id));
            if (i == this.node0)
            {
                nodePrefab.GetComponent<Renderer>().material.color = GlobalParameter.NodeColor;
            }
            else
            {
                nodePrefab.GetComponent<Renderer>().material.color = Utility.cluster2Color(nodeData.cluster);
            }
            i += 1;
        }

        foreach (var edgeData in this.links)
        {
            string source = edgeData.source, target = edgeData.target;
            //高亮之前先恢复至原来的color
            GameObject edge = GameObject.Find(GlobalParameter.EdgePrefabId(source, target));
            edge.GetComponent<Renderer>().material.color = GlobalParameter.EdgeColor;
        }

        HashSet<string> highlightNodes = new HashSet<string>();
        highlightNodes.Add(nodeId);
        foreach (var edgeData in this.links)
        {
            string source = edgeData.source, target = edgeData.target;
            if ((this.nbrMap[nodeId].Contains(source) && this.nbrMap[nodeId].Contains(target)) || (nodeId == source)|| (nodeId == target))
            {//如果构成三角形
                //高亮所有三角形的非邻接边，以及该节点的邻接边
                var linerenderer = GameObject.Find(GlobalParameter.EdgePrefabId(source, target))
                   .GetComponent<Renderer>();
                linerenderer.material.color = GlobalParameter.EdgeHighlightColor;
                highlightNodes.Add(source);
                highlightNodes.Add(target);
            }
        }

        //fade除了highlightnodes以外所有其他节点的颜色, 只高亮子图。
        foreach (var nodeData in this.nodes)
        {
            string id = nodeData.id;
            if (!highlightNodes.Contains(id)){
                GameObject nodePrefab = GameObject.Find(GlobalParameter.NodePrefabId(id));
                nodePrefab.GetComponent<Renderer>().material.color = GlobalParameter.NodeColor;
            }
        }
    }

    void HighlightCubeTriangles(GameObject nodeObject)
    {
        //先取消所有cell的高亮
        foreach (Cell cell in CellList)
        {
            //恢复cell至原来的color
            if (cell.prefab.tag == "Cell")
            {
                cell.prefab.GetComponent<Renderer>().material.color = GlobalParameter.CellColor;

            }
            else
            {
                cell.prefab.GetComponent<Renderer>().material.color = GlobalParameter.CellProjectionColor;
            }
        }

        //高亮当前选中的所有三角形和投影面上的边
        string nodeId = nodeObject.name.Split(": ")[1];
        if (CellPrefabMap.ContainsKey(nodeId))
        {
            List<GameObject> cells = CellPrefabMap[nodeId];
            foreach (GameObject cell in cells)
            {
                if(cell.tag == "Cell")//高亮当前选中的所有三角形
                {
                    cell.GetComponent<Renderer>().material.color = CellClusterColorMap[cell.name];
                    //高亮当前Cell的所有坐标轴label
                    toggleLabelColorOfCell(cell.GetComponent<Renderer>().transform.name, GlobalParameter.labelHightlightColor);
                }
                else//高亮当前选中的所有投影面上的边
                {
                    cell.GetComponent<Renderer>().material.color = GlobalParameter.CellProjectionHighlightColor;
                }
            }
        }
    }

    void toggleLabelColorOfCell(string axisname, Color color)
    {
        axisname = axisname.Substring(7, axisname.Length - 8);//E.g.，从"Cell: (1, 2, 3)"提取出"1, 2, 3"
        string[] axises = axisname.Split(", ");
        if (axises.Length < 3) { return; }
        string x = GlobalParameter.LabelXId(this.nodes[order[int.Parse(axises[0])]].id),
            y = GlobalParameter.LabelYId(this.nodes[order[int.Parse(axises[1])]].id),
            z = GlobalParameter.LabelZId(this.nodes[order[int.Parse(axises[2])]].id);
        GameObject labelX = GameObject.Find(x), labelY = GameObject.Find(y), labelZ = GameObject.Find(z);
        labelX.GetComponent<TMP_Text>().color = color;
        labelY.GetComponent<TMP_Text>().color = color;
        labelZ.GetComponent<TMP_Text>().color = color;
    }

    //Compute the position for each selected node index to implement the transition; 
    List<Vector3> expandCubeAtNodeIndex(int index)//index should be from 0~(n-1)
    {
        Dictionary<string, List<Vector3>> result = new Dictionary<string, List<Vector3>>();
        List<Vector3> newCellPositions = new List<Vector3>();

        //沿z轴展开cell
        foreach (Cell cell in CellList)
        {
            if (cell.prefab.tag == "Cell")
            {
                float offset = (cell.z - 1) * interval;

                Vector3 position = cell.initialPosition;
                //这里注意区分：cell.z是int，和index对应；而position.z是坐标，经过了归一化，是float，二者不同
                float x = position.x, y = position.y, z = position.z;

                if (cell.z < index)//选中index的左边区域：直接向右平移offset
                {
                    z += offset;
                }
                else if (cell.z > index) //选中index的右边区域：先向右平移offset，再加上中间的OffsetWidth
                {
                    z += offset + expandWidth;
                }
                else//选中的index
                {
                    //赋值slice的z坐标
                    currentSliceZ = position.z + expandWidth / 2 + offset;

                    //先平移，使得y轴经过plane的中心，为下一步旋转做准备
                    x = x - expandWidth / 2;
                    z = z - position.z;

                    //绕y轴逆时针旋转90°
                    float theta = Mathf.PI / 2; //注意旋转角度为弧度
                    float _x = x, _z = z; //暂存x和z的值
                    x = Mathf.Cos(theta) * _x - Mathf.Sin(theta) * _z;
                    z = Mathf.Sin(theta) * _x + Mathf.Cos(theta) * _z;

                    //旋转完后，再平移回去并加上offset
                    x = x + expandWidth / 2;
                    z = z + position.z + expandWidth / 2 + offset;

                }

                //更新的cell的position
                newCellPositions.Add(new Vector3(x, y, z));
            }

        }

        return newCellPositions;
    }
    void showTrianglesContainSameNode()
    {
        //展示当前展开的node的label
        GameObject nodeLabel = Instantiate(LabelPrefab);
        nodeLabel.transform.rotation = Quaternion.Euler(0, -90, 0f);
        nodeLabel.transform.position = new Vector3(0.8f * plotScale, 0.9f * plotScale, currentSliceZ);
        nodeLabel.transform.tag = "Temp";
        nodeLabel.transform.name = "nodeLabel";
        nodeLabel.transform.parent = this.Label.transform;
        nodeLabel.GetComponent<TMP_Text>().text = this.nodes[order[currentIndex]].id;
        nodeLabel.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;

        //展示坐标轴的label
        Vector3 ofsZ = new Vector3(5f, -1.435f, 0.8f);
        for(int j = 1; j < n; j++)//横轴
        {
            string axis_name = this.nodes[order[j]].id;
            GameObject axisLabel = Instantiate(LabelPrefab);
            axisLabel.transform.tag = "Temp";
            axisLabel.transform.parent = nodeLabel.transform;
            axisLabel.GetComponent<TMP_Text>().text = axis_name;
            axisLabel.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
            axisLabel.transform.rotation = Quaternion.Euler(0, -90, -45f);
            axisLabel.transform.position = ofsZ + new Vector3(0, 0, currentSliceZ - expandWidth / 2 + Convert.ToSingle(j - 1) / n * plotScale);
        }
        Vector3 ofsY = new Vector3(5f,0.15f,-1.6f);
        for (int j = 1; j < n; j++)//竖轴
        {
            string axis_name = this.nodes[order[j]].id;
            GameObject axisLabel = Instantiate(LabelPrefab);
            axisLabel.transform.tag = "Temp";
            axisLabel.transform.parent = nodeLabel.transform;
            axisLabel.GetComponent<TMP_Text>().text = axis_name;
            axisLabel.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
            axisLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;
            axisLabel.transform.rotation = Quaternion.Euler(0, -90, 0f);
            axisLabel.transform.position = ofsY + new Vector3(0, Convert.ToSingle(j - 1) / n * plotScale, currentSliceZ - expandWidth / 2);
        }

        //展示对称过来的三角形
        for (int x = 1; x < n; x++)
        {
            for (int y = 1; y < n; y++)
            {
                for (int z = 1; z < n; z++)
                {
                    if (cube[x, y, z] == 1)
                    {
                        float pos_x = 0.5f * plotScale, pos_y = 0f * plotScale, pos_z = currentSliceZ - expandWidth/2;
                        if (x == currentIndex)
                        {
                            if (y > z)
                            {
                                pos_y += Convert.ToSingle(y) / n * plotScale;
                                pos_z += Convert.ToSingle(z) / n * plotScale;
                            }
                            else
                            {
                                pos_y += Convert.ToSingle(z) / n * plotScale;
                                pos_z += Convert.ToSingle(y) / n * plotScale;
                            }
                        }
                        else if (y == currentIndex)
                        {
                            if (x > z)
                            {
                                pos_y += Convert.ToSingle(x) / n * plotScale;
                                pos_z += Convert.ToSingle(z) / n * plotScale;
                            }
                            else
                            {
                                pos_y += Convert.ToSingle(z) / n * plotScale;
                                pos_z += Convert.ToSingle(x) / n * plotScale;
                            }
                        }
                        else
                        {
                            continue;
                        }
                        // Instantiate as gameobject variable so that it can be manipulated within loop
                        GameObject dataPoint = Instantiate(
                            CellPrefab,
                            new Vector3(pos_x, pos_y, pos_z),
                            Quaternion.identity);
                        dataPoint.transform.tag = "Temp";
                        // Make dataPoint child of CellHolder object 
                        dataPoint.transform.parent = this.PivotHolder.transform;
                        // Gets material color and sets it to a new RGBA color we define
                        dataPoint.GetComponent<Renderer>().material.color = CellClusterColorMap[GlobalParameter.CellPrefabId(x,y,z)];
                        dataPoint.GetComponent<Renderer>().transform.localScale = this.cellPrefabScale;
                    }
                }
            }
        }
    }
    void initNodelinkParameters()
    {
        GraphData graph;
        switch (this.datasetIndex)
        {
            case 0:
                graph = DataProcessor0.graph;
                this.nodePrefabScale = new Vector3(0.3f, 0.3f,0.3f);
                NodeHolder_0.SetActive(true);
                EdgeHolder_0.SetActive(true);
                break;
            case 1:
                graph = DataProcessor1.graph;
                this.nodePrefabScale = new Vector3(0.3f, 0.3f, 0.3f);
                NodeHolder_1.SetActive(true);
                EdgeHolder_1.SetActive(true);
                break;
            case 2:
                graph = DataProcessor2.graph;
                this.nodePrefabScale = new Vector3(0.3f, 0.3f, 0.3f);
                NodeHolder_2.SetActive(true);
                EdgeHolder_2.SetActive(true);
                break;
            default:
                graph = DataProcessor0.graph;
                this.nodePrefabScale = new Vector3(0.3f, 0.3f, 0.3f);
                break;
        }

        //初始化Nodelink Offset，用于不同的Condition
        if (this.lastCondition == -1)
        {
            if (this.condition == 0)
            {
                this.NL_ofs = new Vector3(0, 0, this.plotScale);
            }
            else
            {
                //this.NL_ofs = new Vector3(0, 0, 0);
                this.NL_ofs = new Vector3(0, 0, 2.2f * this.plotScale);
            }
        }
        else if(this.lastCondition == this.condition)
        {
            this.NL_ofs = new Vector3(0, 0, 0);
        } else if(this.lastCondition == 0)
        {
            this.NL_ofs = new Vector3(0, 0, -this.plotScale);
        } else if(this.lastCondition == 1)
        {
            this.NL_ofs = new Vector3(0, 0, this.plotScale);
        }

        //根据不同的任务，渲染的节点和cluster有所不同
        int i;
        switch (this.taskType)
        {
            case 0:
                this.cluster0 = GlobalParameter.TaskList[this.TaskIndex]["cluster0"];
                this.cluster1 = GlobalParameter.TaskList[this.TaskIndex]["cluster1"];
                foreach (var nodeData in graph.nodes)
                {
                    string nodeId = nodeData.id;
                    GameObject nodePrefab = GameObject.Find(GlobalParameter.NodePrefabId(nodeId));
                    nodePrefab.GetComponent<Renderer>().transform.position += this.NL_ofs;
                    nodePrefab.GetComponent<Renderer>().transform.localScale = nodePrefabScale;
                    if (nodeData.cluster == this.cluster0 || nodeData.cluster == this.cluster1)
                    {
                        nodePrefab.GetComponent<Renderer>().material.color = Utility.cluster2Color(nodeData.cluster);
                    }
                    else
                    {
                        nodePrefab.GetComponent<Renderer>().material.color = GlobalParameter.NodeColor;
                    }
                }
                break;
            case 1:
                this.node0 = GlobalParameter.TaskList[this.TaskIndex]["node0"];
                i = 0;
                foreach (var nodeData in graph.nodes)
                {
                    string nodeId = nodeData.id;
                    GameObject nodePrefab = GameObject.Find(GlobalParameter.NodePrefabId(nodeId));
                    nodePrefab.GetComponent<Renderer>().transform.position += this.NL_ofs;
                    nodePrefab.GetComponent<Renderer>().transform.localScale = nodePrefabScale;
                    nodePrefab.GetComponent<Renderer>().material.color = Utility.cluster2Color(nodeData.cluster);
                    if(i == this.node0)
                    {
                        //隐藏其cluster颜色
                        nodePrefab.GetComponent<Renderer>().material.color = GlobalParameter.NodeColor;
                        //显示node的label
                        this.TaskNodeLabel0 = Instantiate(LabelPrefab);
                        this.TaskNodeLabel0.transform.rotation = Quaternion.Euler(0, -90, 0f);
                        this.TaskNodeLabel0.transform.name = "task_label_0";
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().fontSize = 2;
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().text = nodePrefab.name.Substring(6);
                        this.TaskNodeLabel0.transform.position = this.HoverNodeLabelOfs + nodePrefab.transform.position;
                    }
                    i++;
                }
                break;
            case 2:
                this.node1 = GlobalParameter.TaskList[this.TaskIndex]["node1"];
                this.node2 = GlobalParameter.TaskList[this.TaskIndex]["node2"];

                i = 0;
                foreach (var nodeData in graph.nodes)
                {
                    string nodeId = nodeData.id;
                    GameObject nodePrefab = GameObject.Find(GlobalParameter.NodePrefabId(nodeId));
                    nodePrefab.GetComponent<Renderer>().transform.position += this.NL_ofs;
                    nodePrefab.GetComponent<Renderer>().transform.localScale = nodePrefabScale;
                    nodePrefab.GetComponent<Renderer>().material.color = Utility.cluster2Color(nodeData.cluster);
                    if (i == this.node1)
                    {
                        //显示node的label
                        this.TaskNodeLabel0 = Instantiate(LabelPrefab);
                        this.TaskNodeLabel0.transform.rotation = Quaternion.Euler(0, -90, 0f);
                        this.TaskNodeLabel0.transform.name = "task_label_0";
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().fontSize = 2;
                        this.TaskNodeLabel0.GetComponent<TMP_Text>().text = nodePrefab.name.Substring(6);
                        this.TaskNodeLabel0.transform.position = this.HoverNodeLabelOfs + nodePrefab.transform.position;
                    }
                    else if (i == this.node2)
                    {
                        //显示node的label
                        this.TaskNodeLabel1 = Instantiate(LabelPrefab);
                        this.TaskNodeLabel1.transform.rotation = Quaternion.Euler(0, -90, 0f);
                        this.TaskNodeLabel1.transform.name = "task_label_1";
                        this.TaskNodeLabel1.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
                        this.TaskNodeLabel1.GetComponent<TMP_Text>().fontSize = 2;
                        this.TaskNodeLabel1.GetComponent<TMP_Text>().text = nodePrefab.name.Substring(6);
                        this.TaskNodeLabel1.transform.position = this.HoverNodeLabelOfs + nodePrefab.transform.position;
                    }
                    i++;
                }
                break;
        }

        foreach (var edgeData in graph.links)
        {
            string source = edgeData.source, target = edgeData.target;
            GameObject edgePrefab = GameObject.Find(GlobalParameter.EdgePrefabId(source, target));
            // Get the start and end points of the line
            Vector3 startPoint = edgePrefab.GetComponent<LineRenderer>().GetPosition(0);
            Vector3 endPoint = edgePrefab.GetComponent<LineRenderer>().GetPosition(edgePrefab.GetComponent<LineRenderer>().positionCount - 1);
            edgePrefab.GetComponent<LineRenderer>().SetPositions(new Vector3[] { startPoint + this.NL_ofs, endPoint + this.NL_ofs });
            edgePrefab.GetComponent<LineRenderer>().material.color = GlobalParameter.EdgeColor;

        }
    }
    void initCubeParameters()
    {
        switch (this.datasetIndex)
        {
            case 0:
                CellHolder_0.SetActive(true);
                Label_0.SetActive(true);
                break;
            case 1:
                CellHolder_1.SetActive(true);
                Label_1.SetActive(true);
                break;
            case 2:
                CellHolder_2.SetActive(true);
                Label_2.SetActive(true);
                break;
        }

        this.Label = GameObject.Find("Label_" + this.datasetIndex);

        CellList = new List<Cell>(); //所有的Cell，存的位置是原始的cube坐标，不随着展开而更新
        CellPrefabMap = new Dictionary<string, List<GameObject>>(); // key: the nodeId that is contained by cell; value: the cell prefab
        CellClusterColorMap = new Dictionary<string, Color>();
        lastCellPositions = new List<Vector3>();
        currentCellPositions = new List<Vector3>();

        //更新cube相关的全局变量，用于交互
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                for (int z = 0; z < n; z++)
                {
                    if (cube[x, y, z] == 1)
                    {
                        var initialPosition = new Vector3(Convert.ToSingle(x) / n, Convert.ToSingle(y) / n, Convert.ToSingle(z) / n) * plotScale;
                        string cellId = GlobalParameter.CellPrefabId(x, y, z);
                        GameObject dataPoint = GameObject.Find(cellId);
                        if (x > 0 && y > 0 && z > 0)
                        { //triangle cells
                            // Store prefabs to the maps for interaction
                            string Xid = this.nodes[order[x]].id,
                            Yid = this.nodes[order[y]].id,
                            Zid = this.nodes[order[z]].id;
                            if (!CellPrefabMap.ContainsKey(Xid)) { CellPrefabMap.Add(Xid, new List<GameObject>()); }
                            if (!CellPrefabMap.ContainsKey(Yid)) { CellPrefabMap.Add(Yid, new List<GameObject>()); }
                            if (!CellPrefabMap.ContainsKey(Zid)) { CellPrefabMap.Add(Zid, new List<GameObject>()); }
                            CellPrefabMap[Xid].Add(dataPoint);
                            CellPrefabMap[Yid].Add(dataPoint);
                            CellPrefabMap[Zid].Add(dataPoint);
                            lastCellPositions.Add(initialPosition);

                            Color cellColor;
                            int clusterX = this.nodes[order[x]].cluster,
                                clusterY = this.nodes[order[y]].cluster,
                                clusterZ = this.nodes[order[z]].cluster;
                            if (clusterX == clusterY || clusterX == clusterZ)
                            {
                                CellClusterColorMap.Add(cellId, Utility.cluster2Color(clusterX));
                                if (clusterX==this.cluster0 || clusterX == this.cluster1)
                                {
                                    cellColor = Utility.cluster2Color(clusterX);
                                }
                                else { cellColor = GlobalParameter.CellColor;}
                                
                            }
                            else if (clusterY == clusterZ)
                            {
                                CellClusterColorMap.Add(cellId, Utility.cluster2Color(clusterY));
                                if (clusterY == this.cluster0 || clusterY == this.cluster1)
                                {
                                    cellColor = Utility.cluster2Color(clusterY);
                                }
                                else { cellColor = GlobalParameter.CellColor;}
                            }
                            else
                            {
                                cellColor = GlobalParameter.OtherCellColor;
                                CellClusterColorMap.Add(cellId, cellColor);
                            }
                            // Gets material color and sets it to a new RGBA color we define
                            dataPoint.GetComponent<Renderer>().material.color = cellColor;
                            CellList.Add(new Cell() { prefab = dataPoint, x = x, y = y, z = z, initialPosition = initialPosition });
                        }
                        else
                        { //cell projections
                            if (x > 0)
                            {
                                string Xid = this.nodes[order[x]].id;
                                if (!CellPrefabMap.ContainsKey(Xid)) { CellPrefabMap.Add(Xid, new List<GameObject>()); }
                                CellPrefabMap[Xid].Add(dataPoint);
                            }
                            if (y > 0)
                            {
                                string Yid = this.nodes[order[y]].id;
                                if (!CellPrefabMap.ContainsKey(Yid)) { CellPrefabMap.Add(Yid, new List<GameObject>()); }
                                CellPrefabMap[Yid].Add(dataPoint);
                            }
                            if (z > 0)
                            {
                                string Zid = this.nodes[order[z]].id;
                                if (!CellPrefabMap.ContainsKey(Zid)) { CellPrefabMap.Add(Zid, new List<GameObject>()); }
                                CellPrefabMap[Zid].Add(dataPoint);
                            }

                            // Gets material color and sets it to a new RGBA color we define
                            dataPoint.GetComponent<Renderer>().material.color = GlobalParameter.CellProjectionColor;
                        }
                        dataPoint.GetComponent<Renderer>().transform.localScale = this.cellPrefabScale;
                    }

                }
            }
        }
    }

    //初始化成员变量
    public void initView()
    {
        this.TaskIndex = GlobalParameter.realTaskByUserGroup(this.TaskforControl, this.userIndex);
        //初始化任务参数
        this.datasetIndex = GlobalParameter.TaskList[this.TaskIndex]["dataset"];
        this.stage = GlobalParameter.TaskList[this.TaskIndex]["stage"];
        this.lastCondition = this.condition;
        this.condition = GlobalParameter.TaskList[this.TaskIndex]["condition"];
        this.taskType = GlobalParameter.TaskList[this.TaskIndex]["task"];

        //初始化node label
        if (this.HoverNodeLabel != null)
        {
            Destroy(this.HoverNodeLabel);
        }
        this.HoverNodeLabel = Instantiate(LabelPrefab);
        this.HoverNodeLabel.transform.rotation = Quaternion.Euler(0, -90, 0f);
        this.HoverNodeLabel.transform.name = "hover_label";
        this.HoverNodeLabel.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        this.HoverNodeLabel.GetComponent<TMP_Text>().fontSize = 2;
        this.HoverNodeLabel.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;
        this.HoverNodeLabel.SetActive(false);

        //初始化任务面板文本
        foreach (GameObject option in GameObject.FindGameObjectsWithTag("Option"))
        {
            option.GetComponent<TMP_Text>().color = GlobalParameter.OptionColor;
        }
        this.Next.GetComponent<TMP_Text>().color = GlobalParameter.OptionColor;
        this.OptionA.text = "A. " + GlobalParameter.OptionList[this.TaskIndex]["OptionA"];
        this.OptionB.text = "B. " + GlobalParameter.OptionList[this.TaskIndex]["OptionB"];
        //默认隐藏答案，只在train模式且选择完后显示其中一个
        this.Correct.alpha = 0;
        this.Wrong.alpha = 0;

        switch (this.stage)
        {
            case 0:
                this.Stage.text = "Train";
                this.Progress.text = "(" + (this.TaskforControl + 1) + " / 6)";
                break;
            case 1:
                this.Stage.text = "Test";
                this.Progress.text = "(" + (this.TaskforControl - 5) + " / 12)";
                break;
        }
        switch (this.condition)
        {
            case 0:
                this.Condition.text = "C1";
                CubeObject.SetActive(false);
                this.HoverNodeLabelOfs = new Vector3(0.2f, -0.3f, 0f);
                break;

            case 1:
                this.Condition.text = "C2";
                CubeObject.SetActive(true);
                //this.HoverNodeLabelOfs = new Vector3(0.2f, -0.3f, 0.15f);
                this.HoverNodeLabelOfs = new Vector3(0.2f, -0.3f, 0);
                break;
        }
        switch (this.taskType)
        {
            case 0:
                this.TaskType.text = "Type 1";
                this.TaskDescription.text = "Two clusters are highlighted. \n Which one is denser?";
                break;
            case 1:
                this.TaskType.text = "Type 2";
                this.TaskDescription.text = "A node is highlighted in white. \n Which cluster is it in?";
                break;
            case 2:
                this.TaskType.text = "Type 3";
                this.TaskDescription.text = "Two nodes are highlighted. \n Which one is more influential?";
                break;
        }
        //初始化Cube旋转参数
        CubeObject.transform.rotation = Quaternion.Euler(0, 0, 0);
        initialCubeRotation = CubeObject.transform.rotation;
        //CubeObject.transform.position = CubeObject.transform.position + new Vector3(0.5f, 0.5f, 0.5f) * plotScale;
        //PivotHolder.transform.position = PivotHolder.transform.position - new Vector3(0.5f, 0.5f, 0.5f) * plotScale;

        //初始化坐标轴颜色
        XAxis.GetComponent<Renderer>().sharedMaterial.color = Color.red;
        YAxis.GetComponent<Renderer>().sharedMaterial.color = Color.green;
        ZAxis.GetComponent<Renderer>().sharedMaterial.color = Color.blue;
        XRotate.GetComponent<Renderer>().sharedMaterial.color = new Color(255 / 255f, 0 / 255f, 0 / 255f, 0.6f);
        YRotate.GetComponent<Renderer>().sharedMaterial.color = new Color(0 / 255f, 255 / 255f, 0 / 255f, 0.6f);
        ZRotate.GetComponent<Renderer>().sharedMaterial.color = new Color(0 / 255f, 0 / 255f, 255 / 255f, 0.6f);
        RotateIcon.SetActive(false);

        //初始化坐标面颜色
        XYPlane.GetComponent<Renderer>().sharedMaterial.color = GlobalParameter.PlaneColor;
        YZPlane.GetComponent<Renderer>().sharedMaterial.color = GlobalParameter.PlaneColor;
        XZPlane.GetComponent<Renderer>().sharedMaterial.color = GlobalParameter.PlaneColor;


        //初始化Crosshairs（十字线）

        crossHairX.transform.name = "crossHairX";
        crossHairY.transform.name = "crossHairY";
        crossHairZ.transform.name = "crossHairZ";

        crossHairX.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
        crossHairY.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
        crossHairZ.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;

        crossHairX.SetActive(false);
        crossHairY.SetActive(false);
        crossHairZ.SetActive(false);

        //初始化Controller
        L_controller = OVRInput.Controller.LTouch;
        R_controller = OVRInput.Controller.RTouch;

        //初始化Slice，高亮用
        slice.transform.parent = XYPlane.transform.parent;
        slice.transform.name = "slice";
        slice.GetComponent<Renderer>().sharedMaterial.color = GlobalParameter.SliceColor;
        slice.GetComponent<Renderer>().transform.position = new Vector3(0.5f, 0.5f, 0f) * plotScale;
        slice.SetActive(false);//默认隐藏，只在展开时显示
        startRotation = XYPlane.transform.rotation;
        endRotation = XYPlane.transform.rotation * Quaternion.Euler(0, 0, 90f);

        //初始化展开动画参数
        expandWidth = plotScale;
        interval = 0;

        //初始化其他参数
        lastStatusOfButtonA = false;
        lastStatusOfButtonB = false;
        lastStatusOfButtonX = false;
        lastStatusOfButtonY = false;
        lastStatusOfButtonStick = false;

        shouldUpdateCube = false;
        shouldUpdateSlice = false;
        isCubeRotating = false;
        duration = 1f;
        optionSelected = false;

        this.response = null;

        //先全部隐藏所有数据，再按需开启
        CellHolder_0.SetActive(false);
        Label_0.SetActive(false);
        NodeHolder_0.SetActive(false);
        EdgeHolder_0.SetActive(false);

        CellHolder_1.SetActive(false);
        Label_1.SetActive(false);
        NodeHolder_1.SetActive(false);
        EdgeHolder_1.SetActive(false);

        CellHolder_2.SetActive(false);
        Label_2.SetActive(false);
        NodeHolder_2.SetActive(false);
        EdgeHolder_2.SetActive(false);

        //task_label只在渲染nodelink的时候才初始化
        if (this.TaskNodeLabel0 != null)
        {
            Destroy(this.TaskNodeLabel0);
        }
        if (this.TaskNodeLabel1 != null)
        {
            Destroy(this.TaskNodeLabel1);
        }
        //for task type 0
        this.cluster0 = -1;
        this.cluster1 = -1;
        //for task type 1
        this.node0 = -1;
        //for task type 2
        this.node1 = -1;
        this.node2 = -1;
    }
    void init()
    {
        initView();
        //初始化公共参数
        switch (this.datasetIndex)
        {
            case 0:
                this.n = DataProcessor0.n + 1;
                this.nodes = DataProcessor0.graph.nodes;
                this.links = DataProcessor0.graph.links;
                var dataProcessor0 = new DataProcessor0();
                this.nbrMap = DataProcessor0.nbrMap;
                this.cube = dataProcessor0.getCube(0.75f);
                this.order = dataProcessor0.order;
                this.cellPrefabScale = new Vector3(0.2f, 0.2f, 0.2f);
                break;
            case 1:
                this.n = DataProcessor1.n + 1;
                this.nodes = DataProcessor1.graph.nodes;
                this.links = DataProcessor1.graph.links;
                var dataProcessor1 = new DataProcessor1();
                this.nbrMap = DataProcessor1.nbrMap;
                this.cube = dataProcessor1.getCube(0.75f);
                this.order = dataProcessor1.order;
                this.cellPrefabScale = new Vector3(0.1f, 0.1f, 0.1f);
                break;
            case 2:
                this.n = DataProcessor2.n + 1;
                this.nodes = DataProcessor2.graph.nodes;
                this.links = DataProcessor2.graph.links;
                var dataProcessor2 = new DataProcessor2();
                this.nbrMap = DataProcessor2.nbrMap;
                this.cube = dataProcessor2.getCube(0.75f);
                this.order = dataProcessor2.order;
                this.cellPrefabScale = new Vector3(0.1f, 0.1f, 0.1f);
                break;
        }
        //基于当前TaskIndex更新Cube相关的Map等全局变量
        initNodelinkParameters();
        if (this.condition == 1)//C2才需要展示cube,否则隐藏
        {
            initCubeParameters();
        }
        //初始化时间
        this.taskStart = Time.time;
        this.TimeConsumed.text = "0";
    }

    public void initGraph()//仅用于外部创建，不考虑任何全局变量的存储，因为存了也没用。此处只考虑静态绘制功能
    {
        //初始化Cube相关Object
        this.CellHolder = new GameObject();
        this.CellHolder.name = "CellHolder_" + this.graphIndex;
        this.CellHolder.transform.parent = this.PivotHolder.transform;

        this.Label = new GameObject();
        this.Label.name = "Label_" + this.graphIndex;
        this.Label.transform.parent = this.PivotHolder.transform;

        this.LabelX = new GameObject();
        this.LabelX.name = "LabelX";
        this.LabelX.transform.parent = this.Label.transform;

        this.LabelY = new GameObject();
        this.LabelY.name = "LabelY";
        this.LabelY.transform.parent = this.Label.transform;

        this.LabelZ = new GameObject();
        this.LabelZ.name = "LabelZ";
        this.LabelZ.transform.parent = this.Label.transform;

        //初始化NodeHolder和EdgeHolder
        this.NodeHolder = new GameObject();
        this.NodeHolder.name = "NodeHolder_" + this.graphIndex;
        this.NodeHolder.transform.parent = this.NodeLink.transform;

        this.EdgeHolder = new GameObject();
        this.EdgeHolder.name = "EdgeHolder_" + this.graphIndex;
        this.EdgeHolder.transform.parent = this.NodeLink.transform;

        //绘制3D Cube
        init3DCube();
        //绘制nodelink
        initNodelink();
    }

    void init3DCube()
    {
        CubePlotter();
        LabelPlotter();
    }

    void CubePlotter()
    {
        switch (this.graphIndex)
        {
            case 0:
                this.n = DataProcessor0.n + 1;
                this.nodes = DataProcessor0.graph.nodes;
                this.links = DataProcessor0.graph.links;
                var dataProcessor0 = new DataProcessor0();
                this.cube = dataProcessor0.getCube(0.75f);
                this.order = dataProcessor0.order;
                this.nbrMap = DataProcessor0.nbrMap;
                break;
            case 1:
                this.n = DataProcessor1.n + 1;
                this.nodes = DataProcessor1.graph.nodes;
                this.links = DataProcessor1.graph.links;
                var dataProcessor1 = new DataProcessor1();
                this.cube = dataProcessor1.getCube(0.75f);
                this.order = dataProcessor1.order;
                this.nbrMap = DataProcessor1.nbrMap;
                break;
            case 2:
                this.n = DataProcessor2.n + 1;
                this.nodes = DataProcessor2.graph.nodes;
                this.links = DataProcessor2.graph.links;
                var dataProcessor2 = new DataProcessor2();
                this.cube = dataProcessor2.getCube(0.75f);
                this.order = dataProcessor2.order;
                this.nbrMap = DataProcessor2.nbrMap;
                break;
        }
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                for (int z = 0; z < n; z++)
                {
                    if (cube[x, y, z] == 1)
                    {
                        var initialPosition = new Vector3(Convert.ToSingle(x) / n, Convert.ToSingle(y) / n, Convert.ToSingle(z) / n) * plotScale;
                        GameObject dataPoint;
                        string cellId = GlobalParameter.CellPrefabId(x, y, z);

                        if (x > 0 && y > 0 && z > 0)
                        { //triangle cells
                          // Instantiate as gameobject variable so that it can be manipulated within loop
                            dataPoint = Instantiate(
                               CellPrefab,
                               initialPosition,
                               Quaternion.identity);
                        }
                        else
                        { //cell projections
                            // Instantiate as gameobject variable so that it can be manipulated within loop
                            dataPoint = Instantiate(
                                ProjectionPrefab,
                                initialPosition,
                                Quaternion.identity);
                        }

                        // Make dataPoint child of CellHolder object 
                        dataPoint.transform.parent = CellHolder.transform;

                        // Assign name to the prefab
                        dataPoint.transform.name = cellId;
                    }

                }
            }
        }
    }

    void LabelPlotter()
    {
        Vector3 ofsX = new Vector3(1.35f, -1.2f, 0);
        Vector3 ofsY = new Vector3(0, 0.28f, -1.7f);
        Vector3 ofsZ = new Vector3(0, -1.5f, 1f);

        for (int i = 0; i < n - 1; i++)
        {
            string label_text = this.nodes[order[i + 1]].id;
            GameObject labelX = Instantiate(LabelPrefab);
            labelX.transform.rotation = Quaternion.Euler(0, 180f, 45f);
            labelX.transform.name = GlobalParameter.LabelXId(label_text);
            labelX.transform.position = ofsX + new Vector3(Convert.ToSingle(i) / n, 0f, 0f) * plotScale;
            labelX.transform.parent = LabelX.transform;
            labelX.GetComponent<TMP_Text>().text = label_text;
            labelX.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;
            labelX.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;

            GameObject labelY = Instantiate(LabelPrefab);
            labelY.transform.rotation = Quaternion.Euler(0, -90, 0f);
            labelY.transform.name = GlobalParameter.LabelYId(label_text);
            labelY.transform.position = ofsY + new Vector3(0f, Convert.ToSingle(i) / n, 0f) * plotScale;
            labelY.transform.parent = LabelY.transform;
            labelY.GetComponent<TMP_Text>().text = label_text;
            labelY.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;
            labelY.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;

            GameObject labelZ = Instantiate(LabelPrefab);
            labelZ.transform.rotation = Quaternion.Euler(0, -90, -45f);
            labelZ.transform.name = GlobalParameter.LabelZId(label_text);
            labelZ.transform.position = ofsZ + new Vector3(0f, 0f, Convert.ToSingle(i) / n) * plotScale;
            labelZ.transform.parent = LabelZ.transform;
            labelZ.GetComponent<TMP_Text>().text = label_text;
            labelZ.GetComponent<TMP_Text>().color = GlobalParameter.labelColor;

        }
    }

    void initNodelink()//只画图，不存储任何全局变量。全局变量在initNodelinkParameter函数中去获取
    {
        GraphData graph;
        switch (this.graphIndex)
        {
            case 0:
                graph = DataProcessor0.graph;
                break;
            case 1:
                graph = DataProcessor1.graph;
                break;
            case 2:
                graph = DataProcessor2.graph;
                break;
            default:
                graph = DataProcessor0.graph;
                break;
        }

        nodesMap = new Dictionary<string, Node>();

        foreach (var nodeData in graph.nodes)
        {
            string nodeId = nodeData.id;
            //pos范围是[-1, 1]，需要映射为[centerX-width/2, centerX+width/2]以及[centerY-height/2, centerY+height/2]
            Vector3 nodePos = new Vector3(
                0f,
                nodeData.pos[0] / 2 * width + center.x,
                nodeData.pos[1] / 2 * height + center.y);
            var node = new Node()
            {
                id = nodeId,
                cluster = nodeData.cluster,
                prefab = Instantiate(nodePrefab, nodePos, Quaternion.identity, transform),
            };
            // Make prefab child of NodeHolder object 
            node.prefab.transform.parent = NodeHolder.transform;
            // Assigns original values to dataPointName
            string dataPointName = GlobalParameter.NodePrefabId(nodeId);
            // Assigns name to the prefab
            node.prefab.transform.name = dataPointName;
            nodesMap.Add(nodeId, node);
        }

        foreach (var edgeData in graph.links)
        {
            string source = edgeData.source, target = edgeData.target;
            string edgeId = source + "->" + target;
            var edge = new Edge()
            {
                id = edgeId,
                prefab = Instantiate(edgePrefab, transform),
            };
            edge.prefab.GetComponent<LineRenderer>().SetPositions(new Vector3[] { nodesMap[source].prefab.transform.position, nodesMap[target].prefab.transform.position });
            // Make prefab child of EdgeHolder object 
            edge.prefab.transform.parent = EdgeHolder.transform;
            // Assigns original values to dataPointName
            string dataPointName = GlobalParameter.EdgePrefabId(source, target);
            // Assigns name to the prefab
            edge.prefab.transform.name = dataPointName;
        }
    }
}