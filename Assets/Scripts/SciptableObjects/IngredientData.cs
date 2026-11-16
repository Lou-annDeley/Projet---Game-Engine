using UnityEngine;

// Les types d'ingrédients (repris de l'Excel KAN-22)
public enum TypeIngredient { Normal, Toxique, Secret, Magique, Remede }

// "Fiche" d'un ingrédient. Ce script s'écrit une seule fois :
// ensuite chaque ingrédient se crée sans code avec
// clic droit > Create > 3Witches > Ingrédient
[CreateAssetMenu(fileName = "ingr_nouveau", menuName = "3Witches/Ingrédient", order = 0)]
public class IngredientData : ScriptableObject
{
    [Header("Identité")]
    public string id = "ING00";
    public string nom = "Nouvel ingrédient";
    public Sprite sprite;
    [TextArea(2, 4)] public string description;

    [Header("Gameplay")]
    public TypeIngredient type = TypeIngredient.Normal;
    [Tooltip("1 = de base. Pour les ingrédients qui s'améliorent avec les niveaux (KAN-50).")]
    [Range(1, 3)] public int qualite = 1;
    [Tooltip("Numéro du niveau où l'ingrédient se débloque.")]
    public int niveauDeblocage = 1;

    [Header("Économie")]
    [Tooltip("Prix d'achat en pièces dans la Boutique.")]
    public int prixAchat = 2;
    [Tooltip("Quantité dans le stock au début de la partie.")]
    public int stockDepart = 10;
    public string obtention = "Boutique";

    public bool EstToxique => type == TypeIngredient.Toxique;
}
