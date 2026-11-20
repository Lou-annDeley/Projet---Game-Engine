using System.Collections.Generic;
using UnityEngine;

public enum SiRemedeRate { PlusMalade, Meurt }

// Fiche d'un remède (onglet « Remèdes » de l'Excel KAN-22).
// Nouveau remède sans code : clic droit > Create > 3Witches > Remède
[CreateAssetMenu(fileName = "rem_nouveau", menuName = "3Witches/Remède", order = 2)]
public class RemedeData : ScriptableObject
{
    [Header("Identité")]
    public string id = "REM00";
    public string nom = "Nouveau remède";

    [Header("Maladie soignée")]
    [Tooltip("Ce que le client a quand il revient (ex : Peau verdâtre).")]
    public string anomalie;
    [Tooltip("Code de l'anomalie pour les sprites des clients (ex : peau_verte → client_meunier_peau_verte.png).")]
    public string codeAnomalie;
    [Tooltip("L'ingrédient toxique qui provoque cette anomalie.")]
    public IngredientData causePoison;

    [Header("Recette du remède")]
    public List<IngredientData> ingredients = new List<IngredientData>();
    public int tempsPreparation = 20;
    public int prixVente = 12;
    public int niveauDeblocage = 3;

    [Header("Si le remède est raté ou toxique")]
    public SiRemedeRate siRate = SiRemedeRate.PlusMalade;

    // Bien préparé = exactement les bons ingrédients (l'ordre ne compte pas), rien de toxique
    public bool EstBienPrepare(IReadOnlyList<IngredientData> contenu)
    {
        if (contenu == null || contenu.Count != ingredients.Count) return false;
        var reste = new List<IngredientData>(ingredients);
        foreach (var ing in contenu)
        {
            if (ing == null || ing.EstToxique) return false;
            if (!reste.Remove(ing)) return false;
        }
        return reste.Count == 0;
    }

    public string ListeIngredients()
    {
        return string.Join(" + ", ingredients.ConvertAll(i => i != null ? i.nom : "?"));
    }
}
