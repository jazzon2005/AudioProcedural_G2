using System.Collections;
using UnityEngine;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class PlatformSlider : MonoBehaviour
    {
        public Vector3 m_MoveDirection = Vector3.right;
        public float m_MoveDistance = 5f;
        public float m_MoveSpeed = 2f;

        private Vector3 m_StartPos;
        private Vector3 m_TargetPos;
        private Coroutine m_MoveRoutine;
        private bool m_KeepSliding = false;

        void Start()
        {
            m_StartPos = transform.position;
            m_TargetPos = m_StartPos + m_MoveDirection.normalized * m_MoveDistance;
        }

        public void StartSlideLoop()
        {
            if (m_MoveRoutine == null)
            {
                m_KeepSliding = true;
                m_MoveRoutine = StartCoroutine(SlideLoop());
            }
        }

        public void StopSlide()
        {
            m_KeepSliding = false;
            if (m_MoveRoutine != null)
            {
                StopCoroutine(m_MoveRoutine);
                m_MoveRoutine = null;
            }
        }

        private IEnumerator SlideLoop()
        {
            while (m_KeepSliding)
            {
                // SFX Procedural: Iniciar desplazamiento de plataforma
                GameAudio.Instance?.Play(GameClip.ArcMoveMid);

                // Mover al objetivo
                while (Vector3.Distance(transform.position, m_TargetPos) > 0.01f && m_KeepSliding)
                {
                    transform.position = Vector3.MoveTowards(transform.position, m_TargetPos, m_MoveSpeed * Time.deltaTime);
                    yield return null;
                }

                yield return new WaitForSeconds(0.1f);

                if (m_KeepSliding)
                {
                    // SFX Procedural: Retorno de plataforma
                    GameAudio.Instance?.Play(GameClip.ArcMoveLow);
                }

                // Mover de vuelta al origen
                while (Vector3.Distance(transform.position, m_StartPos) > 0.01f && m_KeepSliding)
                {
                    transform.position = Vector3.MoveTowards(transform.position, m_StartPos, m_MoveSpeed * Time.deltaTime);
                    yield return null;
                }

                yield return new WaitForSeconds(0.1f);
            }

            m_MoveRoutine = null;
        }
    }
}