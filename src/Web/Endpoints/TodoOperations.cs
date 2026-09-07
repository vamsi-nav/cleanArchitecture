using Microsoft.AspNetCore.Http.HttpResults;

namespace CleanArchitecture.Web.Endpoints;

public class TodoOperations : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetBacklog, "backlog");
        groupBuilder.MapGet(GetArchive, "archive");
        groupBuilder.MapGet(GetAudit, "audit");
        groupBuilder.MapGet(ExportCsv, "export/csv");
        groupBuilder.MapGet(ExportXml, "export/xml");
        groupBuilder.MapPost(Reindex, "reindex");
        groupBuilder.MapPost(Recalculate, "recalculate");
        groupBuilder.MapPost(BulkClose, "bulk-close");
        groupBuilder.MapPut(Reassign, "reassign/{id}");
        groupBuilder.MapDelete(PurgeArchive, "archive/purge");
    }

    // ...ten handler methods
    public static Ok<string> GetBacklog() => TypedResults.Ok("backlog");
    public static Ok<string> GetArchive() => TypedResults.Ok("archive");
    public static Ok<string> GetAudit() => TypedResults.Ok("audit");
    public static Ok<string> ExportCsv() => TypedResults.Ok("export/csv");
    public static Ok<string> ExportXml() => TypedResults.Ok("export/xml");
    public static Ok<string> Reindex() => TypedResults.Ok("reindex");
    public static Ok<string> Recalculate() => TypedResults.Ok("recalculate");
    public static Ok<string> BulkClose() => TypedResults.Ok("bulk-close");
    public static Ok<string> Reassign(int id) => TypedResults.Ok($"reassign/{id}");
    public static NoContent PurgeArchive() => TypedResults.NoContent();
}
