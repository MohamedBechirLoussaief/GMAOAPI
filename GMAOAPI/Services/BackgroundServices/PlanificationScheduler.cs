
using GMAOAPI.Data;
using GMAOAPI.DTOs.CreateDTOs;
using GMAOAPI.Models.Entities;
using GMAOAPI.Models.Enumerations;
using GMAOAPI.Services.implementation;
using GMAOAPI.Services.Interfaces;
using GMAOAPI.Services.SeriLog;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace GMAOAPI.Services.BackgroundServices
{
    public class PlanificationScheduler : BackgroundService
    {

        private readonly IServiceProvider _serviceProvider;

        public PlanificationScheduler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await GenerateRecurringInterventions();
                await Task.Delay(NextDelay(), stoppingToken);
            }
        }
        private static TimeSpan NextDelay()
        {
            var now = DateTime.Now;
            var sixPm = now.Date.AddHours(18);
            if (now > sixPm) sixPm = sixPm.AddDays(1);
            return sixPm - now;
        }

        private async Task GenerateRecurringInterventions()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GmaoDbContext>();
                var planificationService = scope.ServiceProvider.GetRequiredService<IPlanificationService>();
                var serilogService = scope.ServiceProvider.GetRequiredService<ISerilogService>();

                var plans = await context.Planifications.Include(p => p.Intervention).Include(p => p.Intervention.Equipement)
                    .Where(p => p.IsRecurring && p.IsArchived == false && p.ProchaineGeneration <= DateTime.Now && p.Intervention.Type == TypeIntervention.Preventive).ToListAsync();
                foreach (var plan in plans)
                {
                    plan.IsRecurring = false;
                    context.Planifications.Update(plan);

                    var newIntervention = new Intervention
                    {

                        Statut = StatutIntervention.EnAttente,
                        CreateurId = "90e46007-f2b3-4654-8e23-c3a6ff5a7a77",
                        EquipementId = plan.Intervention.EquipementId,
                        Description = plan.Intervention.Description,
                        Type = plan.Intervention.Type,
                        IsArchived = false
                    };

                    await context.Interventions.AddAsync(newIntervention);
                    await context.SaveChangesAsync();

                    var audit = new Audit
                    {
                        ActionEffectuee = "Intervention planifiée générée automatiquement",
                        EntityId = newIntervention.Id.ToString(),
                        EntityName = "Intervention",
                        type = ActionType.Creation,
                        UtilisateurId = "90e46007-f2b3-4654-8e23-c3a6ff5a7a77",
                        UserName ="system"

                    };


                    context.Audits.Add(audit);

                    var newPlan = new Planification
                    {
                        DateDebut = planificationService.CalculateNextGeneration(plan.Frequence, plan.DateDebut),
                        DateFin = planificationService.CalculateNextGeneration(plan.Frequence, plan.DateFin),
                        Frequence = plan.Frequence,
                        InterventionId = newIntervention.Id,
                        ProchaineGeneration = planificationService.CalculateNextGeneration(plan.Frequence, plan.ProchaineGeneration ?? DateTime.Now),
                        IsRecurring = true,
                        IsArchived = false
                    };
                    await context.Planifications.AddAsync(newPlan);
                    await context.SaveChangesAsync();
                }

            }
        }
    }
}
