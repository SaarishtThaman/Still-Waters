using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagementScript : MonoBehaviour
{

    public static void JumpToScene(int index)
    {
        SceneManager.LoadScene(index);
    }
}
