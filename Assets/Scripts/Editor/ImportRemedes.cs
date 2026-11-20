using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Outil d'éditeur : crée les 4 fiches de remèdes de l'Excel (KAN-22) en un clic,
// et remplit la liste du ClientsEmpoisonnes de la scène ouverte.
// Menu : 3Witches > Créer les remèdes de l'Excel
public static class ImportRemedes
{
    const string Dossier = "Assets/Data/Remedes";
    const string DossierIngr = "Assets/Data/Ingredients";

    struct Ligne
    {
        public string id, nom, fichier, anomalie, code, cause;
        public string[] ingredients;
        public int temps, prix, niveau;
        public SiRemedeRate siRate;
    }

    static readonly Ligne[] donnees =
    {
        new Ligne { id = "REM01", nom = "Baume vert", fichier = "rem_baume_vert", anomalie = "Peau verdâtre", code = "peau_verte",
            cause = "ingr_belladone", ingredients = new[] { "ingr_aloe_des_marais", "ingr_eau_de_source_pure" },
            temps = 20, prix = 12, niveau = 3, siRate = SiRemedeRate.PlusMalade },
        new Ligne { id = "REM02", nom = "Tisane anti-toux", fichier = "rem_tisane_anti_toux", anomalie = "Toux violette", code = "toux_violette",
            cause = "ingr_digitale", ingredients = new[] { "ingr_racine_de_gingembre_blanc", "ingr_miel", "ingr_eau_de_source_pure" },
            temps = 25, prix = 14, niveau = 4, siRate = SiRemedeRate.PlusMalade },
        new Ligne { id = "REM03", nom = "Élixir de lucidité", fichier = "rem_elixir_de_lucidite", anomalie = "Yeux brillants / hallucinations", code = "yeux_brillants",
            cause = "ingr_champignon_pourpre", ingredients = new[] { "ingr_petale_de_lotus", "ingr_eau_de_source_pure" },
            temps = 25, prix = 15, niveau = 4, siRate = SiRemedeRate.PlusMalade },
        new Ligne { id = "REM04", nom = "Sirop de croassement", fichier = "rem_sirop_de_croassement", anomalie = "Voix de grenouille", code = "voix_grenouille",
            cause = "ingr_venin_de_crapaud", ingredients = new[] { "ingr_racine_de_mandragore", "ingr_miel", "ingr_aloe_des_marais" },
            temps = 30, prix = 18, niveau = 6, siRate = SiRemedeRate.Meurt },
    };

    static IngredientData Ingr(string fichier)
    {
        var ing = AssetDatabase.LoadAssetAtPath<IngredientData>(DossierIngr + "/" + fichier + ".asset");
        if (ing == null) Debug.LogWarning($"[Import remèdes] Ingrédient introuvable : {fichier}");
        return ing;
    }

    [MenuItem("3Witches/Créer les remèdes de l'Excel")]
    public static void Creer()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Data", "Remedes");

        var tous = new List<RemedeData>();
        foreach (var l in donnees)
        {
            string chemin = Dossier + "/" + l.fichier + ".asset";
            var rem = AssetDatabase.LoadAssetAtPath<RemedeData>(chemin);
            if (rem == null)
            {
                rem = ScriptableObject.CreateInstance<RemedeData>();
                AssetDatabase.CreateAsset(rem, chemin);
            }
            rem.id = l.id;
            rem.nom = l.nom;
            rem.anomalie = l.anomalie;
            rem.codeAnomalie = l.code;
            rem.causePoison = Ingr(l.cause);
            rem.ingredients = new List<IngredientData>();
            foreach (var f in l.ingredients) rem.ingredients.Add(Ingr(f));
            rem.tempsPreparation = l.temps;
            rem.prixVente = l.prix;
            rem.niveauDeblocage = l.niveau;
            rem.siRate = l.siRate;
            EditorUtility.SetDirty(rem);
            tous.Add(rem);
        }
        AssetDatabase.SaveAssets();

        // Remplit aussi la liste du ClientsEmpoisonnes de la scène ouverte
        string messageScene = "Aucun ClientsEmpoisonnes trouvé dans la scène ouverte : pense à remplir sa liste.";
        var gestion = Object.FindFirstObjectByType<ClientsEmpoisonnes>();
        if (gestion != null)
        {
            Undo.RecordObject(gestion, "Remplir les remèdes");
            gestion.remedes = new List<RemedeData>(tous);
            EditorUtility.SetDirty(gestion);
            EditorSceneManager.MarkSceneDirty(gestion.gameObject.scene);
            messageScene = "La liste du ClientsEmpoisonnes a été remplie. Pense à Ctrl+S.";
        }

        EditorUtility.DisplayDialog("3Witches", $"{tous.Count} remèdes créés dans {Dossier}.\n\n{messageScene}", "OK");
    }
}
