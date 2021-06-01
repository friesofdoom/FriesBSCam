using UnityEngine;

namespace FriesBSCameraPlugin.Camera
{
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

        public PositionAndRotation GetInterTransitionPositionAndRotation(float timeSinceSceneStart)
        {
            if (transitionDuration <= 0f)
            {
                return new PositionAndRotation
                {
                    position = targetPosition,
                    rotation = targetRotation,
                };
            }

            // Handle custom curves
            float linearT = timeSinceSceneStart / transitionDuration;
            float filteredT = (float) GetTransitionCurveValue(linearT, transitionCurveType);
            return new PositionAndRotation
            {
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

        public override string ToString()
        {
            return
                "--- Camera Transition--- " + "\n\n" +
                "Original Position: " + originPosition + "\n" +
                "Original Rotation: " + originRotation + "\n" +
                "Target Position: " + targetPosition + "\n" +
                "Target Rotation: " + targetRotation + "\n" +
                "transitionDuration: " + transitionDuration + "\n" +
                "transitionCurve: " + System.Enum.GetName(typeof(CameraTransitionCurve), transitionCurveType) + "\n";
        }

        private static double GetTransitionCurveValue(float x, CameraTransitionCurve curveType)
        {
            switch (curveType)
            {
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
}