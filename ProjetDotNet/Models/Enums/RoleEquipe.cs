namespace ProjetDotNet.Models.Enums
{
    //public enum RoleEquipe
    //{
    //    ADMIN,
    //    MEMBRE,
    //    Membre
    //}
    public enum RoleEquipe
    {
        Admin,          // Responsable de l'équipe
        Membre,         // Développeur ou membre classique
        ScrumMaster,    // Facilite les réunions et la méthodologie Agile
        ProductOwner,   // Définit les priorités et maintient le backlog produit
        Designer,       // Conçoit les interfaces et l'expérience utilisateur
        QA,             // Testeur / vérificateur de qualité
        Observateur     // Suit l'équipe mais n'intervient pas directement
    }
}
