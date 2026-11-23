using UnityEngine;
using UnityEngine.UI;

// Un rond de niveau sur la carte.
[System.Serializable]
public class NoeudNiveau
{
    public Button bouton;
    public Image anneau;
    public Image fond;
    public Text numero;
    public Text nom;
    public Image[] etoiles = new Image[3];
}

// Carte des niveaux (scène MenuCarte), d'après la maquette de Lou-Ann :
// un chemin qui relie les niveaux, les niveaux verrouillés sont assombris,
// le panneau de droite détaille le niveau choisi.
// C'est là que la fin de niveau ramène (KAN-30). La version complète est KAN-48.
public class CarteNiveaux : MonoBehaviour
{
    public ListeNiveaux liste;
    public NoeudNiveau[] noeuds;

    [Header("Panneau de gauche : progression")]
    public Text texteProgression;
    public RectTransform curseurEmpoisonnement;
    public float largeurJauge = 300f;

    [Header("Panneau de droite : niveau choisi")]
    public Text etiquetteNiveau;
    public Text titreNiveau;
    public Text texteDifficulte;
    public Text texteObjectifs;
    public Image[] etoilesDetail = new Image[3];
    public Button boutonJouer;
    public Text texteJouer;

    [Header("Rubans")]
    public Button rubanBoutique;
    public Button rubanOptions;
    public Text texteMessage;

    GameManager gm;
    NiveauData choisi;

    void Start()
    {
        gm = GameManager.Assurer(liste);

        for (int i = 0; i < noeuds.Length; i++)
        {
            int index = i;
            bool existe = i < liste.niveaux.Count;
            noeuds[i].bouton.gameObject.SetActive(existe);
            if (existe) noeuds[i].bouton.onClick.AddListener(() => Choisir(liste.niveaux[index]));
        }

        boutonJouer.onClick.AddListener(() =>
        {
            if (choisi != null && gm.EstDebloque(choisi)) gm.JouerNiveau(choisi);
        });
        rubanBoutique.onClick.AddListener(() => gm.AllerA(GameManager.SceneBoutique));
        rubanOptions.onClick.AddListener(() => texteMessage.text = "Le menu Options arrive bientôt (KAN-81).");

        // On se place sur le dernier niveau débloqué
        int dernier = Mathf.Clamp(gm.NiveauMaxDebloque, 1, liste.niveaux.Count) - 1;
        Choisir(liste.niveaux[dernier]);
        texteMessage.text = "";
    }

    void Choisir(NiveauData niveau)
    {
        choisi = niveau;
        texteMessage.text = gm.EstDebloque(niveau) ? "" : "Termine le niveau précédent pour débloquer celui-ci.";
    }

    void Update()
    {
        for (int i = 0; i < noeuds.Length && i < liste.niveaux.Count; i++)
            AfficherNoeud(noeuds[i], liste.niveaux[i]);
        AfficherDetail();
        AfficherProgression();
    }

    void AfficherNoeud(NoeudNiveau noeud, NiveauData niveau)
    {
        bool ouvert = gm.EstDebloque(niveau);
        bool estChoisi = niveau == choisi;

        noeud.anneau.color = estChoisi ? Palette.MagentaPotion : ouvert ? Palette.Laiton : Palette.PierreLavande;
        noeud.fond.color = ouvert ? Palette.BoisRouge : Palette.Hex("#2A1F33");
        noeud.numero.color = ouvert ? Palette.Bougie : Palette.AvecAlpha(Palette.Lavande, 0.6f);
        noeud.nom.color = ouvert ? Palette.Parchemin : Palette.AvecAlpha(Palette.Parchemin, 0.45f);

        int etoiles = gm.EtoilesDe(niveau);
        for (int e = 0; e < noeud.etoiles.Length; e++)
            noeud.etoiles[e].color = e < etoiles ? Palette.Laiton : Palette.AvecAlpha(Palette.Parchemin, 0.25f);
    }

    void AfficherDetail()
    {
        if (choisi == null) return;
        bool ouvert = gm.EstDebloque(choisi);

        etiquetteNiveau.text = "NIVEAU " + choisi.numero;
        titreNiveau.text = choisi.nom;
        texteDifficulte.text = choisi.TexteDifficulte;
        texteDifficulte.color = CouleurDifficulte(choisi.difficulte);

        int minutes = Mathf.RoundToInt(choisi.dureeSecondes / 60f);
        texteObjectifs.text =
            "<b>Objectif</b>\n" +
            "•  Servir " + choisi.objectifClients + " clients sur " + choisi.nombreClients + "\n" +
            "•  Journée de " + minutes + " minutes\n" +
            "•  Jusqu'à " + choisi.commandesMax + (choisi.commandesMax > 1 ? " commandes" : " commande") + " en même temps\n\n" +
            "<b>Nouveautés</b>\n" + choisi.nouveautes +
            (string.IsNullOrEmpty(choisi.particularite) ? "" : "\n\n<i>" + choisi.particularite + "</i>");

        int etoiles = gm.EtoilesDe(choisi);
        for (int e = 0; e < etoilesDetail.Length; e++)
            etoilesDetail[e].color = e < etoiles ? Palette.Laiton : Palette.AvecAlpha(Palette.Contour, 0.3f);

        boutonJouer.interactable = ouvert;
        texteJouer.text = ouvert ? "JOUER" : "VERROUILLÉ";
    }

    // Jauge « sorcière gentille ↔ empoisonneuse » (prépare KAN-55)
    void AfficherProgression()
    {
        int debloques = Mathf.Min(gm.NiveauMaxDebloque, liste.niveaux.Count);
        texteProgression.text =
            "Pièces : " + gm.Argent + "\n" +
            "Niveaux : " + debloques + " / " + liste.niveaux.Count + "\n" +
            "Étoiles : " + gm.EtoilesTotales + " / " + liste.niveaux.Count * 3 + "\n" +
            "Clients empoisonnés : " + gm.EmpoisonnesTotal;

        float ratio = Mathf.Clamp01(gm.EmpoisonnesTotal / (float)(GameManager.SeuilFinEmpoisonneuse * 2));
        curseurEmpoisonnement.anchoredPosition = new Vector2(
            -largeurJauge / 2f + ratio * largeurJauge, curseurEmpoisonnement.anchoredPosition.y);
    }

    public static Color CouleurDifficulte(Difficulte d)
    {
        switch (d)
        {
            case Difficulte.Facile: return Palette.BleuBrume;
            case Difficulte.Moyen: return Palette.Laiton;
            case Difficulte.Difficile: return Palette.Cuivre;
            default: return Palette.PatienceFurieux;
        }
    }
}
