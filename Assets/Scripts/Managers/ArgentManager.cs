using System;
using UnityEngine;

// Le porte-monnaie de la sorcière (pièces). À mettre sur l'objet StockManager.
// Pour Lou-Ann (Budget, KAN-43) : quand un client paie → ArgentManager.Instance.Gagner(prix)
public class ArgentManager : MonoBehaviour
{
    public static ArgentManager Instance { get; private set; }

    [Tooltip("Pièces au début de la partie (à équilibrer).")]
    public int budgetDepart = 50;

    public int Pieces { get; private set; }

    // Prévient les autres scripts quand l'argent change (nouveau total)
    public event Action<int> ArgentChange;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        Pieces = budgetDepart;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool PeutPayer(int montant)
    {
        return montant >= 0 && Pieces >= montant;
    }

    // Retire des pièces. Renvoie false s'il n'y en a pas assez.
    public bool Depenser(int montant)
    {
        if (!PeutPayer(montant)) return false;
        Pieces -= montant;
        ArgentChange?.Invoke(Pieces);
        return true;
    }

    public void Gagner(int montant)
    {
        if (montant <= 0) return;
        Pieces += montant;
        ArgentChange?.Invoke(Pieces);
    }

    [ContextMenu("Test : +20 pièces")]
    void TestGagner() { Gagner(20); }
}
