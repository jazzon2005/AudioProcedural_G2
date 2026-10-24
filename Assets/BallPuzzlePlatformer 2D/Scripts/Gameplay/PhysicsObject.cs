using UnityEngine;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class PhysicsObject : MonoBehaviour
    {
        public GameObject m_SmashPrefab;

        public void Smash()
        {
            // SFX Procedural: Destrucción de objeto
            GameAudio.Instance?.Play(GameClip.MovePushLow);

            GameObject obj = Instantiate(m_SmashPrefab);
            obj.transform.position = transform.position + new Vector3(0, 0, -.1f);
            obj.transform.rotation = transform.rotation;
            Destroy(obj, 5);

            Destroy(gameObject);
        }
    }
}