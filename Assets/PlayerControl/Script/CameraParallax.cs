using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
public class CameraParallax : MonoBehaviour
{

    [Header("Parallax Settings")]
    public float rotationStrength = 10f; // 鼠标影响旋转的强度（角度）
    public float smoothSpeed = 5f;       // 旋转平滑速度

    private Quaternion baseRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        baseRotation = transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = GetMousePosition();
        Vector2 normalized = new Vector2(
            (mousePos.x / Screen.width) * 2f - 1f,
            (mousePos.y / Screen.height) * 2f - 1f
        );
        normalized.x = Mathf.Clamp(normalized.x, -1f, 1f);
        normalized.y = Mathf.Clamp(normalized.y, -1f, 1f);
        
        Quaternion targetRotation = baseRotation * Quaternion.Euler(
            -normalized.y * rotationStrength,  // 上下控制Pitch（绕X轴）
            normalized.x * rotationStrength,   // 左右控制Yaw（绕Y轴）
            0
        );

        // 平滑插值旋转
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smoothSpeed);
        
        #if ENABLE_INPUT_SYSTEM
            Debug.Log("new");
        #endif
    }
    
    private Vector2 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();
#endif
        return Input.mousePosition;
    }
}
