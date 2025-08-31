using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ProjectVG.Infrastructure.Auth.OAuth2.Models;

namespace ProjectVG.Infrastructure.Auth.OAuth2.Utils
{
    /// <summary>
    /// OAuth2 콜백 URL 파싱 유틸리티
    /// </summary>
    public static class OAuth2CallbackParser
    {
        /// <summary>
        /// OAuth2 콜백 URL 파싱
        /// </summary>
        /// <param name="callbackUrl">콜백 URL</param>
        /// <returns>파싱 결과</returns>
        public static OAuth2CallbackResult ParseCallbackUrl(string callbackUrl)
        {
            if (string.IsNullOrEmpty(callbackUrl))
            {
                return OAuth2CallbackResult.ErrorResult("콜백 URL이 비어있습니다.");
            }
            
            try
            {
                // URL 파싱
                var uri = new Uri(callbackUrl);
                var query = HttpUtility.ParseQueryString(uri.Query);
                
                // 쿼리 파라미터를 Dictionary로 변환 (대소문자 무시)
                var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in query.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        queryParams[key] = query[key];
                    }
                }
                
                // success 파라미터 확인
                if (!queryParams.TryGetValue("success", out var successRaw) || string.IsNullOrEmpty(successRaw))
                {
                    return OAuth2CallbackResult.ErrorResult("success 파라미터가 비어있습니다.", callbackUrl);
                }
                var success = successRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
                
                if (success)
                {
                    // 성공 케이스: state 파라미터 확인
                    if (!queryParams.ContainsKey("state"))
                    {
                        return OAuth2CallbackResult.ErrorResult("state 파라미터가 없습니다.", callbackUrl);
                    }
                    
                    var state = queryParams["state"];
                    if (string.IsNullOrEmpty(state))
                    {
                        return OAuth2CallbackResult.ErrorResult("state 파라미터가 비어있습니다.", callbackUrl);
                    }
                    
                    var result = OAuth2CallbackResult.SuccessResult(state, callbackUrl);
                    result.QueryParameters = queryParams;
                    return result;
                }
                else
                {
                    // 실패 케이스: error 파라미터 확인
                    var error = queryParams.ContainsKey("error") ? queryParams["error"] : "알 수 없는 오류";
                    
                    var result = OAuth2CallbackResult.ErrorResult(error, callbackUrl);
                    result.QueryParameters = queryParams;
                    return result;
                }
            }
            catch (UriFormatException ex)
            {
                return OAuth2CallbackResult.ErrorResult($"잘못된 URL 형식: {ex.Message}", callbackUrl);
            }
            catch (Exception ex)
            {
                return OAuth2CallbackResult.ErrorResult($"콜백 URL 파싱 실패: {ex.Message}", callbackUrl);
            }
        }
        
        /// <summary>
        /// 커스텀 스킴 URL 파싱 (모바일 앱)
        /// </summary>
        /// <param name="schemeUrl">스킴 URL (예: myapp://oauth/callback?success=true&state=...)</param>
        /// <returns>파싱 결과</returns>
        public static OAuth2CallbackResult ParseSchemeUrl(string schemeUrl)
        {
            if (string.IsNullOrEmpty(schemeUrl))
            {
                return OAuth2CallbackResult.ErrorResult("스킴 URL이 비어있습니다.");
            }
            
            try
            {
                // 스킴 URL에서 쿼리 부분 추출
                var queryStartIndex = schemeUrl.IndexOf('?');
                if (queryStartIndex == -1)
                {
                    return OAuth2CallbackResult.ErrorResult("쿼리 파라미터가 없습니다.", schemeUrl);
                }
                
                var queryString = schemeUrl.Substring(queryStartIndex + 1);
                var query = HttpUtility.ParseQueryString(queryString);
                
                // 쿼리 파라미터를 Dictionary로 변환
                var queryParams = new Dictionary<string, string>();
                foreach (string key in query.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        queryParams[key] = query[key];
                    }
                }
                
                // success 파라미터 확인
                if (!queryParams.ContainsKey("success"))
                {
                    return OAuth2CallbackResult.ErrorResult("success 파라미터가 없습니다.", schemeUrl);
                }
                
                var success = queryParams["success"].ToLower();
                
                if (success == "true")
                {
                    // 성공 케이스: state 파라미터 확인
                    if (!queryParams.ContainsKey("state"))
                    {
                        return OAuth2CallbackResult.ErrorResult("state 파라미터가 없습니다.", schemeUrl);
                    }
                    
                    var state = queryParams["state"];
                    if (string.IsNullOrEmpty(state))
                    {
                        return OAuth2CallbackResult.ErrorResult("state 파라미터가 비어있습니다.", schemeUrl);
                    }
                    
                    var result = OAuth2CallbackResult.SuccessResult(state, schemeUrl);
                    result.QueryParameters = queryParams;
                    return result;
                }
                else
                {
                    // 실패 케이스: error 파라미터 확인
                    var error = queryParams.ContainsKey("error") ? queryParams["error"] : "알 수 없는 오류";
                    
                    var result = OAuth2CallbackResult.ErrorResult(error, schemeUrl);
                    result.QueryParameters = queryParams;
                    return result;
                }
            }
            catch (Exception ex)
            {
                return OAuth2CallbackResult.ErrorResult($"스킴 URL 파싱 실패: {ex.Message}", schemeUrl);
            }
        }
        
        /// <summary>
        /// URL에서 특정 파라미터 추출
        /// </summary>
        /// <param name="url">URL</param>
        /// <param name="parameterName">파라미터 이름</param>
        /// <returns>파라미터 값</returns>
        public static string ExtractParameter(string url, string parameterName)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(parameterName))
            {
                return null;
            }
            
            try
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query[parameterName];
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// URL이 OAuth2 콜백인지 확인
        /// </summary>
        /// <param name="url">확인할 URL</param>
        /// <returns>OAuth2 콜백 여부</returns>
        public static bool IsOAuth2Callback(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            
            try
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                
                // success 파라미터가 있으면 OAuth2 콜백으로 간주
                return query.AllKeys.Contains("success");
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// URL이 커스텀 스킴인지 확인
        /// </summary>
        /// <param name="url">확인할 URL</param>
        /// <param name="expectedScheme">예상 스킴</param>
        /// <returns>커스텀 스킴 여부</returns>
        public static bool IsCustomScheme(string url, string expectedScheme = null)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }
            
            try
            {
                var uri = new Uri(url);
                
                // 스킴이 http, https가 아니면 커스텀 스킴으로 간주
                if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                // 특정 스킴을 기대하는 경우
                if (!string.IsNullOrEmpty(expectedScheme))
                {
                    return uri.Scheme.Equals(expectedScheme, StringComparison.OrdinalIgnoreCase);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 디버그 정보 생성
        /// </summary>
        /// <param name="callbackResult">콜백 결과</param>
        /// <returns>디버그 정보</returns>
        public static string GetDebugInfo(OAuth2CallbackResult callbackResult)
        {
            if (callbackResult == null)
            {
                return "콜백 결과가 null입니다.";
            }
            
            var info = $"OAuth2 Callback Debug Info:\n";
            info += $"Success: {callbackResult.Success}\n";
            info += $"State: {callbackResult.State}\n";
            info += $"Error: {callbackResult.Error}\n";
            info += $"Original URL: {callbackResult.OriginalUrl}\n";
            info += $"Query Parameters Count: {callbackResult.QueryParameters?.Count ?? 0}\n";
            
            if (callbackResult.QueryParameters != null && callbackResult.QueryParameters.Count > 0)
            {
                info += "Query Parameters:\n";
                foreach (var kvp in callbackResult.QueryParameters)
                {
                    info += $"  {kvp.Key}: {kvp.Value}\n";
                }
            }
            
            return info;
        }
    }
}
