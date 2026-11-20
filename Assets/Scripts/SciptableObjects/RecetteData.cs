using System.Collections.Generic;
using UnityEngine;

public enum CategorieRecette { Soupe, Potion }

// Fiche d'une recette (onglet « Recettes » de l'Excel KAN-22).
// Nouvelle recette sans code : clic droit > Create > 3Witches > Recette
[CreateAssetMenu(fileName = "rec_nouvelle", menuName = "3Witches/Recette", order = 1)]
public class RecetteData : ScriptableObject
{
    [Header("Identité")]
    public string id = "REC00";
    public string nom = "Nouvelle recette";
    public CategorieRecette categorie = CategorieRecette.Soupe;

    [Header("Recette")]
    public List<IngredientData> ingredients = new List<IngredientData>();
    public int tempsPreparation = 20;
    public int prixVente = 10;
    public int niveauDeblocage = 1;

    [Header("Empoisonnement")]
    [Tooltip("L'ingrédient toxique qui remplace un ingrédient de cette recette pour l'empoisonner.")]
    public IngredientData ingredientPoison;

    // Bien préparée = exactement les bons ingrédients (l'ordre ne compte pas)
    public bool EstBienPreparee(IReadOnlyList<IngredientData> contenu)
    {
        if (contenu == null || contenu.Count != ingredients.Count) return false;
        var reste = new List<IngredientData>(ingredients);
        foreach (var ing in contenu)
            if (ing == null || !reste.Remove(ing)) return false;
        return reste.Count == 0;
    }
}
