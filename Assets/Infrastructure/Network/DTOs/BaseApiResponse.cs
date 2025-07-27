using System;

namespace ProjectVG.Infrastructure.Network.DTOs
{
    /// <summary>
    /// API 응답의 기본 구조
    /// </summary>
    [Serializable]
    public class BaseApiResponse
    {
        public bool success;
        public string message;
        public long timestamp;
        public string requestId;
    }

    /// <summary>
    /// 데이터를 포함하는 API 응답
    /// </summary>
    [Serializable]
    public class ApiResponse<T> : BaseApiResponse
    {
        public T data;
    }

    /// <summary>
    /// 페이지네이션 정보
    /// </summary>
    [Serializable]
    public class PaginationInfo
    {
        public int page;
        public int limit;
        public int total;
        public int totalPages;
        public bool hasNext;
        public bool hasPrev;
    }

    /// <summary>
    /// 페이지네이션된 API 응답
    /// </summary>
    [Serializable]
    public class PaginatedApiResponse<T> : BaseApiResponse
    {
        public T[] data;
        public PaginationInfo pagination;
    }

    /// <summary>
    /// 에러 응답
    /// </summary>
    [Serializable]
    public class ErrorResponse : BaseApiResponse
    {
        public string errorCode;
        public string errorType;
        public string[] details;
    }
} 