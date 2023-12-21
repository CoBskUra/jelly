using Assets;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class SpringCube : MonoBehaviour
{
    public int edgesOnLine = 1;
    public float radius = 0.1f;
    public GameObject pointSkin;
    public GameObject targetPoint;
    public GameObject room;
    public float delta = 0.1f;
    public float dampingScalar = 1;

    public Material lineMat;
    public Mesh cylinderMesh;

    EdgeController edgesAndPoints;
    public GameObject[] points { get; private set; }
    private GameObject[] visableSprings;
    private GameObject[] targetPoints;

    private float lineLength;
    private float halfLineLength;

    private float timeSinceLastCalculation = 0;


    // Start is called before the first frame update
    void Start()
    {
        lineLength = gameObject.transform.localScale.x;
        halfLineLength = lineLength * 0.5f;

        Vector3 start =  -new Vector3(halfLineLength, halfLineLength, halfLineLength);
        edgesAndPoints = new EdgeController(edgesOnLine, lineLength, start);
        CreateSkinsForPoints();
        CreateTargetPoint(start);
        edgesAndPoints.ConectWithTarget(targetPoints);
        CreateLines();
    }

    void Update()
    {
        if(timeSinceLastCalculation + Time.deltaTime < delta)
        {
            timeSinceLastCalculation += Time.deltaTime;
            return;
        }
        edgesAndPoints.UpdateTargetPointPosition(targetPoints);

        for (int i = 0; i*delta < Time.deltaTime; i++)
        {
            edgesAndPoints.CalculateNextStep(delta, dampingScalar);
        }

        for (int i = 0; i < edgesAndPoints.points.Length; i++)
            points[i].transform.position = edgesAndPoints.points[i].position;


        DisplayLines();
        timeSinceLastCalculation = 0;
    }


    void CreateTargetPoint(Vector3 start)
    {
        Vector3[] corners = edgesAndPoints.CornerPoints(start);
        targetPoints = new GameObject[corners.Length];
        for (int i = 0; i < corners.Length; i++)    
        {
            var point = corners[i];
            GameObject currentEntity = Instantiate(targetPoint, point, Quaternion.identity, gameObject.transform);
            var ceScal = currentEntity.transform.localScale;
            var tmp2 = gameObject.transform.localScale;
            currentEntity.transform.localScale =  new Vector3(ceScal.x / tmp2.x, ceScal.y / tmp2.y, ceScal.z / tmp2.z);
            currentEntity.name = string.Format("TargetPoint_{0}", i);
            targetPoints[i] = currentEntity;
        }
    }

    void CreateSkinsForPoints()
    {
        GameObject pointsParent = new GameObject();
        pointsParent.name = "Points";
        points = new GameObject[edgesAndPoints.points.Length];
        for (int i = 0; i < edgesAndPoints.points.Length; i++) 
        {
            var point = edgesAndPoints.points[i].position;
            GameObject currentEntity = Instantiate(pointSkin, point, Quaternion.identity, pointsParent.transform);
            currentEntity.name = string.Format("Point_{0}", i);
            points[i] = currentEntity;
        }
    }

    private void DisplayLines()
    {
        for (int i = 0; i < edgesAndPoints.numberOfStraightEdges; i++)
        {
            Assets.Spring edge = edgesAndPoints.straightEdges[i];
            int start = edge.first;
            int end = edge.second;
            // Move the ring to the point
            this.visableSprings[i].transform.position = edgesAndPoints.points[start].position;

            // Match the scale to the distance
            float cylinderDistance = 0.5f * Vector3.Distance(edgesAndPoints.points[start].position, edgesAndPoints.points[end].position);
            this.visableSprings[i].transform.localScale = new Vector3(this.visableSprings[i].transform.localScale.x,
                cylinderDistance / transform.localScale.x, this.visableSprings[i].transform.localScale.z);

            // Make the cylinder look at the main point.
            // Since the cylinder is pointing up(y) and the forward is z, we need to offset by 90 degrees.
            this.visableSprings[i].transform.LookAt(edgesAndPoints.points[end].position, Vector3.up);
            this.visableSprings[i].transform.rotation *= Quaternion.Euler(90, 0, 0);
        }
    }

    private void CreateLines()
    {
        this.visableSprings = new GameObject[edgesAndPoints.numberOfStraightEdges];
        //this.connectingRings = new ProceduralRing[points.Length];
        for (int i = 0; i < edgesAndPoints.numberOfStraightEdges; i++)
        {
            // Make a gameobject that we will put the ring on
            // And then put it as a child on the gameobject that has this Command and Control script
            this.visableSprings[i] = new GameObject();
            this.visableSprings[i].name = string.Format("Line_{0}", i);
            this.visableSprings[i].transform.parent = this.gameObject.transform;

            // We make a offset gameobject to counteract the default cylindermesh pivot/origin being in the middle
            GameObject ringOffsetCylinderMeshObject = new GameObject();
            ringOffsetCylinderMeshObject.transform.parent = this.visableSprings[i].transform;

            // Offset the cylinder so that the pivot/origin is at the bottom in relation to the outer ring gameobject.
            ringOffsetCylinderMeshObject.transform.localPosition = new Vector3(0f, 1f, 0f);
            // Set the radius
            ringOffsetCylinderMeshObject.transform.localScale = new Vector3(radius, 1f, radius);

            // Create the the Mesh and renderer to show the connecting ring
            MeshFilter ringMesh = ringOffsetCylinderMeshObject.AddComponent<MeshFilter>();
            ringMesh.mesh = this.cylinderMesh;

            MeshRenderer ringRenderer = ringOffsetCylinderMeshObject.AddComponent<MeshRenderer>();
            ringRenderer.material = lineMat;
        }
    }

}
