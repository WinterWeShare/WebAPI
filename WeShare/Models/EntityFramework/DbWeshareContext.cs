using Microsoft.EntityFrameworkCore;

namespace WebAPI.Models.EntityFramework;

public partial class DbWeshareContext : DbContext
{
    public DbWeshareContext()
    {
    }

    public DbWeshareContext(DbContextOptions<DbWeshareContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Action> Actions { get; set; }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<AdminSession> AdminSessions { get; set; }

    public virtual DbSet<DeactivatedUser> DeactivatedUsers { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Invite> Invites { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<ToBePaid> ToBePaids { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<UserToGroup> UserToGroups { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        var connectionString = configuration.GetConnectionString("WeShare")!;
        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Action>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Actions__3214EC271D09D4A5");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Admin).WithMany(p => p.Actions)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Actions__AdminID__267ABA7A");
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Admin__3214EC27FA30659D");

            entity.ToTable("Admin");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AdminSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AdminSes__3214EC27DBADDFF9");

            entity.ToTable("AdminSession");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Salt).IsUnicode(false);
            entity.Property(e => e.SessionKey).IsUnicode(false);

            entity.HasOne(d => d.Admin).WithMany(p => p.AdminSessions)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AdminSess__Admin__4D94879B");
        });

        modelBuilder.Entity<DeactivatedUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Deactiva__3214EC2748CE2A0D");

            entity.ToTable("DeactivatedUser");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.DeactivatedUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Deactivat__UserI__38996AB5");
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Friendsh__3214EC2727E51494");

            entity.ToTable("Friendship");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FriendId).HasColumnName("FriendID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Friend).WithMany(p => p.FriendshipFriends)
                .HasForeignKey(d => d.FriendId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Friendshi__Frien__2F10007B");

            entity.HasOne(d => d.User).WithMany(p => p.FriendshipUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Friendshi__UserI__2E1BDC42");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Group__3214EC2728DDD4B6");

            entity.ToTable("Group");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invite__3214EC27C03B7A26");

            entity.ToTable("Invite");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");

            entity.HasOne(d => d.Group).WithMany(p => p.Invites)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invite__GroupID__35BCFE0A");

            entity.HasOne(d => d.Receiver).WithMany(p => p.InviteReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invite__Receiver__33D4B598");

            entity.HasOne(d => d.Sender).WithMany(p => p.InviteSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invite__SenderID__34C8D9D1");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3214EC27863672A9");

            entity.ToTable("Payment");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UserToGroupId).HasColumnName("UserToGroupID");

            entity.HasOne(d => d.UserToGroup).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserToGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__UserToG__4222D4EF");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Receipt__3214EC270989A407");

            entity.ToTable("Receipt");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.UserToGroupId).HasColumnName("UserToGroupID");

            entity.HasOne(d => d.UserToGroup).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.UserToGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Receipt__UserToG__3F466844");
        });

        modelBuilder.Entity<ToBePaid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ToBePaid__3214EC2786140A04");

            entity.ToTable("ToBePaid");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.UserToGroupId).HasColumnName("UserToGroupID");

            entity.HasOne(d => d.UserToGroup).WithMany(p => p.ToBePaids)
                .HasForeignKey(d => d.UserToGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ToBePaid__UserTo__44FF419A");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC27BBA8877C");

            entity.ToTable("User");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserSess__3214EC27025F80B3");

            entity.ToTable("UserSession");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Salt).IsUnicode(false);
            entity.Property(e => e.SessionKey).IsUnicode(false);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserSessi__UserI__5070F446");
        });

        modelBuilder.Entity<UserToGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserToGr__3214EC27852757AD");

            entity.ToTable("UserToGroup");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Group).WithMany(p => p.UserToGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserToGro__Group__3C69FB99");

            entity.HasOne(d => d.User).WithMany(p => p.UserToGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserToGro__UserI__3B75D760");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Wallet__3214EC27120479DB");

            entity.ToTable("Wallet");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallet__UserID__2B3F6F97");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
