using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
public abstract class StateManager<EState> : MonoBehaviour where EState : Enum
{
    protected Dictionary<EState, BaseState<EState>> state = new Dictionary<EState, BaseState<EState>>();

    protected BaseState<EState> currentState;

    protected bool isTransitioningState = false;

    void Start()
    {
        currentState.EnterState();
    }

    void Update()
    {
        EState nextStateKey = currentState.GetNextState();
        if (!isTransitioningState && nextStateKey.Equals(currentState.stateKey))
        {
            currentState.UpdateState();
        }
        else
        {
            TransitionToState(nextStateKey);
        }
        
    }

    public void TransitionToState(EState statekey)
    {
        isTransitioningState = true;
        currentState.ExitState();
        currentState = state[statekey];
        currentState.EnterState();
        isTransitioningState = false;
    }

    void OnTriggerEnter(Collider other)
    {
        currentState.OnTriggerEnter(other);
    }

    void OnTriggerStay(Collider other)
    {
        currentState.OnTriggerStay(other);
    }

    void OnTriggerExit(Collider other)
    {
        currentState.OnTriggerExit(other);
    }

    
}
