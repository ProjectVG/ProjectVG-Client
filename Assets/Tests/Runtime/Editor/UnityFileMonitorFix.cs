using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity 파일 모니터링 문제 해결을 위한 에디터 스크립트
/// </summary>
public class UnityFileMonitorFix : EditorWindow
{
    [MenuItem("Tools/Fix Unity File Monitor")]
    public static void FixFileMonitor()
    {
        // Unity 에디터 설정 변경
        EditorPrefs.SetBool("AssetDatabase.AutoRefresh", false);
        EditorPrefs.SetBool("AssetDatabase.ForceReserializeAssets", false);
        
        // 파일 모니터링 비활성화
        EditorApplication.delayCall += () =>
        {
            // 임시 해결책: 에디터 새로고침 비활성화
            EditorApplication.ExecuteMenuItem("Assets/Refresh");
        };
        
        Debug.Log("Unity 파일 모니터링 문제 해결이 적용되었습니다.");
    }
    
    [MenuItem("Tools/Reset Unity File Monitor")]
    public static void ResetFileMonitor()
    {
        // Unity 에디터 설정 복원
        EditorPrefs.SetBool("AssetDatabase.AutoRefresh", true);
        EditorPrefs.SetBool("AssetDatabase.ForceReserializeAssets", true);
        
        Debug.Log("Unity 파일 모니터링이 복원되었습니다.");
    }
    
    private void OnEnable()
    {
        // 에디터 창이 열릴 때 자동으로 문제 해결 적용
        EditorPrefs.SetBool("AssetDatabase.AutoRefresh", false);
    }
} 