using Microsoft.EntityFrameworkCore;

namespace WebAPI.Models;

public partial class WeshareContext : DbContext
{
    public WeshareContext()
    {
    }

    public WeshareContext(DbContextOptions<WeshareContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<DeactivatedUser> DeactivatedUsers { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Invite> Invites { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Receipt> Receipts { get; set; }

    public virtual DbSet<ToBePaid> ToBePaids { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserToGroup> UserToGroups { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https: //go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer(
            "Server=135.125.207.90;database=db_weshare;user id=admin_weshare;password=WeSh@r33;trusted_connection=true;TrustServerCertificate=True;integrated security=false;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Admin__3214EC27F4245E80");

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

        modelBuilder.Entity<DeactivatedUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Deactiva__3214EC27D9D8388F");

            entity.ToTable("DeactivatedUser");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Until).HasColumnType("datetime");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.DeactivatedUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Deactivat__UserI__37703C52");
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Friendsh__3214EC27BC07BB65");

            entity.ToTable("Friendship");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.User1Id).HasColumnName("User1ID");
            entity.Property(e => e.User2Id).HasColumnName("User2ID");

            entity.HasOne(d => d.User1).WithMany(p => p.FriendshipUser1s)
                .HasForeignKey(d => d.User1Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Friendshi__User1__2CF2ADDF");

            entity.HasOne(d => d.User2).WithMany(p => p.FriendshipUser2s)
                .HasForeignKey(d => d.User2Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Friendshi__User2__2DE6D218");
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Group__3214EC27F05C23EF");

            entity.ToTable("Group");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invite__3214EC271845F6DD");

            entity.ToTable("Invite");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.ReceiverId).HasColumnName("ReceiverID");
            entity.Property(e => e.SenderId).HasColumnName("SenderID");

            entity.HasOne(d => d.Group).WithMany(p => p.Invites)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invite__GroupID__3493CFA7");

            entity.HasOne(d => d.Receiver).WithMany(p => p.InviteReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invite__Receiver__32AB8735");

            entity.HasOne(d => d.Sender).WithMany(p => p.InviteSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Invite__SenderID__339FAB6E");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3214EC2794089977");

            entity.ToTable("Payment");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.UserToGroupId).HasColumnName("UserToGroupID");

            entity.HasOne(d => d.UserToGroup).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserToGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__UserToG__40F9A68C");
        });

        modelBuilder.Entity<Receipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Receipt__3214EC27D32637D6");

            entity.ToTable("Receipt");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.UserToGroupId).HasColumnName("UserToGroupID");

            entity.HasOne(d => d.UserToGroup).WithMany(p => p.Receipts)
                .HasForeignKey(d => d.UserToGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Receipt__UserToG__3E1D39E1");
        });

        modelBuilder.Entity<ToBePaid>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ToBePaid__3214EC27E93EE9ED");

            entity.ToTable("ToBePaid");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.UserToGroupId).HasColumnName("UserToGroupID");

            entity.HasOne(d => d.UserToGroup).WithMany(p => p.ToBePaids)
                .HasForeignKey(d => d.UserToGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ToBePaid__UserTo__43D61337");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC27E97B566B");

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

        modelBuilder.Entity<UserToGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserToGr__3214EC270D7C09BE");

            entity.ToTable("UserToGroup");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.Group).WithMany(p => p.UserToGroups)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserToGro__Group__3B40CD36");

            entity.HasOne(d => d.User).WithMany(p => p.UserToGroups)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserToGro__UserI__3A4CA8FD");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Wallet__3214EC276B4A9BEE");

            entity.ToTable("Wallet");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Wallet__UserID__2A164134");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}