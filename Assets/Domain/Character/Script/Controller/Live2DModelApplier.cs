using UnityEngine;
using ProjectVG.Domain.Character.Live2D.Model;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// Live2D 모델에 설정을 적용하는 구현체
    /// </summary>
    public class Live2DModelApplier : MonoBehaviour, ILive2DModelApplier
    {
        [Header("Components")]
        [SerializeField] private EmotionController _emotionController;
        [SerializeField] private ActionController _actionController;
        
        public void Apply(GameObject activeModel, Live2DModelConfig characterConfig)
        {
            if (activeModel == null || characterConfig == null)
            {
                Debug.LogWarning("[Live2DModelApplier] 모델 또는 설정이 null입니다.");
                return;
            }
            
            try
            {
                // 컴포넌트들 찾기
                FindComponents(activeModel);
                
                // 기본 설정 적용
                ApplyBasicSettings(activeModel, characterConfig);
                
                // 컨트롤러들 초기화
                InitializeControllers();
                
                Debug.Log($"[Live2DModelApplier] 모델 '{activeModel.name}'에 설정 적용 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Live2DModelApplier] 설정 적용 실패: {ex.Message}");
            }
        }
        
        private void FindComponents(GameObject activeModel)
        {
            // 감정 컨트롤러 찾기
            if (_emotionController == null)
            {
                _emotionController = activeModel.GetComponent<EmotionController>();
            }
            
            // 액션 컨트롤러 찾기
            if (_actionController == null)
            {
                _actionController = activeModel.GetComponent<ActionController>();
            }
        }
        
        private void ApplyBasicSettings(GameObject activeModel, Live2DModelConfig characterConfig)
        {
            // 기본 감정 설정
            if (_emotionController != null)
            {
                _emotionController.Initialize();
                _emotionController.SetEmotion("neutral", 1.0f, 0);
            }
            
            // 기본 액션 설정
            if (_actionController != null)
            {
                _actionController.Initialize();
            }
        }
        
        private void InitializeControllers()
        {
            // 컨트롤러들이 제대로 초기화되었는지 확인
            if (_emotionController == null)
            {
                Debug.LogWarning("[Live2DModelApplier] EmotionController를 찾을 수 없습니다.");
            }
            
            if (_actionController == null)
            {
                Debug.LogWarning("[Live2DModelApplier] ActionController를 찾을 수 없습니다.");
            }
        }
    }
}


