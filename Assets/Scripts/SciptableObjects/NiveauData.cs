using System.Collections.Generic;
using UnityEngine;

public enum Difficulte { Facile, Moyen, Difficile, TresDifficile }

// "Fiche" d'un niveau : une ligne de la feuille "Liste des niveaux" (KAN-23).
[CreateAssetMenu(fileName = "niveau_00", menuName = "3Witches/Niveau", order = 3)]
public class NiveauData : ScriptableObject
{
    [Header("Identité")]
    public int numero = 1;
    public string nom = "Nouveau niveau";
    public Difficulte difficulte = Difficulte.Facile;
    [TextArea(2, 4)] public string nouveautes;
    [TextArea(2, 4)] public string particularite;

    [Header("Clients et objectif")]
    public int nombreClients = 5;
    public int objectifClients = 3;
    [Tooltip("Commandes en même temps (4 au maximum)")]
    [Range(1, 4)] public int commandesMax = 1;

    [Header("Temps (en secondes)")]
    [Tooltip("Durée de la journée : après, plus aucun client n'entre")]
    public float dureeSecondes = 240f;
    [Tooltip("Patience de base d'un client (multipliée par celle du client)")]
    public float patienceSecondes = 90f;
    [Tooltip("Temps entre deux arrivées de clients")]
    public float delaiEntreClients = 4f;

    [Header("Contenu")]
    [Tooltip("Recettes que les clients peuvent commander à ce niveau. Les ingrédients affichés en cuisine " +
             "viennent de l'étagère de Sarah (niveau de déblocage + option empoisonner).")]
    public List<RecetteData> recettes = new List<RecetteData>();

    [Tooltip("Démo guidée au début du niveau (prévue pour le niveau 1)")]
    public bool avecDemo;

    public string TexteDifficulte
    {
        get
        {
            switch (difficulte)
            {
                case Difficulte.Facile: return "Facile";
                case Difficulte.Moyen: return "Moyen";
                case Difficulte.Difficile: return "Difficile";
                default: return "Très difficile";
            }
        }
    }
}
