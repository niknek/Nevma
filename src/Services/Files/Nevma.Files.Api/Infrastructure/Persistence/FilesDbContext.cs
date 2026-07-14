using Microsoft.EntityFrameworkCore;
using Nevma.Files.Api.Application;
using Nevma.Files.Api.Domain;

namespace Nevma.Files.Api.Infrastructure.Persistence;

public sealed class FilesDbContext(DbContextOptions<FilesDbContext> options)
    : DbContext(options), IFilesUnitOfWork
{
    public DbSet<FileAsset> FileAssets => Set<FileAsset>();
    public DbSet<FileAccessGrant> FileAccessGrants => Set<FileAccessGrant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("files");
        builder.ApplyConfigurationsFromAssembly(typeof(FilesDbContext).Assembly);
    }
}
