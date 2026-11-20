using UnityEngine;

// Fiche de réglages de l'empoisonnement.
// Pour changer le niveau où l'option se débloque : clique sur la fiche
// (Assets/Data/reglages_poison) et change le chiffre dans l'Inspector. Aucun code.
[CreateAssetMenu(fileName = "reglages_poison", menuName = "3Witches/Réglages poison", order = 1)]
public class ReglagesPoison : ScriptableObject
{
    [Tooltip("Niveau à partir duquel les ingrédients toxiques apparaissent (le jeu a 10 niveaux).")]
    [Range(1, 10)] public int niveauDeblocage = 5;
}
