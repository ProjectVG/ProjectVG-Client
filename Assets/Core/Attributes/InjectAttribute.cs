using UnityEngine;

namespace ProjectVG.Core.Attributes
{
    /// <summary>
    /// 의존성 주입을 위한 커스텀 어트리뷰트
    /// </summary>
    public class InjectAttribute : PropertyAttribute
    {
        public string DependencyName { get; }
        
        public InjectAttribute(string dependencyName = "")
        {
            DependencyName = dependencyName;
        }
    }
} 