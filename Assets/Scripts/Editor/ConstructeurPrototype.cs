using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Menu "3Witches > Construire le prototype" (KAN-27).
// Version qui s'appuie sur le travail de l'équipe :
//  - Comptoir : décor de Lou-Ann (KAN-63). On ajoute les clients (dessinés entre le fond et le comptoir),
//    les tickets, la bulle, la cloche et les rubans.
//  - Cuisine : décor de Lou-Ann, étagère / chaudron / grimoire de Sarah. On ajoute les mini-tickets,
//    le texte du parchemin, Préparer / Vider, le bol ou la fiole et les rubans.
//  - Boutique : étal de Sarah. On ajoute les rubans et les tickets.
//  - MenuCarte : la carte des niveaux (KAN-48).
//  Le HUD et l'écran de fin sont ceux de Lou-Ann (créés tout seuls au lancement).
// On peut le relancer : il remplace seulement les objets "Proto_..." des scènes.
public static class ConstructeurPrototype
{
    const string DossierScenes = "Assets/Scenes/";
    const string NomCanvas = "Proto_Canvas";
    const string NomCanvasClients = "Proto_Clients";
    const string NomEventSystem = "Proto_EventSystem";
    const string NomControleur = "Proto_Controleur";
    const string DossierSorciere = "Assets/Sprites/Sorciere/";

    static Sprite carre;
    static Sprite rond;
    static Font police;
    static Sprite[] sorciere;   // neutre, contente, surprise, malicieuse

    [MenuItem("3Witches/Construire le prototype")]
    public static void Construire()
    {
        bool ok = EditorUtility.DisplayDialog("3Witches",
            "Les scènes MenuCarte, Comptoir, Cuisine et Boutique vont recevoir la boucle de jeu.\n" +
            "Seuls les objets Proto_... sont remplacés : le décor et les scripts de l'équipe ne bougent pas.",
            "Construire", "Annuler");
        if (!ok || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ListeNiveaux liste = ImportNiveaux.CreerTout(out string erreur);
        if (liste == null)
        {
            EditorUtility.DisplayDialog("3Witches", erreur, "OK");
            return;
        }

        carre = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        rond = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sorciere = new[]
        {
            SpriteSorciere("sorciere_neutre"), SpriteSorciere("sorciere_contente"),
            SpriteSorciere("sorciere_surprise"), SpriteSorciere("sorciere_malicieuse")
        };

        ConstruireCarte(liste);
        ConstruireComptoir(liste);
        ConstruireCuisine(liste);
        ConstruireBoutique(liste);
        AjouterScenesAuBuild();

        EditorSceneManager.OpenScene(DossierScenes + "MenuCarte.unity", OpenSceneMode.Single);
        EditorUtility.DisplayDialog("3Witches",
            "C'est prêt !\nAppuie sur Play depuis la scène MenuCarte (ou directement depuis Comptoir).", "OK");
    }

    // Réglages d'import de la DA : Sprite, Pixels Per Unit 100, Bilinear, 2048, pivot en bas au centre
    static Sprite SpriteSorciere(string nom)
    {
        string chemin = DossierSorciere + nom + ".png";
        var importeur = AssetImporter.GetAtPath(chemin) as TextureImporter;
        if (importeur == null) return null;

        importeur.textureType = TextureImporterType.Sprite;
        importeur.spriteImportMode = SpriteImportMode.Single;
        importeur.spritePixelsPerUnit = 100;
        importeur.filterMode = FilterMode.Bilinear;
        importeur.maxTextureSize = 2048;
        importeur.alphaIsTransparency = true;
        var reglages = new TextureImporterSettings();
        importeur.ReadTextureSettings(reglages);
        reglages.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        importeur.SetTextureSettings(reglages);
        importeur.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(chemin);
    }

    static readonly Vector2[] positionsNoeuds =
    {
        new Vector2(-200, -420), new Vector2(200, -315), new Vector2(-200, -210), new Vector2(200, -105),
        new Vector2(-200, 0), new Vector2(200, 105), new Vector2(-200, 210), new Vector2(150, 330)
    };

    static void ConstruireCarte(ListeNiveaux liste)
    {
        var scene = EditorSceneManager.OpenScene(DossierScenes + "MenuCarte.unity", OpenSceneMode.Single);
        Transform canvas = PreparerScene();
        AssurerStockManager();
        var c = new GameObject(NomControleur).AddComponent<CarteNiveaux>();
        c.liste = liste;

        Bloc(canvas, "Fond", Palette.NuitViolette, Vector2.zero, new Vector2(1920, 1080));

        // Carte au centre : ciel violet qui se réchauffe vers le bas
        RectTransform carte = Vide(canvas, "Carte", Vector2.zero, new Vector2(960, 1080));
        Bloc(carte, "Ciel_1", Palette.Hex("#2A1440"), new Vector2(0, 380), new Vector2(960, 320));
        Bloc(carte, "Ciel_2", Palette.Hex("#4A2463"), new Vector2(0, 100), new Vector2(960, 260));
        Bloc(carte, "Ciel_3", Palette.Hex("#7A4A6E"), new Vector2(0, -130), new Vector2(960, 220));
        Bloc(carte, "Ciel_4", Palette.Hex("#A5624D"), new Vector2(0, -320), new Vector2(960, 180));
        Bloc(carte, "Ciel_5", Palette.Hex("#C88F60"), new Vector2(0, -470), new Vector2(960, 140));
        Bloc(carte, "Lune", Palette.Bougie, new Vector2(-370, 430), new Vector2(90, 90), true);
        Bloc(carte, "Halo_Lune", Palette.AvecAlpha(Palette.Bougie, 0.15f), new Vector2(-370, 430), new Vector2(170, 170), true);
        Texte(carte, "Titre", "CARTE DES NIVEAUX", 30, Palette.Bougie, new Vector2(250, 475), new Vector2(420, 44)).fontStyle = FontStyle.Bold;

        // Décor : tombes, citrouilles, bougies
        foreach (Vector2 p in new[] { new Vector2(-330, 250), new Vector2(380, 380), new Vector2(380, 40) })
        {
            Bloc(carte, "Tombe", Palette.PierreLavande, p, new Vector2(46, 64));
            Bloc(carte, "Tombe_Haut", Palette.PierreLavande, p + new Vector2(0, 30), new Vector2(46, 46), true);
        }
        foreach (Vector2 p in new[] { new Vector2(-400, -470), new Vector2(-350, -320), new Vector2(390, -430), new Vector2(-100, -495) })
        {
            Bloc(carte, "Citrouille", Palette.Hex("#D9822B"), p, new Vector2(56, 46), true);
            Bloc(carte, "Tige", Palette.BoisSombre, p + new Vector2(0, 26), new Vector2(8, 14));
        }

        // Chemin qui relie les niveaux
        for (int i = 0; i + 1 < positionsNoeuds.Length && i + 1 < liste.niveaux.Count; i++)
            Segment(carte, positionsNoeuds[i], positionsNoeuds[i + 1], 34, Palette.AvecAlpha(Palette.Parchemin, 0.35f));

        var noeuds = new List<NoeudNiveau>();
        for (int i = 0; i < positionsNoeuds.Length && i < liste.niveaux.Count; i++)
            noeuds.Add(CreerNoeud(carte, liste.niveaux[i], positionsNoeuds[i], positionsNoeuds[i].x < 0));
        c.noeuds = noeuds.ToArray();

        // Panneau de gauche : chapitre et progression
        Image gauche = Parchemin(canvas, "Panneau_Chapitre", new Vector2(-723, 200), new Vector2(392, 440));
        Texte(gauche.transform, "Chapitre", "CHAPITRE 1", 18, Palette.MagentaPotion, new Vector2(0, 180), new Vector2(340, 28)).fontStyle = FontStyle.Bold;
        Texte(gauche.transform, "Titre", "La boutique s'éveille", 30, Palette.Encre, new Vector2(0, 140), new Vector2(340, 44)).fontStyle = FontStyle.Bold;
        c.texteProgression = Texte(gauche.transform, "Progression", "", 22, Palette.Encre, new Vector2(0, 35), new Vector2(330, 120), TextAnchor.UpperLeft);
        Texte(gauche.transform, "Gentille", "Gentille", 17, Palette.Encre, new Vector2(0, -72), new Vector2(300, 26), TextAnchor.MiddleLeft);
        Texte(gauche.transform, "Empoisonneuse", "Empoisonneuse", 17, Palette.Encre, new Vector2(0, -72), new Vector2(300, 26), TextAnchor.MiddleRight);
        RectTransform jauge = Vide(gauche.transform, "Jauge", new Vector2(0, -105), new Vector2(300, 18));
        Bloc(jauge, "Gentille", Palette.Lavande, new Vector2(-75, 0), new Vector2(150, 18));
        Bloc(jauge, "Empoisonneuse", Palette.VertPoison, new Vector2(75, 0), new Vector2(150, 18));
        c.curseurEmpoisonnement = Bloc(jauge, "Curseur", Palette.Bougie, new Vector2(-150, 0), new Vector2(10, 36)).rectTransform;
        c.largeurJauge = 300;
        Texte(gauche.transform, "Note", "Plus on empoisonne, plus la nuit tombe sur la fin du jeu...", 17,
            Palette.Encre, new Vector2(0, -165), new Vector2(330, 50)).fontStyle = FontStyle.Italic;

        // Panneau de droite : le niveau choisi
        Image droite = Parchemin(canvas, "Panneau_Niveau", new Vector2(723, 80), new Vector2(398, 570));
        c.etiquetteNiveau = Texte(droite.transform, "Etiquette", "NIVEAU 1", 18, Palette.MagentaPotion, new Vector2(0, 248), new Vector2(340, 28));
        c.etiquetteNiveau.fontStyle = FontStyle.Bold;
        c.titreNiveau = Texte(droite.transform, "Titre", "", 30, Palette.Encre, new Vector2(0, 208), new Vector2(350, 44));
        c.titreNiveau.fontStyle = FontStyle.Bold;
        c.texteDifficulte = Texte(droite.transform, "Difficulte", "", 22, Palette.Laiton, new Vector2(0, 170), new Vector2(340, 30));
        c.texteDifficulte.fontStyle = FontStyle.Bold;
        c.etoilesDetail = Etoiles(droite.transform, new Vector2(0, 134), 26);
        c.texteObjectifs = Texte(droite.transform, "Objectifs", "", 18, Palette.Encre, new Vector2(0, -40), new Vector2(340, 280), TextAnchor.UpperLeft);
        c.boutonJouer = Bouton(droite.transform, "Bouton_Jouer", "JOUER", Palette.MagentaPotion, Palette.Bougie,
            new Vector2(0, -235), new Vector2(300, 64), 32);
        c.texteJouer = c.boutonJouer.GetComponentInChildren<Text>();

        c.rubanBoutique = Ruban(canvas, "Ruban_Boutique", "Boutique", new Vector2(-830, -370), new Vector2(240, 70));
        c.rubanOptions = Ruban(canvas, "Ruban_Options", "Options", new Vector2(-830, -460), new Vector2(240, 70));
        c.texteMessage = Texte(canvas, "Message", "", 22, Palette.Bougie, new Vector2(0, -510), new Vector2(900, 34));
        Cadre(canvas);

        Sauver(scene);
    }

    static NoeudNiveau CreerNoeud(Transform carte, NiveauData niveau, Vector2 position, bool nomADroite)
    {
        var n = new NoeudNiveau();
        Image anneau = Bloc(carte, "Niveau_" + niveau.numero, Palette.Laiton, position, new Vector2(118, 118), true);
        anneau.raycastTarget = true;
        n.anneau = anneau;
        n.bouton = anneau.gameObject.AddComponent<Button>();
        n.bouton.targetGraphic = anneau;
        n.fond = Bloc(anneau.transform, "Fond", Palette.BoisRouge, Vector2.zero, new Vector2(94, 94), true);
        n.numero = Texte(anneau.transform, "Numero", niveau.numero.ToString(), 44, Palette.Bougie, Vector2.zero, new Vector2(100, 100));
        n.numero.fontStyle = FontStyle.Bold;

        float decalage = nomADroite ? 190f : -190f;
        Image etiquette = Bloc(anneau.transform, "Etiquette", Palette.AvecAlpha(Palette.Prune, 0.9f),
            new Vector2(decalage, -10), new Vector2(250, 38));
        n.nom = Texte(etiquette.transform, "Nom", niveau.nom, 18, Palette.Parchemin, Vector2.zero, new Vector2(240, 34));
        n.etoiles = Etoiles(anneau.transform, new Vector2(0, 76), 20);
        return n;
    }

    // ==================================================================
    // Comptoir : décor de Lou-Ann + boucle de jeu
    // ==================================================================

    static void ConstruireComptoir(ListeNiveaux liste)
    {
        var scene = EditorSceneManager.OpenScene(DossierScenes + "Comptoir.unity", OpenSceneMode.Single);
        Transform canvas = PreparerScene();
        AssurerStockManager();
        var c = new GameObject(NomControleur).AddComponent<ComptoirController>();
        c.liste = liste;
        c.niveauParDefaut = liste.niveaux[0];

        // Le client est sur un canvas « caméra » placé entre le fond (-100) et le comptoir (50) :
        // le comptoir de Lou-Ann cache le bas de son corps.
        Transform clients = CanvasEntreLesDecors();
        RectTransform client = Vide(clients, "Client", new Vector2(-540, -40), new Vector2(560, 700));
        c.client = client;
        c.capuche = Bloc(client, "Capuche", Palette.BleuBrume, new Vector2(0, 205), new Vector2(330, 330), true);
        c.cape = Bloc(client, "Cape", Palette.BleuBrume, new Vector2(0, -150), new Vector2(440, 520));
        Bloc(c.cape.transform, "Epaules", Palette.BleuBrume, new Vector2(0, 230), new Vector2(440, 170), true);
        c.tete = Bloc(client, "Tete", Palette.Hex("#F2D2B6"), new Vector2(0, 190), new Vector2(220, 220), true);
        Bloc(c.tete.transform, "Joue_G", Palette.AvecAlpha(Palette.Hex("#E79AAE"), 0.6f), new Vector2(-60, -20), new Vector2(40, 22), true);
        Bloc(c.tete.transform, "Joue_D", Palette.AvecAlpha(Palette.Hex("#E79AAE"), 0.6f), new Vector2(60, -20), new Vector2(40, 22), true);
        Bloc(c.tete.transform, "Oeil_G", Palette.Contour, new Vector2(-40, 10), new Vector2(22, 26), true);
        Bloc(c.tete.transform, "Oeil_D", Palette.Contour, new Vector2(40, 10), new Vector2(22, 26), true);
        c.sourcilGauche = Bloc(c.tete.transform, "Sourcil_G", Palette.Contour, new Vector2(-40, 42), new Vector2(46, 8)).rectTransform;
        c.sourcilDroit = Bloc(c.tete.transform, "Sourcil_D", Palette.Contour, new Vector2(40, 42), new Vector2(46, 8)).rectTransform;
        c.bouche = Bloc(c.tete.transform, "Bouche", Palette.Hex("#8A5A4A"), new Vector2(0, -45), new Vector2(46, 9));
        c.spriteClient = Bloc(client, "Sprite_Client", Color.white, new Vector2(0, -20), new Vector2(600, 640));
        c.spriteClient.type = Image.Type.Simple;
        c.spriteClient.preserveAspect = true;
        c.spriteClient.gameObject.SetActive(false);

        // Bulle de dialogue (texte lettre par lettre, KAN-41)
        Image bulle = Bloc(canvas, "Bulle", Palette.Parchemin, new Vector2(-79, 254), new Vector2(570, 165));
        bulle.raycastTarget = true;
        bulle.gameObject.AddComponent<Button>().targetGraphic = bulle;
        Bloc(bulle.transform, "Pointe", Palette.Parchemin, new Vector2(-282, -50), new Vector2(34, 34)).rectTransform.localEulerAngles = new Vector3(0, 0, 45);
        var bulleDialogue = bulle.gameObject.AddComponent<BulleDialogue>();
        bulleDialogue.texteNom = Texte(bulle.transform, "Nom", "", 17, Palette.MagentaPotion, new Vector2(0, 58), new Vector2(520, 26), TextAnchor.MiddleLeft);
        bulleDialogue.texteNom.fontStyle = FontStyle.Bold;
        bulleDialogue.texte = Texte(bulle.transform, "Texte", "", 26, Palette.Encre, new Vector2(0, -16), new Vector2(520, 112), TextAnchor.UpperLeft);
        c.bulle = bulleDialogue;

        // Sur le comptoir : la préparation prête (bol ou fiole de Lou-Ann)
        // (le liquide est posé par-dessus l'image : la surface de la soupe, le contenu de la fiole)
        RectTransform bol = Vide(canvas, "Bol", new Vector2(-660, -360), new Vector2(240, 200));
        c.bol = bol.gameObject;
        SpriteOuForme(bol, "Bol_Sprite", "Assets/Sprites/Ingredients/cont_bol.png", Vector2.zero, new Vector2(240, 200));
        c.contenuBol = Bloc(bol, "Soupe", Palette.MagentaPotion, new Vector2(0, 54), new Vector2(172, 40), true);
        RectTransform fiole = Vide(canvas, "Fiole", new Vector2(-660, -350), new Vector2(200, 200));
        c.fiole = fiole.gameObject;
        SpriteOuForme(fiole, "Fiole_Sprite", "Assets/Sprites/Ingredients/cont_fiole.png", Vector2.zero, new Vector2(200, 200));
        c.contenuFiole = Bloc(fiole, "Potion", Palette.MagentaPotion, new Vector2(0, -17), new Vector2(104, 104), true);
        c.texteService = Texte(canvas, "Texte_Service", "", 20, Palette.Bougie, new Vector2(-660, -478), new Vector2(380, 30));
        c.texteService.fontStyle = FontStyle.Bold;

        // La cloche : on la fait sonner pour servir
        Bloc(canvas, "Cloche_Socle", Palette.Cuivre, new Vector2(330, -440), new Vector2(200, 34), true);
        Image dome = Bloc(canvas, "Cloche", Palette.Laiton, new Vector2(330, -390), new Vector2(160, 130), true);
        Bloc(dome.transform, "Reflet", Palette.AvecAlpha(Palette.Bougie, 0.5f), new Vector2(-34, 20), new Vector2(30, 40), true);
        Bloc(canvas, "Cloche_Bouton", Palette.Cuivre, new Vector2(330, -315), new Vector2(30, 30), true);
        dome.raycastTarget = true;
        c.cloche = dome.gameObject.AddComponent<Button>();
        c.cloche.targetGraphic = dome;
        c.texteCloche = Texte(canvas, "Texte_Cloche", "Servir", 22, Palette.Bougie, new Vector2(330, -478), new Vector2(200, 30));
        c.texteCloche.fontStyle = FontStyle.Bold;
        c.gainVolant = Texte(canvas, "Gain", "+0", 44, Palette.Laiton, new Vector2(80, -330), new Vector2(200, 56));
        c.gainVolant.fontStyle = FontStyle.Bold;

        // Rubans lavande à droite
        c.rubanCuisine = Ruban(canvas, "Ruban_Cuisine", "Cuisine  →", new Vector2(843, -100), new Vector2(230, 72));
        c.rubanBoutique = Ruban(canvas, "Ruban_Boutique", "Boutique  →", new Vector2(843, -190), new Vector2(230, 72));

        // Tickets en haut à droite, sous le bandeau du HUD de Lou-Ann
        c.tickets = new TicketUI[4];
        for (int i = 0; i < 4; i++)
            c.tickets[i] = CreerTicket(canvas, new Vector2(330 + i * 160, 395), new Vector2(150, 125), false);
        Texte(canvas, "Touches", "Tab cuisine  ·  B boutique  ·  1-4 tickets  ·  Espace servir",
            16, Palette.AvecAlpha(Palette.Parchemin, 0.8f), new Vector2(0, -528), new Vector2(900, 22));
        c.texteMessage = Texte(canvas, "Message", "", 22, Palette.Bougie, new Vector2(-60, -505), new Vector2(1100, 30));

        var cibles = new RectTransform[Demo.NombreEtapes];
        cibles[(int)EtapeDemo.Bienvenue] = bulle.rectTransform;
        cibles[(int)EtapeDemo.Patience] = (RectTransform)c.tickets[0].transform;
        cibles[(int)EtapeDemo.AllerCuisine] = (RectTransform)c.rubanCuisine.transform;
        cibles[(int)EtapeDemo.Servir] = dome.rectTransform;
        ConstruireDemo(canvas, GameManager.SceneComptoir, new Vector2(300, -40), new Vector2(660, 240),
            cibles, (RectTransform)c.rubanCuisine.transform);

        Sauver(scene);
    }

    // ==================================================================
    // Cuisine : décor de Lou-Ann + étagère, chaudron et grimoire de Sarah
    // ==================================================================

    static void ConstruireCuisine(ListeNiveaux liste)
    {
        var scene = EditorSceneManager.OpenScene(DossierScenes + "Cuisine.unity", OpenSceneMode.Single);
        Transform canvas = PreparerScene();
        AssurerStockManager();
        var c = new GameObject(NomControleur).AddComponent<CuisineController>();
        c.liste = liste;
        c.niveauParDefaut = liste.niveaux[0];
        c.chaudron = Object.FindFirstObjectByType<Chaudron>();
        c.grimoire = Object.FindFirstObjectByType<Grimoire>();

        c.rubanComptoir = Ruban(canvas, "Ruban_Comptoir", "←  Comptoir", new Vector2(-830, 300), new Vector2(220, 62));
        c.rubanBoutique = Ruban(canvas, "Ruban_Boutique", "←  Boutique", new Vector2(-830, 225), new Vector2(220, 62));

        // Texte écrit sur le parchemin du décor (à droite)
        RectTransform parchemin = Vide(canvas, "Parchemin_Commande", new Vector2(800, -300), new Vector2(260, 360));
        parchemin.localEulerAngles = new Vector3(0, 0, -3);
        c.titreCommande = Texte(parchemin, "Titre", "", 15, Palette.MagentaPotion, new Vector2(0, 140), new Vector2(240, 44));
        c.titreCommande.fontStyle = FontStyle.Bold;
        c.nomRecette = Texte(parchemin, "Recette", "", 22, Palette.Encre, new Vector2(0, 92), new Vector2(240, 56));
        c.nomRecette.fontStyle = FontStyle.Bold;
        c.listeIngredients = Texte(parchemin, "Ingredients", "", 18, Palette.Encre, new Vector2(4, -50), new Vector2(232, 220), TextAnchor.UpperLeft);

        // Préparer / Vider sous le chaudron, barre de cuisson au-dessus
        Image fondPrep = Bloc(canvas, "Preparation", Palette.Contour, new Vector2(335, 70), new Vector2(320, 24));
        c.fondPreparation = fondPrep.gameObject;
        Image remplissage = Bloc(fondPrep.transform, "Remplissage", Palette.MagentaPotion, new Vector2(-160, 0), new Vector2(320, 24));
        remplissage.rectTransform.pivot = new Vector2(0f, 0.5f);
        c.barrePreparation = remplissage.rectTransform;
        Texte(fondPrep.transform, "Texte", "Ça bout...", 18, Palette.Bougie, new Vector2(0, 28), new Vector2(320, 26));
        fondPrep.gameObject.SetActive(false);
        c.boutonPreparer = Bouton(canvas, "Bouton_Preparer", "Préparer", Palette.Laiton, Palette.Encre, new Vector2(240, -478), new Vector2(170, 58), 26);
        c.boutonVider = Bouton(canvas, "Bouton_Vider", "Vider", Palette.Lavande, Palette.Laiton, new Vector2(425, -478), new Vector2(170, 58), 26);

        // La préparation prête : bol ou fiole de Lou-Ann
        RectTransform bol = Vide(canvas, "Bol", new Vector2(-120, -250), new Vector2(230, 190));
        c.bol = bol.gameObject;
        SpriteOuForme(bol, "Bol_Sprite", "Assets/Sprites/Ingredients/cont_bol.png", Vector2.zero, new Vector2(230, 190));
        c.liquideBol = Bloc(bol, "Soupe", Palette.MagentaPotion, new Vector2(0, 51), new Vector2(164, 38), true);
        RectTransform fiole = Vide(canvas, "Fiole", new Vector2(-120, -240), new Vector2(190, 190));
        c.fiole = fiole.gameObject;
        SpriteOuForme(fiole, "Fiole_Sprite", "Assets/Sprites/Ingredients/cont_fiole.png", Vector2.zero, new Vector2(190, 190));
        c.liquideFiole = Bloc(fiole, "Potion", Palette.MagentaPotion, new Vector2(0, -16), new Vector2(98, 98), true);
        c.texteService = Texte(canvas, "Texte_Service", "", 20, Palette.Bougie, new Vector2(-120, -362), new Vector2(420, 30));
        c.texteService.fontStyle = FontStyle.Bold;
        c.boutonJeter = Bouton(canvas, "Bouton_Jeter", "Jeter", Palette.Lavande, Palette.Laiton, new Vector2(-120, -410), new Vector2(130, 44), 20);

        // Mini-tickets en bas, message au centre
        c.tickets = new TicketUI[4];
        for (int i = 0; i < 4; i++)
            c.tickets[i] = CreerTicket(canvas, new Vector2(-520 + i * 150, -478), new Vector2(140, 78), true);
        c.texteMessage = Texte(canvas, "Message", "", 21, Palette.Bougie, new Vector2(-400, 20), new Vector2(600, 30));
        Texte(canvas, "Touches", "Glisser les ingrédients  ·  Espace préparer  ·  Retour arrière vider  ·  G grimoire  ·  Tab comptoir",
            16, Palette.AvecAlpha(Palette.Parchemin, 0.85f), new Vector2(-400, -18), new Vector2(600, 22));

        var cibles = new RectTransform[Demo.NombreEtapes];
        cibles[(int)EtapeDemo.Preparer] = (RectTransform)c.boutonPreparer.transform;
        cibles[(int)EtapeDemo.RetourComptoir] = (RectTransform)c.rubanComptoir.transform;
        c.demo = ConstruireDemo(canvas, GameManager.SceneCuisine, new Vector2(-560, -160), new Vector2(620, 230),
            cibles, (RectTransform)c.rubanComptoir.transform);

        Sauver(scene);
    }

    // ==================================================================
    // Boutique : étal de Sarah (KAN-45) + navigation
    // ==================================================================

    static void ConstruireBoutique(ListeNiveaux liste)
    {
        var scene = EditorSceneManager.OpenScene(DossierScenes + "Boutique.unity", OpenSceneMode.Single);
        Transform canvas = PreparerScene();
        AssurerStockManager();
        var c = new GameObject(NomControleur).AddComponent<BoutiqueController>();
        c.liste = liste;

        // Tickets en bas à gauche pour ne pas cacher l'étal (les commandes continuent pendant les achats)
        c.tickets = new TicketUI[4];
        for (int i = 0; i < 4; i++)
            c.tickets[i] = CreerTicket(canvas, new Vector2(-840 + i * 150, -478), new Vector2(140, 78), true);
        c.rubanCuisine = Ruban(canvas, "Ruban_Cuisine", "Cuisine  →", new Vector2(843, -370), new Vector2(230, 70));
        c.rubanRetour = Ruban(canvas, "Ruban_Retour", "Retour  →", new Vector2(843, -460), new Vector2(230, 70));
        c.texteMessage = Texte(canvas, "Message", "", 22, Palette.Bougie, new Vector2(-560, -405), new Vector2(700, 30));
        Sauver(scene);
    }

    // ==================================================================
    // Éléments communs
    // ==================================================================

    static TicketUI CreerTicket(Transform parent, Vector2 position, Vector2 taille, bool petit)
    {
        RectTransform racine = Vide(parent, "Ticket", position, taille);
        var t = racine.gameObject.AddComponent<TicketUI>();
        t.afficherManques = petit;

        t.cadreSelection = Bloc(racine, "Selection", Palette.MagentaPotion, Vector2.zero, taille + new Vector2(10, 10));
        Image papier = Bloc(racine, "Papier", Palette.Parchemin, Vector2.zero, taille);
        papier.raycastTarget = true;
        t.bouton = papier.gameObject.AddComponent<Button>();
        t.bouton.targetGraphic = papier;

        RectTransform contenu = Vide(racine, "Contenu", Vector2.zero, taille);
        t.contenu = contenu.gameObject;
        float h = taille.y / 2f;
        t.texteNom = Texte(contenu, "Nom", "", petit ? 12 : 14, Palette.MagentaPotion, new Vector2(0, h - (petit ? 14 : 18)),
            new Vector2(taille.x - 12, 22), TextAnchor.MiddleLeft);
        t.texteNom.fontStyle = FontStyle.Bold;
        t.texteRecette = Texte(contenu, "Recette", "", petit ? 14 : 18, Palette.Encre, new Vector2(0, petit ? 4 : 10),
            new Vector2(taille.x - 12, petit ? 34 : 48), TextAnchor.MiddleLeft);
        t.points = new Image[3];
        for (int i = 0; i < 3; i++)
            t.points[i] = Bloc(contenu, "Point_" + (i + 1), Palette.Lavande,
                new Vector2(-taille.x / 2f + 16 + i * 18, -h + (petit ? 12 : 16)), new Vector2(13, 13), true);
        t.texteEtat = Texte(contenu, "Etat", "", petit ? 11 : 14, Palette.Encre, new Vector2(18, -h + (petit ? 12 : 16)),
            new Vector2(taille.x - 50, 20), TextAnchor.MiddleRight);

        Image libre = Bloc(racine, "Place_Libre", Palette.AvecAlpha(Palette.Lavande, 0.15f), Vector2.zero, taille);
        t.placeLibre = libre.gameObject;
        Texte(libre.transform, "Texte", "place libre", petit ? 14 : 17, Palette.AvecAlpha(Palette.Lavande, 0.8f), Vector2.zero, taille).fontStyle = FontStyle.Italic;
        return t;
    }

    static DemoUI ConstruireDemo(Transform canvas, string nomScene, Vector2 position, Vector2 taille,
        RectTransform[] cibles, RectTransform rubanVersAutreScene)
    {
        RectTransform racine = Vide(canvas, "Demo", Vector2.zero, Vector2.zero);
        var demo = racine.gameObject.AddComponent<DemoUI>();
        demo.scene = nomScene;
        demo.cibles = cibles;
        demo.rubanVersAutreScene = rubanVersAutreScene;
        demo.spriteNeutre = sorciere[0];
        demo.spriteContente = sorciere[1];

        demo.imageSurbrillance = Bloc(racine, "Surbrillance", Palette.AvecAlpha(Palette.Bougie, 0.4f), Vector2.zero, new Vector2(100, 100));
        demo.surbrillance = demo.imageSurbrillance.rectTransform;

        Image panneau = Parchemin(racine, "Panneau_Demo", position, taille);
        panneau.raycastTarget = true;
        demo.panneau = panneau.transform.parent.gameObject;

        // Portrait de la sorcière qui dépasse à gauche du parchemin
        float gaucheX = -taille.x / 2f;
        demo.portrait = Bloc(panneau.transform, "Sorciere", Color.white, new Vector2(gaucheX + 95, 10), new Vector2(190, 260));
        demo.portrait.type = Image.Type.Simple;
        demo.portrait.preserveAspect = true;
        demo.portrait.sprite = sorciere[0];
        demo.portrait.enabled = sorciere[0] != null;

        float largeurTexte = taille.x - 230;
        float texteX = gaucheX + 200 + largeurTexte / 2f;
        Texte(panneau.transform, "Nom", "LA SORCIÈRE", 16, Palette.MagentaPotion, new Vector2(texteX, taille.y / 2f - 24),
            new Vector2(largeurTexte, 24), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
        demo.texte = Texte(panneau.transform, "Texte", "", 21, Palette.Encre, new Vector2(texteX, 10),
            new Vector2(largeurTexte, taille.y - 110), TextAnchor.UpperLeft);
        demo.boutonPasser = Bouton(panneau.transform, "Bouton_Passer", "Passer la démo", Palette.Lavande, Palette.Laiton,
            new Vector2(texteX - 110, -taille.y / 2f + 38), new Vector2(210, 50), 19);
        demo.boutonSuivant = Bouton(panneau.transform, "Bouton_Suivant", "Suivant", Palette.Laiton, Palette.Encre,
            new Vector2(texteX + 120, -taille.y / 2f + 38), new Vector2(190, 50), 21);
        demo.texteSuivant = demo.boutonSuivant.GetComponentInChildren<Text>();
        return demo;
    }

    // Parchemin avec un liseré plus foncé
    // (l'ombre et le papier sont rangés dans un même objet : on les cache ensemble avec Parent)
    static Image Parchemin(Transform parent, string nom, Vector2 position, Vector2 taille)
    {
        RectTransform racine = Vide(parent, nom + "_Groupe", position, taille);
        Bloc(racine, "Ombre", Palette.AvecAlpha(Palette.Contour, 0.5f), new Vector2(6, -6), taille);
        Image p = Bloc(racine, nom, Palette.Parchemin, Vector2.zero, taille);
        Image lisere = Bloc(p.transform, "Lisere", Palette.AvecAlpha(Palette.Laiton, 0.45f), Vector2.zero, taille - new Vector2(18, 18));
        Bloc(lisere.transform, "Interieur", Palette.Parchemin, Vector2.zero, taille - new Vector2(24, 24));
        return p;
    }

    // Ruban lavande avec texte doré (bouton de navigation de la DA)
    static Button Ruban(Transform parent, string nom, string libelle, Vector2 position, Vector2 taille)
    {
        Button b = Bouton(parent, nom, libelle, Palette.Lavande, Palette.Laiton, position, taille, 24);
        Bloc(b.transform, "Ourlet", Palette.AvecAlpha(Palette.VioletMagique, 0.6f), new Vector2(0, -taille.y / 2f + 4), new Vector2(taille.x, 6));
        return b;
    }

    // Bordure violette autour de l'écran (« Cadres : bordure violette ornée »)
    static void Cadre(Transform canvas)
    {
        Color c = Palette.VioletMagique;
        Bloc(canvas, "Cadre_Haut", c, new Vector2(0, 533), new Vector2(1920, 14));
        Bloc(canvas, "Cadre_Bas", c, new Vector2(0, -533), new Vector2(1920, 14));
        Bloc(canvas, "Cadre_Gauche", c, new Vector2(-953, 0), new Vector2(14, 1080));
        Bloc(canvas, "Cadre_Droit", c, new Vector2(953, 0), new Vector2(14, 1080));
        foreach (Vector2 coin in new[] { new Vector2(-940, 520), new Vector2(940, 520), new Vector2(-940, -520), new Vector2(940, -520) })
            Bloc(canvas, "Ornement", Palette.Laiton, coin, new Vector2(20, 20)).rectTransform.localEulerAngles = new Vector3(0, 0, 45);
    }

    static Image[] Etoiles(Transform parent, Vector2 centre, float taille)
    {
        var etoiles = new Image[3];
        for (int i = 0; i < 3; i++)
            etoiles[i] = Bloc(parent, "Etoile_" + (i + 1), Palette.AvecAlpha(Palette.Contour, 0.3f),
                centre + new Vector2((i - 1) * taille * 1.4f, 0), new Vector2(taille, taille), true);
        return etoiles;
    }

    // Trait épais entre deux points (chemin de la carte)
    static void Segment(Transform parent, Vector2 a, Vector2 b, float epaisseur, Color couleur)
    {
        Vector2 d = b - a;
        Image s = Bloc(parent, "Chemin", couleur, (a + b) / 2f, new Vector2(d.magnitude + epaisseur, epaisseur));
        s.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    // ------------------------------------------------------------------
    // Outils de base
    // ------------------------------------------------------------------

    // Le canvas des boutons, tickets, bulle... par-dessus tout
    static Transform PreparerScene()
    {
        foreach (string nom in new[] { NomCanvas, NomCanvasClients, NomEventSystem, NomControleur })
        {
            GameObject ancien = GameObject.Find(nom);
            if (ancien != null) Object.DestroyImmediate(ancien);
        }

        // Un seul EventSystem par scène (la Cuisine et la Boutique de Sarah en ont déjà un)
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject(NomEventSystem, typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var canvasGO = NouveauCanvas(NomCanvas);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        return canvasGO.transform;
    }

    // Canvas « caméra » rangé entre le fond (ordre -100) et le comptoir (ordre 50) de Lou-Ann
    static Transform CanvasEntreLesDecors()
    {
        var canvasGO = NouveauCanvas(NomCanvasClients);
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
        canvas.planeDistance = 5f;
        canvas.sortingOrder = 0;
        Object.DestroyImmediate(canvasGO.GetComponent<GraphicRaycaster>());
        return canvasGO.transform;
    }

    // Réglages de la DA : 1920 x 1080, Match 0,5
    static GameObject NouveauCanvas(string nom)
    {
        var canvasGO = new GameObject(nom, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvasGO;
    }

    // Le prefab StockManager de Sarah (stock, argent, poison) doit exister dès la première scène jouée
    static void AssurerStockManager()
    {
        if (Object.FindFirstObjectByType<StockManager>() != null) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StockManager.prefab");
        if (prefab != null) PrefabUtility.InstantiatePrefab(prefab);
    }

    // Une image de Lou-Ann si elle existe, sinon une forme simple
    static Image SpriteOuForme(Transform parent, string nom, string chemin, Vector2 position, Vector2 taille)
    {
        Image image = Bloc(parent, nom, Palette.AvecAlpha(Palette.Lavande, 0.4f), position, taille, true);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(chemin);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
        return image;
    }

    static void Sauver(UnityEngine.SceneManagement.Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // La carte en premier : c'est l'écran de départ du jeu
    static void AjouterScenesAuBuild()
    {
        var ordre = new[] { "MenuCarte", "Comptoir", "Cuisine", "Boutique" };
        var scenes = new List<EditorBuildSettingsScene>();
        foreach (string nom in ordre)
            scenes.Add(new EditorBuildSettingsScene(DossierScenes + nom + ".unity", true));
        foreach (var s in EditorBuildSettings.scenes)
            if (!scenes.Exists(x => x.path == s.path)) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static RectTransform Vide(Transform parent, string nom, Vector2 position, Vector2 taille)
    {
        var go = new GameObject(nom, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = position;
        rt.sizeDelta = taille;
        return rt;
    }

    static Image Bloc(Transform parent, string nom, Color couleur, Vector2 position, Vector2 taille, bool estRond = false)
    {
        RectTransform rt = Vide(parent, nom, position, taille);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = estRond ? rond : carre;
        image.type = estRond ? Image.Type.Simple : Image.Type.Sliced;
        image.color = couleur;
        image.raycastTarget = false;
        return image;
    }

    static Text Texte(Transform parent, string nom, string contenu, int taille, Color couleur,
        Vector2 position, Vector2 dimensions, TextAnchor alignement = TextAnchor.MiddleCenter)
    {
        RectTransform rt = Vide(parent, nom, position, dimensions);
        var texte = rt.gameObject.AddComponent<Text>();
        texte.font = police;
        texte.fontSize = taille;
        texte.color = couleur;
        texte.alignment = alignement;
        texte.text = contenu;
        texte.supportRichText = true;
        texte.horizontalOverflow = HorizontalWrapMode.Wrap;
        texte.verticalOverflow = VerticalWrapMode.Overflow;
        texte.raycastTarget = false;
        return texte;
    }

    static Button Bouton(Transform parent, string nom, string libelle, Color fond, Color couleurTexte,
        Vector2 position, Vector2 taille, int tailleTexte)
    {
        Image image = Bloc(parent, nom, fond, position, taille);
        image.raycastTarget = true;
        var bouton = image.gameObject.AddComponent<Button>();
        bouton.targetGraphic = image;
        Text texte = Texte(image.transform, "Texte", libelle, tailleTexte, couleurTexte, Vector2.zero, taille);
        texte.fontStyle = FontStyle.Bold;
        return bouton;
    }
}
