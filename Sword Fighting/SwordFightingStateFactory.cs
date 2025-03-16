using System.Collections.Generic;

enum ESwordFightingStates
{
    Reset,
    Search,
    Idle,
    Attack,
}
public class SwordFightingStateFactory
{
    SwordFightingStateMachine _context;
    Dictionary<ESwordFightingStates, SwordFightingBaseState> _states = new Dictionary<ESwordFightingStates, SwordFightingBaseState>();

    public SwordFightingStateFactory(SwordFightingStateMachine currentContext)
    {
        _context = currentContext;
        _states.Add(ESwordFightingStates.Reset, new SwordResetState(_context, this));
        _states.Add(ESwordFightingStates.Search, new SwordSearchState(_context, this));
        _states.Add(ESwordFightingStates.Idle, new SwordIdleState(_context, this));
        _states.Add(ESwordFightingStates.Attack, new SwordAttackState(_context, this));
    }

    public SwordFightingBaseState Reset()
    {
        return _states[ESwordFightingStates.Reset];
    }
    public SwordFightingBaseState Search()
    {
        return _states[ESwordFightingStates.Search];
    }
    public SwordFightingBaseState Idle()
    {
        return _states[ESwordFightingStates.Idle];
    }
    public SwordFightingBaseState Attack()
    {
        return _states[ESwordFightingStates.Attack];
    }
}
