using UnityEngine;

/// <summary>
/// Projétil puramente visual que percorre exatamente a linha já
/// resolvida pelo RaycastPistol. O dano continua sendo hitscan/raycast.
/// </summary>
public class RaycastBulletVisual : MonoBehaviour
{
    private Vector3 startPoint;
    private Vector3 endPoint;
    private float speed;
    private float maxLifetime;
    private float elapsed;
    private float totalDistance;
    private float travelledDistance;
    private Vector3 rotationOffset;
    private bool initialized;
    private bool reachedEnd;
    private float destroyDelay;
    private TrailRenderer trailRenderer;

    public void Initialize(
        Vector3 start,
        Vector3 end,
        float moveSpeed,
        float lifetime,
        Vector3 modelRotationOffset,
        bool enableTrail,
        float trailTime,
        float trailStartWidth,
        float trailEndWidth,
        float trailMinVertexDistance,
        Material trailMaterial,
        Color trailColor
    )
    {
        startPoint = start;
        endPoint = end;
        speed = Mathf.Max(0.01f, moveSpeed);
        maxLifetime = Mathf.Max(0.05f, lifetime);
        rotationOffset = modelRotationOffset;

        Vector3 direction =
            endPoint - startPoint;

        totalDistance =
            direction.magnitude;

        travelledDistance = 0f;
        elapsed = 0f;

        transform.position =
            startPoint;

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized
                ) *
                Quaternion.Euler(
                    rotationOffset
                );
        }

        foreach (Collider col in
                 GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        foreach (Rigidbody body in
                 GetComponentsInChildren<Rigidbody>())
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        SetupTrail(
            enableTrail,
            trailTime,
            trailStartWidth,
            trailEndWidth,
            trailMinVertexDistance,
            trailMaterial,
            trailColor
        );

        reachedEnd = false;
        destroyDelay = enableTrail
            ? Mathf.Max(0.01f, trailTime)
            : 0f;

        initialized = true;
    }

    void SetupTrail(
        bool enableTrail,
        float trailTime,
        float startWidth,
        float endWidth,
        float minVertexDistance,
        Material material,
        Color color
    )
    {
        trailRenderer =
            GetComponent<TrailRenderer>();

        if (!enableTrail)
        {
            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                trailRenderer.enabled = false;
            }

            return;
        }

        if (trailRenderer == null)
        {
            trailRenderer =
                gameObject.AddComponent<TrailRenderer>();
        }

        trailRenderer.enabled = true;
        trailRenderer.emitting = true;
        trailRenderer.time = Mathf.Max(0.01f, trailTime);
        trailRenderer.startWidth = Mathf.Max(0.0001f, startWidth);
        trailRenderer.endWidth = Mathf.Max(0f, endWidth);
        trailRenderer.minVertexDistance = Mathf.Max(0.001f, minVertexDistance);
        trailRenderer.alignment = LineAlignment.View;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.numCapVertices = 2;
        trailRenderer.numCornerVertices = 2;

        if (material != null)
        {
            trailRenderer.material = material;
        }

        Gradient gradient =
            new Gradient();

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        trailRenderer.colorGradient = gradient;
        trailRenderer.Clear();
    }


    void Update()
    {
        if (!initialized)
            return;

        if (reachedEnd)
        {
            destroyDelay -= Time.deltaTime;

            if (destroyDelay <= 0f)
            {
                Destroy(gameObject);
            }

            return;
        }

        elapsed +=
            Time.deltaTime;

        if (elapsed >= maxLifetime ||
            totalDistance <= 0.0001f)
        {
            Destroy(gameObject);
            return;
        }

        travelledDistance +=
            speed *
            Time.deltaTime;

        float t =
            Mathf.Clamp01(
                travelledDistance /
                totalDistance
            );

        transform.position =
            Vector3.Lerp(
                startPoint,
                endPoint,
                t
            );

        if (t >= 1f)
        {
            reachedEnd = true;

            foreach (Renderer renderer in
                     GetComponentsInChildren<Renderer>())
            {
                if (renderer == trailRenderer)
                    continue;

                renderer.enabled = false;
            }

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }

            if (destroyDelay <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
