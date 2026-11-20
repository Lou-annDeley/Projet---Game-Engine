using System.Collections.Generic;
using UnityEngine;

// L'étal de la Boutique : affiche en grille les ingrédients qu'on peut acheter.
// Mêmes règles que l'étagère : niveau de déblocage, toxiques seulement si l'option empoisonner est débloquée.
// Les ingrédients Secret et Magique ne sont pas en vente (récompenses).
public class EtalBoutique : MonoBehaviour
{
    [Tooltip("Laisse vide pour vendre tous les ingrédients du StockManager.")]
    public List<IngredientData> ingredients = new List<IngredientData>();
    public int colonnes = 5;
    public float espacementX = 3.2f;
    public float espacementY = 2.5f;
    public float tailleArticle = 1f;

    void Start()
    {
        Progression.NiveauChange += SurNiveauChange;
        Construire();
    }

    void OnDestroy()
    {
        Progression.NiveauChange -= SurNiveauChange;
    }

    void SurNiveauChange(int niveau)
    {
        Construire();
    }

    bool EstEnVente(IngredientData ing)
    {
        if (ing == null || ing.prixAchat <= 0) return false;
        if (ing.type == TypeIngredient.Secret || ing.type == TypeIngredient.Magique) return false;
        if (ing.EstToxique && !PoisonManager.PoisonDebloque) return false;
        if (ing.niveauDeblocage > Progression.NiveauActuel) return false;
        return true;
    }

    public void Construire()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        List<IngredientData> liste = ingredients;
        if (liste.Count == 0 && StockManager.Instance != null)
            liste = StockManager.Instance.ingredients;

        int place = 0;
        foreach (var ing in liste)
        {
            if (!EstEnVente(ing)) continue;
            var go = new GameObject("Article_" + ing.id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((place % colonnes) * espacementX, -(place / colonnes) * espacementY, 0f);
            go.transform.localScale = Vector3.one * tailleArticle;
            go.AddComponent<ArticleBoutique>().Initialiser(ing);
            place++;
        }
    }
}
