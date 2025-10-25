using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// 数据模型类
[Serializable]
public class BangumiCalendar
{
    public Weekday weekday;
    public List<CalendarItem> items;
}

[Serializable]
public class Weekday
{
    public string en;
    public string cn;
    public string ja;
    public int id;
}

[Serializable]
public class CalendarItem
{
    public int id;
    public string url;
    public int type;
    public string name;
    public string name_cn;
    public string summary;
    public string air_date;
    public int air_weekday;
    public CalendarImages images;
    public Collection collection;
    public float rating;
    public int rank;
}

[Serializable]
public class CalendarImages
{
    public string large;
    public string common;
    public string medium;
    public string small;
    public string grid;
}

[Serializable]
public class Collection
{
    public int wish;
    public int collect;
    public int doing;
    public int on_hold;
    public int dropped;
}

public class BangumiCalendarAPI : MonoBehaviour
{
    // Bangumi API 配置
    private const string API_BASE_URL = "https://api.bgm.tv";
    private const string CALENDAR_ENDPOINT = "/calendar";
    
    // 你的应用名称和版本（必须设置）
    private const string USER_AGENT = "DesktopKanojo/1.0.0 (https://github.com/Mephisto1024/DesktopKanojo)";

    void Start()
    {
        // 启动时获取日历数据
        StartCoroutine(GetCalendar());
    }

    /// <summary>
    /// 获取每日放送日历
    /// </summary>
    public IEnumerator GetCalendar()
    {
        string url = API_BASE_URL + CALENDAR_ENDPOINT;
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // 设置 User Agent（这是必须的！）
            request.SetRequestHeader("User-Agent", USER_AGENT);
            
            // 发送请求
            yield return request.SendWebRequest();
            
            // 检查错误
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 解析 JSON 数据
                string jsonResponse = request.downloadHandler.text;
                ProcessCalendarData(jsonResponse);
            }
            else
            {
                Debug.LogError($"请求失败: {request.error}");
                Debug.LogError($"状态码: {request.responseCode}");
            }
        }
    }

    /// <summary>
    /// 处理日历数据
    /// </summary>
    private void ProcessCalendarData(string jsonData)
    {
        try
        {
            // 由于返回的是数组，需要手动处理
            BangumiCalendar[] calendars = JsonHelper.FromJson<BangumiCalendar>(jsonData);
            
            Debug.Log($"获取到 {calendars.Length} 天的放送数据");
            
            // 遍历每一天的数据
            foreach (var calendar in calendars)
            {
                Debug.Log($"\n===== {calendar.weekday.cn} ({calendar.weekday.en}) =====");
                Debug.Log($"今日共有 {calendar.items.Count} 部作品");
                
                // 遍历每部作品
                foreach (var item in calendar.items)
                {
                    Debug.Log($"\n作品: {item.name_cn} ({item.name})");
                    Debug.Log($"ID: {item.id}");
                    Debug.Log($"放送日期: {item.air_date}");
                    Debug.Log($"在看人数: {item.collection.doing}");
                    Debug.Log($"评分: {item.rating}");
                    Debug.Log($"排名: {item.rank}");
                    Debug.Log($"封面图: {item.images.large}");
                    Debug.Log($"简介: {item.summary}");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"解析 JSON 失败: {e.Message}");
        }
    }

    /// <summary>
    /// 根据星期几筛选数据
    /// </summary>
    public IEnumerator GetCalendarByWeekday(int weekdayId)
    {
        yield return GetCalendar();
        // 在 ProcessCalendarData 中进行筛选
    }

    /// <summary>
    /// 下载封面图片
    /// </summary>
    public IEnumerator DownloadCoverImage(string imageUrl, Action<Texture2D> callback)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            request.SetRequestHeader("User-Agent", USER_AGENT);
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                callback?.Invoke(texture);
            }
            else
            {
                Debug.LogError($"下载图片失败: {request.error}");
                callback?.Invoke(null);
            }
        }
    }
}

// JSON 数组辅助类
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}