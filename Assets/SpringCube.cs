using Assets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class SpringCube : MonoBehaviour
{
    public int edgesOnLine = 1;
    public float radius = 0.1f;
    public GameObject pointSkin;
    public GameObject room;
    public GameObject target;

    public Material lineMat;
    public Mesh cylinderMesh;

    EdgeController edgesAndPoints;
    public GameObject[] pointObjects { get; private set; }

    private GameObject[] pointsAsGameObjects;

    private float lineLength;
    private float halfLineLength;



    // Start is called before the first frame update
    void Start()
    {
        lineLength = gameObject.transform.localScale.x;
        halfLineLength = lineLength * 0.5f;

        Vector3 start =  -new Vector3(halfLineLength, halfLineLength, halfLineLength);
        edgesAndPoints = new EdgeController(edgesOnLine, lineLength, start);
        CreateSkinsForPoints();
        CreateLines();
        //CreateCubeSection(start, direction_z, direction_y, direction_x, 0);
        //CreateCubeSection(start, direction_x, direction_z, direction_y, (int)(numberOfEdges / 3.0f));
        //CreateCubeSection(start, direction_y, direction_x, direction_z, (int)(2.0f * numberOfEdges / 3.0f));
    }

    // Update is called once per frame
    void Update()
    {
        DisplayLines();
    }

    

    void CreateSkinsForPoints()
    {
        pointObjects = new GameObject[edgesAndPoints.points.Length];
        for (int i = 0; i < edgesAndPoints.points.Length; i++) 
        {
            var point = edgesAndPoints.points[i];
            GameObject currentEntity = Instantiate(pointSkin, point, Quaternion.identity, gameObject.transform);
            currentEntity.name = string.Format("Point_{0}", i);
            pointObjects[i] = currentEntity;
        }
    }

   

    private void DisplayLines()
    {
        for (int pointId = 0; pointId < edgesAndPoints.numberOfPoints; pointId++)
        {
            for (int axisId = 0; axisId < 3; axisId++)
            {
                Axis axis = (Axis)axisId;
                int neighbourId = edgesAndPoints.GetNeighbourId(pointId, axis, Direction.forward);
                // Move the ring to the point
                this.pointsAsGameObjects[pointId].transform.position = edgesAndPoints.points[pointId];

                // Match the scale to the distance
                float cylinderDistance = 0.5f * Vector3.Distance(edgesAndPoints.points[pointId], edgesAndPoints.points[neighbourId]);
                this.pointsAsGameObjects[pointId].transform.localScale = new Vector3(this.pointsAsGameObjects[pointId].transform.localScale.x,
                    cylinderDistance / transform.localScale.x, this.pointsAsGameObjects[pointId].transform.localScale.z);

                // Make the cylinder look at the main point.
                // Since the cylinder is pointing up(y) and the forward is z, we need to offset by 90 degrees.
                this.pointsAsGameObjects[pointId].transform.LookAt(edgesAndPoints.points[neighbourId], Vector3.up);
                this.pointsAsGameObjects[pointId].transform.rotation *= Quaternion.Euler(90, 0, 0);
            }
        }
    }

    

   

    private void CreateLines()
    {
        this.pointsAsGameObjects = new GameObject[edgesAndPoints.numberOfStraightEdges];
        //this.connectingRings = new ProceduralRing[points.Length];
        for (int i = 0; i < edgesAndPoints.numberOfStraightEdges; i++)
        {
            // Make a gameobject that we will put the ring on
            // And then put it as a child on the gameobject that has this Command and Control script
            this.pointsAsGameObjects[i] = new GameObject();
            this.pointsAsGameObjects[i].name = string.Format("Line_{0}", i);
            this.pointsAsGameObjects[i].transform.parent = this.gameObject.transform;

            // We make a offset gameobject to counteract the default cylindermesh pivot/origin being in the middle
            GameObject ringOffsetCylinderMeshObject = new GameObject();
            ringOffsetCylinderMeshObject.transform.parent = this.pointsAsGameObjects[i].transform;

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
