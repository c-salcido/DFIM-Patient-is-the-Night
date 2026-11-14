using UnityEngine;
using UnityEngine.Playables;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private PlayableDirector introCutscene;
    [SerializeField] private GameObject pressEnterPrompt;
    [SerializeField] private PlayableDirector wakeUpCutscene;

    private bool waitingForEnter;

    private void Awake()
    {
        pressEnterPrompt.SetActive(false);
        introCutscene.stopped += HandleIntroFinished;
    }

    private void OnDestroy() => introCutscene.stopped -= HandleIntroFinished;

    private void HandleIntroFinished(PlayableDirector _)
    {
        CutsceneControlLock.Hold(this);
        pressEnterPrompt.SetActive(true);
        waitingForEnter = true;
    }

    private void Update()
    {
        if (!waitingForEnter)
            return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            waitingForEnter = false;
            pressEnterPrompt.SetActive(false);
            wakeUpCutscene.Play();

            CutsceneControlLock.Release(this);
        }
    }
}