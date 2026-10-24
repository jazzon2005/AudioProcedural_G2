using System.Collections;
using UnityEngine;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class Player : MonoBehaviour
    {
        public GameObject m_SmashPrefab;
        private float m_OriginalSpeed;
        private bool m_IsBoosted = false;
        public float m_MoveSpeed = 50f;
        public float m_JumpForce = 10f;

        private Rigidbody2D rb;
        private bool m_CanJump = true;
        private bool m_IsClimbing = false;
        public float m_ClimbSpeed = 4f;
        private float m_OriginalGravity;
        private Vector3 m_OriginalScale;

        public Transform m_Shadow;

        // Mobile joystick
        private Joystick m_Joystick;
        private JoystickButton m_JumpButton;
        public static Player m_Main;

        private void Awake()
        {
            m_Main = this;
        }

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            m_OriginalGravity = rb.gravityScale;
            m_OriginalSpeed = m_MoveSpeed;
            m_OriginalScale = transform.localScale;

            if (InputControl.m_Main != null && InputControl.m_Main.m_MobileControl)
            {
                m_Joystick = Joystick.GeneralJoystick;
                m_JumpButton = m_Joystick.ButtonA;
            }
        }

        void Update()
        {
            if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.Space) || 
                (InputControl.m_Main != null && InputControl.m_Main.m_MobileControl && m_JumpButton != null && m_JumpButton.Pressed)) 
                && m_CanJump && !m_IsClimbing)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * m_JumpForce, ForceMode2D.Impulse);
                m_CanJump = false;

                // SFX Procedural: Salto
                GameAudio.Instance?.Play(GameClip.MovePushHigh);
            }

            Vector3 pos = transform.position;
            bool hasHit = false;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, Vector2.down, 100);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject == gameObject)
                    continue;

                hasHit = true;
                pos = hit.point;
            }

            if (hasHit)
            {
                m_Shadow.gameObject.SetActive(true);
                m_Shadow.position = pos;
                m_Shadow.rotation = Quaternion.identity;
            }
            else
            {
                m_Shadow.gameObject.SetActive(false);
            }
        }

        void FixedUpdate()
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            if (m_Joystick != null && m_Joystick.LeftStick != null)
            {
                if (Mathf.Abs(m_Joystick.LeftStick.StickOffset.x) > 0.1f)
                    moveX = m_Joystick.LeftStick.StickOffset.x;
            }

            if (m_IsClimbing)
            {
                rb.linearVelocity = new Vector2(0f, moveY * m_ClimbSpeed);
                rb.gravityScale = 0f;
            }
            else
            {
                rb.AddForce(new Vector2(moveX * m_MoveSpeed, 0));
                rb.gravityScale = m_OriginalGravity;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Ground"))
            {
                if (!m_CanJump)
                {
                    // SFX Procedural: Aterrizaje/Impacto
                    GameAudio.Instance?.Play(GameClip.MovePushLow);
                }
                m_CanJump = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Ladder"))
            {
                m_IsClimbing = true;
                GameAudio.Instance?.Play(GameClip.NodeEnter);
            }
            else if (collision.CompareTag("FallDetector"))
            {
                Smash();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Ladder"))
            {
                m_IsClimbing = false;
            }
        }

        public void ApplySpeedBoost(float boostAmount, float duration)
        {
            if (!m_IsBoosted)
            {
                GameAudio.Instance?.Play(GameClip.ArcMoveHigh);
                StartCoroutine(SpeedBoostRoutine(boostAmount, duration));
            }
        }

        private IEnumerator SpeedBoostRoutine(float boostAmount, float duration)
        {
            m_IsBoosted = true;
            m_MoveSpeed += boostAmount;
            yield return new WaitForSeconds(duration);
            m_MoveSpeed = m_OriginalSpeed;
            m_IsBoosted = false;
        }

        public void ApplyJumpBoost(float boostAmount, float duration)
        {
            GameAudio.Instance?.Play(GameClip.ArcMoveHigh);
            StartCoroutine(JumpBoostRoutine(boostAmount, duration));
        }

        private IEnumerator JumpBoostRoutine(float boostAmount, float duration)
        {
            m_JumpForce += boostAmount;
            yield return new WaitForSecondsRealtime(duration);
            m_JumpForce -= boostAmount;
        }

        public void ApplySizeChange(float scaleMultiplier, float duration)
        {
            GameAudio.Instance?.Play(GameClip.ArcMoveHigh);
            StartCoroutine(SizeChangeRoutine(scaleMultiplier, duration));
        }

        private IEnumerator SizeChangeRoutine(float scaleMultiplier, float duration)
        {
            transform.localScale = m_OriginalScale * scaleMultiplier;
            yield return new WaitForSeconds(duration);
            transform.localScale = m_OriginalScale;
        }

        public void Smash()
        {
            // SFX Procedural: Muerte / Impacto
            GameAudio.Instance?.Play(GameClip.GameEnd);

            GameObject obj = Instantiate(m_SmashPrefab);
            obj.transform.position = transform.position + new Vector3(0, 0, -.1f);
            obj.transform.rotation = transform.rotation;
            Destroy(obj, 5);

            Destroy(gameObject);
            GameControl.m_Current.HandleLose();
        }
    }
}