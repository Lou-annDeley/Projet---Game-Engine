using UnityEngine;
using UnityEngine.UI;

// Un ticket de commande sur parchemin (jusqu'à 4, KAN-38).
// Un clic dessus sélectionne la commande : le client vient au comptoir
// et le parchemin de la cuisine affiche sa recette.
public class TicketUI : MonoBehaviour
{
    public Button bouton;
    public GameObject contenu;
    public GameObject placeLibre;
    public Image cadreSelection;
    public Text texteNom;
    public Text texteRecette;
    public Text texteEtat;
    public Image[] points = new Image[3];
    [Tooltip("En cuisine : afficher les ingrédients qui manquent")]
    public bool afficherManques;

    int index;

    public void Initialiser(int indexDansLaFile)
    {
        index = indexDansLaFile;
        bouton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.Selectionner(index);
        });
    }

    public void Afficher(GameManager gm)
    {
        bool occupe = index < gm.File.Count;
        contenu.SetActive(occupe);
        placeLibre.SetActive(!occupe);
        bouton.interactable = occupe;
        cadreSelection.enabled = false;
        if (!occupe) return;

        CommandeClient c = gm.File[index];
        cadreSelection.enabled = c == gm.Selection;
        texteNom.text = c.Metier.ToUpper();
        texteRecette.text = c.EstUnRemede ? "<color=#597CB0>Remède :</color> " + c.NomCommande : c.NomCommande;

        for (int i = 0; i < points.Length; i++)
        {
            bool plein = !c.EnTrainDePartir && i < c.PointsDePatience;
            points[i].color = plein ? Palette.CouleurPatience(c.Humeur) : Palette.AvecAlpha(Palette.Contour, 0.35f);
        }

        texteEtat.text = TexteEtat(gm, c);
    }

    string TexteEtat(GameManager gm, CommandeClient c)
    {
        switch (c.reaction)
        {
            case Reaction.Content:
            case Reaction.Satisfait:
            case Reaction.Empoisonne:
                return "<color=#704941>Payé +" + c.gain + "</color>";
            case Reaction.Mecontent:
                return "<color=#A8324A>Rien payé</color>";
            case Reaction.PartiFache:
                return "<color=#A8324A>Parti !</color>";
            case Reaction.Mort:
                return "<color=#597CB0>Fantôme...</color>";
        }

        if (afficherManques)
        {
            var manquants = gm.IngredientsManquants(c);
            if (manquants.Count > 0) return "<color=#A8324A>Manque : " + manquants[0].nom + "</color>";
        }
        if (c.EstUnRemede) return "<color=#6BA027>Empoisonné</color>";
        return c.estDeRetour ? "<color=#693287>De retour</color>" : "";
    }
}
