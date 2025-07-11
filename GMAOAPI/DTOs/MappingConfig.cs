using GMAOAPI.DTOs.AuthDTO;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Entities;
using Mapster;

namespace GMAOAPI.DTOs
{
    public class MappingConfig
    {
        public static void Config()
        {
            TypeAdapterConfig<Intervention, InterventionDto>
            .NewConfig()
            .Map(dest => dest.Statut, src => src.Statut)
            .Map(dest => dest.Type, src => src.Type)
            .Map(dest => dest.EquipementDto, src => src.Equipement.Adapt<EquipementDto>())
            .Map(dest => dest.RapportId, src => src.RapportId)
            .Map(dest => dest.CreateurDto, src => src.Createur.Adapt<UtilisateurDto>())
              .Map(dest => dest.Techniciens, src => src.InterventionTechniciens.Select(i => i.Technicien.Adapt<UtilisateurDto>()))
           .Map(dest => dest.PieceDetachees, src => src.InterventionPieceDetachees.Select(i => i.Adapt<InterventionPieceDetacheeDto>()));


            TypeAdapterConfig<InterventionTechnicien, InterventionTechnicienDto>
             .NewConfig()
             //.Map(dest => dest.InterventionDto, src => src.Intervention.Adapt<InterventionDto>())
             .Map(dest => dest.TechnicienDto, src => src.Technicien.Adapt<UtilisateurDto>());

            TypeAdapterConfig<PieceDetachee, PieceDetacheeDto>
                .NewConfig()
                .Map(dest => dest.FournisseurDto, src => src.Fournisseur.Adapt<FournisseurDto>());

            TypeAdapterConfig<Notification, NotificationDto>
                .NewConfig()
                .Map(dest => dest.DistinataireUserName, src => src.Destinataire.UserName);


            TypeAdapterConfig<Utilisateur, UtilisateurDto>
                .NewConfig()
                .Map(dest => dest.Role, src => src.RoleUtilisateur.ToString())
                .Map(dest => dest.Specialite,
                     src => src is Technicien
                         ? ((Technicien)src).Specialite.ToString()
                         : null
                    );

   


            TypeAdapterConfig<Planification, PlanificationDto>
                .NewConfig()
                .Map(dest => dest.InterventionDto, src => src.Intervention.Adapt<InterventionDto>());

            TypeAdapterConfig<Rapport, RapportDto>
           .NewConfig()
           .Map(dest => dest.InterventionDto, src => src.Intervention.Adapt<InterventionDto>())
           .Map(dest => dest.CreateurDto, src => src.Createur.Adapt<UtilisateurDto>())
                      .Map(dest => dest.ValideurDto, src => src.Valideur.Adapt<UtilisateurDto>());






            TypeAdapterConfig<InterventionPieceDetachee, InterventionPieceDetacheeDto>
           .NewConfig()
           .Map(dest => dest.PieceDetacheeDto, src => src.PieceDetachee.Adapt<PieceDetacheeDto>())
           .Map(dest => dest.Quantite, src => src.Quantite);








        }
    }
}
