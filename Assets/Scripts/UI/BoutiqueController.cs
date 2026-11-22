using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scène Boutique : pour l'instant seulement la navigation (KAN-29).
// L'achat d'ingrédients viendra avec KAN-45 (Sarah) : les commandes en cours
// continuent pendant qu'on est ici, et les tickets restent visibles.
public class BoutiqueController : MonoBehaviour
{
    public ListeNiveaux liste;
    public TicketUI[] tickets = new TicketUI[4];
    public Button rubanRetour;
    public Button rubanCuisine;
    public Text texteMessage;

    GameManager gm;

    void Start()
    {
        gm = GameManager.Assurer(liste);
        for (int i = 0; i < tickets.Length; i++) tickets[i].Initialiser(i);

        rubanRetour.onClick.AddListener(Retour);
        rubanCuisine.onClick.AddListener(() => gm.AllerA(GameManager.SceneCuisine));
    }

    void Retour()
    {
        gm.AllerA(gm.NiveauEnCours ? GameManager.SceneComptoir : GameManager.SceneCarte);
    }

    void Update()
    {
        bool enJeu = gm.NiveauEnCours;
        rubanCuisine.gameObject.SetActive(enJeu);
        foreach (TicketUI t in tickets) t.gameObject.SetActive(enJeu);
        if (enJeu) foreach (TicketUI t in tickets) t.Afficher(gm);

        texteMessage.text = enJeu && gm.ClientsQuiAttendent > 0
            ? "Attention, des clients attendent au comptoir !"
            : "";

        Keyboard clavier = Keyboard.current;
        if (clavier != null && (clavier.tabKey.wasPressedThisFrame || clavier.escapeKey.wasPressedThisFrame)) Retour();
    }
}
