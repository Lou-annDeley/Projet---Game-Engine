using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Le chaudron reçoit les ingrédients glissés depuis l'étagère.
// Il a besoin d'un Collider 2D (ex. Circle Collider 2D) pour détecter le dépôt.
public class Chaudron : MonoBehaviour
{
    public int capaciteMax = 4;
    [Tooltip("Le sprite du liquide. Vide = le sprite de cet objet.")]
    public SpriteRenderer liquide;
    public Color couleurNormale = new Color32(0xBF, 0x47, 0x94, 0xFF); // Magenta potion
    public Color couleurPoison  = new Color32(0x6B, 0xA0, 0x27, 0xFF); // Vert poison

    readonly List<IngredientData> contenu = new List<IngredientData>();

    public IReadOnlyList<IngredientData> Contenu => contenu;
    public bool PeutAjouter => contenu.Count < capaciteMax;
    public bool EstEmpoisonne => contenu.Exists(i => i != null && i.EstToxique);

    // Prévient les autres scripts (calcul du résultat KAN-34, UI...) quand le contenu change
    public event Action ContenuChange;

    Vector3 tailleDeBase;

    void Start()
    {
        if (liquide == null) liquide = GetComponent<SpriteRenderer>();
        tailleDeBase = transform.localScale;
        MettreAJourCouleur();
    }

    public bool AjouterIngredient(IngredientData ing)
    {
        if (ing == null || !PeutAjouter) return false;
        contenu.Add(ing);
        Debug.Log($"[Chaudron] + {ing.nom}  ({contenu.Count}/{capaciteMax})  stock restant : {StockManager.Instance.Quantite(ing)}");
        MettreAJourCouleur();
        StopAllCoroutines();
        StartCoroutine(Plouf());
        ContenuChange?.Invoke();
        return true;
    }

    // Vide le chaudron (les ingrédients sont perdus). Peut être relié à un bouton UI.
    [ContextMenu("Vider")]
    public void Vider()
    {
        contenu.Clear();
        MettreAJourCouleur();
        ContenuChange?.Invoke();
    }

    void MettreAJourCouleur()
    {
        if (liquide == null) return;
        liquide.color = EstEmpoisonne ? couleurPoison : couleurNormale;
        // Liquide séparé (enfant) : caché tant que le chaudron est vide
        if (liquide.gameObject != gameObject) liquide.enabled = contenu.Count > 0;
    }

    // Petit rebond quand un ingrédient tombe dedans
    IEnumerator Plouf()
    {
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            float s = 1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.12f;
            transform.localScale = tailleDeBase * s;
            yield return null;
        }
        transform.localScale = tailleDeBase;
    }
}
