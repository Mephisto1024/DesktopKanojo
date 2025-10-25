using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using TMPro;
using UnityEngine.UI;

// 定义JSON数据对应的实体类（用于解析返回结果，与文档“返回结果参数说明”对应）
[System.Serializable] // 序列化类，方便在Inspector面板查看（可选）
public class WeatherResponse
{
    public string status; // 返回状态：1=成功，0=失败
    public string count;  // 返回结果总数
    public string info;   // 状态信息
    public string infocode; // 状态说明：10000=正确
    public LiveWeather[] lives; // 实况天气数据（extensions=base时返回）
    public ForecastWeather[] forecast; // 预报天气数据（extensions=all时返回）
}

[System.Serializable]
public class LiveWeather
{
    public string province; // 省份名
    public string city;     // 城市名
    public string adcode;   // 区域编码
    public string weather;  // 天气现象（如“晴”“多云”）
    public string temperature; // 实时气温（℃）
    public string winddirection; // 风向（如“东北风”）
    public string windpower; // 风力（级）
    public string humidity; // 空气湿度（%）
    public string reporttime; // 数据发布时间
}

[System.Serializable]
public class ForecastWeather
{
    public string city;     // 城市名
    public string adcode;   // 城市编码
    public string province; // 省份名
    public string reporttime; // 预报发布时间
    public ForecastCast[] casts; // 预报列表（当天、第二天、第三天）
}

[System.Serializable]
public class ForecastCast
{
    public string date; // 日期（如“2025-03-18”）
    public string week; // 星期几（如“2”=星期二）
    public string dayweather; // 白天天气
    public string nightweather; // 晚上天气
    public string daytemp; // 白天温度（℃）
    public string nighttemp; // 晚上温度（℃）
    public string daywind; // 白天风向
    public string nightwind; // 晚上风向
    public string daypower; // 白天风力
    public string nightpower; // 晚上风力
}

public class AmapWeatherQuery : MonoBehaviour
{
    public GameObject textWidget;
    // 1. 配置API参数（需替换为你的实际信息）
    [Header("API配置")]
    public string apiKey = "YOUR_AMAP_WEB_SERVICE_KEY"; // 替换为你的Key
    public string cityAdcode = "110101"; // 替换为目标城市adcode（如北京东城区）
    public string weatherType = "base"; // base=实况，all=预报
    public string responseFormat = "JSON"; // 固定为JSON
    
    void Start()
    {
        StartWeatherQuery();
    }
    
    // 2. 发起天气查询请求（需在协程中执行，因HTTP请求是异步的）
    public void StartWeatherQuery()
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("请先配置API Key！");
            return;
        }
        if (string.IsNullOrEmpty(cityAdcode))
        {
            Debug.LogError("请先配置城市Adcode！");
            return;
        }

        // 拼接请求URL（按文档格式：https://restapi.amap.com/v3/weather/weatherInfo?parameters）
        string requestUrl = $"https://restapi.amap.com/v3/weather/weatherInfo?key={apiKey}&city={cityAdcode}&extensions={weatherType}&output={responseFormat}&lang=en";
        
        // 启动协程发送请求
        StartCoroutine(SendWeatherRequest(requestUrl));
    }

    // 3. 协程：发送HTTP GET请求并处理响应
    private IEnumerator SendWeatherRequest(string url)
    {
        Debug.Log($"正在请求天气数据，URL：{url}");

        // 创建GET请求
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // 发送请求并等待响应
            yield return webRequest.SendWebRequest();

            // 4. 处理响应结果
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // 请求成功：解析JSON数据
                string responseJson = webRequest.downloadHandler.text;
                Debug.Log($"请求成功，返回数据：{responseJson}");

                // 解析JSON到实体类（使用Unity内置的JsonUtility）
                WeatherResponse weatherData = JsonUtility.FromJson<WeatherResponse>(responseJson);

                // 验证API返回状态（文档中status=1为成功）
                if (weatherData.status == "1" && weatherData.infocode == "10000")
                {
                    // 显示解析后的天气信息（根据weatherType区分实况/预报）
                    if (weatherType == "base" && weatherData.lives != null && weatherData.lives.Length > 0)
                    {
                        ShowLiveWeather(weatherData.lives[0]); // 显示实况天气
                    }
                    else if (weatherType == "all" && weatherData.forecast != null && weatherData.forecast.Length > 0)
                    {
                        ShowForecastWeather(weatherData.forecast[0]); // 显示预报天气
                    }
                }
                else
                {
                    Debug.LogError($"API返回失败：info={weatherData.info}，infocode={weatherData.infocode}");
                }
            }
            else
            {
                // 请求失败：打印错误信息
                Debug.LogError($"请求失败！错误信息：{webRequest.error}，状态码：{webRequest.responseCode}");
            }
        }
    }

    // 5. 显示实况天气信息（可替换为UI文本显示）
    private void ShowLiveWeather(LiveWeather live)
    {
        Debug.Log("================== 实况天气 ==================");
        Debug.Log($"省份：{live.province}");
        Debug.Log($"城市：{live.city}");
        Debug.Log($"天气：{live.weather}");
        
        textWidget.GetComponent<TMP_Text>().SetText(live.weather);
            
        Debug.Log($"实时气温：{live.temperature}℃");
        Debug.Log($"风向：{live.winddirection}");
        Debug.Log($"风力：{live.windpower}级");
        Debug.Log($"湿度：{live.humidity}%");
        Debug.Log($"数据更新时间：{live.reporttime}");
        Debug.Log("==============================================");
    }

    // 6. 显示预报天气信息（可替换为UI文本显示）
    private void ShowForecastWeather(ForecastWeather forecast)
    {
        Debug.Log("================== 预报天气 ==================");
        Debug.Log($"省份：{forecast.province}");
        Debug.Log($"城市：{forecast.city}");
        Debug.Log($"预报发布时间：{forecast.reporttime}");
        
        foreach (var cast in forecast.casts)
        {
            Debug.Log($"日期：{cast.date}（周{cast.week}）");
            Debug.Log($"白天：{cast.dayweather}，{cast.daytemp}℃，{cast.daywind}{cast.daypower}级");
            Debug.Log($"晚上：{cast.nightweather}，{cast.nighttemp}℃，{cast.nightwind}{cast.nightpower}级");
            Debug.Log("----------------------------------------------");
        }
        Debug.Log("==============================================");
    }

    
    /*private void OnGUI()
    {
        if (GUILayout.Button("查询天气", GUILayout.Width(200), GUILayout.Height(50)))
        {
            StartWeatherQuery();
        }
    }*/
}