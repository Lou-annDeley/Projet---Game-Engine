using System;
using System.Collections.Generic;
using UnityEngine;

// Garde le stock de chaque ingrédient et le conserve entre les scènes.
// (À relier au GameManager de Syrine quand il existera.)
public class StockManager : MonoBehaviour
{
    public static StockManager Instance { get; private set; }

    [Tooltip("Glisse ici tous les ingrédients du jeu (Assets/Data/Ingredients).")]
    public List<IngredientData> ingredients = new List<IngredientData>();

    readonly Dictionary<IngredientData, int> stock = new Dictionary<IngredientData, int>();

    // Prévient les autres scripts quand une quantité change (ingrédient, nouvelle quantité)
    public event Action<IngredientData, int> StockChange;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RemplirStock();
    }

    // Remet chaque ingrédient à son stock de départ
    public void RemplirStock()
    {
        stock.Clear();
        foreach (var ing in ingredients)
            if (ing != null) stock[ing] = ing.stockDepart;
    }

    public int Quantite(IngredientData ing)
    {
        return ing != null && stock.TryGetValue(ing, out int q) ? q : 0;
    }

    // Retire du stock. Renvoie false s'il n'y en a pas assez.
    public bool Retirer(IngredientData ing, int quantite = 1)
    {
        if (Quantite(ing) < quantite) return false;
        stock[ing] -= quantite;
        StockChange?.Invoke(ing, stock[ing]);
        return true;
    }

    // Ajoute au stock (achat en Boutique, récompense de fée...)
    public void Ajouter(IngredientData ing, int quantite = 1)
    {
        if (ing == null) return;
        stock[ing] = Quantite(ing) + quantite;
        StockChange?.Invoke(ing, stock[ing]);
    }
}
