using UnityEngine;

// Outil de test en attendant les vrais clients de Syrine.
// En mode Play : clic sur les ⋮ du composant → « 1. Empoisonner » puis « 2. Servir ».
public class TestClientEmpoisonne : MonoBehaviour
{
    public string clientId = "client_test";
    public string nomClient = "Client de test";
    [Tooltip("L'ingrédient toxique avec lequel on empoisonne le client de test.")]
    public IngredientData poison;
    [Tooltip("Vide = trouvé automatiquement dans la scène.")]
    public Chaudron chaudron;

    [ContextMenu("1. Empoisonner le client de test")]
    void TestEmpoisonner()
    {
        if (ClientsEmpoisonnes.Instance == null) { Debug.LogError("Pas de ClientsEmpoisonnes dans la scène (à mettre sur StockManager)."); return; }
        if (poison == null) { Debug.LogError("Choisis un ingrédient toxique dans la case Poison."); return; }
        ClientsEmpoisonnes.Instance.Empoisonner(clientId, nomClient, poison);
    }

    [ContextMenu("2. Servir le contenu du chaudron comme remède")]
    void TestServir()
    {
        if (chaudron == null) chaudron = FindFirstObjectByType<Chaudron>();
        if (ClientsEmpoisonnes.Instance == null || chaudron == null) { Debug.LogError("Il manque ClientsEmpoisonnes ou le Chaudron."); return; }
        ClientsEmpoisonnes.Instance.Soigner(clientId, chaudron.Contenu);
        chaudron.Vider();
    }
}
