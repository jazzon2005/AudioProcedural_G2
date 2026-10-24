using UnityEngine;
using System.Collections;
using View.Control;

namespace BallPuzzlePlatformer2D
{
    public class JoystickButton : MonoBehaviour
    {
        [HideInInspector]
        public bool Hold;
        [HideInInspector]
        public bool PreHold;
        [HideInInspector]
        public bool Pressed;

        [HideInInspector]
        public Joystick MyJoystick;

        public Transform ButtonShape;

        [HideInInspector]
        public Vector3 HitPosition;

        void Start()
        {
            MyJoystick = Joystick.GeneralJoystick;
            Hold = false;
        }

        void Update()
        {
            Hold = false;
            Pressed = false;

            Vector3[] PointerPos = new Vector3[2];

            if (Application.platform == RuntimePlatform.Android)
            {
                PointerPos = new Vector3[Input.touchCount];
                for (int i = 0; i < Input.touchCount; i++)
                {
                    PointerPos[i] = Input.touches[i].position;
                }
            }
            else
            {
                PointerPos = new Vector3[2];
                PointerPos[0] = Input.mousePosition;
                PointerPos[1] = Vector3.zero;
            }

            bool TempHold = false;
            HitPosition = Vector3.zero;

            for (int i = 0; i < PointerPos.Length; i++)
            {
                Ray ray = Camera.main.ScreenPointToRay(PointerPos[i]);
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, LayerMask.GetMask("UI"));

                foreach (RaycastHit r in hits)
                {
                    if (r.collider.gameObject == gameObject)
                    {
                        TempHold = true;
                        HitPosition = r.point;
                        break;
                    }
                }
            }

            if (TempHold)
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    if (!PreHold)
                    {
                        Pressed = true;
                        // SFX Procedural: Pulsación táctil
                        GameAudio.Instance?.Play(GameClip.MenuSelect);
                        print(gameObject.name + "- Pressed");
                    }
                    Hold = true;
                    PreHold = true;
                    print(gameObject.name + "- Hold");
                }
                else
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Pressed = true;
                        // SFX Procedural: Clic en botón UI
                        GameAudio.Instance?.Play(GameClip.MenuSelect);
                        print(gameObject.name + "- Pressed");
                    }

                    if (Input.GetMouseButton(0))
                    {
                        Hold = true;
                        print(gameObject.name + "- Hold");
                    }
                }
            }
            else
            {
                PreHold = false;
                Hold = false;
            }

            if (Hold)
            {
                ButtonShape.localScale = 0.9f * Vector3.one;
            }
            else
            {
                ButtonShape.localScale = Vector3.one;
            }
        }

        void OnGUI()
        {
        }
    }
}