#region
using Asset.Domain.Identity;
using Asset.Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using AssetEntity = Asset.Domain.Models.Asset;
using MyView = Asset.Domain.Models.vw_AssetSearch;
#endregion

namespace Asset.Infastructure.Models;

public partial class AssetManagementDbContext : DbContext
{
    #region Constructor
    public AssetManagementDbContext()
    {
    }

    public AssetManagementDbContext(DbContextOptions<AssetManagementDbContext> options)
        : base(options)
    {
    }
    #endregion
    public virtual DbSet<AssetEntity> Assets { get; set; }

    public virtual DbSet<AssetTransfer> AssetTransfers { get; set; }

    public virtual DbSet<AssetType> AssetTypes { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<MyView> VwAssetSearches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}