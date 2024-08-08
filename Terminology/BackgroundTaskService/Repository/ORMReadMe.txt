To Create:
> <Solution FullPath>\Interneuron.Synapse\Terminology\Repository>dotnet ef dbcontext scaffold "interneuron-ne-db-test.postgres.database.azure.com;User Id=nedbtestadmin@interneuron-ne-db-test;Password=N3ur0n!nedbtest;Database=mcc_terminology;Port=5432;SSL Mode=Require;" -o ..\Model\DomainModels --context-dir DBModelsContext  --schema terminology -c TerminologyDBContext Npgsql.EntityFrameworkCore.PostgreSQL


dotnet tool install --global dotnet-ef

To Create:
> <Solution FullPath>\Interneuron.Synapse\Terminology\Repository>dotnet ef dbcontext scaffold "Server=interneuron-ne-db-test.postgres.database.azure.com;User Id=nedbtestadmin@interneuron-ne-db-test;Password=N3ur0n!nedbtest;Database=mcc_terminology;Port=5432;SSL Mode=Require;" -o ..\Model\DomainModels --context-dir DBModelsContext --schema terminology -c TerminologyDBContext Npgsql.EntityFrameworkCore.PostgreSQL -f

> cd C:\Projects\Interneuron\POCs\Apps\interneuron-synapse\Terminology\Repository

> dotnet ef dbcontext scaffold "Server=interneuron-ind-db-test.cjdliyabgdwt.ap-south-1.rds.amazonaws.com;User Id=Inddbtestadmin;Password=N3ur0n!inddbtest;Database=mmc_dev_delta;Port=5432;" -o Model\DomainModels --context-dir DBModelsContext --schema terminology --schema=local_formulary -c Repository\TerminologyDBContext Npgsql.EntityFrameworkCore.PostgreSQL -f

Replace Constructors with below code:

private IConfiguration _configuration;

public TerminologyDBContext(IConfiguration configuration)
{
    _configuration = configuration;
}

public TerminologyDBContext(DbContextOptions<TerminologyDBContext> options, IConfiguration configuration)
    : base(options)
{
    _configuration = configuration;
}

 protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    //            if (!optionsBuilder.IsConfigured)
    //            {
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
    //                optionsBuilder.UseNpgsql("Server=interneuron-ne-db-test.postgres.database.azure.com;User Id=nedbtestadmin@interneuron-ne-db-test;Password=N3ur0n!nedbtest;Database=mcc_terminology;Port=5432;SSL Mode=Require;");
    //            }

    if (!optionsBuilder.IsConfigured)
    {
        var connString = _configuration.GetValue<string>("TerminologyBackgroundTaskConfig:Connectionstring");
        optionsBuilder.UseNpgsql(connString);
    }
}