using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField, Space]
        private DataStorage m_DataStorage;

        [SerializeField, Space]
        private GameplayData m_GameplayData;

        public void BtnExit()
        {
            // SFX Procedural: Clic en salir de la aplicación
            GameAudio.Instance?.Play(GameClip.MenuSelect);

            Application.Quit();
        }

        public void BtnLevel(int num)
        {
            // SFX Procedural: Selección de nivel
            GameAudio.Instance?.Play(GameClip.MenuSelect);

            m_GameplayData.LevelNumber = num;
            SceneManager.LoadScene("Scene_1");
        }
    }
}