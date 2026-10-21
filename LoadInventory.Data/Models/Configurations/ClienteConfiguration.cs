using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpinionesETL.Data.Models;
using System;
using System.Collections.Generic;

namespace OpinionesETL.Data.Models.Configurations
{
    public partial class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> entity)
        {
            entity.HasKey(e => e.IdCliente).HasName("pk_clientes");

            entity.HasIndex(e => e.Email, "UQ__Clientes__A9D10534230FC39C").IsUnique();

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            OnConfigurePartial(entity);
        }

        partial void OnConfigurePartial(EntityTypeBuilder<Cliente> entity);
    }
}
