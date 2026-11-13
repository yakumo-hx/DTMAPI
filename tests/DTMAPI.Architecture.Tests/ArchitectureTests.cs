using Xunit;

namespace DTMAPI.Architecture.Tests
{
    // architecture.md "架构检查"一节的三项检查。这是仓库里仅有的读源码的测试。
    // 旧代码里已知的问题记在 known-violations.txt。出现新问题，测试失败；清单里的问题修好了，测试也失败，提醒你删掉那一行。
    public sealed class ArchitectureTests
    {
        [Fact]
        public void ReferencesFollowTheLayers() =>
            KnownViolations.AssertSame("references", RepositoryScan.Current.References);

        [Fact]
        public void PlatformCodeDoesNotNameMods() =>
            KnownViolations.AssertSame("mod-names", RepositoryScan.Current.ModNames);

        [Fact]
        public void CodeHasNoStageNumbers() =>
            KnownViolations.AssertSame("phase-tokens", RepositoryScan.Current.StageNumbers);
    }
}
