using System.Collections.Generic;
using UnityEngine;

// "Fiche" d'un client (KAN-37) : sprite, nom, répliques, patience,
// commandes possibles et indicateur de fée cachée.
// Créés par le menu 3Witches > Créer les clients
[CreateAssetMenu(fileName = "client_nouveau", menuName = "3Witches/Client", order = 2)]
public class ClientData : ScriptableObject
{
    [Header("Identité")]
    public string id = "CLI00";
    public string nom = "Nouveau client";
    [Tooltip("Ce qu'on lit sur le ticket : « le meunier », « la boulangère »...")]
    public string metier = "le voyageur";

    [Header("Sprites (une expression par étape de patience)")]
    [Tooltip("client_nom_calme.png")] public Sprite spriteCalme;
    [Tooltip("client_nom_enerve.png")] public Sprite spriteEnerve;
    [Tooltip("client_nom_furieux.png")] public Sprite spriteFurieux;
    [Tooltip("Couleur des formes provisoires tant qu'il n'y a pas de sprite (jamais de vert)")]
    public Color couleurProvisoire = new Color(0.35f, 0.49f, 0.69f);

    [Header("Répliques ({recette} = « une soupe de citrouille »)")]
    [TextArea(1, 3)] public string[] repliquesCommande = { "Bonsoir ! Je voudrais {recette}, s'il vous plaît." };
    [TextArea(1, 3)] public string repliqueRetour = "Re-bonjour ! Je reviens vous voir.";
    [TextArea(1, 3)] public string repliqueContent = "Merci, c'est parfait !";
    [TextArea(1, 3)] public string repliqueMecontent = "Ce n'est pas ce que j'ai demandé !";
    [TextArea(1, 3)] public string repliqueEmpoisonne = "Mmh... ça a un drôle de goût.";
    [TextArea(1, 3)] public string repliqueImpatient = "J'en ai assez d'attendre !";

    [Header("Gameplay")]
    [Tooltip("1 = normal, moins de 1 = pressé, plus de 1 = patient")]
    [Range(0.5f, 2f)] public float facteurPatience = 1f;
    [Tooltip("Recettes que ce client commande (vide = toutes celles du niveau)")]
    public List<RecetteData> commandesPossibles = new List<RecetteData>();
    [Tooltip("Fée cachée parmi les clients (KAN-60 / KAN-61)")]
    public bool estFeeCachee;
    [Tooltip("Premier niveau où ce client vient à la boutique")]
    public int niveauApparition = 1;

    public string NomComplet => nom + ", " + metier;

    public Sprite SpritePour(Humeur humeur)
    {
        if (humeur == Humeur.Furieux && spriteFurieux != null) return spriteFurieux;
        if (humeur != Humeur.Calme && spriteEnerve != null) return spriteEnerve;
        return spriteCalme;
    }
}
