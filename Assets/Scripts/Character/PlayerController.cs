using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;

    private Vector2 input;

    private Character character;

    void Awake()
    {
        character = GetComponent<Character>();
    }

    public void HandleUpdate()
    {
        if (!character.IsMoving)
        {
            //Stores 1 or -1 depending on the input
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            //Remove diagonal move
            if (input.x != 0)
                input.y = 0;

            //Updates the animator to change the idle animation depending on the direction the player is facing
            if (input != Vector2.zero)
            {
                StartCoroutine(character.Move(input, OnMoveOver));
            }
        }

        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Z))
            Interact();
    }

    void Interact()
    {
        //Getting the facing direction of the player
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        //Debug.DrawLine(transform.position, interactPos, Color.red, 1f);

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer);

        //Check if there is a collider with the interactableLayer
        if (collider != null)
        {
            //If there is something that the player can interact to... then
            collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    private void OnMoveOver()
    {
        //Check if the player has triggered an interaction after completing it's movement
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, character.OffsetY), 0.2f, GameLayers.i.TriggerableLayers);

        foreach (var collider in colliders)
        {
            var triggerable = collider.GetComponent<IPlayerTriggerable>();

            if (triggerable != null)
            {
                character.Animator.IsMoving = false;
                triggerable.OnPLayerTriggered(this);
                break;
            }
        }
    }

    public string Name 
    {
        get => name;        
    }

    public Sprite Sprite 
    {
        get => sprite;        
    }

    public Character Character => character;
}
