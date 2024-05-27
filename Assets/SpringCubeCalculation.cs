using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Collections;
using UnityEditor.PackageManager;
using System;
using System.Net.NetworkInformation;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.WSA;

namespace Assets
{
    public class SpringCubeCalculation
    {
        public float defaultEleasticyScalar = 60f;
        public float defaultEleasticyScalarTarget = 5;
        public float defaultPointMass = 0.1f;
        public float lineLength { get; private set; }
        public float straightSpringDefaultLength { get; private set; }
        public float diagonalSpringDefaultLength { get; private set; }
        public float targetSpringDefaultLength { get; private set; }


        public float halfLineLength { get; private set; }


        public int pointsOnLine { get; private set; }

        public SpringsHolder straightSpringsHolder { get; private set; }
        public SpringsHolder  diagonalSpringsHolder { get; private set; }
        public SpringsHolder springsConectedToTargerHolder { get;  set; }

        public Point[] movingPoints { get; private set; }
        public Point[] targetsPoints { get;  set; }

        private List<SpringsHolder> _allSprings;
        private List<Point[]> _allPoints;

        private readonly int _costOfMovingAlong_x;
        private readonly int _costOfMovingAlong_y;
        private readonly int _costOfMovingAlong_z;
        private readonly int[] _costOfMovingAlong;

        private enum Axis
        {
            x,
            y,
            z
        }

        public SpringCubeCalculation(int edgesOnLine, float lineLength, Vector3 start, GameObject[] goTargetPoints, float defaultEleasticyScalar, float defaultEleasticyScalarTarget, float defaultPointMass)
        {
            this.defaultEleasticyScalar = defaultEleasticyScalar;
            this.defaultEleasticyScalarTarget = defaultEleasticyScalarTarget;
            this.defaultPointMass = defaultPointMass;

            this.lineLength = lineLength;
            halfLineLength = lineLength * 0.5f;
            straightSpringDefaultLength = lineLength / edgesOnLine;
            diagonalSpringDefaultLength = straightSpringDefaultLength * Mathf.Pow(2, 0.5f);
            targetSpringDefaultLength = 0;

            pointsOnLine = 2 + edgesOnLine - 1;
            int numberOfStraightEdges = 2 * edgesOnLine * pointsOnLine * (2 * pointsOnLine - 1);
            int numberOfDiagonalEdges = pointsOnLine * edgesOnLine * edgesOnLine * 2 * 3;

            int numberOfPoints = pointsOnLine * pointsOnLine * pointsOnLine;

            _costOfMovingAlong_x = 1;
            _costOfMovingAlong_y = pointsOnLine;
            _costOfMovingAlong_z = pointsOnLine * pointsOnLine;
            _costOfMovingAlong=  new int[3] {_costOfMovingAlong_z, _costOfMovingAlong_y, _costOfMovingAlong_x};

            movingPoints = new Point[numberOfPoints];
            Spring[] straightSprings = new Spring[numberOfStraightEdges];
            Spring[] diagonalSprings = new Spring[numberOfDiagonalEdges];

            straightSpringsHolder = new SpringsHolder(movingPoints, movingPoints, straightSprings);
            diagonalSpringsHolder = new SpringsHolder(movingPoints, movingPoints, diagonalSprings);

            CreatePoints();
            ConectPointsWithSprings();
            ConectWithTarget(goTargetPoints);

            for (int i = 0; i < movingPoints.Length; i++)
                movingPoints[i].position += start;

            _allPoints = new List<Point[]>() { movingPoints, targetsPoints };
            _allSprings = new List<SpringsHolder>() { straightSpringsHolder, diagonalSpringsHolder, springsConectedToTargerHolder };
        }


        private void ConectWithTarget(GameObject[] goTargetPoints)
        {
            targetsPoints = new Point[goTargetPoints.Length];
            Spring[] springsConnectedToTarget = new Spring[goTargetPoints.Length];

            for ( int i = 0; i < goTargetPoints.Length; i++ )
            {
                targetsPoints[i].inversMass = 0;
                springsConnectedToTarget[i].first = i;
                springsConnectedToTarget[i].l_0 = 0;
                springsConnectedToTarget[i].elasticityScalar = defaultEleasticyScalarTarget;

            }

            // połączenie punktów z tablicy points tak by każdy róg odpowiadał rogowi
            springsConnectedToTarget[0].second = 0;
            springsConnectedToTarget[1].second = pointsOnLine - 1;
            springsConnectedToTarget[2].second = pointsOnLine* (pointsOnLine - 1) ;
            springsConnectedToTarget[3].second = pointsOnLine * (pointsOnLine - 1)  + pointsOnLine - 1;

            springsConnectedToTarget[4].second = pointsOnLine * pointsOnLine * (pointsOnLine - 1);
            springsConnectedToTarget[5].second = pointsOnLine * pointsOnLine  * pointsOnLine - ( pointsOnLine - 1)* pointsOnLine -1;
            springsConnectedToTarget[6].second = pointsOnLine * pointsOnLine * pointsOnLine - pointsOnLine;
            springsConnectedToTarget[7].second = pointsOnLine * pointsOnLine * pointsOnLine - 1;

            springsConectedToTargerHolder = new SpringsHolder(targetsPoints, movingPoints, springsConnectedToTarget);
        }

        void CreatePoints()
        {
            var direction_x = new Vector3(straightSpringDefaultLength, 0, 0);
            var direction_y = new Vector3(0, straightSpringDefaultLength, 0);
            var direction_z = new Vector3(0, 0, straightSpringDefaultLength);

            Vector3 start = -new Vector3(halfLineLength, halfLineLength, halfLineLength);

            int id = 0;
            for (int i = 0; i < pointsOnLine; i++)
            {
                for (int j = 0; j < pointsOnLine; j++)
                {
                    for (int k = 0; k < pointsOnLine; k++)
                    {
                        movingPoints[id].position = start + i * direction_x + j * direction_y + k * direction_z;
                        movingPoints[id].mass = defaultPointMass;
                        id++;
                    }
                }
            }
        }

        

        private void ConectPointsWithSprings()
        {
            int id = 0;
            int idDiagonal = 0;
            (Axis axis, int scalar)[] straigthDirections = new (Axis axis, int scalar)[3] {
                (Axis.x, 1),
                (Axis.y, 1),
                (Axis.z, 1) };

            (Axis firstAxis, int firstScalar, Axis secendAxis, int secendScalar)[] diagonalDirections =
                new (Axis firstAxis, int firstScalar, Axis secendAxis, int secendScalar)[6] {
                    (Axis.x, 1, Axis.y, 1),
                    ( Axis.x, -1, Axis.y, 1),
                    ( Axis.x, 1, Axis.z, 1),
                    ( Axis.x, -1, Axis.z, 1),
                    ( Axis.y, 1, Axis.z, 1),
                    ( Axis.y, -1, Axis.z, 1)
                };

            for (int pointId = 0; pointId < movingPoints.Length; pointId++)
            {
                // proste linie
                foreach(var directionInfo in straigthDirections)
                {
                    if (SetEdgeAlong(straightSpringsHolder, id, pointId,
                        straightSpringDefaultLength, defaultEleasticyScalar,
                        directionInfo.axis, directionInfo.scalar))
                    {
                        id++;
                    }
                }

                // diagonalne linie
                foreach (var directionInfo in diagonalDirections)
                {
                    if (SetEdgeAlong(
                            diagonalSpringsHolder, idDiagonal, pointId,
                            diagonalSpringDefaultLength, defaultEleasticyScalar,
                            directionInfo.firstAxis, directionInfo.firstScalar,
                            directionInfo.secendAxis, directionInfo.secendScalar))
                    {
                        idDiagonal++;
                    }
                }
            }
        }


       private bool SetEdgeAlong(SpringsHolder holder, int id, int pointId, float springLength, float elesticyScalar, Axis axis1,  int scalarAxis1, Axis axis2, int scalarAxis2)
        {
            Vector3 point = movingPoints[pointId].position;
            int directionCost = scalarAxis1 * _costOfMovingAlong[(int)axis1] + scalarAxis2* _costOfMovingAlong[(int)axis2];

            if (pointId + directionCost < movingPoints.Length &&
                point[(int)axis1] * scalarAxis1 < halfLineLength &&
                point[(int)axis2] * scalarAxis2 < halfLineLength)
            {
                holder.springs[id].Set(pointId, pointId + directionCost, elesticyScalar, springLength);
                return true;
            }

            return false;
        }

        private bool SetEdgeAlong(SpringsHolder holder, int id, int pointId,  float springLength, float elesticyScalar, Axis axis1, int scalarAxis1)
        {
            Vector3 point = movingPoints[pointId].position;
            int directionCost = scalarAxis1 * _costOfMovingAlong[(int)axis1];

            if (pointId + directionCost < movingPoints.Length &&
                point[(int)axis1] * scalarAxis1 < halfLineLength )
            {
                holder.springs[id].Set(pointId, pointId + directionCost, elesticyScalar, springLength);
                return true;
            }

            return false;
        }

        public void CalculateNextStep(float delta, float dampingScalar, float range, float u)
        {
            // zerowanie wektora siły sprężystości
            foreach(var pointsTable in _allPoints)
            {
                for (int i = 0; i < pointsTable.Length; i++)
                {
                    pointsTable[i].springForce = Vector3.zero;
                }
            }

            // obliczanie nowego wektorowa siły sprężystości 
            foreach(var springs in _allSprings)
            {
                springs.UpdateSpringForce();
            }

            // obliczanie nowej prędkości i jej pochodnych
            for ( int  i = 0;i < movingPoints.Length; i++)
            {
                movingPoints[i].UpdatePositionAndDerivatives(delta, dampingScalar, range, u);
            }
        }

        public void DistortPoints(float maxVelocity)
        {
            System.Random r = new System.Random();
            for(int i = 0; i < movingPoints.Length; i++)
            {
                movingPoints[i].velocity = new Vector3((float) (r.NextDouble() - 0.5) * maxVelocity*2, (float)(r.NextDouble() - 0.5) * maxVelocity * 2, (float)(r.NextDouble() - 0.5) * maxVelocity * 2);
            }
        }

        public void AssigneeRandomMassys(float maxRandomMass, float miniRandomMass)
        {
            System.Random r = new System.Random();
            for (int i = 0; i < movingPoints.Length; i++)
            {
                movingPoints[i].mass = (float)r.NextDouble() * (maxRandomMass - miniRandomMass) + miniRandomMass;
            }
        }
    }
}
