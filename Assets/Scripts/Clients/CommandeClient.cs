using System.Collections.Generic;
using UnityEngine;

public enum Humeur { Calme, Enerve, Furieux }

// Comment le client a réagi quand il a été servi (KAN-40)
public enum Reaction
{
    Aucune,      // il attend encore
    Content,     // bonne commande, servie à temps : prix + pourboire
    Satisfait,   // bonne commande mais il s'est énervé : prix (ou moitié s'il était furieux)
    Mecontent,   // mauvaise commande ou potion ratée : il ne paie rien
    Empoisonne,  // bonne recette mais empoisonnée : il paie sans rien remarquer... pour l'instant
    PartiFache,  // il a attendu trop longtemps : il part sans payer
    Mort         // remède raté de trop : il ne s'en remet pas (fantôme, KAN-57)
}

// Un client présent dans la boutique et sa commande (un ticket).
public class CommandeClient
{
    public int numero;
    public ClientData client;
    public RecetteData recette;          // commande normale
    public RemedeData remede;            // ou remède, pour un client empoisonné qui revient (KAN-56)
    public bool estDeRetour;

    public float patienceMax;
    public float patienceRestante;

    public Reaction reaction = Reaction.Aucune;
    public int gain;
    public int pourboire;
    public float tempsAvantDepart;

    // Ce que le client dit en ce moment (affiché lettre par lettre)
    public string replique = "";

    public bool EnTrainDePartir => reaction != Reaction.Aucune;
    public string Nom => client != null ? client.nom : "Client";
    public string Metier => client != null ? client.metier : "de passage";
    public string Id => client != null ? client.id : "client_" + numero;

    public bool EstUnRemede => remede != null;
    public string NomCommande => remede != null ? remede.nom : recette != null ? recette.nom : "?";
    public List<IngredientData> IngredientsCommande => remede != null ? remede.ingredients : recette.ingredients;
    public int PrixCommande => remede != null ? remede.prixVente : recette.prixVente;

    public float RatioPatience => patienceMax > 0f ? Mathf.Clamp01(patienceRestante / patienceMax) : 0f;

    // 3 étapes de patience : calme, énervé, furieux (puis il part)
    public Humeur Humeur
    {
        get
        {
            if (RatioPatience > 2f / 3f) return Humeur.Calme;
            if (RatioPatience > 1f / 3f) return Humeur.Enerve;
            return Humeur.Furieux;
        }
    }

    // Nombre de points de patience sur le ticket (3, 2 ou 1)
    public int PointsDePatience => Humeur == Humeur.Calme ? 3 : Humeur == Humeur.Enerve ? 2 : 1;
}
