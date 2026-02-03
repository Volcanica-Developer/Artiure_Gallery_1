using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GyroRotate : MonoBehaviour
{
    [Header("Gyro Settings")]
    [SerializeField] private bool canDoGyro = true;
    [SerializeField] private bool usePlayerPrefsToggle = false;
    [SerializeField] private string playerPrefsKey = "GYRO_SETTING";

    [Header("Rotation Limits (x = minY, y = maxY, z = minX, w = maxX)")]
    [SerializeField] private Vector4 cardRotationLimits = new Vector4(-45f, 45f, -30f, 30f);

    [Header("Sensitivity")]
    [SerializeField] private float cardScreenGyroSpeed = 1f;

    private Vector3 delta;

    void OnEnable()
    {
        bool tCanDoGyroSetting = true;
        if (usePlayerPrefsToggle)
        {
            tCanDoGyroSetting = PlayerPrefs.GetInt(playerPrefsKey, 0) > 0;
        }

        if (tCanDoGyroSetting && SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            transform.localRotation = Quaternion.Euler(Vector3.zero);
            delta = GyroToUnity(Input.gyro.attitude).eulerAngles;
        }
    }

    void OnDisable()
    {
        Input.gyro.enabled = false;
    }

    void FixedUpdate()
    {
        if (Input.gyro.enabled && canDoGyro)
            GyroModifyCamera();
    }

    // The Gyroscope is right-handed. Unity is left handed.
    // Make the necessary change to the camera.
    void GyroModifyCamera()
    {
        Vector3 tGyroRotation = Input.gyro.rotationRateUnbiased;
        //Log.Debug("gyro rate: " + tGyroRotation);
        delta = tGyroRotation;

        // In landscape mode the device axes are effectively swapped relative to the UI.
        // Swap X and Y so that horizontal phone movement still maps to horizontal rotation.
        if (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight)
        {
            float tmp = delta.x;
            delta.x = delta.y;
            delta.y = tmp;
        }

        delta.z = 0f;

        Vector3 tLocationRotation = transform.localRotation.eulerAngles;
        tLocationRotation -= delta * 5 * cardScreenGyroSpeed;

        //if (tLocationRotation.y >= cardRotationLimits.x && tLocationRotation.y < 180f)
        //    tLocationRotation.y = cardRotationLimits.x;
        //else if (tLocationRotation.y <= cardRotationLimits.y && tLocationRotation.y > 180f)
        //    tLocationRotation.y = cardRotationLimits.y;

        //if (tLocationRotation.x >= cardRotationLimits.z && tLocationRotation.x < 180f)
        //    tLocationRotation.x = cardRotationLimits.z;
        //else if (tLocationRotation.x <= cardRotationLimits.w && tLocationRotation.x > 180f)
        //    tLocationRotation.x = cardRotationLimits.w;

        tLocationRotation.z = 0f;

        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(tLocationRotation), 0.1f);

        //Log.Debug("GyroInput: " + Input.gyro.attitude.eulerAngles.ToString() + " :: " + transform.localRotation.eulerAngles.ToString());

        //delta = tGyroRotation;
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    public void AllowGyroscope()
    {
        // User agreed: enable gyro (optionally wrap with UNITY_IOS)
#if UNITY_IOS && !UNITY_EDITOR
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("Gyroscope enabled on iOS.");
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device.");
        }
#else
        Debug.Log("Gyro allowed (non‑iOS / Editor).");
#endif

    }
}
