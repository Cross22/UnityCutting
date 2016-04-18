/*
Developed by Michalis Zervos - http://michal.is
http://michal.is/projects/convex-hull-gift-wrapping-method/
*/

using System;
using System.Collections.Generic;
using UnityEngine;

class ConvexHull
{
    public static List<Vector3> CH2(List<Vector3> points)
    {
        return CH2(points, false);
    }

    public static List<Vector3> CH2(List<Vector3> points, bool removeFirst)
    {
        List<Vector3> vertices = new List<Vector3>();

        if (points.Count == 0)
            return null;
        else if (points.Count == 1)
        {
            // If it's a single point, return it
            vertices.Add(points[0]);
            return vertices;
        }


        Vector3 leftMost = CH2Init(points);
        vertices.Add(leftMost);

        Vector3 prev = leftMost;
        Vector3? next;
        double rot = 0;
        do
        {
            next = CH2Step(prev, points, ref rot);

            // If it's not the first vertex (leftmost) or we want spiral (instead of CH2)
            // remove it
            if (prev != leftMost || removeFirst)
                points.Remove(prev);

            // If this isn't the last vertex, save it
            if (next.HasValue)
            {
                vertices.Add(next.Value);
                prev = next.Value;
            }

        } while (points.Count > 0 && next.HasValue && next.Value != leftMost);
        points.Remove(leftMost);

        return vertices;

    }

    private static Vector3 CH2Init(List<Vector3> points)
    {
        // Initialization - Find the leftmost point
        Vector3 leftMost = points[0];
        float leftX = leftMost.x;

        foreach (Vector3 p in points)
        {
            if (p.x < leftX)
            {
                leftMost = p;
                leftX = p.x;
            }
        }
        return leftMost;
    }

    private static Vector3? CH2Step(Vector3 currentPoint, List<Vector3> points, ref double rot)
    {
        double angle, angleRel, smallestAngle = 2 * Math.PI, smallestAngleRel = 4 * Math.PI;
        Vector3? chosen = null;
        float xDiff, yDiff;

        foreach (Vector3 candidate in points)
        {
            if (candidate == currentPoint)
                continue;

            xDiff = candidate.x - currentPoint.x;
            yDiff = -(candidate.y - currentPoint.y); //Y-axis starts on top
            angle = ComputeAngle(new Vector3(xDiff, yDiff));

            // angleRel is the angle between the line and the rotated y-axis
            // y-axis has the direction of the last computed supporting line
            // given by variable rot.
            angleRel = 2 * Math.PI - (rot - angle);

            if (angleRel >= 2 * Math.PI)
                angleRel -= 2 * Math.PI;
            if (angleRel < smallestAngleRel)
            {
                smallestAngleRel = angleRel;
                smallestAngle = angle;
                chosen = candidate;
            }

        }

        // Save the smallest angle as the rotation of the y-axis for the
        // computation of the next supporting line.
        rot = smallestAngle;

        return chosen;
    }


    private static double ComputeAngle(Vector3 p)
    {
        if (p.x > 0 && p.y > 0)
            return Math.Atan((double)p.x / p.y);
        else if (p.x > 0 && p.y == 0)
            return (Math.PI / 2);
        else if (p.x > 0 && p.y < 0)
            return (Math.PI + Math.Atan((double)p.x / p.y));
        else if (p.x == 0 && p.y >= 0)
            return 0;
        else if (p.x == 0 && p.y < 0)
            return Math.PI;
        else if (p.x < 0 && p.y > 0)
            return (2 * Math.PI + Math.Atan((double)p.x / p.y));
        else if (p.x < 0 && p.y == 0)
            return (3 * Math.PI / 2);
        else if (p.x < 0 && p.y < 0)
            return (Math.PI + Math.Atan((double)p.x / p.y));
        else
            return 0;
    }

}
