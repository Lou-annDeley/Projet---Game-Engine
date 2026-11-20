using UnityEngine;

// Affiche le nombre de pièces (texte provisoire en attendant le HUD de Lou-Ann).
public class AffichageArgent : MonoBehaviour
{
    public float taille = 0.06f;
    TextMesh texte;

    void Start()
    {
        texte = ArticleBoutique.CreerTexte(transform, Vector3.zero, taille, TextAnchor.MiddleCenter);
        texte.color = new Color32(0xC8, 0x8F, 0x60, 0xFF); // Laiton
        if (ArgentManager.Instance != null) ArgentManager.Instance.ArgentChange += MettreAJour;
        MettreAJour(0);
    }

    void OnDestroy()
    {
        if (ArgentManager.Instance != null) ArgentManager.Instance.ArgentChange -= MettreAJour;
    }

    void MettreAJour(int pieces)
    {
        texte.text = ArgentManager.Instance != null
            ? $"Pièces : {ArgentManager.Instance.Pieces}"
            : "Pas d'ArgentManager sur le StockManager";
    }
}
