namespace ScenarioTest.Support;

// 동시 LoadAsync 거부 시나리오용. Manager 의 Validate 안에서 첫 로드를 붙잡아 두 번째 호출이 반드시
// 진행 중인 로드와 겹치게 한다. 붙잡지 않으면 CSV 가 작고 캐시돼 있을 때 첫 로드가 동기적으로 끝나 경합이 된다.
public sealed class LoadGate : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly ManualResetEventSlim entered = new(false);
    private readonly ManualResetEventSlim released = new(false);

    public void Hold()
    {
        entered.Set();
        var releasedInTime = released.Wait(Timeout);
        if (!releasedInTime)
        {
            throw new TimeoutException("LoadGate 가 제한 시간 안에 풀리지 않았다.");
        }
    }

    // 첫 로드가 Validate 에 닿을 때까지 기다린다. 그 전에 첫 로드가 끝나 버리면(실패 포함) 기다리지 않고 그 결과를 드러낸다.
    public void WaitUntilHeld(Task firstLoad)
    {
        var handles = new[] { entered.WaitHandle, ((IAsyncResult)firstLoad).AsyncWaitHandle };
        var signaled = WaitHandle.WaitAny(handles, Timeout);
        if (signaled == 1)
        {
            firstLoad.GetAwaiter().GetResult();
            Assert.Fail("첫 LoadAsync 가 Validate 에 닿기 전에 끝나 게이트가 붙잡지 못했다.");
        }

        Assert.True(signaled == 0, "LoadAsync 가 제한 시간 안에 Validate 에 닿지 않았다.");
    }

    public void Release()
    {
        released.Set();
    }

    public void Dispose()
    {
        entered.Dispose();
        released.Dispose();
    }
}
