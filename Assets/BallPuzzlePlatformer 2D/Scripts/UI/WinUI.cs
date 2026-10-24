using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class WinUI : MonoBehaviour
    {
        [SerializeField, Space]
        private GameplayData m_GameplayData;
        [SerializeField, Space]
        private Contents m_Contents;

        public void Continue()
        {
            // SFX Procedural: Clic en continuar
            GameAudio.Instance?.Play(GameClip.MenuSelect);

            m_GameplayData.LevelNumber++;
            if (m_GameplayData.LevelNumber > m_Contents.m_Levels.Length - 1)
            {
                m_GameplayData.LevelNumber = 0;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}