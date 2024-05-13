using System.Collections.Generic;
using UnityEngine;

namespace InGameMap.Utils
{
    public static class MathUtils
    {
        public static Vector2 GetRotatedVector2(Vector2 vector, float degreeRotation)
        {
            var x = vector.x;
            var y = vector.y;
            var sin = Mathf.Sin(degreeRotation * Mathf.Deg2Rad);
            var cos = Mathf.Cos(degreeRotation * Mathf.Deg2Rad);
            return new Vector2(x * cos - y * sin, x * sin + y * cos);
        }

        public static Vector2 GetRotatedRectangle(Vector2 rectangle, float degreeRotation)
        {
            // adapted from https://stackoverflow.com/questions/54072295/get-bounds-of-unrotated-rotated-rectangle
            // Under CC BY-SA 4.0 Deed License
            var AB = rectangle.x;
            var AD = rectangle.y;
            var alpha = degreeRotation * Mathf.Deg2Rad;
            var gamma = Mathf.PI / 2f;
            var beta = gamma - alpha;
            var EA = Mathf.Abs(AD * Mathf.Sin(alpha));
            var ED = Mathf.Abs(AD * Mathf.Sin(beta));
            var FB = Mathf.Abs(AB * Mathf.Sin(alpha));
            var AF = Mathf.Abs(AB * Mathf.Sin(beta));

            return new Vector2(EA + AF, ED + FB);
            // END CC BY-SA 4.0 Deed License
        }

        public static Vector2 GetBoundingRectangle(IEnumerable<Vector2> points)
        {
            // TODO: there is probably a better way to do this
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            foreach (var point in points)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            return new Vector2(maxX - minX, maxY - minY);
        }

        public static Vector2 GetMidpoint(IEnumerable<Vector2> points)
        {
            var sum = Vector2.zero;
            var count = 0;

            foreach (var point in points)
            {
                sum += point;
                count++;
            }

            return sum / count;
        }
    }
}
