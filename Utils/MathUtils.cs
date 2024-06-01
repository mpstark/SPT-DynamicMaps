using UnityEngine;

namespace DynamicMaps.Utils
{
    public static class MathUtils
    {
        public enum RotationAxis
        {
            X, Y, Z
        }

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

        public static Vector2 GetMidpoint(Vector2 minBound, Vector2 maxBound)
        {
            return (minBound + maxBound) / 2;
        }

        public static Vector3 ConvertToMapPosition(Vector3 unityPosition)
        {
            return new Vector3(unityPosition.x, unityPosition.z, unityPosition.y);
        }

        public static Vector3 ConvertToMapPosition(Transform transform)
        {
            return ConvertToMapPosition(transform.position);
        }

        public static float ConvertToMapRotation(Transform transform, RotationAxis axis = RotationAxis.Y)
        {
            switch (axis)
            {
                case RotationAxis.X:
                    return -transform.rotation.eulerAngles.x;
                case RotationAxis.Y:
                    return -transform.rotation.eulerAngles.y;
                case RotationAxis.Z:
                    return -transform.rotation.eulerAngles.z;
            }

            return 0f;
        }

        public static bool ApproxEquals(float first, float second)
        {
            return Mathf.Abs(first - second) < float.Epsilon;
        }
    }
}
