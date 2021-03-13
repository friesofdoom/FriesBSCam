using UnityEngine;

public enum CameraTransitionCurve
{
    LINEAR,
    CUBIC,
    SLERP,
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
    public float transitionDuration = 0f;
    public CameraTransitionCurve transitionCurveType = CameraTransitionCurve.LINEAR;

    public PositionAndRotation getInterTransitionPositionAndRotation(float timeSinceSceneStart)
    {

        float t = (float)getTransitionCurveValue(timeSinceSceneStart / transitionDuration, transitionCurveType);
        t = Mathf.Clamp(t, 0, 1);

        return new PositionAndRotation{
            position = Vector3.Lerp(
                originPosition,
                targetPosition,
                t
            ),

            rotation = Quaternion.Lerp(
                originRotation,
                targetRotation,
                t
            ),
        };
    }

    override public string ToString()
    {
        return
            "--- Camera Transition--- " + "\n\n" + 
            "Origional Position: " + originPosition.ToString() + "\n" +
            "Origional Rotation: " + originRotation.ToString() + "\n" +
            "Target Position: " + targetPosition.ToString() + "\n" +
            "Target Rotation: " + targetRotation.ToString() + "\n" +
            "transitionDuration: " + transitionDuration.ToString() + "\n";
    }

    private double getTransitionCurveValue(float x, CameraTransitionCurve curveType)
    {
        switch (curveType){
            case CameraTransitionCurve.LINEAR:
                return x;
            case CameraTransitionCurve.CUBIC:
                return x < 0.5 ? 4 * x * x * x : 1 - System.Math.Pow(-2 * x + 2, 3) / 2;
            case CameraTransitionCurve.SLERP:
                //Forgive me, for I have sinned.  But unity doesn't offer a slerp for scalars.
                return Vector3.Slerp(new Vector3(0, 0, 0), new Vector3(1, 0, 0), x).x;
            default:
                return x;
        }
    }
}
