using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpinionesETL.Data.Models;
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models.Configurations
{
    public partial class SurveyConfiguration : IEntityTypeConfiguration<Survey>
    {
        public void Configure(EntityTypeBuilder<Survey> entity)
        {
            entity.HasKey(e => e.IdOpinion).HasName("pk_Surveys");

            entity.Property(e => e.Clasificacion)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);
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

        partial void OnConfigurePartial(EntityTypeBuilder<Survey> entity);
    }
}
