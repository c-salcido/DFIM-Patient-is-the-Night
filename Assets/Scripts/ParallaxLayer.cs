using UnityEngine;

//this script was made using this tutorial https://www.youtube.com/watch?v=MEy-kIGE-lI

[ExecuteInEditMode]
public class ParallaxLayer : MonoBehaviour
{
    public float parallaxFactor;
 
    public void Move(float delta)
    {
        Vector3 newPos = transform.localPosition;
        newPos.x -= delta * parallaxFactor;
 
        transform.localPosition = newPos;
    }
 
}
 
