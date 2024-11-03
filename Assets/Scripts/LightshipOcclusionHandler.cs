using UnityEngine;
using Niantic.Lightship.AR;
using UnityEngine.XR.ARFoundation;
using Niantic.Lightship.AR.Occlusion;
using Niantic.Lightship.AR.Common;

public class LightshipOcclusionHandler : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager cameraManager;
    [SerializeField]
    private AROcclusionManager occlusionManager;
    [SerializeField]
    private LightshipOcclusionExtension occlusionExtension;

    [Header("檢測設定")]
    [SerializeField] 
    private float checkInterval = 0.1f;
    [SerializeField]
    private float depthThreshold = 0.1f; // 深度差異閾值
    [SerializeField]
    private int samplePoints = 9; // 檢測點數量
    [SerializeField]
    private bool enableDebugLogs = true;

    private float lastCheckTime;
    private bool isCurrentlyOccluded = false;
    private bool previousOcclusionState = false;
    private Camera arCamera;

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        try
        {
            // 如果沒有手動指定，嘗試自動獲取組件
            if (cameraManager == null)
                cameraManager = FindObjectOfType<ARCameraManager>();
            if (occlusionManager == null)
                occlusionManager = FindObjectOfType<AROcclusionManager>();
            if (occlusionExtension == null)
                occlusionExtension = FindObjectOfType<LightshipOcclusionExtension>();
            
            arCamera = cameraManager?.GetComponent<Camera>();

            if (occlusionExtension == null || arCamera == null)
            {
                Debug.LogError("[LightshipOcclusion] 缺少必要組件!");
                return;
            }

            // 配置遮擋設定
            occlusionExtension.OverrideOcclusionManagerSettings = true;
            occlusionExtension.Mode = LightshipOcclusionExtension.OptimalOcclusionDistanceMode.ClosestOccluder;
            occlusionExtension.PreferSmoothEdges = true;

            LogDebug("[LightshipOcclusion] 組件初始化成功");

            // 註冊幀更新事件
            if (cameraManager != null)
            {
                cameraManager.frameReceived += OnFrameReceived;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"初始化組件時發生錯誤: {e.Message}");
        }
    }

    private void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!enabled || Time.time - lastCheckTime < checkInterval) return;
        lastCheckTime = Time.time;
        
        CheckOcclusion();
    }

    private void CheckOcclusion()
    {
        if (!enabled || occlusionManager == null || occlusionManager.environmentDepthTexture == null) 
        {
            LogDebug("[LightshipOcclusion] 深度紋理未準備好");
            return;
        }

        // 取得物體在螢幕上的位置
        Vector3 centerPoint = arCamera.WorldToScreenPoint(transform.position);
        if (centerPoint.z < 0)
        {
            LogDebug("[LightshipOcclusion] 物體在相機後方");
            return; // 物體在相機後方
        }

        // 使用多點採樣來提高準確性
        bool newOcclusionState = CheckMultiplePoints(centerPoint);

        // 如果遮擋狀態改變
        if (newOcclusionState != previousOcclusionState)
        {
            previousOcclusionState = newOcclusionState;
            isCurrentlyOccluded = newOcclusionState;

            if (isCurrentlyOccluded)
            {
                OnBecameOccluded();
            }
            else
            {
                OnBecameVisible();
            }
        }
    }

    private bool CheckMultiplePoints(Vector3 centerPoint)
    {
        float radius = 10f; // 檢測範圍半徑
        int occludedPoints = 0;
        Vector3 objectPosition = transform.position;
        float objectDepth = Vector3.Distance(arCamera.transform.position, objectPosition);

        // 檢查中心點
        if (CheckSinglePoint(centerPoint, objectDepth))
            occludedPoints++;

        // 檢查周圍點
        for (int i = 0; i < samplePoints - 1; i++)
        {
            float angle = i * (360f / (samplePoints - 1));
            float x = centerPoint.x + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
            float y = centerPoint.y + radius * Mathf.Sin(angle * Mathf.Deg2Rad);

            if (CheckSinglePoint(new Vector3(x, y, centerPoint.z), objectDepth))
                occludedPoints++;
        }

        // 如果超過一半的點被遮擋，就認為物體被遮擋
        return occludedPoints > samplePoints / 2;
    }

    private bool CheckSinglePoint(Vector3 screenPoint, float objectDepth)
    {
        // 確保點在螢幕範圍內
        if (screenPoint.x < 0 || screenPoint.x > Screen.width ||
            screenPoint.y < 0 || screenPoint.y > Screen.height)
        {
            return false;
        }

        try
        {
            if (occlusionExtension.TryGetDepth((int)screenPoint.x, (int)screenPoint.y, out float depth))
            {
                // 考慮深度閾值
                bool isOccluded = depth < (objectDepth - depthThreshold);
                LogDebug($"點 ({screenPoint.x:F0},{screenPoint.y:F0}) 深度檢測: " +
                        $"深度={depth:F2}, 物體深度={objectDepth:F2}, 被遮擋={isOccluded}");
                return isOccluded;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"深度檢測錯誤: {e.Message}");
        }

        return false;
    }

    private void OnBecameOccluded()
    {
        LogDebug($"[LightshipOcclusion] 物體被遮擋了!");
        // 觸發震動
        #if UNITY_IOS
            Handheld.Vibrate();
        #endif
    }

    private void OnBecameVisible()
    {
        LogDebug($"[LightshipOcclusion] 物體變得可見了!");
    }

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }

    private void OnDisable()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived -= OnFrameReceived;
        }
    }

    private void OnDestroy()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived -= OnFrameReceived;
        }
    }
}