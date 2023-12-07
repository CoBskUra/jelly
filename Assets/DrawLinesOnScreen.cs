using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using Assets;

// Put this script on a Camera
public class DrawLinesOnScreen : MonoBehaviour
{
     
    // Fill/drag these in from the editor

    // Choose the Unlit/Color shader in the Material Settings
    // You can change that color, to change the color of the connecting lines
    public Material lineMat;
    public SpringCube sCube;



    // Connect all of the `points` to the `mainPoint`
    void DrawConnectingLines()
    {
        //Material material = sCube.GetComponent<Renderer>().sharedMaterial;
        //Vector3 position = sCube.transform.position;
        //// Loop through each point to connect to the mainPoint
        //foreach (Assets.Edge edge in sCube.edges)
        //{
        //    Vector3 p1 = sCube.PositionOnScreen(edge.first) ;
        //    Vector3 p2 = sCube.PositionOnScreen(edge.second);
        //    GL.Begin(GL.LINES);
        //    material.SetPass(0);
        //    GL.Color(new Color(1-material.color.r, 1-material.color.g, material.color.b, 1 - material.color.a));
        //    GL.Vertex3(p1.x, p1.y, p1.z);
        //    GL.Vertex3(p2.x, p2.y, p2.z);
        //    GL.End();
        //}

    }

    // To show the lines in the game window whne it is running
    void OnPostRender()
    {
        DrawConnectingLines();
    }

    // To show the lines in the editor
    void OnDrawGizmos()
    {
        DrawConnectingLines();
    }
}