#nullable enable
using UnityEngine;

namespace ProjectVG.Core.Audio
{
    public interface IAudioController
    {
        void Initialize();
        void SetVolume(float volume);
        void Stop();
        bool IsPlaying();
        float GetVolume();
        string GetControllerName();
    }
}
