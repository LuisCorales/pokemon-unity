using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, MoveToForget, BattleOver }
public enum BattleAction { Move, SwitchPokemon, UseItem, Run }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogueBox dialogueBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;
    
    public event Action<bool> OnBattleOver;

    BattleState state;
    BattleState? prevState;

    int currentAction;
    int currentMove;
    int currentMember;
    bool aboutToUseChoice = true;

    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    int escapeAttempts;
    MoveBase moveToLearn;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon)
    {
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;

        player = playerParty.GetComponent<PlayerController>();
        isTrainerBattle = false;

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (!isTrainerBattle)
        {
            //Wild pokemon battle
            playerUnit.Setup(playerParty.GetHealthyPokemon());
            enemyUnit.Setup(wildPokemon);

            dialogueBox.SetMoveNames(playerUnit.Pokemon.Moves);

            yield return dialogueBox.TypeDialogue(enemyUnit.Pokemon.Base.name + " has appeared.");
        }
        else
        {
            //Trainer battle

            //Show trainer and player sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogueBox.TypeDialogue($"{trainer.Name} wants to battle.");

            //Send out first pokemon of trainer
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyPokemon = trainerParty.GetHealthyPokemon();
            enemyUnit.Setup(enemyPokemon);

            yield return dialogueBox.TypeDialogue($"{trainer.Name} send out {enemyPokemon.Base.Name}.");

            //Send out first pokemon of player
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = playerParty.GetHealthyPokemon();
            playerUnit.Setup(playerPokemon);

            yield return dialogueBox.TypeDialogue($"Go {playerPokemon.Base.Name}!");

            dialogueBox.SetMoveNames(playerUnit.Pokemon.Moves);
        }

        escapeAttempts = 0;
        partyScreen.Init();
        ActionSelection();
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;

        //For each pokemon it'll reset their boosted stats to 0 when the battle is over
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogueBox.SetDialogue("Choose an action.");
        dialogueBox.EnableActionSelector(true);
    }

    void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Pokemons);
        partyScreen.gameObject.SetActive(true);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogueBox.EnableActionSelector(false);
        dialogueBox.EnableDialogueText(false);
        dialogueBox.EnableMovesSelector(true);
    }

    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        yield return dialogueBox.TypeDialogue($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to change your pokemon?");

        state = BattleState.AboutToUse;

        dialogueBox.EnableChoiceBox(true);
    }

    IEnumerator ChooseMoveToForget(Pokemon pokemon, MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogueBox.TypeDialogue($"Choose a move you want to forget.");

        moveSelectionUI.gameObject.SetActive(true);
        //                          Will get the base of each Move in the list
        moveSelectionUI.SetMoveData(pokemon.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Pokemon.CurrentMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrentMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrentMove.Base.Priority;

            //Check who goes first
            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
                playerGoesFirst = false;
            else if (playerMovePriority == enemyMovePriority)
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;

            //First turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrentMove);
            if (state == BattleState.BattleOver) 
                yield break;

            //If the second pokemon is alive then...
            if (secondPokemon.HP > 0)
            {
                 //Second turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrentMove);
                if (state == BattleState.BattleOver) 
                    yield break;
            }

            //After turn effects
            yield return RunAfterTurn(firstUnit); //Llamar ambas funciones despues de ambos turnos? tal vez tengo que investigar
            if (state == BattleState.BattleOver) 
                yield break;
            yield return RunAfterTurn(secondUnit);
            if (state == BattleState.BattleOver) 
                yield break;
        }
        else
        {
            //If player switch pokemon
            if (playerAction == BattleAction.SwitchPokemon)
            {
                var selectedPokemon = playerParty.Pokemons[currentMember];
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedPokemon);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                dialogueBox.EnableActionSelector(false);
                yield return ThrowPokeball();
            }
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
            }

            //Enemy turn
            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);

            //If the player new pokemon is currently affected, then apply the after turn effects
            yield return RunAfterTurn(playerUnit);
            if (state == BattleState.BattleOver) 
                yield break;
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) 
                yield break;
        }

        if (state != BattleState.BattleOver)
            ActionSelection();
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();

        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.HUD.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);

        move.PP--;
        yield return dialogueBox.TypeDialogue($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}.");

        if (CheckIfMoveHit(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);

            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.HUD.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }      

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                }
            } 

            if (targetUnit.Pokemon.HP <= 0)
            {
                yield return HandlePokemonFainted(targetUnit);
            }
        }
        else
        {
            yield return dialogueBox.TypeDialogue($"{sourceUnit.Pokemon.Base.Name}'s attack missed.");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
    {
        //Stat boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        //Status condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        //Volatile status condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver)
            yield break;

        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        //Statuses like burn or psn will hurt the pokemon after the turn
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.HUD.UpdateHP();

        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return HandlePokemonFainted(sourceUnit);

            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    //Checks the accuracy and evasion of each pokemon to return if the move will hit or not
    bool CheckIfMoveHit(Move move, Pokemon source, Pokemon target)
    {
        //if the move can't miss, then return true
        if (move.Base.AlwaysHits)
            return true;

        //If not, then calculate the accuracy
        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = source.StatBoosts[Stat.Evasion];

        var boostValues = new float[] {1f,4f/3f,5f/3f,2f,7f/3f,8f/3f,3f};

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[-accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[-evasion];

        return UnityEngine.Random.Range(1,101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogueBox.TypeDialogue(message);
        }
    }

    IEnumerator HandlePokemonFainted(BattleUnit faintedUnit)
    {
        yield return dialogueBox.TypeDialogue($"{faintedUnit.Pokemon.Base.Name} fainted.");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        if(!faintedUnit.IsPlayerUnit)
        {
            //EXP Gain
            int expYield = faintedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faintedUnit.Pokemon.Level;

            //If is trainer battle gain 1.5f extra, otherwise, 1f
            float trainerBonus = (isTrainerBattle)? 1.5f : 1f;

            //Simple formula
            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Pokemon.Exp += expGain;
            yield return dialogueBox.TypeDialogue($"{playerUnit.Pokemon.Base.Name} gained {expGain} exp.");

            yield return playerUnit.HUD.SetExpSmooth();

            //Check level up
            while (playerUnit.Pokemon.CheckForLevelUp())
            {
                playerUnit.HUD.SetLevel();
                yield return dialogueBox.TypeDialogue($"{playerUnit.Pokemon.Base.Name} has leveled up to level {playerUnit.Pokemon.Level}!");

                //Try to learn new move
                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrentLevel();

                if (newMove != null)
                {
                    if (playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumberOfMoves)
                    {
                        //Add new move
                        playerUnit.Pokemon.LearnMove(newMove);
                        yield return dialogueBox.TypeDialogue($"{playerUnit.Pokemon.Base.Name} has learned {newMove.Base.Name}!");
                        dialogueBox.SetMoveNames(playerUnit.Pokemon.Moves);
                    }
                    else
                    {
                        //Forget move to learn new one
                        yield return dialogueBox.TypeDialogue($"{playerUnit.Pokemon.Base.Name} is trying to learn {newMove.Base.Name}.");
                        yield return dialogueBox.TypeDialogue($"But it cannot learn more than {PokemonBase.MaxNumberOfMoves} moves.");
                        yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);

                        //Wait until the player select a move, then end the battle
                        yield return new WaitUntil(() => state != BattleState.MoveToForget);
                        yield return new WaitForSeconds(2f);
                    }
                }

                yield return playerUnit.HUD.SetExpSmooth(true);
            }

            yield return new WaitForSeconds(1f);
        }

        CheckForBattleOver(faintedUnit);
    }

    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = playerParty.GetHealthyPokemon();            
            if (nextPokemon != null)
                OpenPartyScreen();
            else
                BattleOver(false);
        }
        else
        {
            if (!isTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextPokemon = trainerParty.GetHealthyPokemon();

                if (nextPokemon != null)
                    StartCoroutine(AboutToUse(nextPokemon));
                else
                    BattleOver(true);
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogueBox.TypeDialogue("A critical hit!");

        if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogueBox.TypeDialogue("It's super effective!");
        else if (damageDetails.TypeEffectiveness < 1f)
            yield return dialogueBox.TypeDialogue("It's not very effective.");
    }

    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if (state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
        else if (state == BattleState.MoveToForget)
        {
            Action<int> onMoveSelected = (moveIndex) =>
            {
                moveSelectionUI.gameObject.SetActive(false);
                if (moveIndex == PokemonBase.MaxNumberOfMoves)
                {
                    //Don't learn the new move
                    StartCoroutine(dialogueBox.TypeDialogue($"{playerUnit.Pokemon.Base.Name} did not learn {moveToLearn.Name}."));
                }
                else
                {
                    //Forget the selected move and learn new move
                    var selectedMove = playerUnit.Pokemon.Moves[moveIndex].Base;
                    StartCoroutine(dialogueBox.TypeDialogue($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}."));
                    
                    playerUnit.Pokemon.Moves[moveIndex] = new Move(moveToLearn);
                }

                moveToLearn = null;
                state = BattleState.RunningTurn;
            };

            moveSelectionUI.HandleMoveSelection(onMoveSelected);
        }
    }

    void HandleActionSelection()
    {
        //Action selection by keys logic
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentAction;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentAction;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentAction += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentAction -= 2;

        //The current action variable won't surpass the number of actions
        currentAction = Mathf.Clamp(currentAction, 0, 3);
        
        dialogueBox.UpdateActionSelection(currentAction);    

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (currentAction == 0)
            {
                //Fight
                MoveSelection();
            }
            else if (currentAction == 1)
            {
                //Bag
                StartCoroutine(RunTurns(BattleAction.UseItem));
            }
            else if (currentAction == 2)
            {
                //Pokemon
                prevState = state;
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                //Run
                StartCoroutine(RunTurns(BattleAction.Run));
            }
        }
    }

    void HandleMoveSelection()
    {
        //Move selection by keys logic
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMove;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMove;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMove += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMove -= 2;

        //The current move variable won't surpass the number of moves
        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);

        dialogueBox.UpdateMoveSelection(currentMove, playerUnit.Pokemon.Moves[currentMove]);

        //Selecting move
        if (Input.GetKeyDown(KeyCode.Z))
        {
            //Check if the move at least 1 pp to perform
            var move = playerUnit.Pokemon.Moves[currentMove];
            if(move.PP == 0)
                return;

            dialogueBox.EnableMovesSelector(false);
            dialogueBox.EnableDialogueText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogueBox.EnableMovesSelector(false);
            dialogueBox.EnableDialogueText(true);
            ActionSelection();
        }
    }

    void HandlePartySelection()
    {
        //Party selection by keys logic
        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++currentMember;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --currentMember;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            currentMember -= 2;

        //The current member variable won't surpass the number of party members
        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Pokemons.Count - 1);

        partyScreen.UpdateMemberSelection(currentMember);

        //Select the pokemon to switch
        if (Input.GetKeyDown(KeyCode.Z))
        {
            var selectedMember = playerParty.Pokemons[currentMember];

            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMessageText("You can't send out a fainted pokemon.");
                return;
            }

            if (selectedMember == playerUnit.Pokemon)
            {
                partyScreen.SetMessageText("You can't switch with the same pokemon.");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            //If the player chose to switch pokemon then...
            if (prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            }
            else //If the current pokemon fainted then...
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchPokemon(selectedMember));
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            if (playerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SetMessageText("You have to choose a pokemon to continue");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.AboutToUse)
                StartCoroutine(SendNextTrainerPokemon());
            else
                ActionSelection();
        }
    }

    void HandleAboutToUse()
    {
        //The boolean will toggle since we only have two options
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            aboutToUseChoice = !aboutToUseChoice;

        dialogueBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogueBox.EnableChoiceBox(false);
            if (aboutToUseChoice == true)
            {
                //Yes option
                prevState = BattleState.AboutToUse;
                OpenPartyScreen();
            }
            else
            {
                //No option
                StartCoroutine(SendNextTrainerPokemon());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogueBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator SwitchPokemon(Pokemon newPokemon)
    {
        //If the current pokemon has not fainted yet, the pokemon will come back to the party
        if (playerUnit.Pokemon.HP > 0)
        {
            yield return dialogueBox.TypeDialogue($"Come back {playerUnit.Pokemon.Base.Name}");
            playerUnit.PlayFaintAnimation(); //Using this animation while getting a new one

            yield return new WaitForSeconds(2f);
        }     

        //It will setup the data of the new pokemon in the UI and the HUD
        playerUnit.Setup(newPokemon);

        dialogueBox.SetMoveNames(newPokemon.Moves);

        yield return dialogueBox.TypeDialogue($"Go {newPokemon.Base.Name}!");

        if (prevState == null)
        {
            state = BattleState.RunningTurn;
        }
        else if (prevState == BattleState.AboutToUse)
        {
            prevState = null;
            StartCoroutine(SendNextTrainerPokemon());
        }
    }

    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleState.Busy;

        var nextPokemon = trainerParty.GetHealthyPokemon();

        enemyUnit.Setup(nextPokemon);
        yield return dialogueBox.TypeDialogue($"{trainer.Name} send out {nextPokemon.Base.Name}."); 

        state = BattleState.RunningTurn;
    }

    IEnumerator ThrowPokeball()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogueBox.TypeDialogue("You can't steal the trainers pokemon!");
            state = BattleState.RunningTurn;
            yield break;
        }

        yield return dialogueBox.TypeDialogue($"{player.Name} used POKEBALL!");

        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();

        //Animations
        //Throw
        yield return pokeball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        //Capture
        yield return enemyUnit.PlayCaptureAnimation();
        //Drop pokeball to platform
        yield return pokeball.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();
        //Shake 3 times
        int shakeCount = TryToCatchPokemon(enemyUnit.Pokemon);

        for (int i=0; i<Mathf.Min(shakeCount, 3); ++i)
        {
            yield return new WaitForSeconds(0.5f);
            yield return pokeball.transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.8f).WaitForCompletion();
        }

        if (shakeCount == 4)
        {
            //Pokemon is caught
            yield return dialogueBox.TypeDialogue($"{enemyUnit.Pokemon.Base.Name} was caught!");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogueBox.TypeDialogue($"{enemyUnit.Pokemon.Base.Name} has been added to your party.");

            Destroy(pokeball);
            BattleOver(true);
        }
        else
        {
            //Pokemon broke out
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakoutAnimation();

            if (shakeCount < 2)
                yield return dialogueBox.TypeDialogue($"{enemyUnit.Pokemon.Base.Name} broke free!");
            else
                yield return dialogueBox.TypeDialogue("Almost caught it!");
            
            Destroy(pokeball);
            state = BattleState.RunningTurn;
        }
    }

    int TryToCatchPokemon(Pokemon pokemon)
    {
        //GEN 3-4 ALGORITHYM
        float a = (3 * pokemon.MaxHp - 2 * pokemon.HP) * pokemon.Base.CatchRate * ConditionsDB.GetStatusBonus(pokemon.Status) / (3 * pokemon.MaxHp);

        if (a >= 255)
            return 4;
        
        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;

            ++shakeCount;
        }

        return shakeCount;
    }

    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogueBox.TypeDialogue("You can't run from trainer battles!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++escapeAttempts;

        //GEN 3-4 ALGORITHYM
        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogueBox.TypeDialogue("Ran away safely!");
            yield return new WaitForSeconds(1f);
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogueBox.TypeDialogue("Ran away safely!");
                yield return new WaitForSeconds(1f);
                BattleOver(true);
            }
            else
            {
                yield return dialogueBox.TypeDialogue("You couldn't escape!");
                yield return new WaitForSeconds(1f);
                state = BattleState.RunningTurn;
            }
        }
    }
}
