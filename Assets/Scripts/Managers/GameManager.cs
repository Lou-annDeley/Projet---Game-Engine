using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ce qui sort du chaudron une fois la préparation finie.
public class Potion
{
    public RecetteData recette;      // recette reconnue (null = ratée ou remède)
    public RemedeData remede;        // remède reconnu
    public bool empoisonnee;
    public IngredientData poison;    // l'ingrédient toxique utilisé
    public List<IngredientData> contenu = new List<IngredientData>();

    public string Nom => remede != null ? remede.nom : recette != null ? recette.nom : "Préparation ratée";
    public bool DansUnBol => recette == null ? remede == null : Textes.DansUnBol(recette);
}

// GameManager persistant entre les scènes (KAN-28).
// Il n'est placé dans aucune scène : il se crée tout seul la première fois
// et survit aux changements de scène (DontDestroyOnLoad).
//
// Il s'appuie sur le travail de l'équipe au lieu de le refaire :
//   argent       -> ArgentManager (Sarah)       stock      -> StockManager (Sarah)
//   niveau       -> Progression (Sarah)         poison     -> PoisonManager + ClientsEmpoisonnes (Sarah)
//   HUD          -> HUD (Lou-Ann)               fin        -> EcranFinNiveau (Lou-Ann)
// et il garde le reste : clients, commandes en cours, temps, contenu du chaudron, progression de la carte.
public class GameManager : MonoBehaviour
{
    public const string SceneCarte = "MenuCarte";
    public const string SceneComptoir = "Comptoir";
    public const string SceneCuisine = "Cuisine";
    public const string SceneBoutique = "Boutique";

    public const float DureePreparation = 3f;
    public const int CommandesMaxAbsolu = 4;
    const float DureeReaction = 2.6f;
    const float ChanceRetour = 0.3f;        // KAN-42 : chance qu'un ancien client revienne
    const float ChanceRetourMalade = 0.5f;  // KAN-56 : chance qu'un client empoisonné revienne demander un remède
    public const int SeuilFinEmpoisonneuse = 3;

    public static GameManager Instance { get; private set; }

    // ---------- Partie ----------
    public ListeNiveaux Liste { get; private set; }
    public int NiveauMaxDebloque { get; private set; } = 1;
    public Dictionary<int, int> EtoilesParNiveau { get; } = new Dictionary<int, int>();

    readonly Dictionary<ClientData, int> dernierNiveauVu = new Dictionary<ClientData, int>();
    bool demoDejaFaite;
    int empoisonnesSansManager;

    // ---------- Niveau en cours ----------
    public NiveauData Niveau { get; private set; }
    public float TempsRestant { get; private set; }
    public List<CommandeClient> File { get; } = new List<CommandeClient>();
    public CommandeClient Selection { get; private set; }
    public string Message { get; private set; } = "";

    // Copie du contenu du chaudron de Sarah : il reste quand on change de scène
    public List<IngredientData> ContenuChaudron { get; } = new List<IngredientData>();
    public Potion PotionPrete { get; private set; }
    public float PreparationRestante { get; private set; }
    public int PreparationsFinies { get; private set; }   // la cuisine s'en sert pour vider le chaudron
    List<IngredientData> enPreparation;

    public int ClientsArrives { get; private set; }
    public int ClientsServis { get; private set; }
    public int ClientsPartis { get; private set; }
    public int EmpoisonnesDuNiveau { get; private set; }
    public int ArgentGagne { get; private set; }
    public int Pourboires { get; private set; }

    public int CompteurGains { get; private set; }
    public int DernierGain { get; private set; }

    public EtapeDemo EtapeActuelle { get; private set; } = EtapeDemo.Terminee;
    public bool DemoActive => EtapeActuelle != EtapeDemo.Terminee;

    float attenteProchainClient;
    int numeroCommande;
    bool finEnregistree;
    int argentDebutNiveau;
    readonly Dictionary<IngredientData, int> stockDebutNiveau = new Dictionary<IngredientData, int>();

    // ================================================================
    // Création
    // ================================================================

    public static GameManager Assurer(ListeNiveaux liste)
    {
        if (Instance == null)
        {
            var go = new GameObject("GameManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameManager>();
            go.AddComponent<DebugTouches>();
            // Boutons de l'écran de fin de Lou-Ann (KAN-77)
            EcranFinNiveau.Continuer += Instance.RetourCarte;
            EcranFinNiveau.Rejouer += Instance.Rejouer;
        }
        if (Instance.Liste == null) Instance.Liste = liste;
        return Instance;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        EcranFinNiveau.Continuer -= RetourCarte;
        EcranFinNiveau.Rejouer -= Rejouer;
        Instance = null;
    }

    // ---------- Raccourcis vers les managers de Sarah ----------
    public int Argent => ArgentManager.Instance != null ? ArgentManager.Instance.Pieces : 0;

    public int StockDe(IngredientData ingredient) =>
        StockManager.Instance != null ? StockManager.Instance.Quantite(ingredient) : 0;

    public int EmpoisonnesTotal =>
        ClientsEmpoisonnes.Instance != null ? ClientsEmpoisonnes.Instance.totalEmpoisonnes : empoisonnesSansManager;

    List<RemedeData> Remedes =>
        ClientsEmpoisonnes.Instance != null ? ClientsEmpoisonnes.Instance.remedes : new List<RemedeData>();

    // ================================================================
    // Lancer un niveau
    // ================================================================

    public void LancerNiveau(NiveauData niveau)
    {
        Niveau = niveau;
        TempsRestant = niveau.dureeSecondes;
        File.Clear();
        Selection = null;
        ContenuChaudron.Clear();
        PotionPrete = null;
        PreparationRestante = 0f;
        enPreparation = null;
        ClientsArrives = ClientsServis = ClientsPartis = EmpoisonnesDuNiveau = 0;
        ArgentGagne = Pourboires = 0;
        attenteProchainClient = niveau.delaiEntreClients - 1.5f; // le premier client arrive vite
        finEnregistree = false;
        Message = "";

        // L'étagère, le poison et le HUD de l'équipe suivent le numéro du niveau
        Progression.ChangerNiveau(niveau.numero);

        argentDebutNiveau = Argent;
        stockDebutNiveau.Clear();
        if (StockManager.Instance != null)
            foreach (var ingredient in StockManager.Instance.ingredients)
                if (ingredient != null) stockDebutNiveau[ingredient] = StockDe(ingredient);

        if (EcranFinNiveau.Instance != null) EcranFinNiveau.Instance.Cacher();

        EtapeActuelle = EtapeDemo.Terminee;
        if (niveau.avecDemo && !demoDejaFaite)
        {
            EtapeActuelle = EtapeDemo.Bienvenue;
            NouveauClient(RecetteLaPlusSimple());
        }
    }

    RecetteData RecetteLaPlusSimple()
    {
        RecetteData plusSimple = Niveau.recettes[0];
        foreach (var recette in Niveau.recettes)
            if (recette.ingredients.Count < plusSimple.ingredients.Count) plusSimple = recette;
        return plusSimple;
    }

    // ================================================================
    // Infos lues par l'interface
    // ================================================================

    public bool NiveauEnCours => Niveau != null;
    public bool EnPreparation => PreparationRestante > 0f;

    public bool NiveauTermine =>
        Niveau != null && !DemoActive && File.Count == 0 &&
        (TempsRestant <= 0f || ClientsArrives >= Niveau.nombreClients);

    public bool ObjectifAtteint => Niveau != null && ClientsServis >= Niveau.objectifClients;

    public int Etoiles
    {
        get
        {
            if (!ObjectifAtteint) return 0;
            if (ClientsServis >= Niveau.nombreClients) return 3;
            if (ClientsServis >= Niveau.nombreClients - 1) return 2;
            return 1;
        }
    }

    public int EtoilesDe(NiveauData niveau) =>
        EtoilesParNiveau.TryGetValue(niveau.numero, out int etoiles) ? etoiles : 0;

    public bool EstDebloque(NiveauData niveau) => niveau.numero <= NiveauMaxDebloque;

    public int ClientsQuiAttendent
    {
        get
        {
            int n = 0;
            foreach (var c in File) if (!c.EnTrainDePartir) n++;
            return n;
        }
    }

    public int DansLeChaudron(IngredientData ingredient)
    {
        int n = 0;
        foreach (var i in ContenuChaudron) if (i == ingredient) n++;
        return n;
    }

    // Ingrédients de la commande qu'on ne peut plus ajouter (bac vide)
    public List<IngredientData> IngredientsManquants(CommandeClient c)
    {
        var manquants = new List<IngredientData>();
        foreach (var ingredient in c.IngredientsCommande)
            if (StockDe(ingredient) <= 0 && DansLeChaudron(ingredient) == 0 && !manquants.Contains(ingredient))
                manquants.Add(ingredient);
        return manquants;
    }

    // ================================================================
    // Boucle
    // ================================================================

    void Update()
    {
        if (Niveau == null) return;
        float dt = Time.deltaTime;

        if (EnPreparation)
        {
            PreparationRestante -= dt;
            if (PreparationRestante <= 0f) FinirPreparation();
        }

        for (int i = File.Count - 1; i >= 0; i--)
        {
            CommandeClient c = File[i];
            if (!c.EnTrainDePartir) continue;
            c.tempsAvantDepart -= dt;
            if (c.tempsAvantDepart <= 0f) RetirerDeLaFile(c);
        }

        MettreAJourHUD();

        // Pendant la démo : le temps est arrêté, le client attend, personne d'autre n'entre
        if (DemoActive) return;

        if (TempsRestant > 0f) TempsRestant = Mathf.Max(0f, TempsRestant - dt);

        foreach (CommandeClient c in File)
        {
            if (c.EnTrainDePartir) continue;
            c.patienceRestante -= dt;
            if (c.patienceRestante <= 0f) PartirFache(c);
        }

        bool encoreDesClients = TempsRestant > 0f && ClientsArrives < Niveau.nombreClients;
        if (encoreDesClients && ClientsQuiAttendent < Mathf.Min(Niveau.commandesMax, CommandesMaxAbsolu))
        {
            attenteProchainClient += dt;
            if (attenteProchainClient >= Niveau.delaiEntreClients) NouveauClient();
        }

        if (NiveauTermine && !finEnregistree) EnregistrerFin();
    }

    // Le HUD de Lou-Ann : temps restant et objectif (KAN-73)
    void MettreAJourHUD()
    {
        if (HUD.Instance == null) return;
        HUD.Instance.AfficherTemps(TempsRestant);
        HUD.Instance.AfficherObjectif("Servis : " + ClientsServis + " / " + Niveau.objectifClients);
    }

    // ================================================================
    // Clients (KAN-37, KAN-38, KAN-42, KAN-56)
    // ================================================================

    void NouveauClient(RecetteData recetteImposee = null)
    {
        attenteProchainClient = 0f;
        var commande = new CommandeClient { numero = ++numeroCommande };

        // Un client empoisonné revient parfois avec une anomalie et demande son remède
        DossierEmpoisonne malade = recetteImposee == null ? ChoisirMalade() : null;
        if (malade != null)
        {
            commande.client = Liste.clients.Find(c => c != null && c.id == malade.clientId);
            commande.remede = malade.remede;
            commande.estDeRetour = true;
        }
        else
        {
            commande.client = ChoisirClient(out commande.estDeRetour);
            commande.recette = recetteImposee != null ? recetteImposee : ChoisirRecette(commande.client);
        }

        float facteur = commande.client != null ? commande.client.facteurPatience : 1f;
        commande.patienceMax = Niveau.patienceSecondes * facteur;
        commande.patienceRestante = commande.patienceMax;
        commande.replique = PhraseDeCommande(commande);

        File.Add(commande);
        ClientsArrives++;
        if (Selection == null || Selection.EnTrainDePartir) Selection = commande;
        if (commande.client != null) dernierNiveauVu[commande.client] = Niveau.numero;
    }

    DossierEmpoisonne ChoisirMalade()
    {
        if (ClientsEmpoisonnes.Instance == null || Liste == null || Random.value > ChanceRetourMalade) return null;
        var possibles = new List<DossierEmpoisonne>();
        foreach (var dossier in ClientsEmpoisonnes.Instance.malades)
        {
            ClientData client = Liste.clients.Find(c => c != null && c.id == dossier.clientId);
            if (client != null && dossier.remede != null && !EstDansLaFile(client)) possibles.Add(dossier);
        }
        return possibles.Count > 0 ? possibles[Random.Range(0, possibles.Count)] : null;
    }

    // KAN-42 : en général un visage nouveau du niveau, mais parfois un client
    // d'un niveau précédent revient. Jamais deux fois le même en même temps.
    ClientData ChoisirClient(out bool deRetour)
    {
        deRetour = false;
        if (Liste == null || Liste.clients.Count == 0) return null;

        var nouveaux = new List<ClientData>();
        var anciens = new List<ClientData>();
        foreach (ClientData c in Liste.clients)
        {
            if (c == null || c.niveauApparition > Niveau.numero || EstDansLaFile(c)) continue;
            bool dejaVenuAvant = c.niveauApparition < Niveau.numero ||
                                 (dernierNiveauVu.TryGetValue(c, out int n) && n < Niveau.numero);
            if (dejaVenuAvant) anciens.Add(c); else nouveaux.Add(c);
        }

        if (anciens.Count > 0 && (nouveaux.Count == 0 || Random.value < ChanceRetour))
        {
            deRetour = true;
            return anciens[Random.Range(0, anciens.Count)];
        }
        if (nouveaux.Count > 0) return nouveaux[Random.Range(0, nouveaux.Count)];
        return Liste.clients[Random.Range(0, Liste.clients.Count)];
    }

    bool EstDansLaFile(ClientData client)
    {
        foreach (var c in File) if (c.client == client) return true;
        return false;
    }

    RecetteData ChoisirRecette(ClientData client)
    {
        var possibles = new List<RecetteData>();
        if (client != null)
            foreach (var r in client.commandesPossibles)
                if (Niveau.recettes.Contains(r)) possibles.Add(r);
        if (possibles.Count == 0) possibles.AddRange(Niveau.recettes);
        return possibles[Random.Range(0, possibles.Count)];
    }

    string PhraseDeCommande(CommandeClient c)
    {
        if (c.EstUnRemede)
            return "Regardez-moi ça : " + c.remede.anomalie.ToLower() + "... Il me faut " +
                   Textes.AvecArticle(c.remede.nom) + ", vite !";

        string modele = "Bonsoir ! Je voudrais {recette}, s'il vous plaît.";
        if (c.client != null && c.client.repliquesCommande != null && c.client.repliquesCommande.Length > 0)
            modele = c.client.repliquesCommande[Random.Range(0, c.client.repliquesCommande.Length)];

        string phrase = modele.Replace("{recette}", Textes.AvecArticle(c.recette.nom));
        if (c.estDeRetour && c.client != null && !string.IsNullOrEmpty(c.client.repliqueRetour))
            phrase = c.client.repliqueRetour + " " + phrase;
        return phrase;
    }

    public void Selectionner(int index)
    {
        if (index < 0 || index >= File.Count) return;
        Selection = File[index];
    }

    void PartirFache(CommandeClient c)
    {
        c.reaction = Reaction.PartiFache;
        c.gain = 0;
        c.tempsAvantDepart = DureeReaction;
        c.replique = c.client != null ? c.client.repliqueImpatient : "J'en ai assez d'attendre !";
        ClientsPartis++;
        Message = c.Nom + " est parti sans payer.";
    }

    void RetirerDeLaFile(CommandeClient c)
    {
        File.Remove(c);
        if (Selection == c)
        {
            Selection = null;
            foreach (var autre in File)
                if (!autre.EnTrainDePartir) { Selection = autre; break; }
        }
    }

    // ================================================================
    // Cuisine : le chaudron de Sarah (glisser-déposer) est recopié ici
    // ================================================================

    // Appelé par la cuisine à chaque changement du chaudron
    public void SynchroniserChaudron(IReadOnlyList<IngredientData> contenu)
    {
        if (EnPreparation) return;   // ce qui tombe pendant la cuisson ne compte pas
        ContenuChaudron.Clear();
        foreach (var i in contenu) if (i != null) ContenuChaudron.Add(i);

        if (ContenuChaudron.Count > 0 && EtapeActuelle == EtapeDemo.Grimoire) EtapeActuelle = EtapeDemo.Ingredients;
        if (EtapeActuelle == EtapeDemo.Ingredients && ChaudronCorrespondAUneCommande()) EtapeActuelle = EtapeDemo.Preparer;
        if (EtapeActuelle == EtapeDemo.Preparer && ContenuChaudron.Count == 0) EtapeActuelle = EtapeDemo.Ingredients;
    }

    bool ChaudronCorrespondAUneCommande()
    {
        Potion resultat = Evaluer(ContenuChaudron);
        if (resultat.empoisonnee) return false;
        foreach (var c in File)
        {
            if (c.EnTrainDePartir) continue;
            if (resultat.recette != null && c.recette == resultat.recette) return true;
            if (resultat.remede != null && c.remede == resultat.remede) return true;
        }
        return false;
    }

    public bool PeutPreparer => !EnPreparation && ContenuChaudron.Count > 0 && PotionPrete == null;

    public void LancerPreparation()
    {
        if (!PeutPreparer)
        {
            if (PotionPrete != null) Message = "Une préparation attend déjà : va d'abord la servir.";
            return;
        }
        enPreparation = new List<IngredientData>(ContenuChaudron);
        PreparationRestante = DureePreparation;
        Message = "";
    }

    void FinirPreparation()
    {
        PreparationRestante = 0f;
        PotionPrete = Evaluer(enPreparation);
        PreparationsFinies++;
        ContenuChaudron.Clear();   // la cuisine vide le chaudron de Sarah en voyant ça

        if (PotionPrete.recette == null && PotionPrete.remede == null) Message = "Oups, c'est raté... Jette-la et recommence.";
        else if (PotionPrete.empoisonnee) Message = PotionPrete.Nom + " est prête... avec une touche de poison.";
        else Message = PotionPrete.Nom + " est prête ! Retourne au comptoir pour servir.";

        if (EtapeActuelle >= EtapeDemo.Grimoire && EtapeActuelle <= EtapeDemo.Preparer)
        {
            bool bonne = PotionPrete.recette != null && !PotionPrete.empoisonnee;
            EtapeActuelle = bonne ? EtapeDemo.RetourComptoir : EtapeDemo.Ingredients;
        }
    }

    public void JeterPreparation()
    {
        if (PotionPrete == null) return;
        PotionPrete = null;
        Message = "Préparation jetée.";
        if (EtapeActuelle == EtapeDemo.RetourComptoir || EtapeActuelle == EtapeDemo.Servir)
            EtapeActuelle = EtapeDemo.Ingredients;
    }

    // Remède : exactement ses ingrédients (RemedeData.EstBienPrepare).
    // Recette : exactement ses ingrédients (RecetteData.EstBienPreparee), ou empoisonnée si au moins
    // un ingrédient est remplacé par un toxique (règle de l'Excel).
    public Potion Evaluer(List<IngredientData> contenu)
    {
        var potion = new Potion { contenu = new List<IngredientData>(contenu) };

        foreach (var remede in Remedes)
            if (remede != null && remede.EstBienPrepare(contenu)) { potion.remede = remede; return potion; }

        foreach (var recette in Niveau.recettes)
        {
            if (recette.EstBienPreparee(contenu)) { potion.recette = recette; return potion; }
            if (contenu.Count != recette.ingredients.Count) continue;

            var attendus = new List<IngredientData>(recette.ingredients);
            IngredientData poison = null;
            int toxiques = 0;
            bool correspond = true;
            foreach (var ingredient in contenu)
            {
                if (attendus.Remove(ingredient)) continue;
                if (ingredient.EstToxique) { toxiques++; poison = ingredient; continue; }
                correspond = false;
                break;
            }
            if (correspond && toxiques > 0 && toxiques < recette.ingredients.Count)
            {
                potion.recette = recette;
                potion.empoisonnee = true;
                potion.poison = poison;
                return potion;
            }
        }
        return potion;
    }

    // ================================================================
    // Service, réactions et paiement (KAN-40)
    // ================================================================

    public bool PeutServir => PotionPrete != null && Selection != null && !Selection.EnTrainDePartir;

    public void Servir()
    {
        if (!PeutServir) return;
        CommandeClient c = Selection;
        Potion potion = PotionPrete;
        PotionPrete = null;
        c.tempsAvantDepart = DureeReaction;

        if (c.EstUnRemede) ServirRemede(c, potion);
        else ServirRecette(c, potion);

        if (c.gain > 0 && ArgentManager.Instance != null) ArgentManager.Instance.Gagner(c.gain);
        ArgentGagne += c.gain;
        DernierGain = c.gain;
        CompteurGains++;

        if (DemoActive && EtapeActuelle <= EtapeDemo.Servir) EtapeActuelle = EtapeDemo.Fin;
    }

    void ServirRecette(CommandeClient c, Potion potion)
    {
        if (potion.recette == null || potion.recette != c.recette)
        {
            c.reaction = Reaction.Mecontent;
            c.replique = c.client != null ? c.client.repliqueMecontent : "Ce n'est pas ce que j'ai commandé !";
            ClientsPartis++;
            Message = c.Nom + " ne paie rien.";
            return;
        }

        int prix = PrixSelonHumeur(c);
        if (potion.empoisonnee)
        {
            // Il paie sans rien remarquer... et reviendra avec une anomalie (KAN-56, Sarah)
            c.reaction = Reaction.Empoisonne;
            c.replique = c.client != null ? c.client.repliqueEmpoisonne : "Mmh... drôle de goût.";
            EmpoisonnesDuNiveau++;
            if (ClientsEmpoisonnes.Instance != null) ClientsEmpoisonnes.Instance.Empoisonner(c.Id, c.Nom, potion.poison);
            else empoisonnesSansManager++;
        }
        else
        {
            AjouterPourboire(c, prix);
            c.reaction = c.pourboire > 0 ? Reaction.Content : Reaction.Satisfait;
            c.replique = c.client != null ? c.client.repliqueContent : "Merci !";
        }

        c.gain = prix + c.pourboire;
        ClientsServis++;
        Pourboires += c.pourboire;
        Message = MessagePaiement(c, prix);
    }

    void ServirRemede(CommandeClient c, Potion potion)
    {
        ResultatRemede resultat = ClientsEmpoisonnes.Instance != null
            ? ClientsEmpoisonnes.Instance.Soigner(c.Id, potion.contenu)
            : (c.remede.EstBienPrepare(potion.contenu) ? ResultatRemede.Gueri : ResultatRemede.PlusMalade);

        switch (resultat)
        {
            case ResultatRemede.Gueri:
                int prix = PrixSelonHumeur(c);
                AjouterPourboire(c, prix);
                c.reaction = Reaction.Content;
                c.replique = "Ça va beaucoup mieux, merci !";
                c.gain = prix + c.pourboire;
                ClientsServis++;
                Pourboires += c.pourboire;
                Message = MessagePaiement(c, prix);
                break;
            case ResultatRemede.PlusMalade:
                c.reaction = Reaction.Mecontent;
                c.replique = "Je me sens encore plus mal... Je reviendrai !";
                ClientsPartis++;
                Message = c.Nom + " repart encore plus malade.";
                break;
            default:
                c.reaction = Reaction.Mort;
                c.replique = "...";
                ClientsPartis++;
                Message = c.Nom + " ne s'en est pas remis... son fantôme rôde.";
                break;
        }
    }

    static int PrixSelonHumeur(CommandeClient c)
    {
        int prix = c.PrixCommande;
        return c.Humeur == Humeur.Furieux ? Mathf.CeilToInt(prix * 0.5f) : prix;
    }

    // Pourboire de 25 % si le client était encore calme
    static void AjouterPourboire(CommandeClient c, int prix)
    {
        c.pourboire = c.Humeur == Humeur.Calme ? Mathf.Max(1, Mathf.CeilToInt(prix * 0.25f)) : 0;
    }

    static string MessagePaiement(CommandeClient c, int prix) =>
        c.pourboire > 0
            ? c.Nom + " paie " + prix + " pièces et laisse " + c.pourboire + " de pourboire."
            : c.Nom + " paie " + prix + " pièces.";

    // ================================================================
    // Fin de niveau (KAN-30) : l'écran est celui de Lou-Ann (KAN-77)
    // ================================================================

    void EnregistrerFin()
    {
        finEnregistree = true;
        if (ObjectifAtteint)
        {
            NiveauMaxDebloque = Mathf.Max(NiveauMaxDebloque, Niveau.numero + 1);
            EtoilesParNiveau[Niveau.numero] = Mathf.Max(EtoilesDe(Niveau), Etoiles);
        }
        if (EcranFinNiveau.Instance != null)
            EcranFinNiveau.Instance.Afficher(ClientsServis, EmpoisonnesDuNiveau, ArgentGagne, ObjectifAtteint);
    }

    public int EtoilesTotales
    {
        get
        {
            int total = 0;
            foreach (var e in EtoilesParNiveau.Values) total += e;
            return total;
        }
    }

    // ================================================================
    // Démo (KAN-80)
    // ================================================================

    public void SuivantDemo()
    {
        if (EtapeActuelle == EtapeDemo.Bienvenue) EtapeActuelle = EtapeDemo.Patience;
        else if (EtapeActuelle == EtapeDemo.Patience) EtapeActuelle = EtapeDemo.AllerCuisine;
        else if (EtapeActuelle == EtapeDemo.Fin) TerminerDemo();
    }

    public void PasserDemo() => TerminerDemo();

    void TerminerDemo()
    {
        EtapeActuelle = EtapeDemo.Terminee;
        demoDejaFaite = true;
    }

    public void SignalerArrivee(string scene)
    {
        if (!DemoActive) return;
        if (scene == SceneCuisine && EtapeActuelle <= EtapeDemo.AllerCuisine) EtapeActuelle = EtapeDemo.Grimoire;
        if (scene == SceneComptoir && EtapeActuelle == EtapeDemo.RetourComptoir) EtapeActuelle = EtapeDemo.Servir;
    }

    public void SignalerGrimoireOuvert()
    {
        if (EtapeActuelle == EtapeDemo.Grimoire) EtapeActuelle = EtapeDemo.Ingredients;
    }

    // ================================================================
    // Navigation (KAN-29) : les commandes restent ici quand on change de scène
    // ================================================================

    public void AllerA(string scene) => SceneManager.LoadScene(scene);

    public void JouerNiveau(NiveauData niveau)
    {
        LancerNiveau(niveau);
        AllerA(SceneComptoir);
    }

    // On repart avec l'argent et le stock du début du niveau
    public void Rejouer()
    {
        if (Niveau == null) return;
        if (ArgentManager.Instance != null)
        {
            int ecart = Argent - argentDebutNiveau;
            if (ecart > 0) ArgentManager.Instance.Depenser(ecart);
            else if (ecart < 0) ArgentManager.Instance.Gagner(-ecart);
        }
        if (StockManager.Instance != null)
        {
            foreach (var paire in stockDebutNiveau)
            {
                int ecart = paire.Value - StockDe(paire.Key);
                if (ecart > 0) StockManager.Instance.Ajouter(paire.Key, ecart);
                else if (ecart < 0) StockManager.Instance.Retirer(paire.Key, -ecart);
            }
        }
        JouerNiveau(Niveau);
    }

    public void RetourCarte()
    {
        Niveau = null;
        File.Clear();
        Selection = null;
        ContenuChaudron.Clear();
        PotionPrete = null;
        PreparationRestante = 0f;
        EtapeActuelle = EtapeDemo.Terminee;
        if (HUD.Instance != null)
        {
            HUD.Instance.AfficherTemps(-1f);
            HUD.Instance.AfficherObjectif("");
        }
        if (EcranFinNiveau.Instance != null) EcranFinNiveau.Instance.Cacher();
        AllerA(SceneCarte);
    }

    // ================================================================
    // Touches de test (conseil d'Ali sur KAN-27), seulement dans l'éditeur
    // ================================================================

    public void TestNouveauClient()
    {
        if (Niveau == null) return;
        NouveauClient();
        CommandeClient c = File[File.Count - 1];
        Debug.Log("[Test] Nouveau client : " + c.Nom + " veut " + c.NomCommande);
    }

    public void TestPreparationParfaite()
    {
        if (Selection == null) return;
        PotionPrete = Evaluer(new List<IngredientData>(Selection.IngredientsCommande));
        Debug.Log("[Test] Préparation parfaite pour " + Selection.Nom + " : " + PotionPrete.Nom);
    }

    public void TestFinDuTemps()
    {
        TempsRestant = 0f;
        Debug.Log("[Test] Fin de la journée : plus aucun client n'entre.");
    }
}
