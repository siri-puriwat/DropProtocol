using NUnit.Framework;

namespace DropProtocol.Tests.EditMode
{
    public class BotBrainTests
    {
        private const int Magazine = 30;

        private static BotTuning Tuning => new()
        {
            ThinkSeconds = 0.2f,
            FollowStopRadius = 3f,
            FollowResumeRadius = 4.5f,
            EngageRange = 25f,
            DisengageRange = 30f,
            RetreatRange = 4f,
            RetreatStep = 3f,
            ReviveSearchRange = 30f,
            ReviveDangerRange = 8f,
            ReviveAbortRange = 4f,
            ReviveReach = 1.5f,
            ObjectiveAssistRange = 12f,
            ObjectiveReach = 1.5f
        };

        private BotBrain m_brain;

        [SetUp]
        public void SetUp()
        {
            m_brain = new BotBrain(Tuning);
        }

        [Test]
        public void NewBrain_FollowsAndHolds()
        {
            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.Hold));
        }

        [Test]
        public void Update_LeaderFar_MovesToLeader()
        {
            m_brain.Update(Senses(leader: 10f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToLeader));
        }

        [Test]
        public void Update_LeaderClose_Holds()
        {
            m_brain.Update(Senses(leader: 2f));

            Assert.That(m_brain.Move, Is.EqualTo(BotMove.Hold));
        }

        [Test]
        public void Update_LeaderBetweenRadii_KeepsMovingOnlyOnceStarted()
        {
            m_brain.Update(Senses(leader: 4f));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.Hold));

            m_brain.Update(Senses(leader: 10f));
            m_brain.Update(Senses(leader: 4f));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToLeader));
        }

        [Test]
        public void Update_NothingSensed_Holds()
        {
            m_brain.Update(Senses());

            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.Hold));
            Assert.That(m_brain.WantsFire, Is.False);
            Assert.That(m_brain.WantsInteract, Is.False);
        }

        [Test]
        public void Update_EnemyInRange_FightsFromFormation()
        {
            m_brain.Update(Senses(leader: 2f, enemy: 20f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.Hold));
            Assert.That(m_brain.AimAtEnemy, Is.True);
            Assert.That(m_brain.WantsFire, Is.True);
            Assert.That(m_brain.WantsInteract, Is.False);
        }

        [Test]
        public void Update_EnemyBeyondEngageRange_KeepsFollowing()
        {
            m_brain.Update(Senses(enemy: 28f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
            Assert.That(m_brain.WantsFire, Is.False);
        }

        [Test]
        public void Update_EnemyDriftsBetweenRangesDuringCombat_StaysInCombat()
        {
            m_brain.Update(Senses(enemy: 20f));
            m_brain.Update(Senses(enemy: 28f));
            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));

            m_brain.Update(Senses(enemy: 31f));
            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
        }

        [Test]
        public void Update_EnemyInsideRetreatRange_BacksOffWhileFiring()
        {
            m_brain.Update(Senses(leader: 2f, enemy: 3f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.AwayFromEnemy));
            Assert.That(m_brain.WantsFire, Is.True);
        }

        [Test]
        public void Update_CombatWithLeaderFar_StillMovesToLeader()
        {
            m_brain.Update(Senses(leader: 10f, enemy: 20f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToLeader));
        }

        [Test]
        public void Update_DownedTeammateWithoutEnemy_Revives()
        {
            m_brain.Update(Senses(leader: 2f, downed: 10f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Revive));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToDowned));
            Assert.That(m_brain.WantsInteract, Is.True);
            Assert.That(m_brain.WantsFire, Is.False);
        }

        [Test]
        public void Update_DownedTeammateBeyondSearchRange_IsIgnored()
        {
            m_brain.Update(Senses(downed: 40f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
        }

        [Test]
        public void Update_DownedWithEnemyInDangerRange_FightsFirst()
        {
            m_brain.Update(Senses(downed: 10f, enemy: 6f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.WantsInteract, Is.False);
        }

        [Test]
        public void Update_RevivingWithEnemyOutsideAbortRange_KeepsRevivingAndShoots()
        {
            m_brain.Update(Senses(downed: 5f));
            m_brain.Update(Senses(downed: 5f, enemy: 6f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Revive));
            Assert.That(m_brain.WantsInteract, Is.True);
            Assert.That(m_brain.AimAtEnemy, Is.True);
            Assert.That(m_brain.WantsFire, Is.True);
        }

        [Test]
        public void Update_RevivingWithEnemyInsideAbortRange_SwitchesToCombat()
        {
            m_brain.Update(Senses(downed: 5f));
            m_brain.Update(Senses(downed: 5f, enemy: 3f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.WantsInteract, Is.False);
        }

        [Test]
        public void Update_PartialMagazineWithoutEnemy_TriggersReloadOnce()
        {
            m_brain.Update(Senses(ammo: 10));
            Assert.That(m_brain.ReloadTriggered, Is.True);

            m_brain.Update(Senses(ammo: 10));
            Assert.That(m_brain.ReloadTriggered, Is.False);
        }

        [Test]
        public void Update_AfterReloadCompletes_CanTriggerAgain()
        {
            m_brain.Update(Senses(ammo: 10));
            m_brain.Update(Senses(ammo: 10, reloading: true));
            m_brain.Update(Senses(ammo: Magazine));
            m_brain.Update(Senses(ammo: 10));

            Assert.That(m_brain.ReloadTriggered, Is.True);
        }

        [Test]
        public void Update_PartialMagazineInCombat_DoesNotReload()
        {
            m_brain.Update(Senses(enemy: 20f, ammo: 10));

            Assert.That(m_brain.ReloadTriggered, Is.False);
        }

        [Test]
        public void Update_EmptyMagazineInCombat_Reloads()
        {
            m_brain.Update(Senses(enemy: 20f, ammo: 0));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.ReloadTriggered, Is.True);
        }

        [Test]
        public void Update_ObjectiveNearLeader_MovesToObjectiveAndHoldsInteract()
        {
            m_brain.Update(Senses(leader: 2f, objective: 8f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Objective));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToObjective));
            Assert.That(m_brain.WantsInteract, Is.True);
            Assert.That(m_brain.WantsFire, Is.False);
            Assert.That(m_brain.AimAtEnemy, Is.False);
        }

        [Test]
        public void Update_ObjectiveFarFromLeader_Follows()
        {
            m_brain.Update(Senses(leader: 10f, objective: 8f, objectiveNearLeader: false));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToLeader));
            Assert.That(m_brain.WantsInteract, Is.False);
        }

        [Test]
        public void Update_ObjectiveWithEnemyInEngageRange_FightsFirst()
        {
            m_brain.Update(Senses(leader: 2f, enemy: 15f, objective: 8f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Combat));
            Assert.That(m_brain.WantsInteract, Is.False);
        }

        [Test]
        public void Update_ObjectiveWithDownedTeammate_RevivesFirst()
        {
            m_brain.Update(Senses(leader: 2f, downed: 5f, objective: 8f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Revive));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToDowned));
        }

        [Test]
        public void Update_ObjectiveWithoutLeader_StillGoes()
        {
            m_brain.Update(Senses(objective: 20f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Objective));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToObjective));
        }

        [Test]
        public void Update_ObjectiveCleared_ReturnsToFollow()
        {
            m_brain.Update(Senses(leader: 10f, objective: 8f));
            m_brain.Update(Senses(leader: 10f));

            Assert.That(m_brain.State, Is.EqualTo(BotState.Follow));
            Assert.That(m_brain.Move, Is.EqualTo(BotMove.ToLeader));
        }

        private static BotSenses Senses(float? leader = null, float? enemy = null, float? downed = null,
            int ammo = Magazine, bool reloading = false, float? objective = null, bool objectiveNearLeader = true)
        {
            return new BotSenses
            {
                HasLeader = leader.HasValue,
                LeaderDistance = leader ?? float.PositiveInfinity,
                HasEnemy = enemy.HasValue,
                EnemyDistance = enemy ?? float.PositiveInfinity,
                HasDowned = downed.HasValue,
                DownedDistance = downed ?? float.PositiveInfinity,
                HasObjective = objective.HasValue,
                ObjectiveDistance = objective ?? float.PositiveInfinity,
                ObjectiveNearLeader = objective.HasValue && objectiveNearLeader,
                Ammo = ammo,
                MagazineSize = Magazine,
                IsReloading = reloading
            };
        }
    }
}
