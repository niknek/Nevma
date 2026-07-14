using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Files.Api.Domain;

namespace Nevma.Files.Api.Infrastructure.Persistence;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        builder.ToTable("file_assets", "files");
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Id).ValueGeneratedNever();
        builder.Property(asset => asset.OwnerId).IsRequired();
        builder.Property(asset => asset.FileName).HasMaxLength(255).IsRequired();
        builder.Property(asset => asset.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.Size).IsRequired();
        builder.Property(asset => asset.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(asset => asset.StorageKey).HasMaxLength(100).IsRequired();
        builder.Property(asset => asset.CreatedAt).IsRequired();
        builder.Property(asset => asset.DeletedAt);
        builder.HasIndex(asset => new { asset.OwnerId, asset.CreatedAt });
        builder.HasIndex(asset => asset.StorageKey).IsUnique();
        builder.HasMany(asset => asset.AccessGrants).WithOne().HasForeignKey(grant => grant.FileAssetId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(asset => asset.AccessGrants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class FileAccessGrantConfiguration : IEntityTypeConfiguration<FileAccessGrant>
{
    public void Configure(EntityTypeBuilder<FileAccessGrant> builder)
    {
        builder.ToTable("file_access_grants", "files");
        builder.HasKey(grant => new { grant.FileAssetId, grant.UserId });
        builder.Property(grant => grant.CreatedAt).IsRequired();
        builder.HasIndex(grant => grant.UserId);
    }
}
