using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    [CreateAssetMenu(fileName = "DataStorage", menuName = "CustomObjects/DataStorage", order = 1)]
    public class DataStorage : ScriptableObject
    {
        public int LevelUnlocked;
        public bool MuteMusic;

        public void SaveData()
        {
            PlayerPrefs.SetInt("LevelUnlocked", LevelUnlocked);
            PlayerPrefs.SetInt("MuteMusic", MuteMusic ? 1 : 0);
            PlayerPrefs.Save();

            // Sincronizar con el sistema de audio procedural al guardar
            ApplyAudioSettings();
        }

        public void LoadData()
        {
            LevelUnlocked = PlayerPrefs.GetInt("LevelUnlocked", 0);
            MuteMusic = (PlayerPrefs.GetInt("MuteMusic", 0) == 1);

            // Sincronizar con el sistema de audio procedural al cargar
            ApplyAudioSettings();
        }

        /// <summary>
        /// Aplica la preferencia de silencio guardada directamente a los motores de audio procedural.
        /// </summary>
        public void ApplyAudioSettings()
        {
            if (GameAudio.Instance != null)
            {
                GameAudio.Instance.MusicEnabled = !MuteMusic;
            }

            if (ProceduralMusicSystem.Instance != null)
            {
                ProceduralMusicSystem.Instance.MusicEnabled = !MuteMusic;
            }
        }

        public void ResetSaveData()
        {
            SaveData();
        }

        public bool CheckInternet()
        {
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
                Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                return true;

            return false;
        }
    }
}