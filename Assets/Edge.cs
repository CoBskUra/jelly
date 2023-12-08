using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Collections;
using UnityEditor.PackageManager;
using System;

namespace Assets
{
    public class EdgeController
    {
        public Edge[] straightEdges { get; private set; }
        public Edge[] diagonalEdges { get; private set; }
        public Vector3[] points { get; private set; }

        public float lineLength { get; private set; }
        public float edgeLength { get; private set; }
        public float halfLineLength { get; private set; }

        public int numberOfStraightEdges { get; private set; }
        public int numberOfDiagonalEdges { get; private set; }

        private int numberOfEdgesConnectedWithTarget;

        public int numberOfPoints { get; private set; }

        public int pointsOnLine { get; private set; }
        public const int dim = 3;

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
            numberOfEdgesConnectedWithTarget = 4;

            numberOfPoints = pointsOnLine * pointsOnLine * pointsOnLine;

            _costOfMovingAlong_x = 1;
            _costOfMovingAlong_y = pointsOnLine;
            _costOfMovingAlong_z = pointsOnLine * pointsOnLine;
            _costOfMovingAlong=  new int[3] {_costOfMovingAlong_z, _costOfMovingAlong_y, _costOfMovingAlong_x};

            points = new Vector3[numberOfPoints];
            straightEdges = new Edge[numberOfStraightEdges];
            diagonalEdges = new Edge[numberOfDiagonalEdges];

            CreatePoints(start);
            CreateEdges();
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
                        points[id] = start + i * direction_x + j * direction_y + k * direction_z;
                        id++;
                    }
                }
            }
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
                    if (pointId + directionCost < points.Length && points[pointId][axisId] < halfLineLength)
                    {
                        straightEdges[id].Set(pointId, pointId + directionCost);
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
            Vector3 point = points[pointId];
            int directionCost = scalarAxis1 * _costOfMovingAlong[(int)axis1] + scalarAxis2* _costOfMovingAlong[(int)axis2];

            if (pointId + directionCost < points.Length &&
                point[(int)axis1] * scalarAxis1 < halfLineLength &&
                point[(int)axis2] * scalarAxis2 < halfLineLength)
            {
                diagonalEdges[id].Set(pointId, pointId + directionCost);
                return true;
            }

            return false;
        }
    }

    public struct Edge
    {
        public int first;
        public int second;

        public Edge(int first, int second)
        {
            this.first = first;
            this.second = second;
        }

        public void Set(int f, int s)
        {
            first = f;
            second = s;
        }
    }
}
