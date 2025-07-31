using UnityEngine;
using UnityEngine.UI;
using ProjectVG.Domain.Chat.Model;
using ProjectVG.Domain.Chat.Service;

public class ChatBubbleTestUI : MonoBehaviour
{
    [Header("ChatBubbleManager Reference")]
    [SerializeField] private ChatBubbleManager _chatBubbleManager;
    
    [Header("Test Buttons")]
    [SerializeField] private Button _btnCreateUserBubble;
    [SerializeField] private Button _btnCreateCharacterBubble;
    [SerializeField] private Button _btnClearAllBubbles;
    
    [Header("Test Settings")]
    [SerializeField] private string _userTestMessage = "Hello! I am a user.";
    [SerializeField] private string _characterTestMessage = "Hello! I am a character. The weather is really nice today.";
    [Range(0.5f, 5f)]
    [SerializeField] private float _displayTime = 1.5f;  // 타이핑 완료 후 잔존 시간
    
    private void Start()
    {
        InitializeTestUI();
        SetupEventListeners();
    }
    
    /// <summary>
    /// 테스트 UI 초기화
    /// </summary>
    private void InitializeTestUI()
    {
        // ChatBubbleManager 자동 찾기
        if (_chatBubbleManager == null)
        {
            _chatBubbleManager = FindObjectOfType<ChatBubbleManager>();
            if (_chatBubbleManager == null)
            {
                Debug.LogError("ChatBubbleManager를 찾을 수 없습니다!");
                return;
            }
        }
        
        Debug.Log("ChatBubbleTestUI 초기화 완료");
    }
    
    /// <summary>
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        // 버튼 이벤트 설정
        if (_btnCreateUserBubble != null)
        {
            _btnCreateUserBubble.onClick.AddListener(CreateUserBubble);
        }
        
        if (_btnCreateCharacterBubble != null)
        {
            _btnCreateCharacterBubble.onClick.AddListener(CreateCharacterBubble);
        }
        
        if (_btnClearAllBubbles != null)
        {
            _btnClearAllBubbles.onClick.AddListener(ClearAllBubbles);
        }
    }
    
    /// <summary>
    /// 사용자 챗 버블 생성
    /// </summary>
    public void CreateUserBubble()
    {
        if (_chatBubbleManager != null)
        {
            _chatBubbleManager.CreateBubble(Actor.User, _userTestMessage, _displayTime);
            Debug.Log("사용자 챗 버블 생성");
        }
        else
        {
            Debug.LogError("ChatBubbleManager가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 캐릭터 챗 버블 생성
    /// </summary>
    public void CreateCharacterBubble()
    {
        if (_chatBubbleManager != null)
        {
            _chatBubbleManager.CreateBubble(Actor.Character, _characterTestMessage, _displayTime);
            Debug.Log("캐릭터 챗 버블 생성");
        }
        else
        {
            Debug.LogError("ChatBubbleManager가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 모든 챗 버블 제거
    /// </summary>
    public void ClearAllBubbles()
    {
        if (_chatBubbleManager != null)
        {
            _chatBubbleManager.ClearAllBubbles();
            Debug.Log("모든 챗 버블 제거");
        }
        else
        {
            Debug.LogError("ChatBubbleManager가 설정되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 테스트 메시지 설정
    /// </summary>
    public void SetUserTestMessage(string message)
    {
        _userTestMessage = message;
    }
    
    /// <summary>
    /// 테스트 메시지 설정
    /// </summary>
    public void SetCharacterTestMessage(string message)
    {
        _characterTestMessage = message;
    }
    
    /// <summary>
    /// 표시 시간 설정
    /// </summary>
    public void SetDisplayTime(float time)
    {
        _displayTime = time;
    }
}
