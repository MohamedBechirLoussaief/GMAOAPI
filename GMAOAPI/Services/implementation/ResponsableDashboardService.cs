using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Repository;
using GMAOAPI.DTOs.ReadDTOs;
using Mapster;

namespace GMAOAPI.Services.implementation
{

    public class ResponsableDashboardService
    {
        private readonly IGenericRepository<Intervention> _interventionRepo;
        private readonly IGenericRepository<Planification> _planificationRepo;
        private readonly IGenericRepository<Rapport> _rapportRepo;
        private readonly IGenericRepository<Equipement> _equipementRepo;
        private readonly IGenericRepository<PieceDetachee> _pieceDetacheeRepo;

        public ResponsableDashboardService(
            IGenericRepository<Intervention> interventionRepo,
            IGenericRepository<Planification> planificationRepo,
            IGenericRepository<Rapport> rapportRepo,
            IGenericRepository<Equipement> equipementRepo,
            IGenericRepository<PieceDetachee> pieceDetacheeRepo)
        {
            _interventionRepo = interventionRepo;
            _planificationRepo = planificationRepo;
            _rapportRepo = rapportRepo;
            _equipementRepo = equipementRepo;
            _pieceDetacheeRepo = pieceDetacheeRepo;
        }

        public async Task<object> GetStatsAsync()
        {
            var termineesCount = await _interventionRepo.CountAsync(i =>
                i.Statut == StatutIntervention.Terminee && !i.IsArchived);

            var enCoursCount = await _interventionRepo.CountAsync(i =>
                i.Statut == StatutIntervention.EnCours && !i.IsArchived);

            var planifAttenteCount = await _planificationRepo.CountAsync(p =>
                !p.IsArchived &&
                p.ProchaineGeneration.HasValue &&
                p.ProchaineGeneration > DateTime.Now);

            var planifRetardCount = await _planificationRepo.CountAsync(p =>
                !p.IsArchived &&
                p.DateDebut < DateTime.Now);

            var equipPanneCount = await _equipementRepo.CountAsync(e =>
                e.Etat == EtatEquipement.EnPanne);

            var all = await _interventionRepo.FindAllAsync(
               i => !i.IsArchived,
               includeProperties: "Planification");

            var total = all.Count();


            var onTime = all.Count(i =>
                i.Planification != null && i.DateFin <= i.Planification.DateFin);

            var tauxPonctualite = total > 0
                ? Math.Round((double)onTime / total * 100, 2)
                : 0.0;


            var list = await _interventionRepo.FindAllAsync(
               i => i.Statut == StatutIntervention.Terminee,
               includeProperties: ""
           );

            var completedDurations = list
                .Where(i => i.DateFin > i.DateDebut)
                .Select(i => (i.DateFin - i.DateDebut).Value.TotalHours);  

            double avgHours = completedDurations.Any()
                ? Math.Round(completedDurations.Average(), 2)
                : 0.0;


            var stats = new
            {
                InterventionsTerminées = termineesCount,
                PlanificationsEnAttente = planifAttenteCount,
                InterventionsEnCours = enCoursCount,
                EquipementsEnPanne = equipPanneCount,
                PlanificationsEnRetard = planifRetardCount,
                TauxDePonctualité = tauxPonctualite,
                MoyenneHeursIntervention = avgHours
            };

            return stats;
        }





        public async Task<List<PieceDetacheeDto>> GetPiecesAvecStockVideAsync()
        {
            var pieces = await _pieceDetacheeRepo.FindAllAsync(
                p => p.QuantiteStock == 0);

            return pieces
                .Select(p => p.Adapt<PieceDetacheeDto>())
                .ToList();
        }


    }



}
