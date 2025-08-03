#nullable enable
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    [SerializeField] private Button _btnCreateSequence;
    [SerializeField] private Button _btnCreateRapidSequence;
    
    [Header("Test Settings")]
    [SerializeField] private string _userTestMessage = "Hello! I am a user.";
    [SerializeField] private string _characterTestMessage = "Hello! I am a character. The weather is really nice today.";
    [Range(0.5f, 5f)]
    [SerializeField] private float _displayTime = 1.5f;
    
    [Header("Sequence Test Settings")]
    [SerializeField] private string[] _sequenceMessages = {
        "안녕하세요!",
        "오늘 날씨가 정말 좋네요.",
        "무엇을 도와드릴까요?",
        "재미있는 이야기를 해드릴게요.",
        "그럼 이제 안녕히 가세요!"
    };
    [SerializeField] private float _sequenceDelay = 0.5f;
    [SerializeField] private float _rapidSequenceDelay = 0.2f;
    
    private Coroutine? _sequenceCoroutine;
    
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
            _chatBubbleManager = FindAnyObjectByType<ChatBubbleManager>();
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
        
        if (_btnCreateSequence != null)
        {
            _btnCreateSequence.onClick.AddListener(CreateMessageSequence);
        }
        
        if (_btnCreateRapidSequence != null)
        {
            _btnCreateRapidSequence.onClick.AddListener(CreateRapidMessageSequence);
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
    /// 메시지 시퀀스 생성 (토스트 애니메이션 테스트용)
    /// </summary>
    public void CreateMessageSequence()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
        }
        
        _sequenceCoroutine = StartCoroutine(CreateMessageSequenceCoroutine());
    }
    
    /// <summary>
    /// 빠른 메시지 시퀀스 생성 (큐 애니메이션 테스트용)
    /// </summary>
    public void CreateRapidMessageSequence()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
        }
        
        _sequenceCoroutine = StartCoroutine(CreateRapidMessageSequenceCoroutine());
    }
    
    /// <summary>
    /// 메시지 시퀀스 코루틴
    /// </summary>
    private IEnumerator CreateMessageSequenceCoroutine()
    {
        Debug.Log("메시지 시퀀스 시작");
        
        for (int i = 0; i < _sequenceMessages.Length; i++)
        {
            Actor actor = (i % 2 == 0) ? Actor.Character : Actor.User;
            string message = _sequenceMessages[i];
            
            _chatBubbleManager.CreateBubble(actor, message, _displayTime);
            
            Debug.Log($"시퀀스 메시지 {i + 1}/{_sequenceMessages.Length}: {actor} - {message}");
            
            yield return new WaitForSeconds(_sequenceDelay);
        }
        
        Debug.Log("메시지 시퀀스 완료");
    }
    
    /// <summary>
    /// 빠른 메시지 시퀀스 코루틴
    /// </summary>
    private IEnumerator CreateRapidMessageSequenceCoroutine()
    {
        Debug.Log("빠른 메시지 시퀀스 시작");
        
        for (int i = 0; i < _sequenceMessages.Length; i++)
        {
            Actor actor = (i % 2 == 0) ? Actor.Character : Actor.User;
            string message = _sequenceMessages[i];
            
            _chatBubbleManager.CreateBubble(actor, message, _displayTime);
            
            Debug.Log($"빠른 시퀀스 메시지 {i + 1}/{_sequenceMessages.Length}: {actor} - {message}");
            
            yield return new WaitForSeconds(_rapidSequenceDelay);
        }
        
        Debug.Log("빠른 메시지 시퀀스 완료");
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
    
    private void OnDestroy()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
        }
    }
}
