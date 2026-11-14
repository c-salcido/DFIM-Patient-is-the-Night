using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class CutsceneControlLock
{
    private static readonly HashSet<object> playing = new HashSet<object>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        playing.Clear();
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && playing.Count > 0)
        {
            playing.Clear();
            SetPlayerControl(true);
        }

        foreach (PlayableDirector director in Object.FindObjectsOfType<PlayableDirector>(true))
        {
            if (director.extrapolationMode == DirectorWrapMode.Loop)
                continue;

            director.played -= HandlePlayed;
            director.played += HandlePlayed;
            director.stopped -= HandleStopped;
            director.stopped += HandleStopped;

            if (director.state == PlayState.Playing)
                HandlePlayed(director);
        }
    }

    private static void HandlePlayed(PlayableDirector director)
    {
        playing.Add(director);
        SetPlayerControl(false);
    }

    private static void HandleStopped(PlayableDirector director)
    {
        playing.Remove(director);
        if (playing.Count == 0)
            SetPlayerControl(true);
    }

    public static void Hold(object holder)
    {
        playing.Add(holder);
        SetPlayerControl(false);
    }

    public static void Release(object holder)
    {
        if (playing.Remove(holder) && playing.Count == 0)
            SetPlayerControl(true);
    }

    private static void SetPlayerControl(bool enabled)
    {
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (player != null)
            player.SetControlEnabled(enabled);
    }
}