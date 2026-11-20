using System;
using UnityEngine;

// Numéro du niveau en cours.
// Provisoire en attendant le GameManager (KAN-28) : Syrine n'aura qu'à appeler
// Progression.ChangerNiveau(numero) au début de chaque niveau.
public static class Progression
{
    public static int NiveauActuel { get; private set; } = 1;

    // Prévient les autres scripts quand le niveau change (nouveau numéro)
    public static event Action<int> NiveauChange;

    public static void ChangerNiveau(int niveau)
    {
        niveau = Mathf.Max(1, niveau);
        if (niveau == NiveauActuel) return;
        NiveauActuel = niveau;
        NiveauChange?.Invoke(niveau);
    }
}
