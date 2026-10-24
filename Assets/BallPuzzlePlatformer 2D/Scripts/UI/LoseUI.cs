using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class LoseUI : MonoBehaviour
    {
        public void BtnRestart()
        {
            // SFX Procedural: Clic en reiniciar nivel
            GameAudio.Instance?.Play(GameClip.MenuSelect);

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void BtnExit()
        {
            // SFX Procedural: Clic en salir al menú
            GameAudio.Instance?.Play(GameClip.MenuSelect);

            SceneManager.LoadScene("MainMenu");
        }
    }
}