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
        public int[] neighbours { get; private set; }
        public Vector3[] points { get; private set; }

        public float lineLength { get; private set; }
        public float edgeLength { get; private set; }
        public float halfLineLength { get; private set; }
        public int numberOfEdges { get; private set; }
        public int numberOfStraightEdges { get; private set; }
        public int numberOfPoints { get; private set; }

        public int pointsOnLine { get; private set; }
        public const int dim = 3;

        private int _costOfMovingAlong_x;
        private int _costOfMovingAlong_y;
        private int _costOfMovingAlong_z;
        private int _potentialNeighbours = 9;


        public EdgeController(int edgesOnLine, float lineLength, Vector3 start)
        {
            this.lineLength = lineLength;
            halfLineLength = lineLength * 0.5f;
            edgeLength = lineLength / edgesOnLine;
            pointsOnLine = 2 + edgesOnLine - 1;


            //NumberOfStraightEdges = 2 * edgesOnLine * pointsOnLine * (2 * pointsOnLine - 1);
            //NumberOfDiagonalEdges = pointsOnLine * edgesOnLine * edgesOnLine * 2 * 3;
            //NumberOfEdgesConnectedWithTarget = 4;
            numberOfPoints = pointsOnLine * pointsOnLine * pointsOnLine;
            numberOfEdges = pointsOnLine * pointsOnLine * pointsOnLine * _potentialNeighbours;
            numberOfStraightEdges = numberOfPoints * dim;
            _costOfMovingAlong_x = 1;
            _costOfMovingAlong_y = pointsOnLine;
            _costOfMovingAlong_z = pointsOnLine * pointsOnLine;

            points = new Vector3[numberOfPoints];
            neighbours = new int[_potentialNeighbours*numberOfPoints];

            CreatePoints(start);
            CreateEdges();
        }

        public int GetNeighbourId(int pointId, Axis axis, Direction direction, Axis? axisSecond = null, Direction? directionSecond = null)
        {
            return neighbours[DecodeNeighbourId(pointId, axis, direction, axisSecond, directionSecond)];
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
            for (int pointId = 0; pointId < points.Length; pointId++)
            {
                for (int axisId = 0; axisId < 3; axisId++)
                {
                    Axis axis = (Axis)axisId; ;
                    if (CanConnect(pointId, axis, Direction.forward))
                    {
                        neighbours[DecodeNeighbourId(pointId, axis, Direction.forward) ] = pointId + DistanceFromNeighbour((Axis)axisId, Direction.forward);
                    }
                }

                for (int directionId = 0; directionId < 2; directionId++)
                {
                    Direction direction = (Direction)directionId;
                    if (CanConnect(pointId, Axis.x, direction, Axis.y, Direction.forward))
                    {
                        neighbours[DecodeNeighbourId(pointId, Axis.x, direction, Axis.y, Direction.forward)] = pointId + DistanceFromNeighbour(Axis.x, direction)
                            + DistanceFromNeighbour(Axis.y, Direction.forward);
                    }

                    if (CanConnect(pointId, Axis.x, direction, Axis.z, Direction.forward))
                    {
                        neighbours[DecodeNeighbourId(pointId, Axis.x, direction, Axis.z, Direction.forward)] = pointId + DistanceFromNeighbour(Axis.x, direction) 
                            + DistanceFromNeighbour(Axis.z, Direction.forward);
                    }

                    if (CanConnect(pointId, Axis.y, direction, Axis.z, Direction.forward))
                    {
                        neighbours[ DecodeNeighbourId(pointId, Axis.y, direction, Axis.z, Direction.forward)] = pointId + DistanceFromNeighbour(Axis.y, direction) 
                            + DistanceFromNeighbour(Axis.z, Direction.forward);
                    }

                }

            }
        }


        private bool CanConnect(int pointId, Axis axis, Direction direction, Axis? axisSecond = null, Direction? directionSecond = null)
        {

            Vector3 point = points[pointId];
            int distanceFromNeighbour = DistanceFromNeighbour(axis, direction) + DistanceFromNeighbour(axisSecond, directionSecond);

            return Mathf.Abs(pointId + distanceFromNeighbour) < points.Length &&
               DirectionToScalar(direction) * point[(int)axis] < halfLineLength &&
               (axisSecond == null || DirectionToScalar(directionSecond) * point[(int) axisSecond] < halfLineLength );
        }


        private int DirectionToScalar(Direction? direction)
        {
            int directionScalar = 0;
            switch (direction)
            {
                case Direction.forward:
                    directionScalar = 1;
                    break;
                case Direction.backward:
                    directionScalar = -1;
                    break;
                default:
                    break;
            }

            return directionScalar;
        }

        private int DistanceFromNeighbour(Axis? axis, Direction? direction)
        {
            int directionScalar = 0;
            switch (direction)
            {
                case Direction.forward:
                    directionScalar = 1;
                    break;
                case Direction.backward:
                    directionScalar = -1;
                    break;
                default:
                    break;
            }

            int costOfMoving = 0;
            switch (axis)
            {
                case Axis.x:
                    costOfMoving = _costOfMovingAlong_x;
                    break;
                case Axis.y:
                    costOfMoving = _costOfMovingAlong_y;
                    break;
                case Axis.z:
                    costOfMoving = _costOfMovingAlong_z;
                    break;
                default:
                    break;
            }
            return costOfMoving * directionScalar;
        }


        public int DecodeNeighbourId(int pointId, Axis axis, Direction direction, Axis? secondAxis = null, Direction? secondDirection = null)
        {
           
            if (secondAxis == null)
            {
                if (direction == Direction.forward)
                    return _potentialNeighbours * pointId + (int)axis;
                else
                    throw new NotImplementedException();
            }

            int tmp = 0;
            if (direction == Direction.forward && secondDirection == Direction.forward)
            {
                tmp = dim - 1 + (int)axis + (int)secondAxis;
            }
            else if (direction == Direction.backward)
            {
                tmp = dim * 2 - 1 + (int)axis + (int)secondAxis;
            }
            else
            {
                // nie zaimplementowane
                throw new NotImplementedException();
            }


            return _potentialNeighbours * pointId + tmp;
        }

    }

    public struct Edge
    {
        public int first;
        public int second;
    }

    public enum Axis
    {
        x,
        y,
        z
    }

    public enum Direction
    {
        forward,
        backward
    }

}
