using UnityEngine;
using System;

/// <summary>
/// DirectionalLightController
/// —— 模拟太阳与月亮的日夜循环，可滑条调试时间
/// </summary>
[ExecuteAlways]
public class DirectionalLightController : MonoBehaviour
{
    [Header("🌍 地理信息")]
    [Tooltip("纬度（北纬为正，南纬为负）")]
    public float latitude = 37f;
    [Tooltip("经度（东经为正，西经为负）")]
    public float longitude = -122f;
    [Tooltip("是否使用真太阳时（考虑经度和方程时间修正）")]
    public bool useTrueSolarTime = false;

    [Header("🕒 时间控制")]
    [Tooltip("游戏日期")]
    public DateTime gameDate = DateTime.Today;

    [Range(0, 24)]
    [Tooltip("当前小时（滑条可调）")]
    public float timeOfDay = 12f; // 0~24 小时制

    [Tooltip("时间流速倍率（1=实时，60=1秒=游戏1分钟）")]
    public float timeScale = 60f;

    [Header("💡 光照设置")]
    public Light sunLight;
    public Light moonLight;

    [Tooltip("太阳光强度曲线（X=sin(elevation)，Y=intensity）")]
    public AnimationCurve sunIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1.2f);

    [Tooltip("月亮光强度曲线（X=sin(elevation)，Y=intensity）")]
    public AnimationCurve moonIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.3f);

    [Tooltip("是否自动调整光色")]
    public bool autoColor = true;

    private float _lastEditorTimeOfDay = -1f;

    void Start()
    {
        if (sunLight == null)
        {
            sunLight = RenderSettings.sun;
            if (sunLight == null)
                Debug.LogWarning("DirectionalLightController: 未指定太阳光 (sunLight)。");
        }
        if (moonLight == null)
            Debug.LogWarning("DirectionalLightController: 未指定月亮光 (moonLight)。");
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            timeOfDay += Time.deltaTime * (timeScale / 3600f);
            if (timeOfDay >= 24f) timeOfDay -= 24f;
        }
        else
        {
            if (Mathf.Abs(timeOfDay - _lastEditorTimeOfDay) > 0.001f)
            {
                _lastEditorTimeOfDay = timeOfDay;
                UpdateLighting();
            }
        }

        if (Application.isPlaying)
        {
            UpdateLighting();
        }
    }

    private void UpdateLighting()
    {
        if (sunLight == null) return;

        DateTime gameDateTime = gameDate.Date + TimeSpan.FromHours(timeOfDay);

        // 🌞 太阳方向
        Vector3 sunDir = SunDirection(latitude, longitude, gameDateTime, useTrueSolarTime);
        sunLight.transform.rotation = Quaternion.LookRotation(-sunDir, Vector3.up);

        float sinElev = Mathf.Clamp01(Vector3.Dot(sunDir.normalized, Vector3.up));
        sunLight.intensity = sunIntensityCurve.Evaluate(sinElev);

        if (autoColor)
        {
            sunLight.color = Color.Lerp(
                new Color(1f, 0.6f, 0.3f), // 日出橙
                Color.white,               // 白天白
                Mathf.Pow(sinElev, 0.6f)
            );
        }

        // 🌙 月亮方向（与太阳相反）
        if (moonLight != null)
        {
            Vector3 moonDir = -sunDir; // 简化：与太阳对称
            moonLight.transform.rotation = Quaternion.LookRotation(-moonDir, Vector3.up);

            float moonSinElev = Mathf.Clamp01(Vector3.Dot(moonDir.normalized, Vector3.up));
            moonLight.intensity = moonIntensityCurve.Evaluate(moonSinElev);

            if (autoColor)
            {
                moonLight.color = Color.Lerp(
                    new Color(0.5f, 0.6f, 1f),  // 冷蓝
                    Color.white,
                    Mathf.Pow(moonSinElev, 0.5f)
                );
            }

            // 🌗 只在夜间启用月亮光
            bool isNight = sunLight.intensity < 0.05f;
            sunLight.enabled = !isNight;
            moonLight.enabled = isNight;
        }
    }

    // ------------------ 太阳方向计算 ------------------

    private static float Deg2Rad(float d) => d * Mathf.Deg2Rad;
    private static float Rad2Deg(float r) => r * Mathf.Rad2Deg;
    private static int DayOfYear(DateTime dt) => dt.DayOfYear;

    private static float SolarDeclination(int N)
    {
        float ang = (360f / 365f) * (284f + N);
        return 23.44f * Mathf.Sin(Deg2Rad(ang));
    }

    private static float EquationOfTime(int N)
    {
        float B = Deg2Rad((360f / 365f) * (N - 81));
        return 9.87f * Mathf.Sin(2f * B) - 7.53f * Mathf.Cos(B) - 1.5f * Mathf.Sin(B);
    }

    public static Vector3 SunDirection(float latitudeDeg, float longitudeDeg, DateTime localTime, bool useTrueSolarTime = false)
    {
        int N = DayOfYear(localTime.Date);
        float decl = SolarDeclination(N);
        float hour = localTime.Hour + localTime.Minute / 60f + localTime.Second / 3600f;
        float solarTime = hour;

        if (useTrueSolarTime)
        {
            float tz = (float)TimeZoneInfo.Local.GetUtcOffset(localTime).TotalHours;
            float lonStd = tz * 15f;
            float EoT = EquationOfTime(N);
            float tc = 4f * (longitudeDeg - lonStd) + EoT;
            solarTime = hour + tc / 60f;
        }

        float H = 15f * (solarTime - 12f);
        float phi = Deg2Rad(latitudeDeg);
        float delta = Deg2Rad(decl);
        float Hr = Deg2Rad(H);

        float sinAlpha = Mathf.Sin(phi) * Mathf.Sin(delta) + Mathf.Cos(phi) * Mathf.Cos(delta) * Mathf.Cos(Hr);
        sinAlpha = Mathf.Clamp(sinAlpha, -1f, 1f);
        float alpha = Mathf.Asin(sinAlpha);

        float y = Mathf.Sin(Hr);
        float x = Mathf.Cos(Hr) * Mathf.Sin(phi) - Mathf.Tan(delta) * Mathf.Cos(phi);
        float azimuth = Mathf.Atan2(y, x);
        float azDeg = (Rad2Deg(azimuth) + 360f) % 360f;

        float elev = alpha;
        float cosE = Mathf.Cos(elev);
        float azR = Deg2Rad(azDeg);

        Vector3 dir = new Vector3(
            cosE * Mathf.Sin(azR),
            Mathf.Sin(elev),
            cosE * Mathf.Cos(azR)
        );

        return dir.normalized;
    }
}
