using UnityEngine;
using UnityEditor;
using System.IO;

public class AlphaChannelMerger : EditorWindow
{
    private Texture2D baseTexture;
    private Texture2D alphaTexture;
    private string savePath = "Assets/MergedTexture.png";

    private enum AlphaSourceChannel { R, G, B, A, Gray }
    private AlphaSourceChannel channel = AlphaSourceChannel.Gray;

    private bool convertToLinear = false; // 是否进行伽马→线性转换
    private bool convertToGamma = false;  // 是否进行线性→伽马转换

    [MenuItem("Tools/贴图工具/合并 Alpha 通道")]
    public static void ShowWindow()
    {
        GetWindow<AlphaChannelMerger>("合并 Alpha 通道");
    }

    void OnGUI()
    {
        GUILayout.Label("将第二张贴图的某个通道写入第一张贴图的 Alpha 通道", EditorStyles.boldLabel);

        baseTexture = (Texture2D)EditorGUILayout.ObjectField("主贴图 (RGB)", baseTexture, typeof(Texture2D), false);
        alphaTexture = (Texture2D)EditorGUILayout.ObjectField("Alpha 来源贴图", alphaTexture, typeof(Texture2D), false);

        channel = (AlphaSourceChannel)EditorGUILayout.EnumPopup("Alpha 来源通道", channel);

        EditorGUILayout.Space();
        GUILayout.Label("色彩空间选项", EditorStyles.boldLabel);
        convertToLinear = EditorGUILayout.Toggle("伽马 → 线性 (sRGB)", convertToLinear);
        convertToGamma = EditorGUILayout.Toggle("线性 → 伽马 (显示前)", convertToGamma);

        EditorGUILayout.Space();
        savePath = EditorGUILayout.TextField("保存路径", savePath);

        EditorGUILayout.Space();
        if (GUILayout.Button("▶ 合并并保存", GUILayout.Height(30)))
        {
            if (baseTexture == null || alphaTexture == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择两张贴图！", "好的");
                return;
            }

            MergeAlphaChannel(baseTexture, alphaTexture, savePath);
        }
    }

    private void MergeAlphaChannel(Texture2D baseTex, Texture2D alphaTex, string outputPath)
    {
        // 确保贴图可读
        Texture2D baseReadable = GetReadableTexture(baseTex);
        Texture2D alphaReadable = GetReadableTexture(alphaTex);

        int width = baseReadable.width;
        int height = baseReadable.height;

        if (alphaReadable.width != width || alphaReadable.height != height)
        {
            EditorUtility.DisplayDialog("警告", "两张贴图分辨率不同，已自动缩放 Alpha 贴图。", "好的");
            alphaReadable = ResizeTexture(alphaReadable, width, height);
        }

        // 合并
        Color[] basePixels = baseReadable.GetPixels();
        Color[] alphaPixels = alphaReadable.GetPixels();

        for (int i = 0; i < basePixels.Length; i++)
        {
            float value = GetChannel(alphaPixels[i]);
            if (convertToLinear) value = Mathf.LinearToGammaSpace(value);
            if (convertToGamma) value = Mathf.GammaToLinearSpace(value);
            basePixels[i].a = value;
        }

        // 输出结果
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels(basePixels);
        result.Apply();

        byte[] bytes = result.EncodeToPNG();
        File.WriteAllBytes(outputPath, bytes);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成", "已保存合并后的贴图到：\n" + outputPath, "好的");
    }

    // 从选定通道取值
    private float GetChannel(Color c)
    {
        switch (channel)
        {
            case AlphaSourceChannel.R: return c.r;
            case AlphaSourceChannel.G: return c.g;
            case AlphaSourceChannel.B: return c.b;
            case AlphaSourceChannel.A: return c.a;
            default: return c.grayscale;
        }
    }

    // 确保贴图可读
    private Texture2D GetReadableTexture(Texture2D source)
    {
        // 获取资源路径
        string path = AssetDatabase.GetAssetPath(source);

        Texture2D readableTex = null;

        // 如果是导入到项目的贴图资源（.png/.jpg/.tga等）
        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets"))
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            if (assetImporter is TextureImporter importer)
            {
                // 确保可读
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                // 创建副本
                readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                Graphics.CopyTexture(source, readableTex);
                return readableTex;
            }
        }

        // 否则走通用 GPU 拷贝路径
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return readableTex;
    }

    
    private Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
