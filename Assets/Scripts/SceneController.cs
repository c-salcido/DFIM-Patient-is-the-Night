using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{

    public int NextScene; 


    
    // Start is called before the first frame update
    void Start()
    {
        
    }
    IEnumerator SceneTransition()
    {
        yield return new WaitForSeconds(5);
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene(NextScene);
        Debug.Log("it should work");

    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
