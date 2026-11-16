using System.IO;
using UnityEditor;
using UnityEngine;

// Outil d'éditeur : crée les 18 fiches d'ingrédients de l'Excel (KAN-22) en un clic.
// Menu en haut de Unity : 3Witches > Créer les ingrédients de l'Excel
public static class ImportIngredients
{
    const string Dossier = "Assets/Data/Ingredients";
    const string DossierSprites = "Assets/Sprites/Ingredients";

    struct Ligne
    {
        public string id, nom, fichier, desc, obtention;
        public TypeIngredient type;
        public int prix, stock, niveau;
        public Ligne(string id, string nom, string fichier, TypeIngredient type, string desc, int prix, int stock, int niveau, string obtention)
        {
            this.id = id; this.nom = nom; this.fichier = fichier; this.type = type; this.desc = desc;
            this.prix = prix; this.stock = stock; this.niveau = niveau; this.obtention = obtention;
        }
    }

    static readonly Ligne[] donnees =
    {
        new Ligne("ING01", "Herbe lunaire", "ingr_herbe_lunaire", TypeIngredient.Normal, "Base de beaucoup de potions, douce et apaisante", 2, 10, 1, "Boutique"),
        new Ligne("ING02", "Champignon des bois", "ingr_champignon_des_bois", TypeIngredient.Normal, "Base des soupes, goût terreux", 2, 10, 1, "Boutique"),
        new Ligne("ING03", "Citrouille", "ingr_citrouille", TypeIngredient.Normal, "Base de soupe épaisse et réconfortante", 3, 8, 1, "Boutique"),
        new Ligne("ING04", "Miel", "ingr_miel", TypeIngredient.Normal, "Adoucit les recettes, goût sucré", 3, 8, 1, "Boutique"),
        new Ligne("ING05", "Baie rouge", "ingr_baie_rouge", TypeIngredient.Normal, "Fruit énergisant", 2, 8, 2, "Boutique"),
        new Ligne("ING06", "Racine de mandragore", "ingr_racine_de_mandragore", TypeIngredient.Normal, "Racine puissante, utile pour soigner", 5, 6, 3, "Boutique"),
        new Ligne("ING07", "Carotte des brumes", "ingr_carotte_des_brumes", TypeIngredient.Normal, "Légume qui donne une teinte brumeuse aux soupes", 2, 8, 2, "Boutique"),
        new Ligne("ING08", "Pétale de lotus", "ingr_petale_de_lotus", TypeIngredient.Normal, "Fleur calmante, utilisée pour endormir", 4, 6, 4, "Boutique"),
        new Ligne("ING09", "Écorce d'érable noir", "ingr_ecorce_d_erable_noir", TypeIngredient.Normal, "Épice qui renforce les potions", 4, 6, 5, "Boutique"),
        new Ligne("ING10", "Belladone", "ingr_belladone", TypeIngredient.Toxique, "Baie noire, provoque des vertiges et la peau pâle", 3, 5, 3, "Boutique"),
        new Ligne("ING11", "Champignon pourpre", "ingr_champignon_pourpre", TypeIngredient.Toxique, "Ressemble au champignon des bois… en moins sain", 3, 5, 3, "Boutique"),
        new Ligne("ING12", "Digitale", "ingr_digitale", TypeIngredient.Toxique, "Fleur violette, fait trembler et tousser", 4, 4, 4, "Boutique"),
        new Ligne("ING13", "Venin de crapaud", "ingr_venin_de_crapaud", TypeIngredient.Toxique, "Très puissant, effet visible dès le retour du client", 6, 3, 6, "Boutique"),
        new Ligne("ING14", "Larme d'étoile", "ingr_larme_d_etoile", TypeIngredient.Secret, "Indispensable à la méga-potion anti-fantôme", 0, 0, 5, "Récompense (mission / boutique rare)"),
        new Ligne("ING15", "Poussière de fée", "ingr_poussiere_de_fee", TypeIngredient.Magique, "Aide à battre les fantômes (donnée par les fées)", 0, 0, 2, "Fée servie correctement"),
        new Ligne("ING16", "Aloé des marais", "ingr_aloe_des_marais", TypeIngredient.Remede, "Apaise la peau et les brûlures", 4, 5, 3, "Boutique"),
        new Ligne("ING17", "Racine de gingembre blanc", "ingr_racine_de_gingembre_blanc", TypeIngredient.Remede, "Aide la digestion, calme les nausées", 4, 5, 3, "Boutique"),
        new Ligne("ING18", "Eau de source pure", "ingr_eau_de_source_pure", TypeIngredient.Remede, "Base de purification", 1, 15, 3, "Boutique"),
    };

    [MenuItem("3Witches/Créer les ingrédients de l'Excel")]
    public static void Creer()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Data", "Ingredients");

        int crees = 0, misAJour = 0, sprites = 0;
        foreach (var l in donnees)
        {
            string chemin = Dossier + "/" + l.fichier + ".asset";
            var ing = AssetDatabase.LoadAssetAtPath<IngredientData>(chemin);
            if (ing == null)
            {
                ing = ScriptableObject.CreateInstance<IngredientData>();
                AssetDatabase.CreateAsset(ing, chemin);
                crees++;
            }
            else misAJour++;

            ing.id = l.id;
            ing.nom = l.nom;
            ing.type = l.type;
            ing.description = l.desc;
            ing.prixAchat = l.prix;
            ing.stockDepart = l.stock;
            ing.niveauDeblocage = l.niveau;
            ing.obtention = l.obtention;

            // Si Lou-Ann a déjà mis un sprite avec le même nom, on le relie tout seul
            if (ing.sprite == null)
            {
                ing.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DossierSprites + "/" + l.fichier + ".png");
                if (ing.sprite != null) sprites++;
            }
            EditorUtility.SetDirty(ing);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("3Witches",
            $"{crees} ingrédient(s) créé(s), {misAJour} mis à jour, {sprites} sprite(s) relié(s).\n\nDossier : {Dossier}", "OK");
    }
}
