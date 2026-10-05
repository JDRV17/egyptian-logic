using UnityEngine;

public class PC_crouch : MonoBehaviour
{
    public KeyCode crouchKey = KeyCode.LeftControl;

    public float crouchY = -1f;
    public float standY = 0f;
    public float crouchSpeed = 10f; // mayor = más rápido

    private bool isCrouching = false;
    private float targetY;

    void Start()
    {
        targetY = standY;
    }

    void Update()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            ToggleCrouch();
        }

        // Movimiento suave cada frame
        Vector3 pos = transform.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * crouchSpeed);
        transform.localPosition = pos;
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;

        if (isCrouching)
            targetY = crouchY;
        else
            targetY = standY;
    }
}
