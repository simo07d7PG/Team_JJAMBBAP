namespace BariBarista.Minigames
{
    /// <summary>미니게임이 지금 어느 단계인지. 모든 호출 경로는 이 값으로 분기한다.</summary>
    public enum MicrogamePhase
    {
        /// <summary>아무것도 안 하는 상태. 처음, 연출이 끝난 뒤, ForceEnd 뒤.</summary>
        Idle = 0,
        /// <summary>Prepare 뒤 Begin 전. 초기화와 안내만 하고 시간·입력은 꺼져 있다.</summary>
        Preparing,
        /// <summary>Begin 뒤 결과가 나기 전. Tick으로 시간이 흐른다.</summary>
        Playing,
        /// <summary>결과를 확정하는 짧은 순간(ApplyResult → Finished → OnEnd). 이 안에서 들어온 ForceEnd는 무시된다.</summary>
        Resolving,
        /// <summary>결과 연출 중. Tick으로 연출 시계가 흐른다.</summary>
        Presenting,
    }

    /// <summary>PresentationFinished가 어떻게 끝났는지.</summary>
    public enum PresentationEnd
    {
        /// <summary>연출 시간을 다 채우고 끝났다.</summary>
        Completed = 0,
        /// <summary>밖에서 취소했다(ForceEnd, Prepare, Begin). 이벤트로는 보내지 않는다.</summary>
        Cancelled,
        /// <summary>Finished 처리 중 루트가 꺼져서 연출 없이 끝났다.</summary>
        Skipped,
    }
}
