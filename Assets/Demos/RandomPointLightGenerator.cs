using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalPointLightGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int lightCount = 32;
    public float radius = 3f;
    public float movementRange = 1f; 
    public float floatAmplitude = 0.3f;
    public float floatSpeedMin = 1f;
    public float floatSpeedMax = 2f;
    [Header("Color Settings")]
    public Color baseColorA = new Color(1f, 0.85f, 0.5f); 
    public Color baseColorB = new Color(0.8f, 0.5f, 0.2f);
    
    private List<LightMovement> lightMovements = new List<LightMovement>();

    void Start()
    {
        lightMovements.Clear();
        var root = transform.Find("Root");
        if (root)
        {
            foreach (Transform child in root)
            {
                var basePos = child.localPosition;
                var movement = new LightMovement
                {
                    light = child.GetComponent<Light>(),
                    basePosition = basePos,
                    velocity = Random.insideUnitSphere * 0.5f,
                    floatOffset = Random.Range(0f, Mathf.PI * 2),
                    floatSpeed = Random.Range(floatSpeedMin, floatSpeedMax)
                };
                lightMovements.Add(movement);
            }
        }
    }

    private void Generate(Transform parent)
    {
        // Fibonacci Sphere 均匀球面分布
        float offset = 2f / lightCount;
        float increment = Mathf.PI * (3f - Mathf.Sqrt(5f)); // golden angle

        for (int i = 0; i < lightCount; i++)
        {
            float y = i * offset - 1f + (offset / 2f);
            float r = Mathf.Sqrt(1f - y * y);
            float phi = i * increment;

            float x = Mathf.Cos(phi) * r;
            float z = Mathf.Sin(phi) * r;

            Vector3 dir = new Vector3(x, y, z);
            Vector3 pos = dir * radius;

            var go = new GameObject("Light_" + i);
            var light = go.AddComponent<Light>();
            light.transform.SetParent(parent);
            light.transform.localPosition = pos;

            light.type = LightType.Point;
            light.intensity = 1f;
            light.range = Random.Range(1.2f, 1.8f);
            
            // 在 baseColorA ~ baseColorB 之间做 HSV 插值
            Color lerped = Color.Lerp(baseColorA, baseColorB, Random.value);
            Color.RGBToHSV(lerped, out float h, out float s, out float v);

            // 饱和度控制：偏低
            s = Random.Range(0.15f, 0.5f);

            // 亮度控制：不可太暗
            v = Random.Range(0.5f, 0.6f);

            light.color = Color.HSVToRGB(h, s, v);
        }
    }

    [ContextMenu("Regenerate")]
    private void Generate()
    {
        var root = transform.Find("Root");
        if (root)
        {
            if (Application.isPlaying)
                Destroy(root.gameObject);
            else
                DestroyImmediate(root.gameObject);
        }

        root = new GameObject("Root").transform;
        root.SetParent(this.transform, false);
        root.localPosition = Vector3.zero;

        Generate(root);
    }

    void Update()
    {
        foreach (var m in lightMovements)
        {
            Vector3 offset = m.velocity * Time.deltaTime;

            // 限制最大移动范围（与basePosition距离）
            Vector3 toBase = m.light.transform.localPosition - m.basePosition;
            if (toBase.magnitude > movementRange)
            {
                m.velocity = -m.velocity;
                offset = m.velocity * Time.deltaTime;
            }

            Vector3 pos = m.light.transform.localPosition;
            pos += new Vector3(offset.x, offset.y, offset.z);

            // 加上 Y轴浮动（叠加）
            float yOffset = Mathf.Sin(Time.time * m.floatSpeed + m.floatOffset) * floatAmplitude;
            pos.y = m.basePosition.y + yOffset;

            m.light.transform.localPosition = pos;
        }
    }

    public class LightMovement
    {
        public Light light;
        public Vector3 basePosition;
        public Vector3 velocity;
        public float floatOffset;
        public float floatSpeed;
    }
}
