using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scène Comptoir (vue de face) : les clients commandent, attendent, paient et réagissent.
// Le décor est celui de Lou-Ann (KAN-63) : le client est dessiné entre le fond et le comptoir.
public class ComptoirController : MonoBehaviour
{
    [Header("Données")]
    public ListeNiveaux liste;
    [Tooltip("Niveau joué si on lance Play directement depuis cette scène")]
    public NiveauData niveauParDefaut;

    [Header("Tickets (jusqu'à 4)")]
    public TicketUI[] tickets = new TicketUI[4];

    [Header("Client au comptoir")]
    public RectTransform client;
    public Image cape;
    Image[] morceauxCape;
    public Image capuche;
    public Image tete;
    public Image spriteClient;
    [Tooltip("Visage provisoire : sourcils et bouche bougent selon l'humeur")]
    public RectTransform sourcilGauche;
    public RectTransform sourcilDroit;
    public Image bouche;
    public BulleDialogue bulle;

    [Header("Service")]
    public GameObject bol;
    public Image contenuBol;
    public GameObject fiole;
    public Image contenuFiole;
    public Text texteService;
    public Button cloche;
    public Text texteCloche;
    public Text gainVolant;

    [Header("Navigation et messages")]
    public Button rubanCuisine;
    public Button rubanBoutique;
    public Text texteMessage;

    const float PositionClient = -540f;
    const float HorsEcran = -1350f;

    GameManager gm;
    CommandeClient clientAffiche;
    int compteurGainsVu;
    float tempsGain;

    void Start()
    {
        gm = GameManager.Assurer(liste);
        if (!gm.NiveauEnCours)
            gm.LancerNiveau(niveauParDefaut != null ? niveauParDefaut : liste.niveaux[0]);

        for (int i = 0; i < tickets.Length; i++) tickets[i].Initialiser(i);

        rubanCuisine.onClick.AddListener(() => gm.AllerA(GameManager.SceneCuisine));
        rubanBoutique.onClick.AddListener(() => gm.AllerA(GameManager.SceneBoutique));
        cloche.onClick.AddListener(() => gm.Servir());

        // En revenant de la cuisine, le client est déjà là : pas d'entrée animée
        clientAffiche = gm.Selection;
        compteurGainsVu = gm.CompteurGains;
        gainVolant.gameObject.SetActive(false);

        gm.SignalerArrivee(GameManager.SceneComptoir);
    }

    void Update()
    {
        if (!gm.NiveauEnCours) return;
        LireClavier();

        foreach (TicketUI t in tickets) t.Afficher(gm);
        MettreAJourClient();
        MettreAJourService();
        MettreAJourGain();

        texteMessage.text = gm.Message;
    }

    // Tab : cuisine, B : boutique, Espace : servir, 1 à 4 : choisir un ticket
    void LireClavier()
    {
        Keyboard clavier = Keyboard.current;
        if (clavier == null || gm.NiveauTermine) return;

        if (clavier.tabKey.wasPressedThisFrame) gm.AllerA(GameManager.SceneCuisine);
        if (clavier.bKey.wasPressedThisFrame) gm.AllerA(GameManager.SceneBoutique);
        if (clavier.spaceKey.wasPressedThisFrame) gm.Servir();
        if (clavier.digit1Key.wasPressedThisFrame) gm.Selectionner(0);
        if (clavier.digit2Key.wasPressedThisFrame) gm.Selectionner(1);
        if (clavier.digit3Key.wasPressedThisFrame) gm.Selectionner(2);
        if (clavier.digit4Key.wasPressedThisFrame) gm.Selectionner(3);
    }

    void MettreAJourClient()
    {
        CommandeClient c = gm.Selection;

        // Le client choisi vient au comptoir
        if (c != clientAffiche)
        {
            clientAffiche = c;
            if (c != null) DeplacerClient(HorsEcran);
        }

        client.gameObject.SetActive(c != null);
        if (c == null)
        {
            bulle.gameObject.SetActive(false);
            return;
        }

        // Il entre, et repart quand sa réaction est finie
        float cibleX = c.EnTrainDePartir && c.tempsAvantDepart < 0.7f ? HorsEcran : PositionClient;
        float x = Mathf.MoveTowards(client.anchoredPosition.x, cibleX, 2200f * Time.deltaTime);
        DeplacerClient(x);
        bool auComptoir = Mathf.Abs(x - PositionClient) < 1f;

        // Apparence : sprite de Lou-Ann s'il existe, sinon formes provisoires
        Color couleur = c.client != null ? c.client.couleurProvisoire : Palette.BleuBrume;
        Sprite sprite = c.client != null ? c.client.SpritePour(c.Humeur) : null;
        spriteClient.gameObject.SetActive(sprite != null);
        cape.gameObject.SetActive(sprite == null);
        if (sprite != null) spriteClient.sprite = sprite;

        if (morceauxCape == null) morceauxCape = cape.GetComponentsInChildren<Image>(true);   // cape + épaules
        foreach (Image morceau in morceauxCape) morceau.color = couleur;
        capuche.color = Color.Lerp(couleur, Palette.Contour, 0.25f);
        // Vert = poison : le client empoisonné repart avec le teint vert
        Color peau = Palette.Hex("#F2D2B6");
        if (c.reaction == Reaction.Empoisonne || (c.EstUnRemede && c.reaction == Reaction.Aucune))
            peau = Color.Lerp(peau, Palette.VertPoison, 0.45f);
        if (c.reaction == Reaction.Mort) peau = Palette.AvecAlpha(Palette.BleuBrume, 0.6f);
        tete.color = peau;
        DessinerVisage(c);

        bulle.gameObject.SetActive(auComptoir);
        string nom = c.client != null ? c.client.NomComplet : "Client";
        bulle.Afficher(nom.ToUpper(), c.replique);
    }

    void DeplacerClient(float x)
    {
        client.anchoredPosition = new Vector2(x, client.anchoredPosition.y);
    }

    // angle des sourcils (0 = calme, positif = fâché), taille et couleur de la bouche
    void DessinerVisage(CommandeClient c)
    {
        float angle = 0f, hauteurSourcils = 0f;
        Vector2 tailleBouche = new Vector2(46f, 9f);
        Color couleurBouche = Palette.Hex("#8A5A4A");

        switch (c.reaction)
        {
            case Reaction.Content:
                angle = -12f; hauteurSourcils = 8f;
                tailleBouche = new Vector2(62f, 28f); couleurBouche = Palette.Hex("#7A2A3E");
                break;
            case Reaction.Satisfait:
                tailleBouche = new Vector2(50f, 12f);
                break;
            case Reaction.Empoisonne:
                angle = -18f; tailleBouche = new Vector2(36f, 14f); couleurBouche = Palette.VertPoison;
                break;
            case Reaction.Mort:
                angle = -6f; tailleBouche = new Vector2(30f, 6f); couleurBouche = Palette.BleuBrume;
                break;
            case Reaction.Mecontent:
            case Reaction.PartiFache:
                angle = 28f; tailleBouche = new Vector2(50f, 16f); couleurBouche = Palette.Hex("#7A2A3E");
                break;
            default:
                if (c.Humeur == Humeur.Enerve) { angle = 14f; tailleBouche = new Vector2(40f, 8f); }
                if (c.Humeur == Humeur.Furieux) { angle = 28f; tailleBouche = new Vector2(48f, 14f); couleurBouche = Palette.Hex("#7A2A3E"); }
                break;
        }

        sourcilGauche.localEulerAngles = new Vector3(0f, 0f, -angle);
        sourcilDroit.localEulerAngles = new Vector3(0f, 0f, angle);
        sourcilGauche.anchoredPosition = new Vector2(sourcilGauche.anchoredPosition.x, 42f + hauteurSourcils);
        sourcilDroit.anchoredPosition = new Vector2(sourcilDroit.anchoredPosition.x, 42f + hauteurSourcils);
        bouche.rectTransform.sizeDelta = tailleBouche;
        bouche.color = couleurBouche;
    }

    void MettreAJourService()
    {
        Potion potion = gm.PotionPrete;
        bol.SetActive(potion != null && potion.DansUnBol);
        fiole.SetActive(potion != null && !potion.DansUnBol);

        if (potion != null)
        {
            Color couleur = Textes.CouleurPreparation(potion);
            contenuBol.color = couleur;
            contenuFiole.color = Palette.AvecAlpha(couleur, 0.6f);   // le verre reste visible
            texteService.text = potion.Nom;
        }
        else
        {
            texteService.text = "";
        }

        cloche.interactable = gm.PeutServir;
        texteCloche.text = gm.PeutServir ? "Servir" : "Cloche";
    }

    // « +12 » qui s'envole du pot à pièces
    void MettreAJourGain()
    {
        if (gm.CompteurGains != compteurGainsVu)
        {
            compteurGainsVu = gm.CompteurGains;
            tempsGain = 1.3f;
            gainVolant.text = gm.DernierGain > 0 ? "+" + gm.DernierGain : "0";
            gainVolant.gameObject.SetActive(true);
        }
        if (tempsGain <= 0f) return;

        tempsGain -= Time.deltaTime;
        var rt = gainVolant.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -330f + (1.3f - tempsGain) * 90f);
        gainVolant.color = Palette.AvecAlpha(Palette.Laiton, Mathf.Clamp01(tempsGain / 0.5f));
        if (tempsGain <= 0f) gainVolant.gameObject.SetActive(false);
    }
}
