using UnityEngine;
using UnityEngine.SceneManagement;

namespace DropProtocol
{
/// <summary>
///     Entry point of 00_Bootstrap: the persistent systems in this scene initialise themselves in Awake,
///     then the application moves on to its first interactive scene.
/// </summary>
public sealed class Bootstrap : MonoBehaviour
{
    [SerializeField]
    private string m_firstScene = "01_MainMenu";

    private void Start()
    {
        SceneManager.LoadScene(m_firstScene, LoadSceneMode.Single);
    }
}
}
