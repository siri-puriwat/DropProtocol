using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DropProtocol.Tests.PlayMode
{
    public class PlayerCharacterMovementTests
    {
        private const float SettleSeconds = 0.25f;
        private const float DriveSeconds = 0.5f;
        private const float YawTolerance = 1f;

        private GameObject m_ground;
        private GameObject m_characterObject;
        private PlayerCharacter m_character;
        private TestCommandSource m_source;

        [SetUp]
        public void SetUp()
        {
            m_ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            m_ground.transform.localScale = new Vector3(10f, 1f, 10f);

            m_characterObject = CreateCharacterObject("TestPlayerCharacter", new Vector3(0f, 0.05f, 0f));
            m_source = new TestCommandSource();
            m_character = m_characterObject.AddComponent<PlayerCharacter>();
            m_character.SetCommandSource(m_source);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_characterObject);
            Object.Destroy(m_ground);
        }

        [UnityTest]
        public IEnumerator MoveCommand_DrivesCharacterAlongWorldZ()
        {
            yield return Wait(SettleSeconds);
            Vector3 start = m_characterObject.transform.position;

            m_source.Command = new PlayerCommand { Move = new Vector2(0f, 1f) };
            yield return Wait(DriveSeconds);

            Vector3 delta = m_characterObject.transform.position - start;
            Assert.That(delta.z, Is.GreaterThan(1f));
            Assert.That(Mathf.Abs(delta.x), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator AimCommand_TurnsCharacterTowardAim()
        {
            yield return Wait(SettleSeconds);

            m_source.Command = new PlayerCommand { Aim = new Vector2(1f, 0f) };
            yield return Wait(DriveSeconds);

            Assert.That(Mathf.DeltaAngle(m_characterObject.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(YawTolerance));
        }

        [UnityTest]
        public IEnumerator MoveWithoutAim_FacesMoveDirection()
        {
            yield return Wait(SettleSeconds);

            m_source.Command = new PlayerCommand { Move = new Vector2(0f, -1f) };
            yield return Wait(DriveSeconds);

            Assert.That(Mathf.DeltaAngle(m_characterObject.transform.eulerAngles.y, 180f), Is.EqualTo(0f).Within(YawTolerance));
        }

        [UnityTest]
        public IEnumerator NoCommand_LeavesCharacterInPlace()
        {
            yield return Wait(SettleSeconds);
            Vector3 start = m_characterObject.transform.position;

            m_source.Command = PlayerCommand.None;
            yield return Wait(DriveSeconds);

            Vector3 delta = m_characterObject.transform.position - start;
            Assert.That(new Vector2(delta.x, delta.z).magnitude, Is.LessThan(0.01f));
            Assert.That(m_character.LastCommand.HasMove, Is.False);
        }

        [UnityTest]
        public IEnumerator WithoutInjectedSource_UsesSiblingCommandSourceComponent()
        {
            GameObject sibling = CreateCharacterObject("SiblingSourceCharacter", new Vector3(5f, 0.05f, 5f));
            sibling.AddComponent<SiblingCommandSource>();
            sibling.AddComponent<PlayerCharacter>();
            Vector3 start = sibling.transform.position;

            yield return Wait(SettleSeconds + DriveSeconds);

            Assert.That(sibling.transform.position.x - start.x, Is.GreaterThan(1f));
            Object.Destroy(sibling);
        }

        // Position must be set before the CharacterController exists: the controller caches its position
        // and a later transform.position write is overridden by its next Move.
        private static GameObject CreateCharacterObject(string name, Vector3 position)
        {
            var characterObject = new GameObject(name);
            characterObject.transform.position = position;

            var controller = characterObject.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            characterObject.AddComponent<CharacterMotor>();

            return characterObject;
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return null;
        }

        private sealed class SiblingCommandSource : MonoBehaviour, IPlayerCommandSource
        {
            public PlayerCommand GetCommand() => new PlayerCommand { Move = new Vector2(1f, 0f) };
        }
    }
}
