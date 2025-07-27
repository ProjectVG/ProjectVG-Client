using System.Threading;
using Cysharp.Threading.Tasks;
using ProjectVG.Infrastructure.Network.Http;
using ProjectVG.Infrastructure.Network.DTOs.Character;

namespace ProjectVG.Infrastructure.Network.Services
{
    /// <summary>
    /// 캐릭터 API 서비스
    /// </summary>
    public class CharacterApiService
    {
        private readonly HttpApiClient _httpClient;

        public CharacterApiService()
        {
            _httpClient = HttpApiClient.Instance;
        }

        /// <summary>
        /// 모든 캐릭터 목록 조회
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>캐릭터 목록</returns>
        public async UniTask<CharacterInfo[]> GetAllCharactersAsync(CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetAsync<CharacterInfo[]>("character", cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 특정 캐릭터 정보 조회
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>캐릭터 정보</returns>
        public async UniTask<CharacterInfo> GetCharacterAsync(string characterId, CancellationToken cancellationToken = default)
        {
            return await _httpClient.GetAsync<CharacterInfo>($"character/{characterId}", cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 캐릭터 생성
        /// </summary>
        /// <param name="request">캐릭터 생성 요청</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>생성된 캐릭터 정보</returns>
        public async UniTask<CharacterInfo> CreateCharacterAsync(CreateCharacterRequest request, CancellationToken cancellationToken = default)
        {
            return await _httpClient.PostAsync<CharacterInfo>("character", request, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 간편한 캐릭터 생성
        /// </summary>
        /// <param name="name">캐릭터 이름</param>
        /// <param name="description">캐릭터 설명</param>
        /// <param name="role">캐릭터 역할</param>
        /// <param name="isActive">활성화 여부</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>생성된 캐릭터 정보</returns>
        public async UniTask<CharacterInfo> CreateCharacterAsync(
            string name, 
            string description, 
            string role, 
            bool isActive = true,
            CancellationToken cancellationToken = default)
        {
            var request = new CreateCharacterRequest
            {
                name = name,
                description = description,
                role = role,
                isActive = isActive
            };

            return await CreateCharacterAsync(request, cancellationToken);
        }

        /// <summary>
        /// 캐릭터 정보 수정
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="request">수정 요청</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>수정된 캐릭터 정보</returns>
        public async UniTask<CharacterInfo> UpdateCharacterAsync(string characterId, UpdateCharacterRequest request, CancellationToken cancellationToken = default)
        {
            return await _httpClient.PutAsync<CharacterInfo>($"character/{characterId}", request, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 간편한 캐릭터 수정
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="name">캐릭터 이름</param>
        /// <param name="description">캐릭터 설명</param>
        /// <param name="role">캐릭터 역할</param>
        /// <param name="isActive">활성화 여부</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>수정된 캐릭터 정보</returns>
        public async UniTask<CharacterInfo> UpdateCharacterAsync(
            string characterId,
            string name = null,
            string description = null,
            string role = null,
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            var request = new UpdateCharacterRequest();
            
            if (name != null) request.name = name;
            if (description != null) request.description = description;
            if (role != null) request.role = role;
            if (isActive.HasValue) request.isActive = isActive.Value;

            return await UpdateCharacterAsync(characterId, request, cancellationToken);
        }

        /// <summary>
        /// 캐릭터 삭제
        /// </summary>
        /// <param name="characterId">캐릭터 ID</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>삭제 성공 여부</returns>
        public async UniTask<bool> DeleteCharacterAsync(string characterId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _httpClient.DeleteAsync<object>($"character/{characterId}", cancellationToken: cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 