using UnityEngine;

public enum CameraTransitionCurve
{
    Linear,
    EaseInOutCubic,
    EaseOutCubic,
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
    public CameraTransitionCurve transitionCurveType = CameraTransitionCurve.Linear;

    public PositionAndRotation getInterTransitionPositionAndRotation(float timeSinceSceneStart)
    {
        if(transitionDuration <= 0f)
        {
            return new PositionAndRotation {
                position = targetPosition,
                rotation = targetRotation,
            };
        }

        // Handle custom curves
        float linearT = timeSinceSceneStart / transitionDuration;
        float filteredT = (float)getTransitionCurveValue(linearT, transitionCurveType);
        return new PositionAndRotation{
            position = Vector3.Lerp(
                originPosition,
                targetPosition,
                filteredT
            ),

            rotation = Quaternion.Lerp(
                originRotation,
                targetRotation,
                filteredT
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
            "transitionDuration: " + transitionDuration.ToString() + "\n" +
            "transitionCurve: " + System.Enum.GetName(typeof(CameraTransitionCurve), transitionCurveType) + "\n";
    }

    private double getTransitionCurveValue(float x, CameraTransitionCurve curveType)
    {
        switch (curveType){
            case CameraTransitionCurve.Linear:
                return x;
            case CameraTransitionCurve.EaseOutCubic:
                return 1 - System.Math.Pow(1 - x, 3);
            case CameraTransitionCurve.EaseInOutCubic:
                return x < 0.5 ? 4 * x * x * x : 1 - System.Math.Pow(-2 * x + 2, 3) / 2;
            default:
                return x;
        }
    }
}
