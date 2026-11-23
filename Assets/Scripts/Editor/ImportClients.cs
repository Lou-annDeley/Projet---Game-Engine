using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Outil d'éditeur (KAN-37) : crée les fiches des clients du village en un clic.
// Menu en haut de Unity : 3Witches > Créer les clients
// Une fiche déjà créée n'est pas écrasée (on peut la retoucher dans l'Inspector).
public static class ImportClients
{
    public const string Dossier = "Assets/Data/Clients";
    const string DossierSprites = "Assets/Sprites/Clients";

    struct Fiche
    {
        public string id, fichier, nom, metier, couleur;
        public string[] commandes;
        public string retour, content, mecontent, empoisonne, impatient;
        public float patience;
        public bool fee;
        public int niveau;
        public string[] prefere;
    }

    static readonly Fiche[] fiches =
    {
        new Fiche { id = "CLI01", fichier = "client_meunier", nom = "Bertrand", metier = "le meunier", couleur = "#597CB0",
            commandes = new[] { "Bonsoir ! {recette}, s'il vous plaît... et vite.",
                                "J'ai moulu du grain toute la journée. Il me faudrait {recette}." },
            retour = "Encore moi ! Le moulin attendra.",
            content = "Ah, ça, c'est du travail bien fait !", mecontent = "Ce n'est pas ce que j'ai commandé, sorcière !",
            empoisonne = "Hmm... ça pique un peu, votre affaire.", impatient = "Mon moulin tourne plus vite que votre service !",
            patience = 0.9f, niveau = 1 },
        new Fiche { id = "CLI02", fichier = "client_boulangere", nom = "Mireille", metier = "la boulangère", couleur = "#A5624D",
            commandes = new[] { "Bonsoir ma belle ! Tu me ferais {recette} ?",
                                "Je sors du four, il me faut {recette} pour tenir jusqu'à demain." },
            retour = "Me revoilà ! Ta dernière préparation m'a fait un bien fou.",
            content = "Délicieux ! Je t'apporterai une brioche.", mecontent = "Oh... ce n'est pas du tout ça.",
            empoisonne = "C'est... original. Je crois.", impatient = "Mes pains vont brûler, je file !",
            patience = 1.2f, niveau = 1, prefere = new[] { "REC01", "REC02", "REC03", "REC07" } },
        new Fiche { id = "CLI03", fichier = "client_forgeron", nom = "Pépin", metier = "le forgeron", couleur = "#8C5E3C",
            commandes = new[] { "Salut ! Je suis l'apprenti du forgeron. Vite, {recette}, mon maître m'attend !",
                                "Mon maître m'envoie chercher {recette}. Il a dit de ne pas traîner." },
            retour = "C'est encore Pépin ! Mon maître en redemande.",
            content = "Merci ! Je cours !", mecontent = "Mais j'avais dit autre chose...",
            empoisonne = "Ouh, ça me fait tout drôle dans le ventre.", impatient = "Mon maître va me gronder, tant pis !",
            patience = 0.8f, niveau = 1 },
        new Fiche { id = "CLI04", fichier = "client_herboriste", nom = "Dame Ombeline", metier = "l'herboriste", couleur = "#9B7DA0",
            commandes = new[] { "Je pourrais la faire moi-même, mais bon... {recette}.",
                                "On m'a dit que vous saviez faire {recette}. Montrez-moi ça." },
            retour = "Je reviens vérifier si votre talent était un hasard.",
            content = "Hmpf. Pas mal. Pas mal du tout.", mecontent = "Même mon chat ferait mieux.",
            empoisonne = "Intéressant... ce n'était pas dans la recette.", impatient = "Je n'ai pas toute la nuit, moi.",
            patience = 1f, niveau = 2, prefere = new[] { "REC04", "REC05", "REC06", "REC08" } },
        new Fiche { id = "CLI05", fichier = "client_fleuriste", nom = "Rosalie", metier = "la fleuriste", couleur = "#BF4794",
            commandes = new[] { "Bonsoir... Il paraît que vous faites {recette} comme personne.",
                                "Les fleurs m'ont soufflé votre nom. Je prendrai {recette}." },
            retour = "Les fleurs m'ont dit de revenir ici.",
            content = "Vous avez un cœur pur... je le sens.", mecontent = "Oh. Tant pis.",
            empoisonne = "...Vous n'auriez pas dû.", impatient = "Je repasserai quand la lune sera haute.",
            patience = 1.3f, fee = true, niveau = 2 },
        new Fiche { id = "CLI06", fichier = "client_chevalier", nom = "Sire Aubert", metier = "le chevalier", couleur = "#4A5F86",
            commandes = new[] { "Sorcière ! Ma quête exige {recette} !",
                                "Un chevalier ne recule devant rien. Sauf la faim. {recette}, je vous prie." },
            retour = "Mes aventures me ramènent à votre comptoir.",
            content = "Par ma lance, quel breuvage !", mecontent = "C'est une trahison !",
            empoisonne = "Je me sens... moins vaillant.", impatient = "L'honneur m'appelle ailleurs !",
            patience = 1f, niveau = 2 },
        new Fiche { id = "CLI07", fichier = "client_notaire", nom = "Maître Corbin", metier = "le notaire", couleur = "#5C4448",
            commandes = new[] { "Je souhaiterais, dans les règles, {recette}.",
                                "Article premier : je veux {recette}. Article deux : rapidement." },
            retour = "Je reviens, comme convenu par contrat.",
            content = "Tout est en ordre. Parfait.", mecontent = "Ceci n'est pas conforme à ma commande.",
            empoisonne = "Je... note un léger vertige.", impatient = "Le temps, c'est de l'argent. Adieu.",
            patience = 0.9f, niveau = 3 },
        new Fiche { id = "CLI08", fichier = "client_doyenne", nom = "Grand-mère Odile", metier = "la doyenne", couleur = "#B08AA8",
            commandes = new[] { "Ah, ma petite... tu me prépares {recette} ? Prends ton temps.",
                                "De mon temps, on faisait {recette} au feu de bois. Fais-moi plaisir." },
            retour = "Je suis revenue, tes plats me rappellent ma jeunesse.",
            content = "Comme ceux de ma propre grand-mère !", mecontent = "Ce n'est pas grave... mais ce n'est pas ça.",
            empoisonne = "Tiens, je vois des papillons.", impatient = "Même à mon âge, ça fait long.",
            patience = 1.4f, niveau = 4 },
        new Fiche { id = "CLI09", fichier = "client_pecheur", nom = "Capitaine Barnabé", metier = "le pêcheur", couleur = "#3E6E9A",
            commandes = new[] { "Ohé ! Il me faut {recette}, et que ça saute !",
                                "Trois jours en mer... Donnez-moi {recette}." },
            retour = "Le vent m'a ramené au port.",
            content = "Mille sabords, c'est fameux !", mecontent = "Ça, c'est bon pour les poissons !",
            empoisonne = "Le sol tangue... ou c'est moi ?", impatient = "La marée n'attend pas !",
            patience = 1f, niveau = 5 },
        new Fiche { id = "CLI10", fichier = "client_voyageuse", nom = "Céleste", metier = "la voyageuse", couleur = "#7D5BA6",
            commandes = new[] { "Je viens de très loin pour goûter {recette}.",
                                "Avant de reprendre la route, je prendrais bien {recette}." },
            retour = "Je repasse avant de reprendre la route.",
            content = "Merci. Je n'oublierai pas ta gentillesse.", mecontent = "Dommage. On m'avait dit tant de bien de toi.",
            empoisonne = "...Tu as fait un choix, sorcière.", impatient = "La route m'appelle.",
            patience = 1.2f, fee = true, niveau = 5 },
        new Fiche { id = "CLI11", fichier = "client_fossoyeur", nom = "Ignace", metier = "le fossoyeur", couleur = "#493F56",
            commandes = new[] { "Bonsoir. Rude nuit au cimetière... {recette}, je vous prie.",
                                "Les morts ne mangent plus, mais moi si. {recette}." },
            retour = "Je reviens. Les morts, eux, ne sont pas pressés.",
            content = "Enfin un peu de chaleur.", mecontent = "Même mes clients habituels se plaindraient.",
            empoisonne = "Je crois que je vais bientôt avoir du travail...", impatient = "Bon. Je retourne creuser.",
            patience = 1.1f, niveau = 6 },
    };

    [MenuItem("3Witches/Créer les clients")]
    public static void CreerDepuisMenu()
    {
        int crees = Creer();
        EditorUtility.DisplayDialog("3Witches",
            crees + " client(s) créé(s) dans " + Dossier + ".\nLes fiches qui existaient déjà n'ont pas été modifiées.", "OK");
    }

    // Renvoie le nombre de fiches créées
    public static int Creer()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Data", "Clients");

        int crees = 0;
        foreach (Fiche f in fiches)
        {
            string chemin = Dossier + "/" + f.fichier + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ClientData>(chemin) != null) continue;

            var client = ScriptableObject.CreateInstance<ClientData>();
            client.id = f.id;
            client.nom = f.nom;
            client.metier = f.metier;
            client.couleurProvisoire = Palette.Hex(f.couleur);
            client.repliquesCommande = f.commandes;
            client.repliqueRetour = f.retour;
            client.repliqueContent = f.content;
            client.repliqueMecontent = f.mecontent;
            client.repliqueEmpoisonne = f.empoisonne;
            client.repliqueImpatient = f.impatient;
            client.facteurPatience = f.patience;
            client.estFeeCachee = f.fee;
            client.niveauApparition = f.niveau;
            client.commandesPossibles = new List<RecetteData>();
            if (f.prefere != null)
                foreach (string idRecette in f.prefere)
                {
                    RecetteData r = ImportNiveaux.TrouverRecette(idRecette);
                    if (r != null) client.commandesPossibles.Add(r);
                }

            // Sprites de Lou-Ann s'ils existent déjà : client_meunier_calme.png, _enerve, _furieux
            client.spriteCalme = AssetDatabase.LoadAssetAtPath<Sprite>(DossierSprites + "/" + f.fichier + "_calme.png");
            client.spriteEnerve = AssetDatabase.LoadAssetAtPath<Sprite>(DossierSprites + "/" + f.fichier + "_enerve.png");
            client.spriteFurieux = AssetDatabase.LoadAssetAtPath<Sprite>(DossierSprites + "/" + f.fichier + "_furieux.png");

            AssetDatabase.CreateAsset(client, chemin);
            crees++;
        }
        AssetDatabase.SaveAssets();
        return crees;
    }

    public static List<ClientData> TousLesClients()
    {
        var clients = new List<ClientData>();
        foreach (Fiche f in fiches)
        {
            var c = AssetDatabase.LoadAssetAtPath<ClientData>(Dossier + "/" + f.fichier + ".asset");
            if (c != null) clients.Add(c);
        }
        return clients;
    }
}
