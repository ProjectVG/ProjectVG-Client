using System;
using UnityEngine;
using Newtonsoft.Json;
using ProjectVG.Infrastructure.Network.DTOs.Chat;
using ProjectVG.Domain.Chat.Model;

namespace ProjectVG.Tests
{
    /// <summary>
    /// 새로운 WebSocket API 스펙 테스트 클래스
    /// </summary>
    public class WebSocketApiTest : MonoBehaviour
    {
        void Start()
        {
            TestNewApiMessageParsing();
            TestMultipleActionsAndEmotion();
        }
        
        /// <summary>
        /// 새로운 API 메시지 구조 테스트
        /// </summary>
        private void TestNewApiMessageParsing()
        {
            Debug.Log("[WebSocketApiTest] === 새로운 API 메시지 파싱 테스트 ===");
            
            // 새로운 API 형식의 샘플 메시지
            string newApiMessage = @"{
                ""type"": ""chat"",
                ""message_type"": ""json"",
                ""data"": {
                    ""text"": ""안녕하세요! 반가워요!"",
                    ""emotion"": ""happy"",
                    ""actions"": [""clapping"", ""jumping""],
                    ""order"": 0,
                    ""request_id"": ""550e8400-e29b-41d4-a716-446655440000"",
                    ""timestamp"": ""2025-01-01T00:00:00.000Z"",
                    ""audio_data"": ""UklGRnoGAABXQVZFZm10IBAAAA"",
                    ""audio_format"": ""wav"",
                    ""audio_length"": 3.5
                }
            }";
            
            try
            {
                var response = JsonConvert.DeserializeObject<WebSocketResponse>(newApiMessage);
                if (response?.Data != null)
                {
                    Debug.Log($"[WebSocketApiTest] ✓ 새 API 파싱 성공");
                    Debug.Log($"[WebSocketApiTest] Text: {response.Data.Text}");
                    Debug.Log($"[WebSocketApiTest] Emotion: {response.Data.Emotion}");
                    Debug.Log($"[WebSocketApiTest] Actions: [{string.Join(", ", response.Data.Actions ?? new string[0])}]");
                    Debug.Log($"[WebSocketApiTest] RequestId: {response.Data.RequestId}");
                    Debug.Log($"[WebSocketApiTest] Order: {response.Data.Order}");
                    
                    // ChatMessage로 변환 테스트
                    var chatMessage = ChatMessage.FromChatData(response.Data);
                    Debug.Log($"[WebSocketApiTest] ✓ ChatMessage 변환 성공");
                    Debug.Log($"[WebSocketApiTest] ChatMessage.Emotion: {chatMessage.Emotion}");
                    Debug.Log($"[WebSocketApiTest] ChatMessage.RequestId: {chatMessage.RequestId}");
                    Debug.Log($"[WebSocketApiTest] ChatMessage.HasMultipleActions: {chatMessage.HasMultipleActions()}");
                    Debug.Log($"[WebSocketApiTest] ChatMessage.HasEmotionData: {chatMessage.HasEmotionData()}");
                }
                else
                {
                    Debug.LogError("[WebSocketApiTest] ✗ 새 API 파싱 실패: Data가 null");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketApiTest] ✗ 새 API 파싱 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 복수 액션 및 감정 처리 테스트
        /// </summary>
        private void TestMultipleActionsAndEmotion()
        {
            Debug.Log("[WebSocketApiTest] === 복수 액션 및 감정 처리 테스트 ===");
            
            try
            {
                // 다양한 액션과 감정 조합 테스트
                var testActions = new string[] { "clapping", "jumping", "waving", "idle" };
                var actionData = new CharacterActionData(testActions, "excited");
                
                Debug.Log($"[WebSocketApiTest] ✓ CharacterActionData 생성 성공");
                Debug.Log($"[WebSocketApiTest] Primary Action: {actionData.ActionType}");
                Debug.Log($"[WebSocketApiTest] All Actions: [{string.Join(", ", actionData.GetActionSequence())}]");
                Debug.Log($"[WebSocketApiTest] Emotion: {actionData.Emotion}");
                Debug.Log($"[WebSocketApiTest] HasMultipleActions: {actionData.HasMultipleActions()}");
                Debug.Log($"[WebSocketApiTest] HasEmotion: {actionData.HasEmotion()}");
                
                Debug.Log("[WebSocketApiTest] === 모든 테스트 완료 ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocketApiTest] ✗ 액션/감정 테스트 중 오류: {ex.Message}");
            }
        }
    }
}