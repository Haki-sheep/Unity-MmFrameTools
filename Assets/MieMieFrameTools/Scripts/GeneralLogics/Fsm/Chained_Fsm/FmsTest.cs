using System;
using UnityEngine;
using MieMieFrameTools.ChainedFms;

public class FmsTest : MonoBehaviour
{
    private BattleFlow battleFlow;

    void Start()
    {
        battleFlow = new BattleFlow();
        battleFlow.InitFlow();
    }
}

/// <summary>
/// 战斗流程（管理多个阶段的切换）
/// </summary>
public class BattleFlow : BaseGameFlow
{
    protected override void FsmRegisterStages()
    {
        AddStage<BattleStartStage>();
        AddStage<BattleRoundStage>();
        AddStage<BattleEndStage>();
    }

    protected override void SetupTransitions()
    {
        // BattleStartStage -> BattleRoundStage
        GetState<BattleStartStage>().OnFinish += () => FsmSwitchTo<BattleRoundStage>();
        // BattleRoundStage -> BattleEndStage
        GetState<BattleRoundStage>().OnFinish += () => FsmSwitchTo<BattleEndStage>();
    }
}

/// <summary>
/// 战斗开始阶段
/// </summary>
public class BattleStartStage : BaseGameStage
{
    private System.Threading.Timer timer;

    public override void OnEnter(BaseGameFlow owner)
    {
        Debug.Log("战斗开始！");
        timer = new System.Threading.Timer(_ => Finish(), null, 2000, System.Threading.Timeout.Infinite);
    }

    public override void OnExit(BaseGameFlow owner)
    {
        timer?.Dispose();
        timer = null;
        Debug.Log("战斗开始阶段退出");
    }

    public override bool IsFirstState() => true;
}

/// <summary>
/// 战斗回合阶段
/// </summary>
public class BattleRoundStage : BaseGameStage
{
    private int roundCount;

    public override void OnEnter(BaseGameFlow owner)
    {
        roundCount++;
        Debug.Log($"第 {roundCount} 回合开始");
    }

    public override void OnExit(BaseGameFlow owner)
    {
        Debug.Log($"第 {roundCount} 回合结束");
    }

    public void EndRound()
    {
        Finish(); // 回合结束，进入下一阶段
    }

    public override bool IsFirstState()
    {
        return false;
    }
}

/// <summary>
/// 战斗结束阶段
/// </summary>
public class BattleEndStage : BaseGameStage
{
    public override bool IsFirstState()
    {
        return false;
    }

    public override void OnEnter(BaseGameFlow owner)
    {
        Debug.Log("战斗结束！");
    }

    public override void OnExit(BaseGameFlow owner)
    {
        Debug.Log("战斗结束阶段退出");
    }
}
