using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class InGameUI : MonoBehaviour
    {
        [SerializeField, Space]
        private GameplayData m_GameplayData;
        [SerializeField, Space]
        private Contents m_Contents;
        [SerializeField, Space]
        private DataStorage m_DataStorage;

        [SerializeField]
        private Text Text_LevelNum;

        [SerializeField, Space]
        public RectTransform MainCanvas;

        public static InGameUI Current;

        public Image[] m_SelectionFrames;

        [HideInInspector]
        public int m_SelectedItem = -1;

        void Awake()
        {
            Current = this;
        }

        void Update()
        {
            Text_LevelNum.text = "Level " + (m_GameplayData.LevelNumber + 1).ToString();
        }

        public void BtnExit()
        {
            // SFX Procedural: Botón salir de la partida
            GameAudio.Instance?.Play(GameClip.MenuSelect);

            SceneManager.LoadScene("MainMenu");
        }
    }
}