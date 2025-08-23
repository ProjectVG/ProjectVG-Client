using System;
using Cysharp.Threading.Tasks;

namespace ProjectVG.Infrastructure.Auth.Storage
{
    public interface ISecureStorage
    {
        bool IsAvailable { get; }
        string PlatformName { get; }
        
        UniTask<bool> StoreAsync(string key, string value);
        UniTask<string> LoadAsync(string key);
        UniTask<bool> DeleteAsync(string key);
        UniTask<bool> ExistsAsync(string key);
        UniTask ClearAllAsync();
        
        UniTask<bool> StoreEncryptedAsync(string key, byte[] data);
        UniTask<byte[]> LoadEncryptedAsync(string key);
    }
}
