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
    public float maxRandomVelocity;
    public float maxRandomMass = 5;

    public float defaultEleasticyScalar = 60f;
    public float defaultEleasticyScalarTarget = 5;
    public float defaultPointMass = 0.1f;

    public float u;
    public float range;
    public float radius = 0.1f;

    public GameObject pointSkin;
    public GameObject targetPoint;
    public GameObject room;

    public float delta = 0.1f;
    public float dampingScalar = 1;

    public Material lineMat;
    public Mesh cylinderMesh;

    public bool ShowStraightSprings = true;
    public bool showDiagonalSprings = true;
    public bool showConnectedToTargetSprings = true;
    public bool randomMassys = false;

    SpringCubeCalculation springCalculations;
    public GameObject[] movingPoints { get; private set; }

    private GameObject[] visibleStraigthSprings;
    private GameObject[] visibleDiagonalSprings;
    private GameObject[] visibleConectedToTargetSprings;

    private GameObject[] targetPoints;

    private float lineLength;
    private float halfLineLength;

    private float timeSinceLastCalculation = 0;


    // Start is called before the first frame update
    void Start()
    {
        lineLength = gameObject.transform.localScale.x;
        halfLineLength = lineLength * 0.5f;

        CreateTargetPoints();
        springCalculations = new SpringCubeCalculation(edgesOnLine, lineLength, transform.position, targetPoints, 
            defaultEleasticyScalar, defaultEleasticyScalarTarget, defaultPointMass);

        CreateSkinsForMovingPoints();
        visibleStraigthSprings = CreateLines(springCalculations.straightSpringsHolder);
        visibleDiagonalSprings = CreateLines(springCalculations.diagonalSpringsHolder);
        visibleConectedToTargetSprings = CreateLines(springCalculations.springsConectedToTargerHolder);

        springCalculations.DistortPoints(maxRandomVelocity);

        if (randomMassys)
            springCalculations.AssigneeRandomMassys(maxRandomMass, 0.01f);
    }

    void Update()
    {
        if(timeSinceLastCalculation + Time.deltaTime < delta)
        {
            timeSinceLastCalculation += Time.deltaTime;
            return;
        }

        // odœwierzanie pozycji punktów
        for (int i = 0; i < targetPoints.Length; i++)
            springCalculations.targetsPoints[i].position = targetPoints[i].transform.position;

        for (int i = 0; i*delta < Time.deltaTime; i++)
        {
            springCalculations.CalculateNextStep(delta, dampingScalar, range, u);
        }

        for (int i = 0; i < springCalculations.movingPoints.Length; i++)
            movingPoints[i].transform.position = springCalculations.movingPoints[i].position;


        if(ShowStraightSprings)
            DisplayLines(springCalculations.straightSpringsHolder, visibleStraigthSprings);

        if(showDiagonalSprings)
        DisplayLines(springCalculations.diagonalSpringsHolder, visibleDiagonalSprings);

        if(showConnectedToTargetSprings)
            DisplayLines(springCalculations.springsConectedToTargerHolder, visibleConectedToTargetSprings);

        timeSinceLastCalculation = 0;
    }


    void CreateTargetPoints()
    {
        var direction_x = new Vector3(lineLength, 0, 0);
        var direction_y = new Vector3(0, lineLength, 0);
        var direction_z = new Vector3(0, 0, lineLength);

        Vector3 start =  -Vector3.one * halfLineLength;

        Vector3[] corners = new Vector3[8];
        int id = 0;
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    corners[id] = start + i * direction_x + j * direction_y + k * direction_z;
                    id++;
                }
            }
        }

        targetPoints = new GameObject[corners.Length];
        for (int i = 0; i < corners.Length; i++)    
        {
            var point = corners[i];
            GameObject currentEntity = Instantiate(targetPoint,gameObject.transform);
            var ceScal = currentEntity.transform.localScale;
            var tmp2 = gameObject.transform.localScale;
            currentEntity.transform.localScale =  new Vector3(ceScal.x / tmp2.x, ceScal.y / tmp2.y, ceScal.z / tmp2.z);
            currentEntity.transform.localPosition = new Vector3(point.x / tmp2.x, point.y / tmp2.y, point.z / tmp2.z);
            currentEntity.name = string.Format("TargetPoint_{0}", i);
            targetPoints[i] = currentEntity;
        }
    }



    void CreateSkinsForMovingPoints()
    {
        GameObject pointsParent = new GameObject();
        pointsParent.name = "Points";
        movingPoints = new GameObject[springCalculations.movingPoints.Length];
        for (int i = 0; i < springCalculations.movingPoints.Length; i++) 
        {
            var point = springCalculations.movingPoints[i].position;
            GameObject currentEntity = Instantiate(pointSkin, point, Quaternion.identity, pointsParent.transform);
            currentEntity.name = string.Format("Point_{0}", i);
            movingPoints[i] = currentEntity;
        }
    }

    private void DisplayLines(SpringsHolder springsHolder, GameObject[] visableLine)
    {
        for (int i = 0; i < springsHolder.numberOfSprings; i++)
        {
            Assets.Spring spring = springsHolder.springs[i];
            Vector3 start = springsHolder.pointsFirst[spring.first].position;
            Vector3 end = springsHolder.pointsSeconds[spring.second].position;
            // Move the ring to the point
            visableLine[i].transform.position = start;

            // Match the scale to the distance
            float cylinderDistance = 0.5f * Vector3.Distance(start, end);
            visableLine[i].transform.localScale = new Vector3(visableLine[i].transform.localScale.x,
                cylinderDistance / transform.localScale.x, visableLine[i].transform.localScale.z);

            // Make the cylinder look at the main point.
            // Since the cylinder is pointing up(y) and the forward is z, we need to offset by 90 degrees.
            visableLine[i].transform.LookAt(end, Vector3.up);
            visableLine[i].transform.rotation *= Quaternion.Euler(90, 0, 0);
        }
    }

    private GameObject[] CreateLines(SpringsHolder holder)
    {
        GameObject[] visibleSprings = new GameObject[holder.numberOfSprings];
        //this.connectingRings = new ProceduralRing[points.Length];
        for (int i = 0; i < holder.numberOfSprings; i++)
        {
            // Make a gameobject that we will put the ring on
            // And then put it as a child on the gameobject that has this Command and Control script
            visibleSprings[i] = new GameObject();
            visibleSprings[i].name = string.Format("Line_{0}", i);
            visibleSprings[i].transform.parent = this.gameObject.transform;

            // We make a offset gameobject to counteract the default cylindermesh pivot/origin being in the middle
            GameObject ringOffsetCylinderMeshObject = new GameObject();
            ringOffsetCylinderMeshObject.transform.parent = visibleSprings[i].transform;

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
        return visibleSprings;
    }
}
