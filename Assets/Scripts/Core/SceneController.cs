using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{

    public int NextScene; 

    IEnumerator SceneTransition()
    {
        yield return new WaitForSeconds(5);
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(NextScene);
        

    }

}
