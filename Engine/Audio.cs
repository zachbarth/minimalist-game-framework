using SDL2;
using System;
using System.Collections.Generic;

static partial class Engine
{
    private static readonly int AudioChannelCount = 64;

    private static Dictionary<SoundInstance, int> SoundInstances = new Dictionary<SoundInstance, int>();

    private static int GetFadeTimeMs(float fadeTime)
    {
        return (int)(fadeTime * 1000);
    }

    /// <summary>
    /// Plays a sound. Returns an instance handle that can be passed to StopSound() to stop playback of the sound.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="repeat">Whether or not the sound should repeat until stopped.</param>
    /// <param name="fadeTime">The amount of time (in seconds) to fade in the sound's volume.</param>
    public static SoundInstance PlaySound(Sound sound, bool repeat = false, float fadeTime = 0)
    {
        // Start playing the new sound:
        int channel = SDL_mixer.Mix_FadeInChannel(-1, sound.Handle, repeat ? -1 : 0, GetFadeTimeMs(fadeTime));

        // Silently fail when we run out of channels:
        if (channel == -1)
        {
            return new SoundInstance();
        }

        // Invalidate old sound instances using this channel:
        foreach (var instanceAndChannel in SoundInstances)
        {
            if (instanceAndChannel.Value == channel)
            {
                SoundInstances.Remove(instanceAndChannel.Key);
                break;
            }
        }

        // Return a new sound instance for this channel:
        SoundInstance instance = new SoundInstance();
        SoundInstances[instance] = channel;
        return instance;
    }

    /// <summary>
    /// Stops a playing sound.
    /// </summary>
    /// <param name="instance">An instance handle from a prior call to PlaySound().</param>
    /// <param name="fadeTime">The amount of time (in seconds) to fade out the sound's volume.</param>
    public static void StopSound(SoundInstance instance, float fadeTime = 0)
    {
        int channel;
        if (SoundInstances.TryGetValue(instance, out channel))
        {
            SDL_mixer.Mix_FadeOutChannel(channel, GetFadeTimeMs(fadeTime));
        }
    }

    /// <summary>
    /// Plays music, stopping any currently playing music first.
    /// </summary>
    /// <param name="music">The music to play.</param>
    /// <param name="looping">Whether or not the music should repeat until stopped.</param>
    /// <param name="fadeTime">The amount of time (in seconds) to fade in the music's volume.</param>
    public static void PlayMusic(Music music, bool looping = true, float fadeTime = 0)
    {
        SDL_mixer.Mix_FadeInMusic(music.Handle, looping ? -1 : 0, GetFadeTimeMs(fadeTime));
    }

    /// <summary>
    /// Stops the current music.
    /// </summary>
    /// <param name="fadeTime">The amount of time (in seconds) to fade out the music's volume.</param>
    public static void StopMusic(float fadeTime = 0)
    {
        SDL_mixer.Mix_FadeOutMusic(GetFadeTimeMs(fadeTime));
    }
}

class SoundInstance
{
}
