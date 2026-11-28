using System;
using UnityEngine;

// Dit si l'option "empoisonner" est débloquée au niveau en cours.
// Utilisation dans les autres scripts : if (PoisonManager.PoisonDebloque) { ... }
// À mettre sur le même objet que le StockManager.
public class PoisonManager : MonoBehaviour
{
    public static PoisonManager Instance { get; private set; }

    [Tooltip("Glisse ici la fiche reglages_poison (Assets/Data).")]
    public ReglagesPoison reglages;

    [Header("Test (en attendant le GameManager)")]
    [Tooltip("Niveau utilisé quand on lance le jeu, pour tester.")]
    [Range(1, 10)] public int niveauDeDepart = 1;

    // Se déclenche au moment où on atteint le niveau de déblocage (pour un message à l'écran, un son...)
    public static event Action PoisonVientDEtreDebloque;

    public static int NiveauDeblocage =>
        Instance != null && Instance.reglages != null ? Instance.reglages.niveauDeblocage : 5;

    public static bool PoisonDebloque => Progression.NiveauActuel >= NiveauDeblocage;

    // Le manager paie plus cher les potions empoisonnées (réglable dans reglages_poison)
    public static float MultiplicateurPrix =>
        Instance != null && Instance.reglages != null ? Instance.reglages.multiplicateurPrix : 2f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (reglages == null)
            Debug.LogWarning("[Poison] Aucune fiche Réglages poison reliée : niveau 5 utilisé par défaut.");

        Progression.NiveauChange += SurNiveauChange;
        Progression.ChangerNiveau(niveauDeDepart);
        Debug.Log($"[Poison] Niveau {Progression.NiveauActuel} - empoisonner débloqué au niveau {NiveauDeblocage} : {(PoisonDebloque ? "OUI" : "non")}");
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Progression.NiveauChange -= SurNiveauChange;
        Instance = null;
    }

    void SurNiveauChange(int niveau)
    {
        Debug.Log($"[Poison] Passage au niveau {niveau}");
        if (niveau == NiveauDeblocage)
        {
            Debug.Log("[Poison] Option EMPOISONNER débloquée !");
            PoisonVientDEtreDebloque?.Invoke();
        }
    }

    // ---------- Boutons de test (clic sur les 3 points du composant, en mode Play) ----------
    [ContextMenu("Test : niveau suivant")]
    void TestNiveauSuivant() { Progression.ChangerNiveau(Progression.NiveauActuel + 1); }

    [ContextMenu("Test : revenir au niveau 1")]
    void TestNiveau1() { Progression.ChangerNiveau(1); }
}