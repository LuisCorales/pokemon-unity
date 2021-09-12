using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleHUD : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Image type1;
    [SerializeField] Image type2;
    [SerializeField] Image statusImage;
    [SerializeField] GameObject expBar;

    //Types colors
    #region 
    Color normalColor = new Color32(170,170,153,255);
    Color fireColor = new Color32(255,68,34,255);
    Color waterColor = new Color32(51,153,255,255);
    Color electricColor = new Color32(255,204,51,255);
    Color grassColor = new Color32(119,204,85,255);
    Color iceColor = new Color32(102,204,255,255);
    Color fightingColor = new Color32(187,85,68,255);
    Color poisonColor = new Color32(170,85,153,255);
    Color groundColor = new Color32(221,187,85,255);
    Color flyingColor = new Color32(136,153,255,255);
    Color psychicColor = new Color32(239,152,237,255);
    Color bugColor = new Color32(170,187,34,255);
    Color rockColor = new Color32(187,170,102,255);
    Color ghostColor = new Color32(102,102,187,255);
    Color dragonColor = new Color32(119,102,238,255);
    Color darkColor = new Color32(119,85,68,255);
    Color steelColor = new Color32(170,170,187,255);
    Color fairyColor = new Color32(238,153,238,255);
    #endregion

    //Statuses colors
    Color psnColor;
    Color brnColor;
    Color parColor;
    Color slpColor;
    Color frzColor;
    Color toxColor;

    Pokemon _pokemon;

    Dictionary<ConditionID, Color> statusColors;
    Dictionary<PokemonType, Color> typeColors;
    
    void Awake()
    {
        psnColor = poisonColor;
        brnColor = fireColor;
        parColor = electricColor;
        slpColor = normalColor;
        frzColor = iceColor;
        toxColor = poisonColor;
    }

    public void SetData(Pokemon pokemon)
    {
        _pokemon = pokemon;

        nameText.text = pokemon.Base.Name;
        SetLevel();
        hpBar.SetHP((float) pokemon.HP / pokemon.MaxHp);
        SetExp();

        //Set the colors according to the pokemon type
        typeColors = new Dictionary<PokemonType, Color>()
        {
            {PokemonType.Normal, normalColor},
            {PokemonType.Fire, fireColor},
            {PokemonType.Water, waterColor},
            {PokemonType.Electric, electricColor},
            {PokemonType.Grass, grassColor},
            {PokemonType.Ice, iceColor},
            {PokemonType.Fighting, fightingColor},
            {PokemonType.Poison, poisonColor},
            {PokemonType.Ground, groundColor},
            {PokemonType.Flying, flyingColor},
            {PokemonType.Psychic, psychicColor},
            {PokemonType.Bug, bugColor},
            {PokemonType.Rock, rockColor},
            {PokemonType.Ghost, ghostColor},
            {PokemonType.Dragon, dragonColor},
            {PokemonType.Dark, darkColor},
            {PokemonType.Steel, steelColor},
            {PokemonType.Fairy, fairyColor}
        };

        SetTypeColor();

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColor},
            {ConditionID.brn, brnColor},
            {ConditionID.par, parColor},
            {ConditionID.slp, slpColor},
            {ConditionID.frz, frzColor},
            {ConditionID.tox, toxColor}
        };

        SetStatusText();
        _pokemon.OnStatusChanged += SetStatusText;
    }

    void SetTypeColor()
    {
        type1.color = typeColors[_pokemon.Base.Type1];

        if (_pokemon.Base.Type2 == PokemonType.None)
        {
            type2.gameObject.SetActive(false);
        }
        else
        {
            type2.gameObject.SetActive(true);
            type2.color = typeColors[_pokemon.Base.Type2];
        }
    }

    void SetStatusText()
    {
        if (_pokemon.Status == null)
        {
            statusImage.gameObject.SetActive(false);
        }
        else
        {
            statusImage.gameObject.SetActive(true);
            statusText.text = _pokemon.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_pokemon.Status.Id];
        }
    }

    public void SetLevel()
    {
        levelText.text = "Lvl. " + _pokemon.Level;
    }

    public void SetExp()
    {
        if (expBar == null)
            return;
        
        float normalizedExp = GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
    }

    public IEnumerator SetExpSmooth(bool reset = false)
    {
        if (expBar == null)
            yield break;

        //If leveled up, reset the bar
        if (reset)
            expBar.transform.localScale = new Vector3(0, 1, 1);
        
        float normalizedExp = GetNormalizedExp();
        
        yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    float GetNormalizedExp()
    {
        int currentLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level);
        int nextLevelExp = _pokemon.Base.GetExpForLevel(_pokemon.Level + 1);

        float normalizedExp = (float) (_pokemon.Exp - currentLevelExp) / (nextLevelExp - currentLevelExp);

        return Mathf.Clamp01(normalizedExp);
    }

    public IEnumerator UpdateHP()
    {
        //Will only update the HP bar if the HP changed
        if(_pokemon.HpChanged)
        {
            yield return hpBar.SetHPSmooth((float)_pokemon.HP / _pokemon.MaxHp);
            _pokemon.HpChanged = false;
        }
    }
}
