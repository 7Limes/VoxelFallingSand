using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using static UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.DynamicMoveProvider;

public class PlayerFootsteps : MonoBehaviour
{
    void start()
    {
        StartCoroutine(PlayFootsteps));
    }
    
    IEnumerator PlayFootsteps()
    {
            while (true)
            {
                if(//find the public component that tracks how you move in VR through XR origin) Says Vector 3.magnitude for normal player find the same for VR Look at
                //movecameratoworldlocation as a basis and the functions around it
            }
    }
}
