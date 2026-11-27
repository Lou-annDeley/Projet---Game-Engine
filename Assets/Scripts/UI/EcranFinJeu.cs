using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

// Écrans des deux fins du jeu (KAN-59) : « sorcière gentille » ou « empoisonneuse ».
// Le prefab Assets/Resources/EcranFinJeu.prefab est créé tout seul au lancement (caché) :
// aucune scène à modifier.
//
// Pour Syrine (deux fins, KAN-55), après le dernier niveau :
//   EcranFinJeu.Instance?.AfficherSelonScore();          // choisit la fin avec GameManager.EmpoisonnesTotal
//   EcranFinJeu.Retour += () => { /* retour au menu / à la carte */ };
public class EcranFinJeu : MonoBehaviour
{
    public static EcranFinJeu Instance { get; private set; }
    public static event Action Retour;

    [Header("Objets du prefab (à glisser ici)")]
    public GameObject contenu;
    public Image illustration;
    public TMP_Text texteTitre;
    public TMP_Text texteRecit;
    public TMP_Text texteScore;
    public Button boutonRetour;
    [Tooltip("Durée du fondu d'apparition, en secondes.")]
    public float dureeFondu = 1.5f;

    [Header("Fin gentille")]
    public Sprite imageGentille;
    public string titreGentille = "La sorcière bien-aimée";
    [TextArea(3, 6)] public string recitGentille =
        "Grâce à tes soupes et tes potions, le village n'a jamais été aussi heureux.\nTa boutique est devenue la plus aimée de la région.";

    [Header("Fin empoisonneuse")]
    public Sprite imageEmpoisonneuse;
    public string titreEmpoisonneuse = "L'empoisonneuse";
    [TextArea(3, 6)] public string recitEmpoisonneuse =
        "Les rues sont vides, les fantômes rôdent.\nTu règnes désormais seule sur un village de spectres.";

    CanvasGroup groupe;
    float tFondu = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreerAuLancement()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<EcranFinJeu>("EcranFinJeu");
        if (prefab == null)
        {
            Debug.LogWarning("[Fin du jeu] Prefab introuvable : il doit s'appeler EcranFinJeu et être dans Assets/Resources.");
            return;
        }
        Instantiate(prefab).name = "EcranFinJeu";
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (contenu != null)
        {
            groupe = contenu.GetComponent<CanvasGroup>();
            if (groupe == null) groupe = contenu.AddComponent<CanvasGroup>();
            contenu.SetActive(false);
        }
    }

    void Start()
    {
        if (boutonRetour != null) boutonRetour.onClick.AddListener(() => { Cacher(); Retour?.Invoke(); });
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        if (tFondu < 0f || groupe == null) return;
        tFondu += Time.unscaledDeltaTime;
        groupe.alpha = Mathf.Clamp01(tFondu / Mathf.Max(dureeFondu, 0.01f));
        if (groupe.alpha >= 1f) tFondu = -1f;
    }

    // Choisit la fin d'après le compteur d'empoisonnés du GameManager de Syrine
    public void AfficherSelonScore()
    {
        int empoisonnes = GameManager.Instance != null ? GameManager.Instance.EmpoisonnesTotal : 0;
        Afficher(empoisonnes >= GameManager.SeuilFinEmpoisonneuse, empoisonnes);
    }

    public void Afficher(bool empoisonneuse, int empoisonnes = 0)
    {
        if (illustration != null)
        {
            illustration.sprite = empoisonneuse ? imageEmpoisonneuse : imageGentille;
            illustration.enabled = illustration.sprite != null;
        }
        if (texteTitre != null) texteTitre.text = empoisonneuse ? titreEmpoisonneuse : titreGentille;
        if (texteRecit != null) texteRecit.text = empoisonneuse ? recitEmpoisonneuse : recitGentille;
        if (texteScore != null) texteScore.text = $"Clients empoisonnés : {empoisonnes}";

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        if (contenu != null) contenu.SetActive(true);
        if (groupe != null) { groupe.alpha = 0f; tFondu = 0f; }
    }

    public void Cacher() { if (contenu != null) contenu.SetActive(false); }

    [ContextMenu("Test : fin gentille")]
    void TestGentille() { Afficher(false, 0); }

    [ContextMenu("Test : fin empoisonneuse")]
    void TestEmpoisonneuse() { Afficher(true, 7); }
}
