using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpinionesETL.Data.Models;
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models.Configurations
{
    public partial class WebReviewConfiguration : IEntityTypeConfiguration<WebReview>
    {
        public void Configure(EntityTypeBuilder<WebReview> entity)
        {
            entity.HasKey(e => e.IdReview).HasName("pk_webReviews");

            entity.ToTable("Web_Reviews");

            entity.Property(e => e.Comentario)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<WebReview> entity);
    }
}
