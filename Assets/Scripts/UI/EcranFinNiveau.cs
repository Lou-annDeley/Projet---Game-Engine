using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

// Écran de fin de niveau (KAN-77) : clients servis, empoisonnés, gains, objectif réussi ou raté.
// Le prefab Assets/Resources/EcranFinNiveau.prefab est créé tout seul au lancement (caché)
// et reste disponible dans toutes les scènes : aucune scène à modifier.
//
// Pour Syrine (fin de niveau KAN-30) :
//   EcranFinNiveau.Instance?.Afficher(clientsServis, clientsEmpoisonnes, piecesGagnees, objectifReussi);
//   EcranFinNiveau.Continuer += () => { /* retour à la carte des niveaux */ };
//   EcranFinNiveau.Rejouer   += () => { /* relancer le niveau */ };
public class EcranFinNiveau : MonoBehaviour
{
    public static EcranFinNiveau Instance { get; private set; }

    // Prévient le code des niveaux quand le joueur clique sur un bouton
    public static event Action Continuer;
    public static event Action Rejouer;

    [Header("Objets du prefab (à glisser ici)")]
    [Tooltip("Le fond sombre + la carte : affiché seulement en fin de niveau.")]
    public GameObject contenu;
    public TMP_Text texteTitre;
    public TMP_Text texteClientsServis;
    public TMP_Text texteClientsEmpoisonnes;
    public TMP_Text textePieces;
    public TMP_Text texteObjectif;
    public Button boutonContinuer;
    public Button boutonRejouer;

    [Header("Textes et couleurs")]
    public string titreReussi = "Niveau réussi !";
    public string titreRate = "Niveau raté…";
    public Color couleurReussi = new Color32(0x69, 0x32, 0x87, 0xFF); // Violet magique
    public Color couleurRate = new Color32(0xBF, 0x47, 0x94, 0xFF);   // Magenta

    // ---------- Création automatique au lancement ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreerAuLancement()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<EcranFinNiveau>("EcranFinNiveau");
        if (prefab == null)
        {
            Debug.LogWarning("[Fin de niveau] Prefab introuvable : il doit s'appeler EcranFinNiveau et être dans Assets/Resources.");
            return;
        }
        Instantiate(prefab).name = "EcranFinNiveau";
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (contenu != null) contenu.SetActive(false);
    }

    void Start()
    {
        if (boutonContinuer != null) boutonContinuer.onClick.AddListener(ClicContinuer);
        if (boutonRejouer != null) boutonRejouer.onClick.AddListener(ClicRejouer);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool EstAffiche => contenu != null && contenu.activeSelf;

    // ---------- Appelé par le code des niveaux ----------
    public void Afficher(int clientsServis, int clientsEmpoisonnes, int piecesGagnees, bool objectifReussi)
    {
        if (texteTitre != null)
        {
            texteTitre.text = objectifReussi ? titreReussi : titreRate;
            texteTitre.color = objectifReussi ? couleurReussi : couleurRate;
        }
        if (texteClientsServis != null) texteClientsServis.text = $"Clients servis : {clientsServis}";
        if (texteClientsEmpoisonnes != null)
        {
            // Ligne cachée tant que personne n'a été empoisonné
            texteClientsEmpoisonnes.text = $"Clients empoisonnés : {clientsEmpoisonnes}";
            texteClientsEmpoisonnes.gameObject.SetActive(clientsEmpoisonnes > 0);
        }
        if (textePieces != null) textePieces.text = $"Pièces gagnées : {piecesGagnees}";
        if (texteObjectif != null) texteObjectif.text = objectifReussi ? "Objectif atteint" : "Objectif raté";

        VerifierEventSystem();
        if (contenu != null) contenu.SetActive(true);
    }

    public void Cacher()
    {
        if (contenu != null) contenu.SetActive(false);
    }

    void ClicContinuer() { Cacher(); Continuer?.Invoke(); }
    void ClicRejouer() { Cacher(); Rejouer?.Invoke(); }

    // Les boutons ont besoin d'un EventSystem dans la scène (le Comptoir n'en a pas encore)
    static void VerifierEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ---------- Tests (clic droit sur le composant pendant le Play) ----------
    [ContextMenu("Test : niveau réussi")]
    void TestReussi() { Afficher(6, 0, 48, true); }

    [ContextMenu("Test : niveau raté avec empoisonnés")]
    void TestRate() { Afficher(3, 2, 15, false); }
}
