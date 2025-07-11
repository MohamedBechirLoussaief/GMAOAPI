using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GMAOAPI.Data;
using GMAOAPI.Repository;
using GMAOAPI.Models.Entities;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.DTOs.ReadDTOs;
using GMAOAPI.Models.Enumerations;
using Mapster;

namespace GMAOAPI.Services.implementation
{
    public class TechnicienDashboardService
    {
        private readonly IGenericRepository<Intervention> _interventionRepo;
        private readonly IGenericRepository<Planification> _planificationRepo;


        private readonly IGenericRepository<Rapport> _rapportRepo;

        public TechnicienDashboardService(
            IGenericRepository<Intervention> interventionRepo,
            IGenericRepository<Rapport> rapportRepo,
            IGenericRepository<Planification> planificationRepo)

        {
            _planificationRepo = planificationRepo;
            _interventionRepo = interventionRepo;
            _rapportRepo = rapportRepo;
        }

        public async Task<object> GetStatsAsync(string technicienId)
        {
            var inProgress = await _interventionRepo.CountAsync(i =>
                i.InterventionTechniciens.Any(t => t.TechnicienId == technicienId) &&
                i.Statut == StatutIntervention.EnCours &&
                !i.IsArchived
            );

            var completedCount = await _interventionRepo.CountAsync(i =>
                i.InterventionTechniciens.Any(t => t.TechnicienId == technicienId) &&
                i.Statut == StatutIntervention.Terminee &&
                !i.IsArchived
            );

            var upcomingCount = await _planificationRepo.CountAsync(p =>
                p.Intervention != null &&
                p.Intervention.InterventionTechniciens.Any(t => t.TechnicienId == technicienId) &&
                p.DateDebut >= DateTime.Now &&
                !p.IsArchived
            );

            var completedList = await _interventionRepo.FindAllAsync(
                filter: i => i.InterventionTechniciens.Any(t => t.TechnicienId == technicienId) &&
                             i.Statut == StatutIntervention.Terminee &&
                             i.DateFin > i.DateDebut
            );
            double avgDuration = completedList.Any()
                ? Math.Round(completedList
                    .Select(i => (i.DateFin - i.DateDebut).Value.TotalHours)
                    .Average(), 2)
                : 0.0;

            var onTimeCount = await _interventionRepo.CountAsync(i =>
                i.InterventionTechniciens.Any(t => t.TechnicienId == technicienId) &&
                i.Statut == StatutIntervention.Terminee &&
                i.Planification != null &&
                i.DateFin <= i.Planification.DateFin
            );
            double onTimeRate = completedCount > 0
                ? Math.Round(onTimeCount * 100.0 / completedCount, 2)
                : 0.0;

            return new
            {
                InProgress = inProgress,
                AvgDuration = avgDuration,
                CompletedCount = completedCount,
                OnTimeRate = onTimeRate,
                UpcomingCount = upcomingCount
            };
        }






    }
}
