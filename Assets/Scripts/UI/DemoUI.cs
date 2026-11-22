using UnityEngine;
using UnityEngine.UI;

// Bulle d'aide de la démo (KAN-80) : la sorcière explique, et l'élément
// sur lequel il faut cliquer clignote. Un exemplaire dans le Comptoir et un dans la Cuisine.
public class DemoUI : MonoBehaviour
{
    [Tooltip("Nom de la scène où se trouve ce panneau : Comptoir ou Cuisine")]
    public string scene;

    public GameObject panneau;
    public Text texte;
    public Button boutonSuivant;
    public Text texteSuivant;
    public Button boutonPasser;

    [Header("Portrait de la sorcière")]
    public Image portrait;
    public Sprite spriteNeutre;
    public Sprite spriteContente;

    [Header("Surbrillance")]
    public RectTransform surbrillance;
    public Image imageSurbrillance;

    [Tooltip("Ce qu'il faut montrer à chaque étape (une case par étape, vide si rien)")]
    public RectTransform[] cibles = new RectTransform[Demo.NombreEtapes];

    [Tooltip("Le ruban qui mène à l'autre scène")]
    public RectTransform rubanVersAutreScene;

    void Start()
    {
        boutonSuivant.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.SuivantDemo();
        });
        boutonPasser.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.PasserDemo();
        });
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        bool actif = gm != null && gm.DemoActive;
        panneau.SetActive(actif);
        surbrillance.gameObject.SetActive(false);
        if (!actif) return;

        EtapeDemo etape = gm.EtapeActuelle;
        RectTransform cible;

        if (Demo.Scene(etape) == scene)
        {
            texte.text = Demo.Texte(etape);
            boutonSuivant.gameObject.SetActive(Demo.AttendUnClic(etape));
            texteSuivant.text = etape == EtapeDemo.Fin ? "C'est parti !" : "Suivant";
            cible = cibles[(int)etape];
        }
        else
        {
            texte.text = scene == GameManager.SceneComptoir
                ? "La suite se passe en cuisine : clique sur le ruban « Cuisine » (ou touche Tab)."
                : "La suite se passe au comptoir : clique sur le ruban « Comptoir » (ou touche Tab).";
            boutonSuivant.gameObject.SetActive(false);
            cible = rubanVersAutreScene;
        }

        if (portrait != null)
        {
            Sprite s = etape == EtapeDemo.Fin || etape == EtapeDemo.RetourComptoir ? spriteContente : spriteNeutre;
            if (s != null) portrait.sprite = s;
        }

        if (cible != null && cible.gameObject.activeInHierarchy)
        {
            surbrillance.gameObject.SetActive(true);
            surbrillance.position = cible.position;
            surbrillance.sizeDelta = cible.rect.size + new Vector2(30f, 30f);
            float alpha = 0.3f + 0.2f * Mathf.Sin(Time.unscaledTime * 5f);
            imageSurbrillance.color = Palette.AvecAlpha(Palette.Bougie, alpha);
        }
    }
}
