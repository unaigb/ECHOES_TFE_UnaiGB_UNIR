using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Managers
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [SerializeField] private DetectionScreen detectionScreen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void TriggerDetection()
        {
            if (detectionScreen != null)
                detectionScreen.Show(RestartLevel);
            else
                RestartLevel();
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
