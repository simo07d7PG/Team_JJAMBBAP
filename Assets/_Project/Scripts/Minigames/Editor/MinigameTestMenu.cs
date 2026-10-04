using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BariBarista.Minigames.EditorTools
{
    /// <summary>
    /// Tools/BariBarista/Run Minigame Tests — 판정 로직 EditMode 테스트를 돌리고 요약을 콘솔에 남긴다.
    /// </summary>
    public static class MinigameTestMenu
    {
        private const string TestAssembly = "BariBarista.Minigames.Tests";
        private static TestRunnerApi api;
        private static readonly SummaryCallbacks callbacks = new SummaryCallbacks();

        [MenuItem("Tools/BariBarista/Run Minigame Tests")]
        public static void RunTests()
        {
            if (api == null)
            {
                api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.RegisterCallbacks(callbacks);
            }

            Debug.Log($"[MinigameTests] {TestAssembly} EditMode 테스트 실행");
            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { TestAssembly },
            };
            api.Execute(new ExecutionSettings(filter));
        }

        private sealed class SummaryCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                string msg = $"[MinigameTests] 결과: {result.ResultState} — 통과 {result.PassCount}, 실패 {result.FailCount}, 건너뜀 {result.SkipCount + result.InconclusiveCount}, {result.Duration:0.00}초";
                if (result.FailCount > 0) Debug.LogError(msg);
                else Debug.Log(msg);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                if (result.TestStatus == TestStatus.Failed)
                    Debug.LogError($"[MinigameTests] 실패: {result.Test.FullName}\n{result.Message}\n{result.StackTrace}");
            }
        }
    }
}
