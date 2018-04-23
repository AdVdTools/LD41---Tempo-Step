using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour {

    internal Animator anim;

    internal static int activeHash = Animator.StringToHash("active");
    internal static int stateHash = Animator.StringToHash("state");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }
    
    public virtual void OnCellPlaced()
    {
        if (anim != null)
        {
            anim.SetBool(activeHash, false);
        }
    }

    public virtual void OnCellActivated()
    {
        if (anim != null)
        {
            anim.SetBool(activeHash, true);
        }
    }

    public virtual void OnCellStateChanged(byte state, byte target)
    {
        if (anim != null)
        {
            anim.SetInteger(stateHash, state);
        }
    }

    public virtual void OnCellDeactivated()
    {
        if (anim != null)
        {
            anim.SetBool(activeHash, false);
        }
    }

    public virtual void OnCellRemoved()
    {

    }
}
