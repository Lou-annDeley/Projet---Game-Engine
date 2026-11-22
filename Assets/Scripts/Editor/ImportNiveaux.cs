using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Outil d'éditeur (KAN-23) : crée les 8 niveaux de la feuille « Liste des niveaux »
// et la liste qui les relie à la carte. Les recettes sont celles de Sarah
// (menu 3Witches > Créer les recettes de l'Excel) : on ne les recrée pas.
// Menu en haut de Unity : 3Witches > Créer les niveaux
// Les fiches déjà créées ne sont pas écrasées : l'équilibrage (KAN-53) se fait dans l'Inspector.
public static class ImportNiveaux
{
    const string DossierIngredients = "Assets/Data/Ingredients";
    const string DossierRecettes = "Assets/Data/Recettes";
    const string DossierNiveaux = "Assets/Data/Niveaux";
    public const string CheminListe = DossierNiveaux + "/liste_niveaux.asset";

    // ---------- Niveaux (feuille « Liste des niveaux ») ----------

    struct LigneNiveau
    {
        public int numero, clients, objectif, commandesMax;
        public string nom, nouveautes, particularite;
        public Difficulte difficulte;
        public float duree, patience, delai;
        public bool demo;
    }

    static readonly LigneNiveau[] niveaux =
    {
        new LigneNiveau { numero = 1, nom = "Ouverture de la boutique", difficulte = Difficulte.Facile,
            clients = 5, objectif = 3, commandesMax = 1, duree = 240, patience = 90, delai = 4, demo = true,
            nouveautes = "Soupe de citrouille, soupe de champignons.",
            particularite = "Une démo guidée pour apprendre les touches et le service." },
        new LigneNiveau { numero = 2, nom = "Les premiers habitués", difficulte = Difficulte.Facile,
            clients = 7, objectif = 5, commandesMax = 2, duree = 240, patience = 80, delai = 5,
            nouveautes = "Soupe de carottes brumeuses, potion d'énergie. Deux commandes à la fois.",
            particularite = "Une fée se cache parmi les clients." },
        new LigneNiveau { numero = 3, nom = "Le rayon interdit", difficulte = Difficulte.Moyen,
            clients = 8, objectif = 6, commandesMax = 3, duree = 270, patience = 75, delai = 5,
            nouveautes = "Potion de soin. Trois commandes à la fois.",
            particularite = "Le rayon interdit est là... mais il reste fermé pour l'instant." },
        new LigneNiveau { numero = 4, nom = "Nuit agitée", difficulte = Difficulte.Moyen,
            clients = 9, objectif = 7, commandesMax = 3, duree = 300, patience = 70, delai = 4,
            nouveautes = "Potion de sommeil. Sort : endormir un client.",
            particularite = "Pendant la nuit, des ingrédients disparaissent." },
        new LigneNiveau { numero = 5, nom = "La larme d'étoile", difficulte = Difficulte.Difficile,
            clients = 10, objectif = 8, commandesMax = 4, duree = 300, patience = 60, delai = 4,
            nouveautes = "Le poison se débloque : belladone, champignon pourpre, digitale. Soupe du sorcier, élixir de chance.",
            particularite = "Un client empoisonné finit toujours par revenir... il lui faut un remède." },
        new LigneNiveau { numero = 6, nom = "Voix de grenouille", difficulte = Difficulte.Difficile,
            clients = 10, objectif = 8, commandesMax = 4, duree = 300, patience = 60, delai = 4,
            nouveautes = "Venin de crapaud, le poison le plus fort.",
            particularite = "Un remède raté peut changer un client en fantôme." },
        new LigneNiveau { numero = 7, nom = "La boutique hantée", difficulte = Difficulte.TresDifficile,
            clients = 12, objectif = 9, commandesMax = 4, duree = 330, patience = 50, delai = 3,
            nouveautes = "Méga-potion anti-fantôme.",
            particularite = "Un fantôme arrive forcément en milieu de journée." },
        new LigneNiveau { numero = 8, nom = "Le dernier jour", difficulte = Difficulte.TresDifficile,
            clients = 14, objectif = 11, commandesMax = 4, duree = 360, patience = 45, delai = 3,
            nouveautes = "Toutes les recettes sont en jeu.",
            particularite = "La fin dépend du nombre de clients empoisonnés." },
    };

    [MenuItem("3Witches/Créer les niveaux")]
    public static void CreerDepuisMenu()
    {
        ListeNiveaux liste = CreerTout(out string erreur);
        if (liste == null) EditorUtility.DisplayDialog("3Witches", erreur, "OK");
        else EditorUtility.DisplayDialog("3Witches",
            liste.niveaux.Count + " niveaux et " + liste.clients.Count +
            " clients prêts.\nDossiers : Assets/Data/Niveaux et Assets/Data/Clients.", "OK");
    }

    // Crée ce qui manque et renvoie la liste des niveaux (null + message s'il manque les fiches de Sarah)
    public static ListeNiveaux CreerTout(out string erreur)
    {
        erreur = "";
        if (AssetDatabase.LoadAssetAtPath<IngredientData>(DossierIngredients + "/ingr_herbe_lunaire.asset") == null)
        {
            erreur = "Les ingrédients sont introuvables dans " + DossierIngredients + ".\n" +
                     "Fais d'abord un Pull, puis lance 3Witches > Créer les ingrédients de l'Excel.";
            return null;
        }
        if (TousLesRecettes().Count == 0)
        {
            erreur = "Les recettes sont introuvables dans " + DossierRecettes + ".\n" +
                     "Lance d'abord 3Witches > Créer les recettes de l'Excel (outil de Sarah).";
            return null;
        }

        CreerDossier("Assets", "Data");
        CreerDossier("Assets/Data", "Niveaux");
        ImportClients.Creer();

        var liste = AssetDatabase.LoadAssetAtPath<ListeNiveaux>(CheminListe);
        if (liste == null)
        {
            liste = ScriptableObject.CreateInstance<ListeNiveaux>();
            AssetDatabase.CreateAsset(liste, CheminListe);
        }

        liste.niveaux = new List<NiveauData>();
        foreach (LigneNiveau l in niveaux) liste.niveaux.Add(CreerNiveau(l));
        liste.clients = ImportClients.TousLesClients();
        EditorUtility.SetDirty(liste);
        AssetDatabase.SaveAssets();
        return liste;
    }

    static NiveauData CreerNiveau(LigneNiveau l)
    {
        string chemin = DossierNiveaux + "/niveau_" + l.numero.ToString("00") + ".asset";
        // On met à jour la fiche si elle existe déjà (ex. Sarah a ajouté des recettes)
        var n = AssetDatabase.LoadAssetAtPath<NiveauData>(chemin);
        bool nouvelle = n == null;
        if (nouvelle) n = ScriptableObject.CreateInstance<NiveauData>();
        n.numero = l.numero;
        n.nom = l.nom;
        n.difficulte = l.difficulte;
        n.nouveautes = l.nouveautes;
        n.particularite = l.particularite;
        n.nombreClients = l.clients;
        n.objectifClients = l.objectif;
        n.commandesMax = l.commandesMax;
        n.dureeSecondes = l.duree;
        n.patienceSecondes = l.patience;
        n.delaiEntreClients = l.delai;
        n.avecDemo = l.demo;

        // Recettes de Sarah débloquées jusqu'à ce niveau (champ « niveau de déblocage » de chaque fiche)
        n.recettes = new List<RecetteData>();
        foreach (RecetteData r in TousLesRecettes())
            if (r.niveauDeblocage <= l.numero) n.recettes.Add(r);

        if (nouvelle) AssetDatabase.CreateAsset(n, chemin);
        else EditorUtility.SetDirty(n);
        return n;
    }

    // ---------- Outils ----------

    static List<RecetteData> TousLesRecettes()
    {
        var liste = new List<RecetteData>();
        foreach (string guid in AssetDatabase.FindAssets("t:RecetteData", new[] { DossierRecettes }))
        {
            var r = AssetDatabase.LoadAssetAtPath<RecetteData>(AssetDatabase.GUIDToAssetPath(guid));
            if (r != null) liste.Add(r);
        }
        liste.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        return liste;
    }

    public static RecetteData TrouverRecette(string id) => TousLesRecettes().Find(r => r.id == id);

    static void CreerDossier(string parent, string nom)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + nom)) AssetDatabase.CreateFolder(parent, nom);
    }
}
