using UnityEngine;
using UnityEngine.UI;

// Donne à l'image du chiffre la couleur que la carte de Syrine applique au texte (ouvert / verrouillé)
public class CouleurDuTexte : MonoBehaviour
{
    public Text source;
    Image image;

    void Awake() { image = GetComponent<Image>(); }

    void LateUpdate()
    {
        if (source == null || image == null) return;
        // Éclaircit la couleur pour que le chiffre reste lisible même sur un médaillon verrouillé
        Color c = Color.Lerp(source.color, Color.white, 0.45f);
        c.a = Mathf.Max(source.color.a, 0.85f);
        image.color = c;
    }
}
