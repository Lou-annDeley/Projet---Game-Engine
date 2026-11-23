// Démo du début du niveau 1 (KAN-80) : on sert un premier client guidé pas à pas.
public enum EtapeDemo
{
    Bienvenue,
    Patience,
    AllerCuisine,
    Grimoire,
    Ingredients,
    Preparer,
    RetourComptoir,
    Servir,
    Fin,
    Terminee
}

public static class Demo
{
    public const int NombreEtapes = 9;

    public static string Scene(EtapeDemo etape)
    {
        switch (etape)
        {
            case EtapeDemo.Grimoire:
            case EtapeDemo.Ingredients:
            case EtapeDemo.Preparer:
            case EtapeDemo.RetourComptoir:
                return GameManager.SceneCuisine;
            default:
                return GameManager.SceneComptoir;
        }
    }

    // Étapes où l'on lit puis clique sur "Suivant"
    public static bool AttendUnClic(EtapeDemo etape)
    {
        return etape == EtapeDemo.Bienvenue || etape == EtapeDemo.Patience || etape == EtapeDemo.Fin;
    }

    public static string Texte(EtapeDemo etape)
    {
        switch (etape)
        {
            case EtapeDemo.Bienvenue:
                return "Bienvenue dans ma boutique ! Un premier client est arrivé : " +
                       "il passe commande dans sa bulle. On joue à la souris ou au clavier.";
            case EtapeDemo.Patience:
                return "Sa commande est accrochée en haut à droite. Les trois points, c'est sa patience : " +
                       "calme, énervé, furieux... puis il part sans payer. Là, il t'attend.";
            case EtapeDemo.AllerCuisine:
                return "Allons préparer ça. Clique sur le ruban « Cuisine » (ou touche Tab).";
            case EtapeDemo.Grimoire:
                return "Voici la cuisine. Ouvre le grimoire avec son ruban en bas à droite pour lire la recette (ou touche G).";
            case EtapeDemo.Ingredients:
                return "Ferme le grimoire et glisse les bons ingrédients des bacs dans le chaudron. " +
                       "Le parchemin les coche. Une erreur ? « Vider » (Retour arrière).";
            case EtapeDemo.Preparer:
                return "Tout y est ! Clique sur « Préparer » (ou Espace) et attends que ça bouille.";
            case EtapeDemo.RetourComptoir:
                return "C'est prêt. Retourne au comptoir avec le ruban « Comptoir » (ou touche Tab).";
            case EtapeDemo.Servir:
                return "Fais sonner la cloche pour servir le client (ou Espace).";
            case EtapeDemo.Fin:
                return "Voilà le métier ! Les autres clients arrivent. Garde un œil sur l'horloge... " +
                       "et souviens-toi : le vert, c'est du poison.";
            default:
                return "";
        }
    }
}
