using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Image type1;
    [SerializeField] Image type2;
    
    [SerializeField] Color highlightedColor;

    Pokemon _pokemon;

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
        levelText.text = "Lvl. " + pokemon.Level;
        hpBar.SetHP((float) pokemon.HP / pokemon.MaxHp);

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

        /* algun dia mostrar en el party selection si estan con status
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
        */
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

    public void SetSelected(bool selected)
    {
        if (selected)
            nameText.color = highlightedColor;
        else
            nameText.color = Color.black;
    }
}
