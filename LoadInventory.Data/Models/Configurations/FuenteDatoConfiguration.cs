using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpinionesETL.Data.Models;
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models.Configurations
{
    public partial class FuenteDatoConfiguration : IEntityTypeConfiguration<FuenteDato>
    {
        public void Configure(EntityTypeBuilder<FuenteDato> entity)
        {
            entity.HasKey(e => e.IdFuente).HasName("pk_fuentedatos");

            entity.ToTable("Fuente_datos");

            entity.Property(e => e.TipoFuente)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<FuenteDato> entity);
    }
}
