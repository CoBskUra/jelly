using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Collections;
using UnityEditor.PackageManager;
using System;
using System.Net.NetworkInformation;

namespace Assets
{
    public class EdgeController
    {
        public float lineLength { get; private set; }
        public float edgeLength { get; private set; }
        public float halfLineLength { get; private set; }

        public int numberOfStraightEdges { get; private set; }
        public int numberOfDiagonalEdges { get; private set; }
        public int numberOfPoints { get; private set; }

        public int pointsOnLine { get; private set; }
        public const int dim = 3;

        public Spring[] straightEdges { get; private set; }
        public Spring[] diagonalEdges { get; private set; }
        public Spring[] connectedToTarget {  get; private set; }

        public Point[] points { get; private set; }
        public Point[] targetsPoints { get; private set; }


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

        public EdgeController(int edgesOnLine, float lineLength, Vector3 start)
        {
            this.lineLength = lineLength;
            halfLineLength = lineLength * 0.5f;
            edgeLength = lineLength / edgesOnLine;
            pointsOnLine = 2 + edgesOnLine - 1;


            numberOfStraightEdges = 2 * edgesOnLine * pointsOnLine * (2 * pointsOnLine - 1);
            numberOfDiagonalEdges = pointsOnLine * edgesOnLine * edgesOnLine * 2 * 3;

            numberOfPoints = pointsOnLine * pointsOnLine * pointsOnLine;

            _costOfMovingAlong_x = 1;
            _costOfMovingAlong_y = pointsOnLine;
            _costOfMovingAlong_z = pointsOnLine * pointsOnLine;
            _costOfMovingAlong=  new int[3] {_costOfMovingAlong_z, _costOfMovingAlong_y, _costOfMovingAlong_x};

            points = new Point[numberOfPoints];
            straightEdges = new Spring[numberOfStraightEdges];
            diagonalEdges = new Spring[numberOfDiagonalEdges];

            CreatePoints(start);
            CreateEdges();
        }


        public void ConectWithTarget(GameObject[] gmTargetPoints)
        {
            targetsPoints = new Point[gmTargetPoints.Length];
            connectedToTarget = new Spring[gmTargetPoints.Length];

            for ( int i = 0; i < gmTargetPoints.Length; i++ )
            {
                targetsPoints[i].position = gmTargetPoints[i].transform.position;
                targetsPoints[i].inversMass = 0;
                connectedToTarget[i].first = i;
            }

            // połączenie punktów z tablicy points tak by każdy róg odpowiadał rogowi
            connectedToTarget[0].second = 0;
            connectedToTarget[1].second = pointsOnLine - 1;
            connectedToTarget[2].second = pointsOnLine* (pointsOnLine - 1) ;
            connectedToTarget[3].second = pointsOnLine * (pointsOnLine - 1)  + pointsOnLine - 1;

            connectedToTarget[4].second = pointsOnLine * pointsOnLine * (pointsOnLine - 1);
            connectedToTarget[5].second = pointsOnLine * pointsOnLine  * pointsOnLine - ( pointsOnLine - 1)* pointsOnLine;
            connectedToTarget[6].second = pointsOnLine * pointsOnLine * pointsOnLine - pointsOnLine;
            connectedToTarget[7].second = pointsOnLine * pointsOnLine * pointsOnLine - 1;
        }

        void CreatePoints(Vector3 start)
        {
            var direction_x = new Vector3(edgeLength, 0, 0);
            var direction_y = new Vector3(0, edgeLength, 0);
            var direction_z = new Vector3(0, 0, edgeLength);

            int id = 0;
            for (int i = 0; i < pointsOnLine; i++)
            {
                for (int j = 0; j < pointsOnLine; j++)
                {
                    for (int k = 0; k < pointsOnLine; k++)
                    {
                        points[id].position = start + i * direction_x + j * direction_y + k * direction_z;
                        points[id].mass = 1;
                        id++;
                    }
                }
            }
        }

        public Vector3[] CornerPoints(Vector3 start)
        {
            var direction_x = new Vector3(lineLength, 0, 0);
            var direction_y = new Vector3(0, lineLength, 0);
            var direction_z = new Vector3(0, 0, lineLength);

            Vector3[] tmp = new Vector3[8];
            int id = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        tmp[id] = start + i * direction_x + j * direction_y + k * direction_z;
                        id++;
                    }
                }
            }

            return tmp;
        }

        private void CreateEdges()
        {
            int id = 0;
            int idDiagonal = 0;
            for (int pointId = 0; pointId < points.Length; pointId++)
            {
                // proste linie
                for (int axisId = 0; axisId < 3; axisId++)
                {
                    int directionCost = _costOfMovingAlong[axisId];
                    if (pointId + directionCost < points.Length && points[pointId].position[axisId] < halfLineLength)
                    {
                        straightEdges[id].Set(pointId, pointId + directionCost, 0.3f, edgeLength);
                        id++;
                    }
                }

                // linia xy
                if (SetEdgeAlong(idDiagonal, pointId, Axis.x, 1, Axis.y, 1))
                    idDiagonal++;

                // linia -xy
                if (SetEdgeAlong(idDiagonal, pointId, Axis.x, -1, Axis.y, 1))
                    idDiagonal++;

                // linia xz
                if (SetEdgeAlong(idDiagonal, pointId, Axis.x, 1, Axis.z, 1))
                    idDiagonal++;

                // linia -xz
                if (SetEdgeAlong(idDiagonal, pointId, Axis.x, -1, Axis.z, 1))
                    idDiagonal++;

                // linia yz
                if (SetEdgeAlong(idDiagonal, pointId, Axis.y, 1, Axis.z, 1))
                    idDiagonal++;

                // linia -yz
                if (SetEdgeAlong(idDiagonal, pointId, Axis.y, -1, Axis.z, 1))
                    idDiagonal++;

            }
        }


       private bool SetEdgeAlong(int id, int pointId, Axis axis1,  int scalarAxis1, Axis axis2, int scalarAxis2)
        {
            Vector3 point = points[pointId].position;
            int directionCost = scalarAxis1 * _costOfMovingAlong[(int)axis1] + scalarAxis2* _costOfMovingAlong[(int)axis2];

            if (pointId + directionCost < points.Length &&
                point[(int)axis1] * scalarAxis1 < halfLineLength &&
                point[(int)axis2] * scalarAxis2 < halfLineLength)
            {
                diagonalEdges[id].Set(pointId, pointId + directionCost, 0.3f, edgeLength);
                return true;
            }

            return false;
        }

        public void CalculateNextStep(float delta, float dampingScalar)
        {
            // zerowanie wektora siły sprężystości
            for(int i = 0; i < points.Length; i++)
            {
                points[i].springForce = Vector3.zero;
            }

            for (int i = 0; i < targetsPoints.Length; i++)
            {
                targetsPoints[i].springForce = Vector3.zero;
            }


            // obliczanie nowego wektorowa siły sprężystości 
            for (int i = 0; i < straightEdges.Length; i++)
            {
                var spring = straightEdges[i];

                Vector3 springDirection = points[spring.second].position - points[spring.first].position;

                float force = spring.elasticityScalar * (spring.l_0 - springDirection.sqrMagnitude);
                springDirection = springDirection.normalized;

                points[spring.first].springForce -= springDirection * force;
                points[spring.second].springForce += springDirection * force;
            }

            for (int i = 0; i < diagonalEdges.Length; i++)
            {
                var spring = diagonalEdges[i];

                Vector3 springDirection = points[spring.second].position - points[spring.first].position;

                float force = spring.elasticityScalar * (spring.l_0 - springDirection.sqrMagnitude);
                springDirection = springDirection.normalized;
                points[spring.first].springForce -= springDirection * force;
                points[spring.second].springForce += springDirection * force;
            }

            

            for (int i = 0; i < connectedToTarget.Length; i++)
            {
                var spring = connectedToTarget[i];

                Vector3 springDirection = points[spring.second].position - targetsPoints[spring.first].position;

                float force = spring.elasticityScalar * (spring.l_0 - springDirection.sqrMagnitude);
                springDirection = springDirection.normalized;

                targetsPoints[spring.first].springForce -= springDirection * force;
                points[spring.second].springForce += springDirection * force;
            }

            // obliczanie nowej prędkości i jej pochodnych
            for ( int  i = 0;i < points.Length; i++)
            {
                points[i].UpdatePositionAndDerivatives(delta, dampingScalar);
            }

            for (int i = 0; i < targetsPoints.Length; i++)
            {
                targetsPoints[i].UpdatePositionAndDerivatives(delta, dampingScalar);
            }

        }


        public void UpdateTargetPointPosition(GameObject[] targetPoints)
        {
            int i = 0;
           foreach(GameObject targetPoint in targetPoints)
            {
                this.targetsPoints[i].position = targetPoint.transform.position;
            }
        }
    }

    public struct Spring
    {
        public int first;
        public int second;

        public float elasticityScalar;
        public float l_0;


        public Spring(int first, int second)
        {
            this.first = first;
            this.second = second;
            elasticityScalar = 0.1f;
            l_0 = 3;
        }


        public void Set(int f, int s, float elasticityScalar, float l_0)
        {
            first = f;
            second = s;
            this.elasticityScalar = elasticityScalar;
            this.l_0 = l_0;
        }

        
    }


    public struct Point
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 springForce;
        public float inversMass;
        public float mass { set { inversMass = 1/value; } }

        public Point(Vector3 po,  Vector3 v, Vector3 a, Vector3 springForce, float mass)
        {
            this.position = po;
            this.velocity = v;
            this.acceleration = a;
            inversMass = 1 / mass;
            this.springForce = springForce;
        }

        public void UpdatePositionAndDerivatives(float delta, float dampingScalar)
        {
            acceleration = (springForce + velocity * dampingScalar) * inversMass;
            position = position + delta * (velocity + delta * acceleration * 0.5f);
            velocity = velocity + delta * acceleration;
        }
    }


}
