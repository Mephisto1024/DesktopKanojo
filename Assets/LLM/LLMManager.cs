using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class DeepSeekMessage
{
    public string role;
    public string content;
}

[System.Serializable]
public class DeepSeekRequest
{
    public string model;
    public List<DeepSeekMessage> messages;
    public float temperature;
    public int max_tokens;
}

[System.Serializable]
public class DeepSeekResponse
{
    public string id;
    public string obj;
    public long created;
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public int index;
    public DeepSeekMessage message;
    public string finish_reason;
}

public class LLMManager : MonoBehaviour
{
    [Header("API设置")]
    [Tooltip("DeepSeek API密钥")]
    public string apiKey = "";
    
    [Header("模型参数")]
    [Tooltip("系统提示词，定义角色行为")]
    [TextArea(3, 10)]
    public string systemPrompt = "你是一个友好的虚拟桌面伴侣，请用简洁、温暖的语气回答用户的问题。";
    
    [Tooltip("使用的模型")]
    public string model = "deepseek-chat";
    
    [Tooltip("温度参数(0-2)，越高越随机")]
    [Range(0f, 2f)]
    public float temperature = 0.7f;
    
    [Tooltip("最大生成token数")]
    public int maxTokens = 1000;

    private const string API_URL = "https://api.deepseek.com/v1/chat/completions";
    private List<DeepSeekMessage> conversationHistory = new List<DeepSeekMessage>();

    void Start()
    {
        // 初始化对话历史，添加系统提示词
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            conversationHistory.Add(new DeepSeekMessage
            {
                role = "system",
                content = systemPrompt
            });
        }
    }

    /// <summary>
    /// 发送消息到DeepSeek API
    /// </summary>
    /// <param name="userMessage">用户消息</param>
    /// <param name="onSuccess">成功回调</param>
    /// <param name="onError">错误回调</param>
    public void SendMessage(string userMessage, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(SendMessageCoroutine(userMessage, onSuccess, onError));
    }

    private IEnumerator SendMessageCoroutine(string userMessage, Action<string> onSuccess, Action<string> onError)
    {
        // 添加用户消息到历史
        conversationHistory.Add(new DeepSeekMessage
        {
            role = "user",
            content = userMessage
        });

        // 构建请求
        DeepSeekRequest request = new DeepSeekRequest
        {
            model = model,
            messages = conversationHistory,
            temperature = temperature,
            max_tokens = maxTokens
        };

        string jsonData = JsonUtility.ToJson(request);

        // 创建POST请求
        using (UnityWebRequest webRequest = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            
            // 设置请求头
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);

            // 发送请求
            yield return webRequest.SendWebRequest();

            // 处理响应
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    DeepSeekResponse response = JsonUtility.FromJson<DeepSeekResponse>(webRequest.downloadHandler.text);
                    
                    if (response.choices != null && response.choices.Length > 0)
                    {
                        string assistantMessage = response.choices[0].message.content;
                        
                        // 添加助手回复到历史
                        conversationHistory.Add(new DeepSeekMessage
                        {
                            role = "assistant",
                            content = assistantMessage
                        });

                        onSuccess?.Invoke(assistantMessage);
                    }
                    else
                    {
                        onError?.Invoke("响应中没有消息内容");
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke("解析响应失败: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke($"请求失败: {webRequest.error}\n{webRequest.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// 清空对话历史
    /// </summary>
    public void ClearHistory()
    {
        conversationHistory.Clear();
        
        // 重新添加系统提示词
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            conversationHistory.Add(new DeepSeekMessage
            {
                role = "system",
                content = systemPrompt
            });
        }
    }

    /// <summary>
    /// 获取当前对话历史消息数量
    /// </summary>
    public int GetHistoryCount()
    {
        return conversationHistory.Count;
    }
}
