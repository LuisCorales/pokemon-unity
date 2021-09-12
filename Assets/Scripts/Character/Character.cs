using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float moveSpeed;
    public bool IsMoving { get; private set; }

    public float OffsetY { get; private set; } = 0.3f;

    CharacterAnimator animator;

    void Awake()
    {
        animator = GetComponent<CharacterAnimator>();
        SetPositionAndSnapToTile(transform.position);
    }

    public void SetPositionAndSnapToTile(Vector2 pos)
    {
        //Since the characters are placed at 0.5 in x and 0.8 o -0.2 in y
        pos.x = Mathf.Floor(pos.x) + 0.5f;
        pos.y = Mathf.Floor(pos.y) + 0.5f + OffsetY;

        transform.position = pos;
    }

    //Tile-based movement
    public IEnumerator Move(Vector2 moveVec, Action OnMoveOver = null)
    {
        //Updating the animator acording to the target position
        animator.MoveX = Mathf.Clamp(moveVec.x, -1f, 1f);
        animator.MoveY = Mathf.Clamp(moveVec.y, -1f, 1f);

        //Getting the target position
        var targetPos = transform.position;
        targetPos.x += moveVec.x;
        targetPos.y += moveVec.y;

        if (!IsParthClear(targetPos))
            yield break;

        //Moving the character to the target position
        IsMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;

        IsMoving = false;

        OnMoveOver?.Invoke();
    }

    public void HandleUpdate()
    {
        animator.IsMoving = IsMoving;
    }

    private bool IsParthClear(Vector3 targetPos)
    {
        var diff = targetPos - transform.position;
        var dir = diff.normalized;

        //If the path is clear from solid objects, then return true
        return Physics2D.BoxCast(transform.position + dir, new Vector2(0.2f, 0.2f), 0f, dir, diff.magnitude - 1, 
            GameLayers.i.SolidObjectLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer) == false;
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        //Checks if the tile in front of the player is walkable or not
        //by checking its layer
        return Physics2D.OverlapCircle(targetPos, 0.2f, GameLayers.i.SolidObjectLayer | GameLayers.i.InteractableLayer) == null;
    }

    public void LookTowards(Vector3 targetPos)
    {
        var xdiff = Mathf.Floor(targetPos.x) - Mathf.Floor(transform.position.x);
        var ydiff = Mathf.Floor(targetPos.y) - Mathf.Floor(transform.position.y);

        if (xdiff == 0 || ydiff == 0)
        {
            animator.MoveX = Mathf.Clamp(xdiff, -1f, 1f);
            animator.MoveY = Mathf.Clamp(ydiff, -1f, 1f);
        }
        else
            Debug.LogError("Error in Look Towards: You can't ask the character to look diagonally");
    }

    public CharacterAnimator Animator
    {
        get => animator;
    }
}
