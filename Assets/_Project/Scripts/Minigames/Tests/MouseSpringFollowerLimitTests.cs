using NUnit.Framework;
using UnityEngine;

namespace BariBarista.Minigames.Tests
{
    /// <summary>MouseSpringFollower의 축별 이동 제한(월드 Z 고정, 월드 X 범위) 계약 테스트.</summary>
    public class MouseSpringFollowerLimitTests
    {
        private sealed class FakeHand : IHandInput
        {
            public Vector2 Pointer;
            public Vector2 PointerPosition => Pointer;
            public bool GrabHeld => true;
            public bool GrabPressedThisFrame => false;
            public bool GrabReleasedThisFrame => false;
            public bool TiltHeld => false;
            public float TiltDelta => 0f;
        }

        private GameObject camGo, followerGo;
        private Camera cam;
        private MouseSpringFollower follower;
        private FakeHand hand;

        [SetUp]
        public void SetUp()
        {
            camGo = new GameObject("TestCam");
            cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(0f, 3f, -3f);
            camGo.transform.rotation = Quaternion.LookRotation(new Vector3(0f, -3f, 3f), Vector3.up);

            followerGo = new GameObject("TestFollower");
            followerGo.transform.position = new Vector3(0f, 0.5f, 0.2f);
            var rb = followerGo.AddComponent<Rigidbody>();
            rb.useGravity = false;
            follower = followerGo.AddComponent<MouseSpringFollower>();
            hand = new FakeHand();
            follower.Hand = hand;
            follower.ViewCamera = cam;
            follower.InputEnabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(followerGo);
            Object.DestroyImmediate(camGo);
        }

        private Vector3 TargetFor(Vector3 worldPoint)
        {
            hand.Pointer = cam.WorldToScreenPoint(worldPoint);
            follower.SimulationStep(0f);
            return follower.Target;
        }

        [Test]
        public void 옵션을_끄면_평면_위_어디든_따라간다()
        {
            Vector3 t = TargetFor(new Vector3(0.7f, 0.5f, 0.6f));
            Assert.AreEqual(0.7f, t.x, 0.01f);
            Assert.AreEqual(0.6f, t.z, 0.01f);
            Assert.IsFalse((follower.Body.constraints & RigidbodyConstraints.FreezePositionZ) != 0);
        }

        [Test]
        public void 옵션을_끄면_원형_제한만_적용된다()
        {
            Vector3 t = TargetFor(new Vector3(5f, 0.5f, 0f));
            Assert.AreEqual(1.5f, Vector3.Distance(new Vector3(0f, 0.5f, 0.2f), t), 0.01f);
        }

        [Test]
        public void Z고정이면_목표점_Z가_시작_위치에_고정된다()
        {
            follower.SetAxisLimits(true, false, -1f, 1f);
            Vector3 t = TargetFor(new Vector3(0.4f, 0.5f, 0.9f));
            Assert.AreEqual(0.2f, t.z, 1e-4f);
            Assert.AreEqual(0.4f, t.x, 0.01f);
        }

        [Test]
        public void Z고정이면_Rigidbody_Z도_잠기고_해제하면_풀린다()
        {
            follower.SetAxisLimits(true, false, -1f, 1f);
            Assert.IsTrue((follower.Body.constraints & RigidbodyConstraints.FreezePositionZ) != 0);
            follower.SetAxisLimits(false, false, -1f, 1f);
            Assert.IsTrue((follower.Body.constraints & RigidbodyConstraints.FreezePositionZ) == 0);
        }

        [Test]
        public void X범위_제한은_기준점_대비_최소_최대로_자른다()
        {
            follower.SetAxisLimits(false, true, -0.3f, 0.5f);
            Assert.AreEqual(0.5f, TargetFor(new Vector3(1.2f, 0.5f, 0.2f)).x, 0.01f);
            Assert.AreEqual(-0.3f, TargetFor(new Vector3(-1.2f, 0.5f, 0.2f)).x, 0.01f);
            Assert.AreEqual(0.1f, TargetFor(new Vector3(0.1f, 0.5f, 0.2f)).x, 0.01f);
        }

        [Test]
        public void X범위_제한은_Z를_건드리지_않는다()
        {
            follower.SetAxisLimits(false, true, -0.3f, 0.5f);
            Assert.AreEqual(0.7f, TargetFor(new Vector3(1.2f, 0.5f, 0.7f)).z, 0.01f);
        }
    }
}
