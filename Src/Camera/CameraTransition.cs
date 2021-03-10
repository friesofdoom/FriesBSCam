using UnityEngine;

public enum CameraTransitionCurve
{
    LINEAR,
    CUBIC,
}

public struct PositionAndRotation
{
    public Vector3 position;
    public Quaternion rotation;
}

public class CameraTransition
{
    public Vector3 originPosition;
    public Quaternion originRotation;
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public float transitionDuration;
    public CameraTransitionCurve transitionCurveType;

    public PositionAndRotation getInterTransitionPositionAndRotation(float timeSinceSceneStart)
    {

        double t = getTransitionCurveValue(transitionDuration / timeSinceSceneStart, transitionCurveType);

        return new PositionAndRotation{
            position = Vector3.Lerp(
                originPosition,
                targetPosition,
                (float)t
            ),

            rotation = Quaternion.Lerp(
                originRotation,
                targetRotation,
                (float)t
            ),
        };
    }

    private double getTransitionCurveValue(float x, CameraTransitionCurve curveType)
    {
        switch (curveType){
            case CameraTransitionCurve.LINEAR:
                return x;
            case CameraTransitionCurve.CUBIC:
                return x < 0.5 ? 2 * x * x : 1 - System.Math.Pow(-2 * x + 2, 2) / 2;

            default:
                return x;
        }
    }
}
