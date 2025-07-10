using Unity.Mathematics;
using UnityEngine;

namespace Bezier
{
    public static class BezierUtilities
    {
        public static float TranslateT(Segment segment, float t, float targetDistance)
        {
            const int steps = 1000;
            return TranslateT(segment, t, targetDistance, steps);
        }

        public static float TranslateT(Segment segment, float t, float targetDistance, int steps)
        {
            var newPosition = EvaluateCubicBezier(segment, t);
            var oldPosition = newPosition;
            for (var step = t * steps; step <= steps + 1; step += 1)
            {
                t = step / steps;
                if (t > 1)
                    return 1;

                newPosition = EvaluateCubicBezier(segment, t);
                var steppedDistance = math.distance(newPosition, oldPosition);
                if (steppedDistance < targetDistance)
                    continue;

                var previousT = (step - 1) / steps;
                var previousPosition = EvaluateCubicBezier(segment, previousT);
                var previousDistance = math.distance(oldPosition, previousPosition);

                var ratio = (targetDistance - previousDistance) / (steppedDistance - previousDistance);
                var interpretedT = math.lerp(previousT, t, ratio);

                return interpretedT;
            }

            UnityEngine.Debug.LogError("Failed to Translate T");
            return t;
        }

        public static Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return u * u * u * p0 +
                   3 * u * u * t * p1 +
                   3 * u * t * t * p2 +
                   t * t * t * p3;
        }

        public static Vector3 EvaluateCubicBezier(SegmentAuthoring segment, float t)
        {
            var p0 = segment.WorldStart;
            var p1 = segment.WorldStartTangent;
            var p2 = segment.WorldEndTangent;
            var p3 = segment.WorldEnd;

            float u = 1 - t;
            return u * u * u * p0 +
                   3 * u * u * t * p1 +
                   3 * u * t * t * p2 +
                   t * t * t * p3;
        }

        public static float3 EvaluateCubicBezier(Segment segment, float t)
        {
            var p0 = segment.Start;
            var p1 = segment.StartTangent;
            var p2 = segment.EndTangent;
            var p3 = segment.End;

            float u = 1 - t;
            return
                u * u * u * p0 +
                3 * u * u * t * p1 +
                3 * u * t * t * p2 +
                t * t * t * p3;
        }
    }
}
