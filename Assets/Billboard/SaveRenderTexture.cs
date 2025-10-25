using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteInEditMode]
public class SaveRenderTexture : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public RenderTexture sourceRT; // 拖入一个 RenderTexture
    public string savePath = "Assets/SavedTexture.asset"; // 保存路径

#if UNITY_EDITOR
    [ContextMenu("💾 Save RenderTexture As Asset")]
    public void SaveAsAsset()
    {
        if (sourceRT == null)
        {
            Debug.LogError(" 请先指定一个 RenderTexture！");
            return;
        }

        // 1️⃣ 激活RT并读取像素
        RenderTexture.active = sourceRT;
        Texture2D tex = new Texture2D(sourceRT.width, sourceRT.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, sourceRT.width, sourceRT.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // 2️⃣ 在Assets文件夹创建Asset
        AssetDatabase.CreateAsset(tex, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ RenderTexture 已保存为 Asset: {savePath}");
    }
#endif
 
}
