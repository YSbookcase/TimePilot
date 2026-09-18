using TimePilot.WinForms.KYS24;
using Xunit;

namespace TimePilot.Tests
{
    public sealed class DataStorageMigrationPlannerTests
    {
        [Fact]
        public void Decide_InitializesTargetForNewPackagedInstall()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.NotPresent),
                Candidate(isCurrent: false, DataStorageDatabaseState.NotPresent, isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.InitializeTarget, decision.Kind);
            Assert.True(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_MigratesValidCurrentDatabaseWhenTargetIsEmpty()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.Valid, hasDatabase: true),
                Candidate(isCurrent: false, DataStorageDatabaseState.NotPresent, isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.MigrateCurrentToTarget, decision.Kind);
            Assert.True(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_UsesExistingTargetWhenCurrentIsEmpty()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.NotPresent),
                Candidate(
                    isCurrent: false,
                    DataStorageDatabaseState.Valid,
                    hasDatabase: true,
                    isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.UseExistingTarget, decision.Kind);
            Assert.True(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_RequiresUserChoiceWhenBothLocationsContainData()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.Valid, hasDatabase: true),
                Candidate(
                    isCurrent: false,
                    DataStorageDatabaseState.Valid,
                    hasDatabase: true,
                    isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.ConflictRequiresUserChoice, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_BlocksInvalidCurrentDatabase()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.Invalid, hasDatabase: true),
                Candidate(isCurrent: false, DataStorageDatabaseState.NotPresent, isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.BlockedCurrentDatabaseInvalid, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_BlocksInvalidTargetDatabase()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.NotPresent),
                Candidate(
                    isCurrent: false,
                    DataStorageDatabaseState.Invalid,
                    hasDatabase: true,
                    isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.BlockedTargetDatabaseInvalid, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_BlocksWhenCurrentCandidateWasNotInspected()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(
                    isCurrent: true,
                    DataStorageDatabaseState.NotInspected,
                    canInspect: false),
                Candidate(isCurrent: false, DataStorageDatabaseState.NotPresent, isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.InspectionFailed, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_BlocksWhenCurrentCandidateIsUnavailable()
        {
            var decision = DataStorageMigrationPlanner.Decide(CreatePlan(
                Candidate(isCurrent: true, DataStorageDatabaseState.Unavailable),
                Candidate(isCurrent: false, DataStorageDatabaseState.NotPresent, isTarget: true)));

            Assert.Equal(DataStorageMigrationDecisionKind.InspectionFailed, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_KeepsCurrentLocationForUnpackagedPlan()
        {
            var current = Candidate(
                isCurrent: true,
                DataStorageDatabaseState.Valid,
                hasDatabase: true,
                isTarget: true);
            var plan = new DataStorageLocationPlan(
                IsPackaged: false,
                CurrentDirectory: current.DirectoryPath,
                TargetDirectory: current.DirectoryPath,
                Candidates: [current]);

            var decision = DataStorageMigrationPlanner.Decide(plan);

            Assert.Equal(DataStorageMigrationDecisionKind.NoMigrationRequired, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_BlocksWhenPackagedTargetIsUnavailable()
        {
            var current = Candidate(
                isCurrent: true,
                DataStorageDatabaseState.Valid,
                hasDatabase: true,
                isTarget: true);
            var plan = new DataStorageLocationPlan(
                IsPackaged: true,
                CurrentDirectory: current.DirectoryPath,
                TargetDirectory: current.DirectoryPath,
                Candidates: [current],
                IsTargetAvailable: false);

            var decision = DataStorageMigrationPlanner.Decide(plan);

            Assert.Equal(DataStorageMigrationDecisionKind.TargetUnavailable, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        [Fact]
        public void Decide_RequiresUserChoiceWhenMultipleSourceLocationsContainData()
        {
            var current = Candidate(
                isCurrent: true,
                DataStorageDatabaseState.Valid,
                hasDatabase: true);
            var target = Candidate(
                isCurrent: false,
                DataStorageDatabaseState.NotPresent,
                isTarget: true);
            var plan = new DataStorageLocationPlan(
                IsPackaged: true,
                CurrentDirectory: current.DirectoryPath,
                TargetDirectory: target.DirectoryPath,
                Candidates: [current, target],
                HasSourceConflict: true);

            var decision = DataStorageMigrationPlanner.Decide(plan);

            Assert.Equal(DataStorageMigrationDecisionKind.ConflictRequiresUserChoice, decision.Kind);
            Assert.False(decision.AllowsAutomaticAction);
        }

        private static DataStorageLocationPlan CreatePlan(
            DataStorageCandidate current,
            DataStorageCandidate target)
        {
            return new DataStorageLocationPlan(
                IsPackaged: true,
                CurrentDirectory: current.DirectoryPath,
                TargetDirectory: target.DirectoryPath,
                Candidates: [current, target]);
        }

        private static DataStorageCandidate Candidate(
            bool isCurrent,
            DataStorageDatabaseState state,
            bool hasDatabase = false,
            bool isTarget = false,
            bool canInspect = true)
        {
            var name = isCurrent ? "current" : "target";
            return new DataStorageCandidate(
                isCurrent
                    ? DataStorageLocationKind.MsixVirtualizedLocalCache
                    : DataStorageLocationKind.MsixLocalState,
                $@"C:\Data\{name}",
                isCurrent,
                isTarget,
                canInspect,
                hasDatabase,
                false,
                false,
                state,
                null);
        }
    }
}
