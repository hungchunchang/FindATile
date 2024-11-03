using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(XROrigin))]
public class OcclusionDetector : MonoBehaviour
{
    private AROcclusionManager occlusionManager;
    private ARSession arSession;
    private bool isInitialized = false;
    
    [SerializeField]
    private float debugInterval = 1f;
    private float debugTimer = 0f;

    [SerializeField]
    private bool enableDebugLogs = true;

    private void Awake()
    {
        // 檢查平台支援
        CheckPlatformSupport();
    }

    private void Start()
    {
        InitializeComponents();
        ConfigureOcclusion();
    }

    private void CheckPlatformSupport()
    {
        #if !UNITY_IOS
            Debug.LogWarning("當前平台可能不支援人體遮擋功能。此功能主要支援 iOS 設備。");
        #endif
    }

    private void InitializeComponents()
    {
        try
        {
            // 獲取必要組件
            occlusionManager = FindObjectOfType<AROcclusionManager>();
            arSession = FindObjectOfType<ARSession>();

            if (occlusionManager == null)
            {
                Debug.LogError("無法找到 AROcclusionManager! 請確保場景中有此組件。");
                return;
            }

            if (arSession == null)
            {
                Debug.LogError("無法找到 ARSession! 請確保場景中有此組件。");
                return;
            }

            isInitialized = true;
            LogDebug("組件初始化成功");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"初始化組件時發生錯誤: {e.Message}");
        }
    }

    private void ConfigureOcclusion()
    {
        if (!isInitialized) return;

        try
        {
            // 暫時停止 AR Session
            arSession.enabled = false;

            // 配置遮擋設置
            occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
            occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;

            // 重新啟動 AR Session
            arSession.enabled = true;

            LogDebug($"遮擋配置完成\n" +
                    $"Stencil Mode: {occlusionManager.requestedHumanStencilMode}\n" +
                    $"Depth Mode: {occlusionManager.requestedHumanDepthMode}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"配置遮擋時發生錯誤: {e.Message}");
        }
    }

    private void Update()
    {
        if (!isInitialized || occlusionManager == null) return;

        UpdateDebugInfo();
        CheckOcclusion();
    }

    private void UpdateDebugInfo()
    {
        if (!enableDebugLogs) return;

        debugTimer += Time.deltaTime;
        if (debugTimer > debugInterval)
        {
            debugTimer = 0f;
            
            var stencilTexture = occlusionManager.humanStencilTexture;
            var depthTexture = occlusionManager.humanDepthTexture;
            
            LogDebug($"Occlusion Status:\n" +
                     $"Stencil Texture: {(stencilTexture != null ? "可用" : "不可用")}\n" +
                     $"Depth Texture: {(depthTexture != null ? "可用" : "不可用")}\n" +
                     $"當前 Stencil Mode: {occlusionManager.currentHumanStencilMode}\n" +
                     $"當前 Depth Mode: {occlusionManager.currentHumanDepthMode}");

            if (stencilTexture != null)
            {
                LogDebug($"Stencil 材質資訊:\n" +
                         $"尺寸: {stencilTexture.width}x{stencilTexture.height}\n" +
                         $"格式: {stencilTexture.format}");
            }
        }
    }

    private void CheckOcclusion()
    {
        var stencilTexture = occlusionManager.humanStencilTexture;
        if (stencilTexture == null) return;

        try
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            if (!IsPointVisible(screenPoint)) return;

            Color pixel = SampleStencilTexture(stencilTexture, screenPoint);
            if (pixel.r > 0.5f)
            {
                LogDebug($"檢測到遮擋! 遮擋值: {pixel.r:F2}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"檢查遮擋時發生錯誤: {e.Message}");
        }
    }

    private bool IsPointVisible(Vector3 screenPoint)
    {
        return screenPoint.x >= 0 && screenPoint.x < Screen.width &&
               screenPoint.y >= 0 && screenPoint.y < Screen.height &&
               screenPoint.z >= 0;
    }

    private Color SampleStencilTexture(Texture2D stencilTexture, Vector3 screenPoint)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            stencilTexture.width, stencilTexture.height, 0, RenderTextureFormat.R8);
        Graphics.Blit(stencilTexture, renderTexture);
        RenderTexture.active = renderTexture;

        var tempTexture = new Texture2D(1, 1, TextureFormat.R8, false);
        try
        {
            int x = Mathf.RoundToInt((screenPoint.x / Screen.width) * stencilTexture.width);
            int y = Mathf.RoundToInt((screenPoint.y / Screen.height) * stencilTexture.height);
            
            tempTexture.ReadPixels(new Rect(x, y, 1, 1), 0, 0);
            tempTexture.Apply();
            return tempTexture.GetPixel(0, 0);
        }
        finally
        {
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(renderTexture);
            Destroy(tempTexture);
        }
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[OcclusionDetector] {message}");
        }
    }

    private void OnDisable()
    {
        if (arSession != null)
        {
            arSession.enabled = false;
        }
    }

    private void OnDestroy()
    {
        // 清理資源
        if (arSession != null)
        {
            arSession.enabled = false;
        }
    }
}