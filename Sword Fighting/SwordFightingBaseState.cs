public abstract class SwordFightingBaseState
{
    #region Variables
    private SwordFightingStateMachine _ctx;
    private SwordFightingStateFactory _factory;
    #endregion

    #region Getters & Setters
    public SwordFightingStateMachine CTX { get { return _ctx; } }
    public SwordFightingStateFactory Factory { get {  return _factory; } }
    #endregion

    public SwordFightingBaseState(SwordFightingStateMachine context, SwordFightingStateFactory factory)
    {
        _ctx = context;
        _factory = factory;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
    public abstract void CheckSwitchStates();
    protected void SwitchState(SwordFightingBaseState newState)
    {
        ExitState();
        newState.EnterState();
        _ctx.CurrentState = newState;
    }
}
