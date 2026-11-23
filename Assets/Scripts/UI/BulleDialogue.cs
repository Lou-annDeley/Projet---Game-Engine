using UnityEngine;
using UnityEngine.UI;

// Bulle de dialogue des clients (KAN-41) : le texte s'affiche lettre par lettre.
// Un clic sur la bulle affiche tout d'un coup.
public class BulleDialogue : MonoBehaviour
{
    public Text texteNom;
    public Text texte;
    [Tooltip("Lettres par seconde")]
    public float vitesse = 40f;

    string cible = "";
    float lettres;

    public bool Termine => lettres >= cible.Length;

    void Start()
    {
        var bouton = GetComponent<Button>();
        if (bouton != null) bouton.onClick.AddListener(ToutAfficher);
    }

    public void Afficher(string nom, string phrase)
    {
        if (texteNom != null) texteNom.text = nom;
        phrase = phrase ?? "";
        if (phrase == cible) return;
        cible = phrase;
        lettres = 0f;
        MettreAJourTexte();
    }

    public void ToutAfficher()
    {
        lettres = cible.Length;
        MettreAJourTexte();
    }

    void Update()
    {
        if (Termine) return;
        lettres += vitesse * Time.unscaledDeltaTime;
        MettreAJourTexte();
    }

    void MettreAJourTexte()
    {
        int n = Mathf.Clamp(Mathf.FloorToInt(lettres), 0, cible.Length);
        // La suite est écrite en transparent : les mots ne sautent pas de ligne pendant l'écriture
        string reste = cible.Substring(n);
        texte.text = reste.Length > 0
            ? cible.Substring(0, n) + "<color=#00000000>" + reste + "</color>"
            : cible;
    }
}
