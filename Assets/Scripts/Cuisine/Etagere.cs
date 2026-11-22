using System.Collections.Generic;
using UnityEngine;

// Crée automatiquement un bac (IngredientSlot) par ingrédient, rangés en grille.
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

    [Header("Bacs (vue de dessus)")]
    [Tooltip("Sprite du bac vide. Vide = les ingrédients sont affichés seuls, comme avant.")]
    public Sprite spriteBac;
    [Tooltip("Largeur d'un bac en unités (remplace Taille Bocal quand un sprite de bac est mis).")]
    public float tailleBac = 1.35f;
    [Tooltip("Centrer chaque rangée sur la position de l'étagère (sinon : rangées alignées à gauche).")]
    public bool centrerRangees = true;

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

        var visibles = liste.FindAll(EstVisible);
        float echelle = spriteBac != null ? tailleBac / spriteBac.bounds.size.x : tailleBocal;

        for (int place = 0; place < visibles.Count; place++)
        {
            var ing = visibles[place];
            int col = place % colonnes, rang = place / colonnes;
            float x = col * espacement;
            if (centrerRangees)
            {
                int dansCeRang = Mathf.Min(colonnes, visibles.Count - rang * colonnes);
                x = (col - (dansCeRang - 1) / 2f) * espacement;
            }
            var go = new GameObject((spriteBac != null ? "Bac_" : "Bocal_") + ing.id);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(x, -rang * espacement, 0f);
            go.transform.localScale = Vector3.one * echelle;
            go.AddComponent<IngredientSlot>().Initialiser(ing, spriteBac);
        }
    }
}
