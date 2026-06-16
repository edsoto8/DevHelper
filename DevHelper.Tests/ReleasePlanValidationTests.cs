using DevHelper.Web.Models;
using DevHelper.Web.Services;

namespace DevHelper.Tests;

public sealed class ReleasePlanValidationTests
{
    private static ReleasePlanService Service()
        // Validate() is pure and touches none of the collaborators.
        => new(null!, null!, null!, null!, null!, null!, null!,
            TestFactories.Logger<ReleasePlanService>());

    private static ReleasePlanAggregate Valid()
    {
        var aggregate = new ReleasePlanAggregate
        {
            Plan = new ReleasePlan
            {
                ReleaseDate = "2026-06-14",
                Environment = Environments.Production,
                CreatedBy = "Eduardo",
                TemplateId = 1,
            },
        };
        aggregate.Tickets.Add(new ReleasePlanTicket { TicketNumber = "T-1", TicketName = "First" });
        aggregate.Systems.Add(new ReleasePlanSystemLink { SystemEntryId = 1 });
        return aggregate;
    }

    [Fact]
    public void Valid_aggregate_passes()
    {
        Assert.True(Service().Validate(Valid()).IsValid);
    }

    [Fact]
    public void Missing_release_date_fails()
    {
        var a = Valid();
        a.Plan.ReleaseDate = "";
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Missing_environment_fails()
    {
        var a = Valid();
        a.Plan.Environment = "";
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Missing_created_by_fails()
    {
        var a = Valid();
        a.Plan.CreatedBy = "";
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Missing_template_fails()
    {
        var a = Valid();
        a.Plan.TemplateId = null;
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Missing_ticket_fails()
    {
        var a = Valid();
        a.Tickets.Clear();
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Ticket_without_number_fails()
    {
        var a = Valid();
        a.Tickets.Clear();
        a.Tickets.Add(new ReleasePlanTicket { TicketNumber = "", TicketName = "No number" });
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Missing_system_fails()
    {
        var a = Valid();
        a.Systems.Clear();
        Assert.False(Service().Validate(a).IsValid);
    }

    [Fact]
    public void Other_system_name_satisfies_system_requirement()
    {
        var a = Valid();
        a.Systems.Clear();
        a.Systems.Add(new ReleasePlanSystemLink { OtherSystemName = "Legacy" });
        Assert.True(Service().Validate(a).IsValid);
    }
}
