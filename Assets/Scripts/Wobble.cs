using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wobble : MonoBehaviour
{
    private Renderer rend;

    private Vector3 lastPos;
    private Vector3 velocity;

    private Vector3 lastRot;
    private Vector3 angularVelocity;

    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;

    private float wobbleAmountX;
    private float wobbleAmountZ;

    private float wobbleAmountToAddX;
    private float wobbleAmountToAddZ;

    private float pulse;
    private float time = 0.5f;

    void Start()
    {
        rend = GetComponent<Renderer>();

        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Evitar problemas si dt es 0
        if (dt <= 0f) return;

        time += dt;

        // Reducir wobble progresivamente
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, dt * Recovery);
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, dt * Recovery);

        // Generar onda seno
        pulse = 2 * Mathf.PI * WobbleSpeed;

        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        // Enviar al shader
        if (rend != null && rend.material != null)
        {
            rend.material.SetFloat("_WobbleX", wobbleAmountX);
            rend.material.SetFloat("_WobbleZ", wobbleAmountZ);
        }

        // Calcular velocidad correctamente
        velocity = (transform.position - lastPos) / dt;

        angularVelocity = (transform.rotation.eulerAngles - lastRot);

        // Aplicar influencia del movimiento al líquido
        wobbleAmountToAddX += Mathf.Clamp(
            (velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble,
            -MaxWobble,
            MaxWobble
        );

        wobbleAmountToAddZ += Mathf.Clamp(
            (velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble,
            -MaxWobble,
            MaxWobble
        );

        // Guardar estado anterior
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }
}
