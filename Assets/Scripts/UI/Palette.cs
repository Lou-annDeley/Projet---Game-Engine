using UnityEngine;

// Les couleurs du document "Direction artistique".
// Rappel : le vert est réservé au poison.
public static class Palette
{
    public static readonly Color NuitViolette = Hex("#1C092C");
    public static readonly Color Prune = Hex("#361C40");
    public static readonly Color PierreLavande = Hex("#493F56");
    public static readonly Color BoisSombre = Hex("#403031");
    public static readonly Color BoisRouge = Hex("#704941");
    public static readonly Color Cuivre = Hex("#A5624D");
    public static readonly Color Bougie = Hex("#FAE4B1");
    public static readonly Color Parchemin = Hex("#EBD6AB");
    public static readonly Color Laiton = Hex("#C88F60");
    public static readonly Color MagentaPotion = Hex("#BF4794");
    public static readonly Color VioletMagique = Hex("#693287");
    public static readonly Color Lavande = Hex("#9B7DA0");
    public static readonly Color BleuBrume = Hex("#597CB0");
    public static readonly Color VertPoison = Hex("#6BA027");
    public static readonly Color Encre = Hex("#251C0D");
    public static readonly Color Contour = Hex("#251A1C");

    // Jauge de patience : couleurs provisoires (à valider en équipe, sans vert)
    public static readonly Color PatienceCalme = Lavande;
    public static readonly Color PatienceEnerve = Laiton;
    public static readonly Color PatienceFurieux = Hex("#A8324A");

    // Bocal vide ou grisé
    public static readonly Color Epuise = Hex("#5A5560");

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color couleur);
        return couleur;
    }

    public static Color AvecAlpha(Color couleur, float alpha)
    {
        couleur.a = alpha;
        return couleur;
    }

    public static Color CouleurPatience(Humeur humeur)
    {
        switch (humeur)
        {
            case Humeur.Calme: return PatienceCalme;
            case Humeur.Enerve: return PatienceEnerve;
            default: return PatienceFurieux;
        }
    }
}
