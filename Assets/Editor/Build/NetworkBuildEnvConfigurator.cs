#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ProjectVG.Infrastructure.Network.Configs;

namespace ProjectVG.Editor.Build
{
    /// <summary>
    /// 빌드 전에 NetworkConfig 환경 값을 Development/Production으로 자동 설정
    /// </summary>
    public class NetworkBuildEnvConfigurator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            var isDevelopment = EditorUserBuildSettings.development;
            var targetGroup = report.summary.platformGroup;
            
            EnsureNetworkConfigAssetExists();
            var config = Resources.Load<NetworkConfig>("NetworkConfig");
            if (config == null)
            {
                Debug.LogWarning("NetworkConfig.asset을 찾지 못했습니다. 기본 설정이 사용됩니다.");
                return;
            }
            
            var desiredEnv = isDevelopment ? NetworkConfig.EnvironmentType.Development : NetworkConfig.EnvironmentType.Production;
            SetEnvironmentOnAsset(config, desiredEnv);
            
            Debug.Log($"[Build] NetworkConfig 환경 설정: {(isDevelopment ? "Development" : "Production")} ({targetGroup})");
        }
        
        private static void EnsureNetworkConfigAssetExists()
        {
            var asset = Resources.Load<NetworkConfig>("NetworkConfig");
            if (asset != null)
            {
                return;
            }
            
            var instance = ScriptableObject.CreateInstance<NetworkConfig>();
            const string resourcesDir = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            var path = "Assets/Resources/NetworkConfig.asset";
            AssetDatabase.CreateAsset(instance, path);
            AssetDatabase.SaveAssets();
        }
        
        private static void SetEnvironmentOnAsset(NetworkConfig config, NetworkConfig.EnvironmentType env)
        {
            var so = new SerializedObject(config);
            var envProp = so.FindProperty("environment");
            if (envProp != null)
            {
                envProp.enumValueIndex = (int)env;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
