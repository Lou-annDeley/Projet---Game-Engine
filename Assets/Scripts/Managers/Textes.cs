using UnityEngine;

// Petits outils de texte et de couleur communs au comptoir et à la cuisine.
public static class Textes
{
    // « une soupe de citrouille », « un élixir de chance », « un baume vert »
    public static string AvecArticle(string nom)
    {
        if (string.IsNullOrEmpty(nom)) return "";
        string premierMot = nom.Split(' ')[0];
        bool feminin = premierMot == "Soupe" || premierMot == "Potion" || premierMot == "Tisane";
        return (feminin ? "une " : "un ") + char.ToLower(nom[0]) + nom.Substring(1);
    }

    // Une soupe se sert dans un bol, le reste (potions, remèdes) dans une fiole
    public static bool DansUnBol(RecetteData recette) =>
        recette != null && recette.categorie == CategorieRecette.Soupe;

    public static Color CouleurPreparation(Potion potion)
    {
        if (potion.empoisonnee) return Palette.VertPoison;
        if (potion.remede != null) return Palette.BleuBrume;
        if (potion.recette == null) return Palette.BoisSombre;
        return DansUnBol(potion.recette) ? Palette.Hex("#D9822B") : Palette.MagentaPotion;
    }
}
