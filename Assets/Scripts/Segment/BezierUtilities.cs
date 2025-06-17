using Unity.Mathematics;

namespace Bezier
{
    public static class BezierUtilities
    {
        public static float TranslateT(Segment segment, float t, float targetDistance)
        {
            const int steps = 1000;
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
