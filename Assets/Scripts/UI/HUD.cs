using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// HUD en jeu (KAN-73) : pièces, niveau, objectif, temps restant.
// Le prefab Assets/Resources/HUD.prefab est créé tout seul au lancement du jeu
// et reste affiché d'une scène à l'autre : aucune scène à modifier.
//
// Pour Syrine (fin de niveau KAN-30, missions KAN-49) :
//   HUD.Instance?.AfficherTemps(secondesRestantes);   // valeur négative = cacher le temps
//   HUD.Instance?.AfficherObjectif("Servir 5 clients"); // texte vide = cacher l'objectif
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Objets du prefab (à glisser ici)")]
    [Tooltip("Le bandeau à afficher / cacher selon la scène.")]
    public GameObject contenu;
    public TMP_Text textePieces;
    public TMP_Text texteNiveau;
    public TMP_Text texteObjectif;
    public TMP_Text texteTemps;

    [Header("Réglages")]
    [Tooltip("Scènes où le HUD est caché (menus).")]
    public string[] scenesSansHUD = { "MenuCarte" };
    [Tooltip("En dessous de ce nombre de secondes, le temps passe en couleur d'alerte.")]
    public float seuilUrgent = 15f;
    public Color couleurTemps = new Color32(0xEB, 0xD6, 0xAB, 0xFF);   // Parchemin
    public Color couleurUrgent = new Color32(0xBF, 0x47, 0x94, 0xFF);  // Magenta

    bool abonneArgent;

    // ---------- Création automatique au lancement ----------
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreerAuLancement()
    {
        if (Instance != null) return;
        var prefab = Resources.Load<HUD>("HUD");
        if (prefab == null)
        {
            Debug.LogWarning("[HUD] Prefab introuvable : il doit s'appeler HUD et être dans Assets/Resources.");
            return;
        }
        Instantiate(prefab).name = "HUD";
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (Instance != this) return;
        Progression.NiveauChange += SurNiveauChange;
        SceneManager.sceneLoaded += SurSceneChargee;

        AfficherObjectif("");
        AfficherTemps(-1f);
        SurSceneChargee(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Progression.NiveauChange -= SurNiveauChange;
        SceneManager.sceneLoaded -= SurSceneChargee;
        if (abonneArgent && ArgentManager.Instance != null) ArgentManager.Instance.ArgentChange -= SurArgentChange;
        Instance = null;
    }

    void SurSceneChargee(Scene scene, LoadSceneMode mode)
    {
        // Le porte-monnaie peut n'exister qu'à partir d'une certaine scène : on s'abonne dès qu'il est là
        if (!abonneArgent && ArgentManager.Instance != null)
        {
            ArgentManager.Instance.ArgentChange += SurArgentChange;
            abonneArgent = true;
        }

        if (contenu != null)
            contenu.SetActive(System.Array.IndexOf(scenesSansHUD, scene.name) < 0);

        MettreAJourPieces();
        SurNiveauChange(Progression.NiveauActuel);
    }

    // ---------- Pièces et niveau (automatiques) ----------
    void SurArgentChange(int pieces) { MettreAJourPieces(); }

    void MettreAJourPieces()
    {
        if (textePieces == null) return;
        textePieces.text = ArgentManager.Instance != null ? ArgentManager.Instance.Pieces.ToString() : "-";
    }

    void SurNiveauChange(int niveau)
    {
        if (texteNiveau != null) texteNiveau.text = $"Niveau {niveau}";
    }

    // ---------- Objectif et temps (appelés par le code des niveaux) ----------
    public void AfficherObjectif(string objectif)
    {
        if (texteObjectif == null) return;
        texteObjectif.text = objectif;
        texteObjectif.gameObject.SetActive(!string.IsNullOrEmpty(objectif));
    }

    public void AfficherTemps(float secondes)
    {
        if (texteTemps == null) return;
        bool visible = secondes >= 0f;
        texteTemps.gameObject.SetActive(visible);
        if (!visible) return;

        int total = Mathf.CeilToInt(secondes);
        texteTemps.text = $"{total / 60}:{total % 60:00}";
        texteTemps.color = secondes <= seuilUrgent ? couleurUrgent : couleurTemps;
    }

    // ---------- Tests (clic droit sur le composant HUD pendant le Play) ----------
    [ContextMenu("Test : objectif + temps 1:30")]
    void TestAfficher()
    {
        AfficherObjectif("Servir 5 clients");
        AfficherTemps(90f);
    }

    [ContextMenu("Test : temps urgent 0:10")]
    void TestUrgent() { AfficherTemps(10f); }

    [ContextMenu("Test : +20 pièces")]
    void TestPieces() { if (ArgentManager.Instance != null) ArgentManager.Instance.Gagner(20); }
}
