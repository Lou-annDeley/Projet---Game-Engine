using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scène Cuisine : par-dessus le travail de l'équipe (décor de Lou-Ann, étagère, glisser-déposer,
// chaudron et grimoire de Sarah), ce script ajoute la boucle de jeu :
// les commandes en cours, le parchemin qui coche les ingrédients, Préparer / Vider,
// la préparation prête (bol ou fiole) et le retour au comptoir.
public class CuisineController : MonoBehaviour
{
    [Header("Données")]
    public ListeNiveaux liste;
    public NiveauData niveauParDefaut;

    [Header("Objets de l'équipe (trouvés par le menu de construction)")]
    public Chaudron chaudron;
    public Grimoire grimoire;
    [Tooltip("Mini-jeu de mélange (KAN-33, Sarah). Vide = trouvé automatiquement.")]
    public MiniJeuCuisson miniJeu;

    [Header("Mini-tickets")]
    public TicketUI[] tickets = new TicketUI[4];

    [Header("Parchemin (celui du décor, à droite)")]
    public Text titreCommande;
    public Text nomRecette;
    public Text listeIngredients;

    [Header("Préparation")]
    public GameObject fondPreparation;
    public RectTransform barrePreparation;
    public Button boutonPreparer;
    public Button boutonVider;

    [Header("Préparation prête")]
    public GameObject bol;
    public Image liquideBol;
    public GameObject fiole;
    public Image liquideFiole;
    public Text texteService;
    public Button boutonJeter;

    [Header("Navigation")]
    public Button rubanComptoir;
    public Button rubanBoutique;
    public Text texteMessage;

    [Header("Démo")]
    public DemoUI demo;

    GameManager gm;
    bool chaudronRestaure;
    int preparationsVues;
    GameObject livreGrimoire;

    void Start()
    {
        gm = GameManager.Assurer(liste);
        if (!gm.NiveauEnCours)
            gm.LancerNiveau(niveauParDefaut != null ? niveauParDefaut : liste.niveaux[0]);

        for (int i = 0; i < tickets.Length; i++) tickets[i].Initialiser(i);

        rubanComptoir.onClick.AddListener(() => gm.AllerA(GameManager.SceneComptoir));
        rubanBoutique.onClick.AddListener(() => gm.AllerA(GameManager.SceneBoutique));
        boutonPreparer.onClick.AddListener(Preparer);
        boutonVider.onClick.AddListener(Vider);
        boutonJeter.onClick.AddListener(() => gm.JeterPreparation());

        if (chaudron == null) chaudron = FindFirstObjectByType<Chaudron>();
        if (grimoire == null) grimoire = FindFirstObjectByType<Grimoire>();
        if (miniJeu == null) miniJeu = FindFirstObjectByType<MiniJeuCuisson>();
        if (chaudron != null) chaudron.ContenuChange += SurContenuChange;

        gm.SignalerArrivee(GameManager.SceneCuisine);
    }

    void OnDestroy()
    {
        if (chaudron != null) chaudron.ContenuChange -= SurContenuChange;
    }

    void SurContenuChange()
    {
        gm.SynchroniserChaudron(chaudron.Contenu);
    }

    // KAN-33 (Sarah) : le mini-jeu de mélange passe avant la cuisson.
    // S'il n'y a pas de mini-jeu dans la scène, on lance directement la cuisson comme avant.
    void Preparer()
    {
        if (miniJeu != null && miniJeu.isActiveAndEnabled) miniJeu.Demarrer();
        else gm.LancerPreparation();
    }

    void Vider()
    {
        if (gm.EnPreparation || chaudron == null) return;
        if (miniJeu != null && miniJeu.EnCours) return;
        chaudron.Vider();
    }

    void Update()
    {
        if (!gm.NiveauEnCours) return;
        RelierAuChaudron();
        RelierAuGrimoire();
        LireClavier();

        foreach (TicketUI t in tickets) t.Afficher(gm);
        MettreAJourParchemin();
        MettreAJourPreparation();
        MettreAJourService();

        texteMessage.text = gm.NiveauTermine
            ? "La journée est finie : retourne au comptoir pour le bilan."
            : gm.Message;
    }

    // Le chaudron de Sarah ne garde rien d'une scène à l'autre :
    // au retour en cuisine on le remplit avec ce que le GameManager a gardé.
    void RelierAuChaudron()
    {
        if (chaudron == null) return;

        if (!chaudronRestaure)
        {
            chaudronRestaure = true;   // une frame après Start, quand le chaudron est prêt
            preparationsVues = gm.PreparationsFinies;
            chaudron.ContenuChange -= SurContenuChange;
            foreach (var ingredient in gm.ContenuChaudron) chaudron.AjouterIngredient(ingredient);
            chaudron.ContenuChange += SurContenuChange;
        }

        // La préparation est finie : on vide le chaudron
        if (preparationsVues != gm.PreparationsFinies)
        {
            preparationsVues = gm.PreparationsFinies;
            chaudron.ContenuChange -= SurContenuChange;
            chaudron.Vider();
            chaudron.ContenuChange += SurContenuChange;
        }
    }

    // Le ruban et le livre du grimoire sont créés par le script de Sarah au lancement
    void RelierAuGrimoire()
    {
        if (grimoire == null) return;
        if (livreGrimoire == null)
        {
            Transform t = grimoire.transform.Find("Canvas_Grimoire/Livre");
            if (t != null) livreGrimoire = t.gameObject;
            if (demo != null && demo.cibles[(int)EtapeDemo.Grimoire] == null)
                demo.cibles[(int)EtapeDemo.Grimoire] = TrouverRubanGrimoire();
        }
        if (livreGrimoire != null && livreGrimoire.activeSelf) gm.SignalerGrimoireOuvert();
    }

    RectTransform TrouverRubanGrimoire()
    {
        foreach (Button b in grimoire.GetComponentsInChildren<Button>(true))
        {
            Text t = b.GetComponentInChildren<Text>(true);
            if (t != null && t.text == "Grimoire") return (RectTransform)b.transform;
        }
        return null;
    }

    // Tab : comptoir, B : boutique, G : grimoire, Espace : préparer, Retour arrière : vider
    void LireClavier()
    {
        Keyboard clavier = Keyboard.current;
        if (clavier == null) return;

        if (clavier.tabKey.wasPressedThisFrame) gm.AllerA(GameManager.SceneComptoir);
        if (clavier.bKey.wasPressedThisFrame) gm.AllerA(GameManager.SceneBoutique);
        if (clavier.gKey.wasPressedThisFrame && grimoire != null) grimoire.Basculer();
        if (livreGrimoire != null && livreGrimoire.activeSelf) return;
        if (miniJeu != null && miniJeu.EnCours) return;
        if (clavier.spaceKey.wasPressedThisFrame) Preparer();
        if (clavier.backspaceKey.wasPressedThisFrame) Vider();
    }

    // ---------- Parchemin : la commande choisie, cochée au fur et à mesure ----------

    void MettreAJourParchemin()
    {
        CommandeClient c = gm.Selection;
        if (c == null)
        {
            titreCommande.text = "AUCUNE COMMANDE";
            nomRecette.text = "";
            listeIngredients.text = "Choisis un ticket\nen haut pour voir\nla recette ici.";
            return;
        }

        titreCommande.text = "COMMANDE N°" + c.numero + "\n" + c.Metier.ToUpper();
        nomRecette.text = c.NomCommande;

        var dejaComptes = new Dictionary<IngredientData, int>();
        var sb = new StringBuilder();
        foreach (IngredientData ingredient in c.IngredientsCommande)
        {
            dejaComptes.TryGetValue(ingredient, out int n);
            dejaComptes[ingredient] = n + 1;
            bool coche = gm.DansLeChaudron(ingredient) > n;

            if (coche) sb.Append("<color=#704941>√ ").Append(ingredient.nom).Append("</color>\n");
            else if (gm.StockDe(ingredient) <= 0) sb.Append("□ ").Append(ingredient.nom).Append(" <color=#A8324A>(épuisé)</color>\n");
            else sb.Append("□ ").Append(ingredient.nom).Append("\n");
        }
        if (c.EnTrainDePartir) sb.Append("<color=#693287>Commande terminée</color>");
        listeIngredients.text = sb.ToString();
    }

    void MettreAJourPreparation()
    {
        fondPreparation.SetActive(gm.EnPreparation);
        float avancement = 1f - gm.PreparationRestante / GameManager.DureePreparation;
        barrePreparation.localScale = new Vector3(Mathf.Clamp01(avancement), 1f, 1f);

        boutonPreparer.interactable = gm.PeutPreparer;
        boutonVider.interactable = !gm.EnPreparation && gm.ContenuChaudron.Count > 0;
    }

    // ---------- Bol (soupes) et fiole (potions, remèdes) : sprites de Lou-Ann ----------

    void MettreAJourService()
    {
        Potion potion = gm.PotionPrete;
        bol.SetActive(potion != null && potion.DansUnBol);
        fiole.SetActive(potion != null && !potion.DansUnBol);
        boutonJeter.gameObject.SetActive(potion != null);

        if (potion == null)
        {
            texteService.text = gm.EnPreparation ? "Ça bout..." : "";
            return;
        }
        Color couleur = Textes.CouleurPreparation(potion);
        liquideBol.color = couleur;
        liquideFiole.color = Palette.AvecAlpha(couleur, 0.6f);   // le verre reste visible
        texteService.text = potion.empoisonnee ? potion.Nom + " (empoisonnée)" : potion.Nom;
    }
}
