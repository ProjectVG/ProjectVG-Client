using UnityEngine;
using ProjectVG.Infrastructure.Network.Configs;

namespace ProjectVG.Infrastructure.Config
{
    /**
     * 런타임 환경 설정 ScriptableObject
     */
    public class AppEnvironmentConfig : ScriptableObject
    {
        [Header("Override")]
        [SerializeField] private bool overrideEnabled = false;
        [SerializeField] private NetworkConfig.EnvironmentType overrideEnvironment = NetworkConfig.EnvironmentType.Development;
        
        [Header("Per-Platform Default Environments")]
        [SerializeField] private NetworkConfig.EnvironmentType editorEnvironment = NetworkConfig.EnvironmentType.Development;
        [SerializeField] private NetworkConfig.EnvironmentType androidEnvironment = NetworkConfig.EnvironmentType.Production;
        [SerializeField] private NetworkConfig.EnvironmentType iosEnvironment = NetworkConfig.EnvironmentType.Production;
        [SerializeField] private NetworkConfig.EnvironmentType standaloneEnvironment = NetworkConfig.EnvironmentType.Development;
        [SerializeField] private NetworkConfig.EnvironmentType webglEnvironment = NetworkConfig.EnvironmentType.Development;
        
        [Header("Build Flags Mapping")]
        [SerializeField] private bool mapDevelopmentBuildToDevelopment = true;
        
        private static AppEnvironmentConfig _instance;
        public static AppEnvironmentConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AppEnvironmentConfig>("AppEnvironmentConfig");
                    if (_instance == null)
                    {
                        _instance = CreateDefaultAsset();
                    }
                }
                return _instance;
            }
        }
        
        /**
         * 현재 환경 결정
         */
        public NetworkConfig.EnvironmentType GetCurrentEnvironment()
        {
            if (overrideEnabled)
            {
                return overrideEnvironment;
            }
            
#if UNITY_EDITOR
            return editorEnvironment;
#else
            if (mapDevelopmentBuildToDevelopment && Debug.isDebugBuild)
            {
                return NetworkConfig.EnvironmentType.Development;
            }
            
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return androidEnvironment;
                case RuntimePlatform.IPhonePlayer:
                    return iosEnvironment;
                case RuntimePlatform.WebGLPlayer:
                    return webglEnvironment;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.LinuxPlayer:
                    return standaloneEnvironment;
                default:
                    return editorEnvironment;
            }
#endif
        }
        
        private static AppEnvironmentConfig CreateDefaultAsset()
        {
            var asset = CreateInstance<AppEnvironmentConfig>();
#if UNITY_EDITOR
            const string resourcesDir = "Assets/Resources";
            if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesDir))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }
            UnityEditor.AssetDatabase.CreateAsset(asset, "Assets/Resources/AppEnvironmentConfig.asset");
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            return asset;
        }
    }
}
