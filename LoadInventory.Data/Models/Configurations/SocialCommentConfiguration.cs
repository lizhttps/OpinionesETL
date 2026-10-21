using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpinionesETL.Data.Models;
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models.Configurations
{
    public partial class SocialCommentConfiguration : IEntityTypeConfiguration<SocialComment>
    {
        public void Configure(EntityTypeBuilder<SocialComment> entity)
        {
            entity.HasKey(e => e.IdComment).HasName("pk_social");

            entity.ToTable("Social_Comments");

            entity.Property(e => e.Comentario)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fuente)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<SocialComment> entity);
    }
}
