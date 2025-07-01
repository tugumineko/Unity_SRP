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
    public float expandDistance = 0.5f;
    [Header("Color Settings")]
    public Color baseColorA = new Color(1f, 0.85f, 0.5f); 
    public Color baseColorB = new Color(0.8f, 0.5f, 0.2f);
    [Header("Reference Nodes")] 
    public GameObject DynamicSkyCtrl;
    
    private List<LightMovement> lightMovements = new List<LightMovement>();
    private TimeCtrl timeCtrl;
    
    void Start()
    {
        if (timeCtrl == null)
        {
            timeCtrl = DynamicSkyCtrl.GetComponent<TimeCtrl>(); 
        }
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
            s = Random.Range(0.4f, 0.6f);

            // 亮度控制：不可太暗
            v = Random.Range(0.6f, 0.8f);

            light.color = Color.HSVToRGB(h, s, v);
        }
    }

    [ContextMenu("Regenerate")]
    private void Generate()
    {
        if (timeCtrl != null)
        {
            timeCtrl = DynamicSkyCtrl.GetComponent<TimeCtrl>(); 
        }
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
        if (!timeCtrl)
            return;

        float timeOfDay = timeCtrl.TimeofDay;
        float dayEndTime = timeCtrl.DayStartTime + timeCtrl.DayDuration;

        float fadeStart = dayEndTime - 1f;
        float fadeEnd = dayEndTime + 1f;

        bool inTransition = timeOfDay >= fadeStart && timeOfDay <= fadeEnd;
        float fadeT = inTransition ? Mathf.InverseLerp(fadeStart, fadeEnd, timeOfDay) : 0f;
        

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

            // 扩散位置：离开base位置，向外偏移
            if (inTransition)
            {
                // 曲线加速（慢到快）：可调换成 Mathf.SmoothStep 或其他函数
                float expandT = Mathf.Pow(fadeT,5);

                Vector3 dir = (m.basePosition).normalized;
                pos += dir * (expandDistance * expandT); // 2f 是最大扩散距离，可调
            }

            m.light.transform.localPosition = pos;

            // 强度渐变
            m.light.intensity = inTransition ? Mathf.Lerp(1f, 0f, fadeT) : 0f;
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
