using Microsoft.EntityFrameworkCore;
using OpinionesETL.Data.Models.Configurations;
using System;
using System.Collections.Generic;
#nullable disable

namespace OpinionesETL.Data.Models;

public partial class CustomerReviewsDBContext : DbContext
{
    public CustomerReviewsDBContext(DbContextOptions<CustomerReviewsDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<FuenteDato> FuenteDatos { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<SocialComment> SocialComments { get; set; }

    public virtual DbSet<Survey> Surveys { get; set; }

    public virtual DbSet<WebReview> WebReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.ClienteConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.FuenteDatoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProductoConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.SocialCommentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.SurveyConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.WebReviewConfiguration());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
