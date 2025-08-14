using UnityEngine;
using System;
using System.Collections;
using Live2D.Cubism.Framework.Expression;

namespace ProjectVG.Domain.Character.Service
{
    /// <summary>
    /// 감정 → Expression 맵핑과 블렌딩을 구현하는 컨트롤러
    /// </summary>
    public class EmotionController : MonoBehaviour, IEmotionController
    {
        [Header("Components")]
        [SerializeField] private CubismExpressionController _expressionController;
        
        [Header("Emotion Mapping")]
        [SerializeField] private EmotionExpressionMapping[] _emotionMappings;
        
        [Header("Blending Settings")]
        [SerializeField] private float _defaultBlendTime = 0.5f;
        [SerializeField] private float _voiceBlendTime = 1.0f;
        
        private string _currentEmotion = "neutral";
        private string _pendingEmotion;
        private float _currentIntensity = 1.0f;
        private Coroutine _emotionCoroutine;
        private bool _isVoicePlaying = false;
        
        #region IEmotionController Implementation
        
        public void Initialize()
        {
            if (_expressionController == null)
            {
                _expressionController = GetComponent<CubismExpressionController>();
            }
            
            if (_expressionController == null)
            {
                Debug.LogError("[EmotionController] CubismExpressionController를 찾을 수 없습니다.");
                return;
            }
            
            // 기본 감정 설정
            SetEmotion("neutral", 1.0f, 0);
            
            Debug.Log("[EmotionController] 초기화 완료");
        }
        
        public void SetEmotion(string emotion, float intensity, int durationMs)
        {
            if (string.IsNullOrEmpty(emotion))
            {
                Debug.LogWarning("[EmotionController] 감정이 null입니다.");
                return;
            }
            
            // 음성 재생 중이고 과격한 감정 전환이라면 대기열로 보관
            if (_isVoicePlaying && IsIntenseEmotionChange(_currentEmotion, emotion))
            {
                _pendingEmotion = emotion;
                Debug.Log($"[EmotionController] 음성 재생 중 감정 대기: {emotion}");
                return;
            }
            
            StartEmotionTransition(emotion, intensity, durationMs);
        }
        
        public void ClearEmotion()
        {
            if (_emotionCoroutine != null)
            {
                StopCoroutine(_emotionCoroutine);
                _emotionCoroutine = null;
            }
            
            SetEmotion("neutral", 1.0f, 0);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 음성 재생 상태를 설정한다.
        /// </summary>
        public void SetVoicePlaying(bool isPlaying)
        {
            _isVoicePlaying = isPlaying;
            
            // 음성 재생 완료 시 대기 중인 감정 적용
            if (!isPlaying && !string.IsNullOrEmpty(_pendingEmotion))
            {
                var pendingEmotion = _pendingEmotion;
                _pendingEmotion = null;
                SetEmotion(pendingEmotion, _currentIntensity, 2000);
            }
        }
        
        /// <summary>
        /// 현재 감정을 반환한다.
        /// </summary>
        public string GetCurrentEmotion()
        {
            return _currentEmotion;
        }
        
        #endregion
        
        #region Private Methods
        
        private void StartEmotionTransition(string emotion, float intensity, int durationMs)
        {
            if (_emotionCoroutine != null)
            {
                StopCoroutine(_emotionCoroutine);
            }
            
            _emotionCoroutine = StartCoroutine(EmotionTransitionCoroutine(emotion, intensity, durationMs));
        }
        
        private IEnumerator EmotionTransitionCoroutine(string emotion, float intensity, int durationMs)
        {
            var expressionKey = GetExpressionKey(emotion);
            if (string.IsNullOrEmpty(expressionKey))
            {
                Debug.LogWarning($"[EmotionController] 감정 '{emotion}'에 대한 Expression을 찾을 수 없습니다.");
                yield break;
            }
            
            // 블렌드 시간 결정
            var blendTime = _isVoicePlaying ? _voiceBlendTime : _defaultBlendTime;
            
            // Expression 변경
            _expressionController.CurrentExpressionIndex = GetExpressionIndex(expressionKey);
            _currentEmotion = emotion;
            _currentIntensity = intensity;
            
            Debug.Log($"[EmotionController] 감정 변경: {emotion} (강도: {intensity})");
            
            // 지속 시간만큼 대기
            if (durationMs > 0)
            {
                yield return new WaitForSeconds(durationMs / 1000f);
                
                // 지속 시간 종료 시 neutral로 복귀
                if (_currentEmotion == emotion) // 다른 감정으로 변경되지 않았다면
                {
                    SetEmotion("neutral", 1.0f, 0);
                }
            }
        }
        
        private string GetExpressionKey(string emotion)
        {
            foreach (var mapping in _emotionMappings)
            {
                if (mapping.Emotion.Equals(emotion, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.ExpressionKey;
                }
            }
            
            // 기본값 반환
            return "neutral";
        }
        
        private int GetExpressionIndex(string expressionKey)
        {
            if (_expressionController == null) return 0;
            
            // Expression 이름으로 인덱스 찾기
            for (int i = 0; i < _expressionController.ExpressionsList.CubismExpressionObjects.Length; i++)
            {
                if (_expressionController.ExpressionsList.CubismExpressionObjects[i].name.Equals(expressionKey, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            
            return 0; // 기본값
        }
        
        private bool IsIntenseEmotionChange(string currentEmotion, string newEmotion)
        {
            // 과격한 감정 전환 판단 로직
            var intenseEmotions = new[] { "angry", "surprised", "sad" };
            var isCurrentIntense = Array.Exists(intenseEmotions, e => e.Equals(currentEmotion, StringComparison.OrdinalIgnoreCase));
            var isNewIntense = Array.Exists(intenseEmotions, e => e.Equals(newEmotion, StringComparison.OrdinalIgnoreCase));
            
            return isCurrentIntense && isNewIntense && currentEmotion != newEmotion;
        }
        
        #endregion
        
        #region Emotion Mapping
        
        [System.Serializable]
        public class EmotionExpressionMapping
        {
            public string Emotion;
            public string ExpressionKey;
        }
        
        #endregion
    }
}


