using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordFightingStateMachine : MonoBehaviour
{
    #region Variables

    public Sword Sword;
    public CinemachineFreeLook[] Cameras;
    public Transform CheckSpherePos;
    public Transform[] SwordFightRaycastPos;
    public LayerMask CheckSphereLayer;
    public GameObject Player;
    public string EnemyTag;
    public List<SwordScriptableObjects> Combo;

    public float SwordStateChangeRate;
    public float SwordLayerWeightChangeRate;
    public float CheckSphereRadius;
    public float TimeBetweenSwitch = 0.5f;
    public float TimeBetweenEnemyChange = 0.5f;

    Animator _animator;
    PlayerInput _playerInputs;
    SwordHeirarchy _swordPos;
    Collider[] _enemies;
    Collider[] _availableEnemies;
    Collider[] _lastEnemies = null;
    SwordFightingBaseState _currentState;
    SwordFightingStateFactory _states;

    RaycastHit _enemyHit;
    Vector2 _scroll;

    float _currentSwordState;
    float _swordState;
    float _swordLayerWeight;
    float _currentSwordLayerWeight;
    float _requiredSwitchTime;
    float _lastAttackTime;
    float _lastComboEnd;
    int _comboCounter;

    bool _isAttackModePressed;
    bool _requireNewAttackModePress=false;
    bool _isAttackModeEnabled = false;
    bool _wasAttackModeEnabled = false;
    bool _isAttacking = false;
    bool _isRunning = false;
    bool _isInCombo = false;
    bool _requireNewScrollInput=false;
    #endregion

    #region Getters & Setters
    public Animator Animator { get { return _animator; } set { _animator = value; } }
    public SwordFightingBaseState CurrentState { get {  return _currentState; } set { _currentState = value; } }
    public Collider[] Enemies { get {  return _enemies; } set { _enemies = value; } }
    public Collider[] AvailableEnemies { get {  return _availableEnemies; } set { _availableEnemies = value; } }
    public SwordHeirarchy SwordPos { get {  return _swordPos; } set { _swordPos = value; } }
    public RaycastHit EnemyHit {  get { return _enemyHit; } set { _enemyHit = value; } }
    public int ComboCounter { get { return _comboCounter; } set { _comboCounter = value; } }
    public float CurrentSwordState { get { return _currentSwordState;} set {  _currentSwordState = value; } }
    public float CurrentSwordLayerWeight { get { return _currentSwordLayerWeight; } set { _currentSwordLayerWeight = value; } }
    public float RequiredSwitchTime { get { return _requiredSwitchTime; } set { _requiredSwitchTime = value; } }
    public float LastComboEnd { get { return _lastComboEnd; } set{ _lastComboEnd = value; } }
    public bool IsAttackModeEnabled { get { return _isAttackModeEnabled; } set { _isAttackModeEnabled= value; } }
    public bool WasAttackModeEnabled { get { return _wasAttackModeEnabled; } set { _wasAttackModeEnabled= value; } }
    public float ScrollInput { get { return _scroll.y; } }
    public float SwordLayerWeight { get { return _swordLayerWeight; } }
    public float SwordState { get { return _swordState; } }
    public bool RequireNewAttackModePress { get { return _requireNewAttackModePress; } set { _requireNewAttackModePress = value; } }
    public bool RequireNewScrollInput { get { return _requireNewScrollInput; } set { _requireNewScrollInput = value; } }
    public bool IsAttacking { get { return _isAttacking; } }
    public bool IsAttackModePressed { get { return _isAttackModePressed; } }
    public bool IsRunning { get { return _isRunning; } }
    public bool IsInCombo { get { return _isInCombo; } }
    #endregion

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _playerInputs = new PlayerInput();
        _swordPos = GetComponent<SwordHeirarchy>();
        _swordLayerWeight = _animator.GetLayerWeight(1);

        _states = new SwordFightingStateFactory(this);
        _swordPos.SheathSword();
        _currentState = _states.Reset();
        _currentState.EnterState();
        _swordState = 3f;

        _playerInputs.FightingControls.AttackMode.started += onEnterAttackMode;
        _playerInputs.FightingControls.AttackMode.canceled += onEnterAttackMode;

        _playerInputs.FightingControls.Attack.started += onAttack;
        _playerInputs.FightingControls.Attack.canceled += onAttack;

        _playerInputs.CharacterControls.Sprint.started += onRun;
        _playerInputs.CharacterControls.Sprint.canceled += onRun;

        _playerInputs.FightingControls.NextEnemySelect.started += onScroll;
        _playerInputs.FightingControls.NextEnemySelect.canceled += onScroll;

        _requiredSwitchTime = TimeBetweenSwitch;
    }

    #region Input System
    private void OnEnable()
    {
        _playerInputs.CharacterControls.Enable();
        _playerInputs.FightingControls.Enable();
    }
    private void OnDisable()
    {
        _playerInputs.CharacterControls.Disable();
        _playerInputs.FightingControls.Disable();
    }
    void onEnterAttackMode(InputAction.CallbackContext context)
    {
        _isAttackModePressed = context.ReadValueAsButton();
        _requireNewAttackModePress = false;
    }
    void onAttack(InputAction.CallbackContext context)
    {
        _isAttacking = context.ReadValueAsButton();
    }
    void onRun(InputAction.CallbackContext context)
    {
        _isRunning = context.ReadValueAsButton();
    }

    void onScroll(InputAction.CallbackContext context)
    {
        _scroll = context.ReadValue<Vector2>().normalized;
        _requireNewScrollInput = false;
    }
    #endregion

    private void Update()
    {
        if (_currentSwordState == 0f) _swordState = 0f;
        else if (_currentSwordState != 0f && _swordState != _currentSwordState)
        {
            if (_swordState < (_currentSwordState-0.1f)) _swordState += SwordStateChangeRate * Time.deltaTime;
            else if (_swordState > (_currentSwordState+0.1f)) _swordState -= SwordStateChangeRate * Time.deltaTime;
            else if ((_swordState > _currentSwordState && _swordState < (_currentSwordState + 0.1f)) || (_swordState < _currentSwordState && _swordState > (_currentSwordState - 0.1f))) _swordState = _currentSwordState;
        }

        if (_swordLayerWeight != _currentSwordLayerWeight)
        {
            if (_swordLayerWeight < (_currentSwordLayerWeight-0.05f)) _swordLayerWeight += SwordLayerWeightChangeRate * Time.deltaTime;
            else if (_swordLayerWeight > (_currentSwordLayerWeight+0.05f)) _swordLayerWeight -= SwordLayerWeightChangeRate * Time.deltaTime;
            else if ((_swordLayerWeight > _currentSwordLayerWeight && _swordLayerWeight < (_currentSwordLayerWeight + 0.05f)) || (_swordLayerWeight < _currentSwordLayerWeight && _swordLayerWeight > (_currentSwordLayerWeight - 0.05f))) _swordLayerWeight = _currentSwordLayerWeight;
        }

        if(((_enemies != null && _enemies.Length > 0) || (_lastEnemies != null && _lastEnemies.Length > 0))) CheckEnemyDetected();
        _currentState.UpdateState();

        _animator.SetFloat("Sword State", _swordState);
        _animator.SetLayerWeight(1, _swordLayerWeight);
    }
    public void Attack()
    {
        CancelInvoke("EndCombo");
        if (Time.time - _lastAttackTime >= 0.2f)
        {
            _isInCombo = true;
            Animator.runtimeAnimatorController = Combo[_comboCounter].AnimatorOverrideController;
            Animator.Play("Attack", 1, 0);
            _comboCounter++;
            _lastAttackTime = Time.time;
            if (_comboCounter >= Combo.Count) _comboCounter = 0;
        }
    }
    public void EndAttack()
    {
        if (Animator.GetCurrentAnimatorStateInfo(1).normalizedTime > 0.9f && Animator.GetCurrentAnimatorStateInfo(1).IsTag("Attack"))
        { Invoke("EndCombo", 0.5f); }
    }
    void EndCombo()
    {
        _comboCounter = 0;
        _lastComboEnd = Time.time;
        _isInCombo = false;
    }
    void CheckEnemyDetected()
    {
        if (_lastEnemies == null && _enemies != null) _lastEnemies = _enemies;
        if (_enemies != null && _enemies.Length == _lastEnemies.Length)
        {
            foreach (Collider enemy in _enemies)
            {
                 foreach (Collider lastEnemy in _lastEnemies) if (lastEnemy != enemy) _lastEnemies = _enemies;
            }
            return;
        }
        else if (_wasAttackModeEnabled) { _lastEnemies = null; _wasAttackModeEnabled = false; return; }
        int count = 0;
        if ((_enemies == null || _enemies.Length == 0) && (_lastEnemies != null || _lastEnemies.Length != 0)) { for (int i = 0; i < _lastEnemies.Length; i++) _lastEnemies[i].GetComponent<EnemyHighlight>().RemoveAdditionalMaterial(); if (_lastEnemies.Length > 0) _lastEnemies = null; }
        else if (_enemies != null && _enemies.Length > _lastEnemies.Length) _lastEnemies = _enemies;
        else if (_enemies.Length < _lastEnemies.Length && _enemies != null)
        {
            for (int i = 0; i < _lastEnemies.Length; i++)
            {
                for (int j = 0; j < _enemies.Length; j++)
                {
                    if (_lastEnemies[i].name != _enemies[j].name) count++;
                    else if (_lastEnemies[i].name == _enemies[j].name)
                    {
                        count = 0;
                        break;
                    }
                }
                if (count != 0) { _lastEnemies[i].GetComponent<EnemyHighlight>().RemoveAdditionalMaterial(); }
                count = 0;
            }
            _lastEnemies = _enemies;
        }
    }
}
