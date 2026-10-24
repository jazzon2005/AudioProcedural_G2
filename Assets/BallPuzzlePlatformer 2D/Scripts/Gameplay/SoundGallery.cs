using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    /// <summary>
    /// Adaptador de compatibilidad para redirigir llamadas heredadas
    /// directamente hacia el motor de síntesis procedural GameAudio.
    /// </summary>
    public class SoundGallery : MonoBehaviour
    {
        public AudioClip[] sounds;
        public Dictionary<string, AudioClip> SoundList;

        public static SoundGallery MainSoundGallery;

        void Awake()
        {
            MainSoundGallery = this;
            SoundList = new Dictionary<string, AudioClip>();

            for (int i = 0; i < sounds.Length; i++)
            {
                string n = sounds[i].name;
                if (SoundList.ContainsKey(n))
                {
                    n = n + i.ToString();
                }
                SoundList.Add(n, sounds[i]);
            }
        }

        public static void PlaySound(string sound)
        {
            // Redirección al sistema de audio procedural
            GameAudio.Instance?.Play(GameClip.MenuSelect);
        }
    }
}