using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
namespace DM.Editor.View
{
    [RequireComponent(typeof(LineRenderer))]
    public class DMDrawCurve : MonoBehaviour
    {
        public List<Transform> m_allPoints;
        private GameObject m_anchorPoint;
        private GameObject m_controlPoint;
        private GameObject m_pointParent;
        private LineRenderer m_lineRenderer;
        
        private int m_curveCount = 0;
        private int SEGMENT_COUNT = 60;//曲线取点个数（取点越多这个长度越趋向于精确）
 
        private static DMDrawCurve m_instance;
        public static DMDrawCurve Instance
        {
            get {
                if (null == m_instance)
                    m_instance = new DMDrawCurve();
                return m_instance;
            }
        }
        void Awake()
        {
            if (null == m_instance)
                m_instance = this;
            SetLine();
            if (null == m_anchorPoint)
                m_anchorPoint = Resources.Load("Prefabs/AnchorPoint") as GameObject;
            if (null == m_controlPoint)
                m_controlPoint = Resources.Load("Prefabs/ControlPoint") as GameObject;
        }

        //lineRenderer初始化（指定起始点与终止点的颜色、指定线段的宽度）
        void SetLine()
        {
            if (null == m_lineRenderer)
                m_lineRenderer = GetComponent<LineRenderer>();
            m_lineRenderer.material = Resources.Load("Materials/Line") as Material;
            m_lineRenderer.startColor = Color.red;
            m_lineRenderer.endColor = Color.green;
            m_lineRenderer.widthMultiplier = 0.2f;
        }
 
        public void Init(GameObject player)
        {
            //初始化一个基准点（Player）
            if (player == null) return;
            GameObject anchorPoint = LoadPoint(m_anchorPoint, player.transform.position);
            m_allPoints.Add(anchorPoint.transform);
        }      
        public void AddPoint(Vector3 anchorPointPos)
        {
            //初始化时m_allPoints添加了一个player
            if (m_allPoints.Count == 0) return;
            //已经存在的最后一个点（锚点）
            Transform lastPoint = m_allPoints[m_allPoints.Count - 1];
            //根据最后一个点和新点的位置构造2个控制点
            GameObject controlPoint2 = LoadPoint(m_controlPoint, lastPoint.position+new Vector3(0,0,-1));   
            GameObject controlPoint = LoadPoint(m_controlPoint, anchorPointPos + new Vector3(0, 0, 1));
            //根据新点的位置添加1个锚点
            GameObject anchorPoint = LoadPoint(m_anchorPoint, anchorPointPos);
            anchorPoint.GetComponent<CurvePointControl>().m_controlObject = controlPoint;
            lastPoint.GetComponent<CurvePointControl>().m_controlObject2 = controlPoint2;

            //将新点和2个控制点加入点列表
            m_allPoints.Add(controlPoint2.transform);
            m_allPoints.Add(controlPoint.transform);
            m_allPoints.Add(anchorPoint.transform);
            //重新绘制曲线
            DrawCurve();
        }
        public void DeletePoint(GameObject anchorPoint)
        {
            if (anchorPoint == null) return;
            CurvePointControl curvePoint = anchorPoint.GetComponent<CurvePointControl>();

            //是锚点
            if (curvePoint && anchorPoint.tag.Equals("AnchorPoint"))
            {
                //删去控制点1
                if (curvePoint.m_controlObject)
                {
                    m_allPoints.Remove(curvePoint.m_controlObject.transform);
                    Destroy(curvePoint.m_controlObject);
                }

                //删去控制点2
                if (curvePoint.m_controlObject2)
                {
                    m_allPoints.Remove(curvePoint.m_controlObject2.transform);
                    Destroy(curvePoint.m_controlObject2);
                }
                if (m_allPoints.IndexOf(curvePoint.transform) == (m_allPoints.Count - 1))
                {//先判断删除的是最后一个元素再移除
                    m_allPoints.Remove(curvePoint.transform);
                    Transform lastPoint = m_allPoints[m_allPoints.Count - 2];
                    GameObject lastPointCtrObject = lastPoint.GetComponent<CurvePointControl>().m_controlObject2;
                    if (lastPointCtrObject)
                    {
                        m_allPoints.Remove(lastPointCtrObject.transform);
                        Destroy(lastPointCtrObject);
                        lastPoint.GetComponent<CurvePointControl>().m_controlObject2 = null;
                    }
                }
                else
                {
                    m_allPoints.Remove(curvePoint.transform);
                }
                Destroy(anchorPoint);
                if(m_allPoints.Count == 1)
                {
                    m_lineRenderer.positionCount = 0;
                }
            }

            //重新绘制曲线
            DrawCurve();
        }

        //实现通过移动控制点来更新曲线的形状
        public void UpdateLine(GameObject anchorPoint, Vector3 offsetPos1, Vector3 offsetPos2)
        {
            if (anchorPoint == null) return;
            if (anchorPoint.tag.Equals("AnchorPoint"))
            {
                CurvePointControl curvePoint = anchorPoint.GetComponent<CurvePointControl>();

                //分别计算2个控制点的偏移量
                if (curvePoint)
                {
                    if (curvePoint.m_controlObject)
                        curvePoint.m_controlObject.transform.position = anchorPoint.transform.position + offsetPos1;
                    if (curvePoint.m_controlObject2)
                        curvePoint.m_controlObject2.transform.position = anchorPoint.transform.position + offsetPos2;
                }
            }

            //重新绘制曲线
            DrawCurve();
        }
        public List<Vector3> HiddenLine(bool isHidden=false)
        {
            m_pointParent.SetActive(isHidden);
            m_lineRenderer.enabled = isHidden;
            List<Vector3> pathPoints = new List<Vector3>();
            if(!isHidden)
            {
                for(int i = 0; i < m_lineRenderer.positionCount; i++)
                {
                    pathPoints.Add(m_lineRenderer.GetPosition(i));
                }
            }
            return pathPoints;
        }


        private void DrawCurve()//画曲线
        {
            if (m_allPoints.Count < 4) return;
            m_curveCount = (int)m_allPoints.Count / 3;
            for (int j = 0; j < m_curveCount; j++)
            {
                //对于每条曲线分别计算SEGMENT_COUNT个Bezier曲线上点的位置
                for (int i = 1; i <= SEGMENT_COUNT; i++)
                {
                    float t = (float)i / (float)SEGMENT_COUNT;
                    int nodeIndex = j * 3;

                    //调用CalculateCubicBezierPoint计算Bezier曲线上（1个）点的位置
                    Vector3 pixel = CalculateCubicBezierPoint(t, m_allPoints[nodeIndex].position, m_allPoints[nodeIndex + 1].position, m_allPoints[nodeIndex + 2].position, m_allPoints[nodeIndex + 3].position);

                    //用lineRenderer绘制该点
                    m_lineRenderer.positionCount = j * SEGMENT_COUNT + i;
                    m_lineRenderer.SetPosition((j * SEGMENT_COUNT) + (i - 1), pixel);
                }
            }
        }


        private GameObject LoadPoint(GameObject pointPrefab,Vector3 pos)
        {
            //检查预设体
            if (pointPrefab == null)
            {
                Debug.LogError("The Prefab is Null!");
                return null;
            }
            if (null == m_pointParent)
                m_pointParent = new GameObject("AllPoints");
            GameObject pointClone = Instantiate(pointPrefab);
            pointClone.name = pointClone.name.Replace("(Clone)", "");
            pointClone.transform.SetParent(m_pointParent.transform);
            pointClone.transform.position = pos;
 
            return pointClone;
        }
 
        //贝塞尔曲线公式：B(t)=P0*(1-t)^3 + 3*P1*t(1-t)^2 + 3*P2*t^2*(1-t) + P3*t^3 ,t属于[0,1].
        Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
 
            Vector3 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;
 
            return p;
        }
    }
}
