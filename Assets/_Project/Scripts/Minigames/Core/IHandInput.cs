using UnityEngine;
using UnityEngine.InputSystem;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 손 입력 추상화. 지금은 마우스(MouseHandInput)로 구현하고, 나중에 물리 손으로 교체한다.
    /// 값은 프레임 단위라 Update(또는 Tick) 안에서 읽는다.
    /// </summary>
    public interface IHandInput
    {
        /// <summary>화면 좌표 포인터 위치(픽셀).</summary>
        Vector2 PointerPosition { get; }
        bool GrabHeld { get; }
        bool GrabPressedThisFrame { get; }
        bool GrabReleasedThisFrame { get; }
        /// <summary>이번 프레임 기울기 입력(휠 한 칸 = 1). 양수 = 더 기울이기.</summary>
        float TiltDelta { get; }
    }

    /// <summary>Input System 마우스 기반 구현. 좌클릭 = 잡기, 휠 = 기울이기.</summary>
    public sealed class MouseHandInput : IHandInput
    {
        public static readonly MouseHandInput Shared = new MouseHandInput();

        /// <summary>휠 방향 반전(아래로 굴리면 기울이기가 기본).</summary>
        public bool InvertTilt;

        public Vector2 PointerPosition
        {
            get { var m = Mouse.current; return m != null ? m.position.ReadValue() : Vector2.zero; }
        }

        public bool GrabHeld
        {
            get { var m = Mouse.current; return m != null && m.leftButton.isPressed; }
        }

        public bool GrabPressedThisFrame
        {
            get { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        }

        public bool GrabReleasedThisFrame
        {
            get { var m = Mouse.current; return m != null && m.leftButton.wasReleasedThisFrame; }
        }

        public float TiltDelta
        {
            get
            {
                var m = Mouse.current;
                if (m == null) return 0f;
                float y = m.scroll.ReadValue().y;
                // 플랫폼·설정에 따라 한 칸이 120 또는 1로 들어오므로 칸 수로 맞춘다
                float notches = Mathf.Abs(y) > 20f ? y / 120f : y;
                // 휠을 아래로(음수) 굴리면 기울이기
                return InvertTilt ? notches : -notches;
            }
        }
    }

    public static class HandInputResolver
    {
        /// <summary>인스펙터에 IHandInput을 구현한 컴포넌트가 있으면 그것을, 없으면 마우스를 쓴다.</summary>
        public static IHandInput Resolve(MonoBehaviour source)
        {
            return source is IHandInput hand ? hand : MouseHandInput.Shared;
        }
    }
}
