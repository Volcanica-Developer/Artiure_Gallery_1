using UnityEngine;
using UnityEngine.UI;
using SK.GyroscopeWebGL;
using TMPro;

namespace SK.GyroscopeWebGL.Examples
{
    public class SK_GyroscopeTest : MonoBehaviour
    {
        public Text Label;
        public Transform Model;
        public Button Button;
        [Tooltip("Optional UI button to toggle axis swapping at runtime.")]
        public Button AxisSwapButton;

        [Header("Gyro Options")]
        public bool useRelativeToInitialRotation = true;
        [Tooltip("Swap Y and Z axes from the device orientation before applying to the model.")]
        public bool swapYAndZAxes = false;

        private bool hasReference;
        private Quaternion rotationReference;

        void Awake()
        {
            if (Button != null)
            {
                Button.onClick.AddListener(ToggleGyroscope);
            }

            if (AxisSwapButton != null)
            {
                AxisSwapButton.onClick.AddListener(ToggleAxisSwap);
            }
        }

        private void Start()
        {
            SK_DeviceSensor.StartGyroscopeListener(OnGyroscopeReading);
            hasReference = false;

            if (Button != null)
            {
                Button.GetComponentInChildren<TMP_Text>().text = SK_DeviceSensor.IsGyroscopeStarted ? "Gyro Stop" : "Gyro Start";
            }

            if (AxisSwapButton != null)
            {
                AxisSwapButton.GetComponentInChildren<TMP_Text>().text = swapYAndZAxes ? "Axis: Y<->Z" : "Axis: Normal";
            }
        }

        void OnDestroy()
        {
            SK_DeviceSensor.StopGyroscopeListener();
        }

        private void OnGyroscopeReading(GyroscopeData reading)
        {
            //Label.text = $"alpha: {reading.Alpha}, beta: {reading.Beta},gamma: {reading.Gamma} absolute: {reading.Absolute} ,unityRotation: {reading.UnityRotation}";
            if (Model == null)
                return;

            Quaternion deviceRotation = reading.UnityRotation;

            Quaternion targetWorldRotation;

            if (!useRelativeToInitialRotation)
            {
                // Use absolute device orientation
                targetWorldRotation = deviceRotation;
            }
            else
            {
                if (!hasReference)
                {
                    // Capture relation between current model rotation (scene setup) and device rotation
                    // so that the initial orientation in the scene is treated as the neutral pose.
                    rotationReference = Model.rotation * Quaternion.Inverse(deviceRotation);
                    hasReference = true;
                }

                // World-space rotation we want based on current device orientation, relative to start.
                targetWorldRotation = rotationReference * deviceRotation;
            }

            // Work in Euler space for optional axis tweaks
            Vector3 eulerAngles = targetWorldRotation.eulerAngles;

            if (swapYAndZAxes)
            {
                // Optional axis remap to fix Y/Z swap issues between device and model
                eulerAngles = new Vector3(eulerAngles.x, eulerAngles.z, eulerAngles.y);
            }

            targetWorldRotation = Quaternion.Euler(eulerAngles);
            Model.rotation = targetWorldRotation;
        }

        private void ToggleGyroscope()
        {
            if (SK_DeviceSensor.IsGyroscopeStarted)
            {
                SK_DeviceSensor.StopGyroscopeListener();
            }
            else
            {
                hasReference = false;
                SK_DeviceSensor.StartGyroscopeListener(OnGyroscopeReading);
            }

            if (Button != null)
            {
                Button.GetComponentInChildren<TMP_Text>().text = SK_DeviceSensor.IsGyroscopeStarted ? "Gyro Stop" : "Gyro Start";
            }
        }

        private void ToggleAxisSwap()
        {
            swapYAndZAxes = !swapYAndZAxes;

            if (AxisSwapButton != null)
            {
                AxisSwapButton.GetComponentInChildren<TMP_Text>().text = swapYAndZAxes ? "Axis: Y<->Z" : "Axis: Normal";
            }
        }
    }
}
