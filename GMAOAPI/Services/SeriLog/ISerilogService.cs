namespace GMAOAPI.Services.SeriLog
{
    public interface ISerilogService
    {
        void LogAudit(string actionEffectuee, string information = "");
    }


}
