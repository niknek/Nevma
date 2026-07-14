using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Infrastructure.Persistence.Configurations;

public sealed class MessageReceiptConfiguration : IEntityTypeConfiguration<MessageReceipt>
{
    public void Configure(EntityTypeBuilder<MessageReceipt> builder)
    {
        builder.ToTable("message_receipts", "messaging");
        builder.HasKey(receipt => new { receipt.MessageId, receipt.UserId });
        builder.Property(receipt => receipt.UserId).IsRequired();
        builder.Property(receipt => receipt.DeliveredAt);
        builder.Property(receipt => receipt.ReadAt);
        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(receipt => receipt.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(receipt => new { receipt.UserId, receipt.ReadAt });
    }
}
