using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Outil d'éditeur : crée les 8 fiches de recettes de l'Excel (KAN-22) en un clic,
// et remplit les listes du Grimoire de la scène ouverte (recettes + remèdes).
// Menu : 3Witches > Créer les recettes de l'Excel
public static class ImportRecettes
{
    const string Dossier = "Assets/Data/Recettes";
    const string DossierIngr = "Assets/Data/Ingredients";
    const string DossierRemedes = "Assets/Data/Remedes";

    struct Ligne
    {
        public string id, nom, fichier, poison;
        public CategorieRecette categorie;
        public string[] ingredients;
        public int temps, prix, niveau;
    }

    static readonly Ligne[] donnees =
    {
        new Ligne { id = "REC01", nom = "Soupe de citrouille", fichier = "rec_soupe_de_citrouille", categorie = CategorieRecette.Soupe,
            ingredients = new[] { "ingr_citrouille", "ingr_herbe_lunaire", "ingr_miel" }, temps = 20, prix = 10, niveau = 1, poison = "ingr_belladone" },
        new Ligne { id = "REC02", nom = "Soupe de champignons", fichier = "rec_soupe_de_champignons", categorie = CategorieRecette.Soupe,
            ingredients = new[] { "ingr_champignon_des_bois", "ingr_herbe_lunaire" }, temps = 15, prix = 8, niveau = 1, poison = "ingr_champignon_pourpre" },
        new Ligne { id = "REC03", nom = "Soupe de carottes brumeuses", fichier = "rec_soupe_de_carottes_brumeuses", categorie = CategorieRecette.Soupe,
            ingredients = new[] { "ingr_carotte_des_brumes", "ingr_champignon_des_bois", "ingr_miel" }, temps = 20, prix = 10, niveau = 2, poison = "ingr_digitale" },
        new Ligne { id = "REC04", nom = "Potion d'énergie", fichier = "rec_potion_d_energie", categorie = CategorieRecette.Potion,
            ingredients = new[] { "ingr_baie_rouge", "ingr_miel" }, temps = 15, prix = 9, niveau = 2, poison = "ingr_digitale" },
        new Ligne { id = "REC05", nom = "Potion de soin", fichier = "rec_potion_de_soin", categorie = CategorieRecette.Potion,
            ingredients = new[] { "ingr_herbe_lunaire", "ingr_racine_de_mandragore" }, temps = 25, prix = 15, niveau = 3, poison = "ingr_belladone" },
        new Ligne { id = "REC06", nom = "Potion de sommeil", fichier = "rec_potion_de_sommeil", categorie = CategorieRecette.Potion,
            ingredients = new[] { "ingr_petale_de_lotus", "ingr_herbe_lunaire", "ingr_miel" }, temps = 25, prix = 16, niveau = 4, poison = "ingr_venin_de_crapaud" },
        new Ligne { id = "REC07", nom = "Soupe du sorcier", fichier = "rec_soupe_du_sorcier", categorie = CategorieRecette.Soupe,
            ingredients = new[] { "ingr_citrouille", "ingr_racine_de_mandragore", "ingr_ecorce_d_erable_noir" }, temps = 30, prix = 20, niveau = 5, poison = "ingr_venin_de_crapaud" },
        new Ligne { id = "REC08", nom = "Élixir de chance", fichier = "rec_elixir_de_chance", categorie = CategorieRecette.Potion,
            ingredients = new[] { "ingr_baie_rouge", "ingr_petale_de_lotus", "ingr_ecorce_d_erable_noir" }, temps = 30, prix = 22, niveau = 5, poison = "ingr_champignon_pourpre" },
    };

    static IngredientData Ingr(string fichier)
    {
        var ing = AssetDatabase.LoadAssetAtPath<IngredientData>(DossierIngr + "/" + fichier + ".asset");
        if (ing == null) Debug.LogWarning($"[Import recettes] Ingrédient introuvable : {fichier}");
        return ing;
    }

    [MenuItem("3Witches/Créer les recettes de l'Excel")]
    public static void Creer()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Data", "Recettes");

        var recettes = new List<RecetteData>();
        foreach (var l in donnees)
        {
            string chemin = Dossier + "/" + l.fichier + ".asset";
            var rec = AssetDatabase.LoadAssetAtPath<RecetteData>(chemin);
            if (rec == null)
            {
                rec = ScriptableObject.CreateInstance<RecetteData>();
                AssetDatabase.CreateAsset(rec, chemin);
            }
            rec.id = l.id;
            rec.nom = l.nom;
            rec.categorie = l.categorie;
            rec.ingredients = new List<IngredientData>();
            foreach (var f in l.ingredients) rec.ingredients.Add(Ingr(f));
            rec.tempsPreparation = l.temps;
            rec.prixVente = l.prix;
            rec.niveauDeblocage = l.niveau;
            rec.ingredientPoison = Ingr(l.poison);
            EditorUtility.SetDirty(rec);
            recettes.Add(rec);
        }
        AssetDatabase.SaveAssets();

        // Tous les remèdes déjà créés (menu « Créer les remèdes »)
        var remedes = new List<RemedeData>();
        if (AssetDatabase.IsValidFolder(DossierRemedes))
            foreach (string guid in AssetDatabase.FindAssets("t:RemedeData", new[] { DossierRemedes }))
                remedes.Add(AssetDatabase.LoadAssetAtPath<RemedeData>(AssetDatabase.GUIDToAssetPath(guid)));
        remedes.Sort((a, b) => string.Compare(a.id, b.id, System.StringComparison.Ordinal));

        // Remplit le Grimoire de la scène ouverte
        string messageScene = "Aucun Grimoire trouvé dans la scène ouverte : ouvre la scène Cuisine, ajoute le Grimoire, puis relance ce menu.";
        var grimoire = Object.FindFirstObjectByType<Grimoire>();
        if (grimoire != null)
        {
            Undo.RecordObject(grimoire, "Remplir le grimoire");
            grimoire.recettes = new List<RecetteData>(recettes);
            grimoire.remedes = remedes;
            EditorUtility.SetDirty(grimoire);
            EditorSceneManager.MarkSceneDirty(grimoire.gameObject.scene);
            messageScene = $"Le Grimoire a été rempli ({recettes.Count} recettes, {remedes.Count} remèdes). Pense à Ctrl+S.";
        }

        EditorUtility.DisplayDialog("3Witches", $"{recettes.Count} recettes créées dans {Dossier}.\n\n{messageScene}", "OK");
    }
}
