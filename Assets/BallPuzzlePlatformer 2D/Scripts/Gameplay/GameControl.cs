using System.Collections;
using UnityEngine;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class GameControl : MonoBehaviour
    {
        public static GameControl m_Current;

        [SerializeField] private GameplayData m_GameplayData;
        [SerializeField] private Contents m_Contents;

        public Camera m_MainCamera;

        private int m_GameState = 0;

        void Awake()
        {
            m_Current = this;
        }

        void Start()
        {
            int levelNum = m_GameplayData.LevelNumber;
            Instantiate(m_Contents.m_Levels[levelNum].m_LevelPrefab);

            // Adaptar música generativa e iniciar sonido de nivel
            if (ProceduralMusicSystem.Instance != null)
            {
                ProceduralMusicSystem.Instance.seed = (levelNum + 1) * 11;
                ProceduralMusicSystem.Instance.bpm = 90f + levelNum * 2f;
                ProceduralMusicSystem.Instance.SetProgress((float)levelNum / 10f);
                ProceduralMusicSystem.Instance.StartMusic();
            }

            GameAudio.Instance?.Play(GameClip.GameStart);
        }

        public void HandleWin()
        {
            if (m_GameState != 0) return;
            StartCoroutine(Co_HandleWin());
        }

        private IEnumerator Co_HandleWin()
        {
            m_GameState = 1;
            Cursor.visible = true;

            // SFX & Cadencia de Victoria Procedural
            if (ProceduralMusicSystem.Instance != null)
            {
                ProceduralMusicSystem.Instance.PlayCadence();
            }
            GameAudio.Instance?.Play(GameClip.LevelWin);

            yield return new WaitForSeconds(1);
            UISystem.RemoveUI("game-ui");
            yield return new WaitForSeconds(2);
            UISystem.ShowUI("win-ui");
        }

        public void HandleLose()
        {
            if (m_GameState != 0) return;
            StartCoroutine(Co_HandleLose());
        }

        private IEnumerator Co_HandleLose()
        {
            m_GameState = 2;
            Cursor.visible = true;

            // SFX & Detención de música en Derrota
            if (ProceduralMusicSystem.Instance != null)
            {
                ProceduralMusicSystem.Instance.StopMusic();
            }
            GameAudio.Instance?.Play(GameClip.GameEnd);

            yield return new WaitForSeconds(1);
            UISystem.RemoveUI("game-ui");
            yield return new WaitForSeconds(2);
            UISystem.ShowUI("lose-ui");
        }
    }
}