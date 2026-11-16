using System.Collections.Generic;
using UnityEngine;

// Crée automatiquement un bocal (IngredientSlot) par ingrédient, rangés en grille.
// Ajouter un ingrédient = l'ajouter à la liste, aucun code.
public class Etagere : MonoBehaviour
{
    [Tooltip("Laisse vide pour afficher tous les ingrédients du StockManager.")]
    public List<IngredientData> ingredients = new List<IngredientData>();
    public int colonnes = 6;
    public float espacement = 2f;
    public float tailleBocal = 1f;

    void Start()
    {
        List<IngredientData> liste = ingredients;
        if (liste.Count == 0 && StockManager.Instance != null)
            liste = StockManager.Instance.ingredients;

        for (int i = 0; i < liste.Count; i++)
        {
            if (liste[i] == null) continue;
            var go = new GameObject("Bocal_" + liste[i].id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3((i % colonnes) * espacement, -(i / colonnes) * espacement, 0f);
            go.transform.localScale = Vector3.one * tailleBocal;
            go.AddComponent<IngredientSlot>().Initialiser(liste[i]);
        }
    }
}
