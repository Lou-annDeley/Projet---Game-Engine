using System.Collections.Generic;
using UnityEngine;

// Crée automatiquement un bocal (IngredientSlot) par ingrédient, rangés en grille.
// Ajouter un ingrédient = l'ajouter à la liste, aucun code.
// N'affiche que les ingrédients débloqués au niveau en cours,
// et les ingrédients toxiques seulement quand l'option empoisonner est débloquée.
public class Etagere : MonoBehaviour
{
    [Tooltip("Laisse vide pour afficher tous les ingrédients du StockManager.")]
    public List<IngredientData> ingredients = new List<IngredientData>();
    public int colonnes = 6;
    public float espacement = 2f;
    public float tailleBocal = 1f;

    [Tooltip("Cacher les ingrédients dont le niveau de déblocage (dans leur fiche) n'est pas encore atteint.")]
    public bool respecterNiveauDeblocage = true;

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

    bool EstVisible(IngredientData ing)
    {
        if (ing == null) return false;
        if (ing.EstToxique && !PoisonManager.PoisonDebloque) return false; // option empoisonner pas encore débloquée
        if (respecterNiveauDeblocage && ing.niveauDeblocage > Progression.NiveauActuel) return false;
        return true;
    }

    public void Construire()
    {
        // On enlève les anciens bocaux
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        List<IngredientData> liste = ingredients;
        if (liste.Count == 0 && StockManager.Instance != null)
            liste = StockManager.Instance.ingredients;

        int place = 0;
        foreach (var ing in liste)
        {
            if (!EstVisible(ing)) continue;
            var go = new GameObject("Bocal_" + ing.id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((place % colonnes) * espacement, -(place / colonnes) * espacement, 0f);
            go.transform.localScale = Vector3.one * tailleBocal;
            go.AddComponent<IngredientSlot>().Initialiser(ing);
            place++;
        }
    }
}
